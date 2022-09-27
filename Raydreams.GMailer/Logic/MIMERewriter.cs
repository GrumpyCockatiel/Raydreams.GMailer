using System;
using System.Text.RegularExpressions;
using MimeKit;

namespace Raydreams.GMailer
{
    /// <summary></summary>
    public interface IMIMERewriter
    {
        string? SubjectPrefix { get; set; }

        /// <summary>Rewriting a MIME email is left as a delegate so you can use different dependencies</summary>
        (byte[], OriginalHeader?) RewriteMIME( byte[] inMime, string toAddress, string? name );
    }

    /// <summary></summary>
    public class MIMEKitRewriter : IMIMERewriter
    {
        /// <summary></summary>
        public MIMEKitRewriter( string? prefix = null )
        {
            this.SubjectPrefix = prefix;
        }

        /// <summary>Prefix subject with RE</summary>
        public string? SubjectPrefix { get; set; } = String.Empty;

        /// <summary>Rewrite a MIME email</summary>
        /// <param name="inMime">bytes of the source message</param>
        /// <param name="toAddress">New forward to address</param>
        /// <param name="name">name to forward to</param>
        /// <returns></returns>
        public (byte[], OriginalHeader?) RewriteMIME( byte[] inMime, string toAddress, string? name = "anonymous" )
        {
            // validate all the input
            if ( inMime == null || inMime.Length < 1 || String.IsNullOrWhiteSpace( toAddress ) )
                return (new byte[0], null);

            name = String.IsNullOrWhiteSpace( name ) ? "anonymous" : name.Trim();

            var to = new MailboxAddress( name, toAddress );

            // read into a MIMEKit Message
            var mimeMsg = new MimeMessage();
            using MemoryStream inStream = new MemoryStream( inMime );
            mimeMsg = MimeMessage.Load( inStream );

            // check for null body
            if ( mimeMsg.Body == null )
                return (new byte[0], null);

            // save the old header values for later use
            OriginalHeader original = new OriginalHeader( mimeMsg );

            // scan for the text parts and add the orginal header info back
            foreach ( MimeEntity part in mimeMsg.BodyParts )
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
            mimeMsg.To.Clear();
            mimeMsg.Cc.Clear();
            mimeMsg.Bcc.Clear();

            // add the new Forward To
            mimeMsg.To.Add( to );

            // add any prefix
            if ( !String.IsNullOrWhiteSpace( this.SubjectPrefix ) )
                mimeMsg.Subject = $"{this.SubjectPrefix} {mimeMsg.Subject}";

            // write a new message
            using MemoryStream outStream = new MemoryStream();
            mimeMsg.WriteTo( outStream );
            outStream.Position = 0;

            return (outStream.ToArray(), original);
        }
    }
}

