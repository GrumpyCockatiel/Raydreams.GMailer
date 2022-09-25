using System;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;
using static System.Net.Mime.MediaTypeNames;

namespace Raydreams.GMailer
{
    /// <summary></summary>
    public class GMailer
    {
        static int Main( string[] args )
        {
            // load the configuarion file
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile( $"appsettings.json", true, true )
                //.AddJsonFile( $"appsettings.{env}.json", true, true )
                .AddEnvironmentVariables();

            // init AppConfig
            IConfigurationRoot configurationRoot = builder.Build();
            AppConfig config = configurationRoot.GetSection( nameof( AppConfig ) ).Get<AppConfig>();

            int runResult = 0;

            try
            {
                GMailer app = new GMailer( config );
                runResult = app.Run();
            }
            catch (System.Exception exp)
            {
                Console.WriteLine( exp.Message );
                return -1;
            }

            return runResult;
        }

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
        protected GmailService? Host {get; set; }

        /// <summary></summary>
        protected List<Message> Messages { get; set; } = new List<Message>();

        /// <summary></summary>
        protected List<string> Forwaded { get; set; } = new List<string>();

        #endregion [ Properties ]

        /// <summary></summary>
        /// <returns></returns>
        public int Run()
        {
            // init the GMail Service
            if ( !this.InitMailService() )
                return -1;

            // get messages
            var msgIDs = this.ListMessages();

            if ( msgIDs == null )
                return -1;

            // download the last Top messages
            this.DownloadMessages( msgIDs.Messages );

            this.LogIt( $"Downloaded {msgIDs.Messages.Count} messages" );

            // forward all the emails
            foreach ( Message next in this.Messages )
            {
                try
                {
                    var forwardResults = this.ForwardMessage( next );

                    // add the ID of forwarded messages to a list
                    if ( forwardResults != null )
                        this.Forwaded.Add( next.Id );
                }
                catch ( System.Exception exp )
                {
                    this.LogIt( exp.Message );
                }
            }

            return 0;
        }

        /// <summary></summary>
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

            this.LogIt( "Authorization granted or not required (if the saved access token already available)" );

            if ( credential.Token.IsExpired( credential.Flow.Clock ) )
            {
                this.LogIt( "The access token has expired, refreshing it" );

                if ( credential.RefreshTokenAsync( CancellationToken.None ).Result )
                {
                    this.LogIt( "The access token is now refreshed" );
                }
                else
                {
                    this.LogIt( "The access token has expired but we can't refresh it" );
                    return false;
                }
            }
            else
            {
                this.LogIt( "access token OK - continue" );
            }

            // init the host
            this.Host = new GmailService( new BaseClientService.Initializer() { HttpClientInitializer = credential } );

            return true;
        }

        /// <summary>Get the headers of the last Top emails from the inbox</summary>
        /// <returns></returns>
        protected ListMessagesResponse? ListMessages()
        {
            // setup the request
            var req = this.Host?.Users.Messages.List( this.UserID );

            if ( req == null )
                return null;

            // set params
            req.MaxResults = this.Settings.Top;
            req.IncludeSpamTrash = false;
            req.Q = "is:inbox";

            // get the list
            return req.Execute();
        }

        /// <summary>Download all the specified messages in Raw format</summary>
        /// <param name="msgs"></param>
        protected void DownloadMessages( IEnumerable<Message> msgs )
        {
            foreach ( Message msg in msgs )
            {
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
                this.LogIt( $"Wasn't able to forward message {msg.Id} with subject '{original.Subject}'" );
                return null;
            }

            this.LogIt( $"Forwarded message {msg.Id} with subject '{original.Subject}'" );

            return sent;
        }

        /// <summary>Log method holder for now</summary>
        /// <param name="message"></param>
        protected void LogIt(string message)
        {
            Console.WriteLine( message );
        }

    }
}

//var oldHeader = new TextPart( MimeKit.Text.TextFormat.Plain )
//{
//    Text = FormatOriginalHeader(from, subject, "", "", ""),
//    ContentDisposition = new ContentDisposition { FileName = "original.txt", IsAttachment = false }
//};
//string s = new ContentDisposition() { IsAttachment = false }.ToString();
//oldHeader.Headers.Add( "Content-Disposition", s );

// start a new multipart
//Multipart mixed = new Multipart( "mixed" );

//if ( message.Body is Multipart mp )
//{
//    if ( mp.ContentType.IsMimeType( "multipart", "mixed" ) )
//    {
//        mixed = mp;
//    }
//    else if ( mp.ContentType.IsMimeType( "multipart", "alternative" ) )
//    {
//        mixed.Add( oldHeader );
//        message.BodyParts.ToList().ForEach( p => mixed.Add(p) );
//        message.Body = mixed;
//    }
//}
//else // make it multipart
//{
//    // Replace the top-level body part with a new multipart/mixed
//    //mixed = new Multipart( "mixed" );
//    mixed.Add( oldHeader );
//    mixed.Add( message.Body );
//    message.Body = mixed;
//}