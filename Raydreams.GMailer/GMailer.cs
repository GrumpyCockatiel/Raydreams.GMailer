using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using MimeKit;
using System.Text;
using System.Text.RegularExpressions;

namespace Raydreams.GMailer
{
    /// <summary></summary>
    public class GMailer
    {

        /// <summary>Constructor</summary>
        /// <param name="settings"></param>
        public GMailer( AppConfig settings )
        {
            this.Settings = settings;

            this.ForwardTo = new MailboxAddress( this.Settings.ForwardToName, this.Settings.ForwardToAddress );
        }

        #region [ Properties ]

        /// <summary></summary>
        private AppConfig Settings { get; set; }

        /// <summary>The mailbox to forward to</summary>
        public MailboxAddress? ForwardTo { get; set; }

        /// <summary>GMail User ID</summary>
        public string? UserID => this.Settings.UserID;

        /// <summary>Mail Service</summary>
        protected GmailService? Host { get; set; }

        /// <summary>List of downloaded Email Headers</summary>
        protected List<Message> Messages { get; set; } = new List<Message>();

        /// <summary>List of original email IDs that were forwarded</summary>
        protected List<string> AlreadyForwaded { get; set; } = new List<string>();

        /// <summary>List of email IDs sent this run</summary>
        protected List<string> Forwaded { get; set; } = new List<string>();

        #endregion [ Properties ]

        /// <summary></summary>
        /// <returns></returns>
        public int Run()
        {
            // init the GMail Service
            if ( !this.InitMailService() )
                return -1;

            // load any sent emails
            FileManager io = new FileManager( this.Settings.SentFile );
            this.AlreadyForwaded = new List<string>( io.LoadIDs() );

            // get messages from GMail
            var msgIDs = this.ListMessages();

            if ( msgIDs == null )
                return -1;

            // download the last Top messages
            this.DownloadMessages( msgIDs );

            this.LogMessage( $"Downloaded {this.Messages.Count} messages" );

            // forward all the emails
            foreach ( Message next in this.Messages )
            {
                try
                {
                    // forward it
                    var forwardResults = this.ForwardMessage( next );

                    // add the ID of forwarded messages to a list
                    if ( forwardResults != null )
                        this.Forwaded.Add( next.Id );
                }
                catch ( System.Exception exp )
                {
                    this.LogException( exp );
                }
            }

            // append new sent email IDs to the save file
            io.AppendIDs( this.Forwaded );

            return 0;
        }

        /// <summary>Login to the GMail service</summary>
        /// <returns></returns>
        protected bool InitMailService()
        {
            string[] scopes = new string[] { "https://mail.google.com/", Oauth2Service.Scope.UserinfoEmail };

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = this.Settings.ClientID,
                ClientSecret = this.Settings.ClientSecret
            }, scopes, this.UserID, CancellationToken.None ).Result;

            this.LogMessage( "Authorization granted or not required (if the saved access token already available)" );

            if ( credential.Token.IsExpired( credential.Flow.Clock ) )
            {
                this.LogMessage( "The access token has expired, refreshing it" );

                if ( credential.RefreshTokenAsync( CancellationToken.None ).Result )
                {
                    this.LogMessage( "The access token is now refreshed" );
                }
                else
                {
                    this.LogMessage( "The access token has expired but we can't refresh it" );
                    return false;
                }
            }
            else
            {
                this.LogMessage( "access token OK - continue" );
            }

            // init the host
            this.Host = new GmailService( new BaseClientService.Initializer() { HttpClientInitializer = credential } );

            return true;
        }

        /// <summary>Get the headers of the last Top emails from the inbox</summary>
        /// <returns></returns>
        protected List<Message> ListMessages()
        {
            List<Message> resultList = new List<Message>();

            // setup the request
            var req = this.Host?.Users.Messages.List( this.UserID );

            if ( req == null )
                return new List<Message>();

            // set params
            req.MaxResults = ( this.Settings.MaxRead < 500 ) ? this.Settings.MaxRead : 500;
            req.IncludeSpamTrash = false;
            req.Q = "is:inbox";

            ListMessagesResponse resp;

            do
            {
                // get the list
                resp = req.Execute();

                if ( resp?.Messages == null || resp.Messages.Count < 1 )
                    break;

                resultList.AddRange( resp.Messages );

                if ( !String.IsNullOrWhiteSpace( resp.NextPageToken ) )
                    req.PageToken = resp.NextPageToken;
                else
                    break;

            } while ( resultList.Count < this.Settings.MaxRead );

            return resultList;
        }

        /// <summary>Download all the specified messages in Raw format</summary>
        /// <param name="msgs"></param>
        protected void DownloadMessages( IEnumerable<Message> msgs )
        {
            foreach ( Message msg in msgs )
            {
                // if the message is already forwarded
                if ( this.AlreadyForwaded.Contains( msg.Id ) )
                    continue;

                if ( this.Messages.Count >= this.Settings.MaxSend )
                    break;

                var req = this.Host?.Users.Messages.Get( this.UserID, msg.Id );

                if ( req == null )
                    continue;

                req.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                var response = req.Execute();

                if ( response == null )
                    continue;

                this.Messages.Add( response );
            }
        }

        /// <summary></summary>
        /// <param name="msg"></param>
        /// <param name="to"></param>
        protected Message? ForwardMessage( Message msg )
        {
            // get the raw message as bytes
            byte[] bytes = msg.Raw.BASE64UrlDecode();

            // read into a MIMEKit Message
            var message = new MimeMessage();
            using MemoryStream inStream = new MemoryStream( bytes );
            message = MimeMessage.Load( inStream );

            // check for null body
            if ( message.Body == null )
                return null;

            // save the old header values for later use
            OriginalHeader original = new OriginalHeader( message );

            // scan for the text parts and add the orginal header info back
            foreach ( MimeEntity part in message.BodyParts )
            {
                if ( part.ContentType.MimeType.Contains( "text" ) && part is TextPart tp )
                {
                    // plain text
                    if ( tp.IsPlain )
                        tp.Text = $"{original.ToPlainString()}{tp.Text}";
                    // html
                    else if ( tp.IsHtml )
                    {
                        // search for a body tag
                        Match m = new Regex( @"<body(.+)>", RegexOptions.IgnoreCase ).Match( tp.Text );

                        // set an index to write to
                        int idx = ( m.Success ) ? m.Index : 0;
                        tp.Text = tp.Text.Insert( idx, original.ToHTMLString() );
                    }
                    // ignore other formats for now
                }
            }

            // now clear the old values
            message.To.Clear();
            message.Cc.Clear();
            message.Bcc.Clear();

            // add the new Forward To
            message.To.Add( this.ForwardTo );

            // write a new message
            using MemoryStream outStream = new MemoryStream();
            message.WriteTo( outStream );
            outStream.Position = 0;

            // make a new message
            Message forward = new Message() { Raw = outStream.ToArray().BASE64UrlEncode() };

            // send it
            Message? sent = this.Host?.Users.Messages.Send( forward, this.UserID ).Execute();

            if ( sent == null )
            {
                this.LogMessage( $"Wasn't able to forward message {msg.Id} with subject '{original.Subject}'" );
                return null;
            }

            this.LogMessage( $"Forwarded message {msg.Id} with subject '{original.Subject}'" );

            return sent;
        }

        /// <summary>Log method holder for now</summary>
        /// <param name="message"></param>
        protected void LogMessage( string message )
        {
            Console.WriteLine( message );
        }

        /// <summary></summary>
        /// <param name="exp"></param>
        protected void LogException( System.Exception exp )
        {
            StringBuilder msg = new StringBuilder( $"{exp.GetType().FullName} : {exp.Message} " );

            msg.Append( exp.StackTrace );

            // get the inner exception if there is one
            if ( exp.InnerException != null )
            {
                msg.Append( $"{msg}; {exp.InnerException.Message} " );
                msg.Append( exp.InnerException.StackTrace );
            }

            Console.WriteLine( msg.ToString() );
        }
    }
}

