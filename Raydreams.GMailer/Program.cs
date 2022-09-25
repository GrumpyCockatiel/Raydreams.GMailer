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

            IConfigurationRoot configurationRoot = builder.Build();
            AppConfig config = configurationRoot.GetSection( nameof( AppConfig ) ).Get<AppConfig>();

            GMailer app = new GMailer( config );
            app.Run();

            return 0;
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
            string[] scopes = new string[] { "https://mail.google.com/", Oauth2Service.Scope.UserinfoEmail };

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = this.Settings.ClientID,
                ClientSecret = this.Settings.ClientSecret
            }, scopes, this.UserID, CancellationToken.None ).Result;

            Console.WriteLine( "Authorization granted or not required (if the saved access token already available)" );

            if ( credential.Token.IsExpired( credential.Flow.Clock ) )
            {
                Console.WriteLine( "The access token has expired, refreshing it" );

                if ( credential.RefreshTokenAsync( CancellationToken.None ).Result )
                {
                    Console.WriteLine( "The access token is now refreshed" );
                }
                else
                {
                    Console.WriteLine( "The access token has expired but we can't refresh it" );
                    return 0;
                }
            }
            else
            {
                Console.WriteLine( "access token OK - continue" );
            }

            // init the host
            this.Host = new GmailService( new BaseClientService.Initializer() { HttpClientInitializer = credential } );

            // get messages
            var msgIDs = this.ListMessages();

            if ( msgIDs == null )
                return -1;

            // download the last Top messages
            this.DownloadMessages( msgIDs.Messages );

            // forward
            foreach ( Message next in this.Messages )
            {
                var forwardResults = this.ForwardMessage( next );

                // add the ID of forwarded messages to a list
                if ( forwardResults != null )
                    this.Forwaded.Add( next.Id );
            }

            return 0;
        }

        /// <summary></summary>
        /// <param name="initializer"></param>
        /// <returns></returns>
        protected ListMessagesResponse? ListMessages()
        {
            var req = this.Host?.Users.Messages.List( this.UserID );

            if ( req == null )
                return null;

            req.MaxResults = this.Settings.Top;
            req.IncludeSpamTrash = false;
            req.Q = "is:inbox";
            var response = req.Execute();

            return response;
        }

        /// <summary></summary>
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
            byte[] bytes = msg.Raw.BASE64UrlDecode();

            // setup a MIMEKit Message
            var message = new MimeMessage();
            using MemoryStream inStream = new MemoryStream( bytes );
            message = MimeMessage.Load( inStream );

            // check for null body
            if ( message.Body == null )
                return null;

            // save the old values for later use
            OriginalHeader original = new OriginalHeader(message);

            // scan for the text areas and add the orginal info
            foreach ( MimeEntity part in message.BodyParts)
            {
                if ( part.ContentType.MimeType.Contains("text") && part is TextPart tp )
                {
                    if ( tp.IsPlain )
                        tp.Text = $"{original.ToPlainString()}{tp.Text}";
                    else if ( tp.IsHtml )
                    {
                        // search for the body tag
                        Match m = new Regex( @"<body(.+)>", RegexOptions.IgnoreCase ).Match( tp.Text );

                        int idx = ( m.Success ) ? m.Index : 0;
                        tp.Text = tp.Text.Insert( idx, original.ToHTMLHeader() );
                    }
                }
            }

            // now clear the old values
            message.To.Clear();
            message.Cc.Clear();
            message.Bcc.Clear();
            message.To.Add( this.ForwardTo );

            // write back
            using MemoryStream outStream = new MemoryStream();
            message.WriteTo( outStream );
            outStream.Position = 0;

            // make a new message
            Message forward = new Message() { Raw = outStream.ToArray().BASE64UrlEncode() };

            // send it
            var sent = this.Host?.Users.Messages.Send( forward, this.UserID ).Execute();

            return sent;
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