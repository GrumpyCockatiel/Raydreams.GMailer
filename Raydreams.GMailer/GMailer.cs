using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using System.Text;
using System.Text.RegularExpressions;
//using static Google.Apis.Requests.BatchRequest;

namespace Raydreams.GMailer
{
    /// <summary>Main runner class for now</summary>
    /// <remarks>This is the only class that should reference MIMEKit
    /// Break-up this class into an App Runner and IGmailer
    /// </remarks>
    public class GMailer : IGMailer
    {
        public const int MaxPageSize = 500;

        /// <summary>Constructor</summary>
        /// <param name="settings"></param>
        public GMailer( AppConfig settings )
        {
            this.Settings = settings;
        }

        #region [ Properties ]

        /// <summary>Runtime settings to use</summary>
        private AppConfig Settings { get; set; }

        /// <summary>Alias to the GMail User ID in Settings</summary>
        public string? UserID => this.Settings.UserID;

        /// <summary>Mail Service</summary>
        protected GmailService? Host { get; set; }

        /// <summary>List of downloaded Raw Emails</summary>
        protected List<Message> Messages { get; set; } = new List<Message>();

        /// <summary>List of original email IDs that were forwarded - loaded from sent file</summary>
        protected List<string> AlreadyForwaded { get; set; } = new List<string>();

        /// <summary>List of email IDs sent this run</summary>
        protected List<string> Forwaded { get; set; } = new List<string>();

        /// <summary>The MIME rewrite delegate to use</summary>
        public RewriteMIME? Rewriter { get; set; }

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
            var msgIDs = this.ListMessages( this.Settings.MaxRead );

            if ( msgIDs == null )
                return -1;

            // download full orginal message in raw
            foreach ( Message header in msgIDs )
            {
                try
                {
                    // if the message is already forwarded
                    if ( this.AlreadyForwaded.Contains( header.Id ) )
                        continue;

                    // queued the max number of emails to send
                    if ( this.Messages.Count >= this.Settings.MaxSend )
                        break;

                    var dl = this.DownloadMessage( header );

                    if ( dl != null )
                        this.Messages.Add( dl );
                }
                catch ( System.Exception exp )
                {
                    this.LogException( exp );
                }
            }

            this.LogMessage( $"Downloaded {this.Messages.Count} messages" );

            // forward all the emails
            foreach ( Message next in this.Messages )
            {
                try
                {
                    // forward it
                    var forwardResults = this.ForwardMessage( next );

                    // add the ID of forwarded messages to a list
                    if ( forwardResults.IsSuccess )
                    {
                        this.Forwaded.Add( next.Id );
                        this.LogMessage( $"Forwarded message {next.Id} with subject '{forwardResults.OriginalSubject}'" );
                    }
                    else
                        this.LogMessage( $"Wasn't able to forward message {next.Id} with subject '{forwardResults.OriginalSubject}'" );
                }
                catch ( System.Exception exp )
                {
                    this.LogException( exp );
                }
            }

            this.LogMessage( $"Forwarded {this.Forwaded.Count} messages" );

            // append new sent email IDs to the save file
            if ( io.AppendIDs( this.Forwaded ) > 0 )
                this.LogMessage( $"Updated sent file" );

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
        /// <param name="top">The maximum number of email headers to retrieve</param>
        /// <returns></returns>
        public IEnumerable<Message> ListMessages( int top )
        {
            top = Math.Clamp( top, 2, 10000 );

            List<Message> resultList = new List<Message>();

            // setup the request
            var req = this.Host?.Users.Messages.List( this.UserID );

            if ( req == null )
                return new List<Message>();

            // set params
            req.MaxResults = ( top < MaxPageSize ) ? top : MaxPageSize;
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

        /// <summary>Just downloads a single message in Raw format</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Message? DownloadMessage( Message msg )
        {
            var req = this.Host?.Users.Messages.Get( this.UserID, msg.Id );

            if ( req == null )
                return null;

            req.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
            return req.Execute();
        }

        /// <summary>Actually forward a message</summary>
        /// <param name="source">The original message</param>
        /// <param name="prefixFW">Prefix the Subject with FW</param>
        /// <remarks>This is the only method where MIMEKit is used and needs to be broken out</remarks>
        public ForwardResults ForwardMessage( Message source, bool prefixFW = true )
        {
            if ( this.Rewriter == null || String.IsNullOrWhiteSpace( this.Settings.ForwardToAddress ) )
                return new ForwardResults();

            // get the raw message as bytes
            byte[] bytes = source.Raw.BASE64UrlDecode();

            // rewrite the email with new data
            (byte[] Message, OriginalHeader? Header) rewritten = this.Rewriter( bytes, this.Settings.ForwardToAddress, this.Settings.ForwardToName );

            // make a new message from raw bytes
            Message forward = new Message() { Raw = rewritten.Message.BASE64UrlEncode() };
            
            // send the message
            Message? sent = this.Host?.Users.Messages.Send( forward, this.UserID ).Execute();

            if ( sent == null )
                return new ForwardResults();

            return new ForwardResults { OriginalID = source.Id, OriginalSubject = rewritten.Header?.Subject };
        }

        /// <summary>Log method holder for now</summary>
        /// <param name="message"></param>
        protected void LogMessage( string message )
        {
            Console.WriteLine( message );
        }

        /// <summary>Log Exception holder for now</summary>
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
