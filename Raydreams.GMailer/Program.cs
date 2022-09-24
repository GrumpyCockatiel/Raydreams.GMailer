using System;
using System.Collections;
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

namespace Raydreams.GMailer
{
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

        public GMailer( AppConfig settings )
        {
            this.Settings = settings;
        }

        #region [ Properties ]

        /// <summary></summary>
        protected AppConfig Settings { get; set; }

        /// <summary></summary>
        public string? Forward => this.Settings.ForwardTo;

        /// <summary></summary>
        public string? UserID => this.Settings.UserID;

        /// <summary></summary>
        public GmailService? Host {get; set; }

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

            // set the host
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
            StringBuilder sb = new StringBuilder( Encoding.UTF8.GetString( bytes ) );
            string original = sb.ToString();

            // change the To
            Regex pattern = new Regex( @"^To: (.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase );
            Match toMatch = pattern.Match( original );
            if ( toMatch.Success )
                sb.Replace( toMatch.Value, $"To: {this.Forward}" );

            // remove CC
            pattern = new Regex( @"^Cc: (.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase );
            Match ccMatch = pattern.Match( original );
            if ( ccMatch.Success )
                sb.Replace( ccMatch.Value, String.Empty );

            // remove BCC
            pattern = new Regex( @"^Bcc: (.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase );
            Match bccMatch = pattern.Match( original );
            if ( bccMatch.Success )
                sb.Replace( bccMatch.Value, String.Empty );

            // make a new message
            Message forward = new Message() { Raw = Encoding.UTF8.GetBytes( sb.ToString() ).BASE64UrlEncode() };

            // send it
            var sent = this.Host?.Users.Messages.Send( forward, this.UserID ).Execute();

            return sent;
        }

    }
}