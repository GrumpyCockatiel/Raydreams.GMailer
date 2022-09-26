using System;
using System.Net;
using System.Text;
using MimeKit;

namespace Raydreams.GMailer
{
    /// <summary>Hold the original header info</summary>
    public record OriginalHeader
    {
        public OriginalHeader( MimeMessage message )
        {
            this.Subject = message.Subject;
            this.From = message.From.ToString();
            this.Date = message.Date.ToString();
            this.To = message.To.ToString();
            this.CC = message.Cc.ToString();
        }

        /// <summary>Original Sent To</summary>
        public string? To { get; set; }

        /// <summary>Originally From</summary>
        public string? From { get; set; }

        /// <summary>Original Sent Date</summary>
        public string? Date { get; set; }

        /// <summary>Original CC</summary>
        public string? CC { get; set; }

        /// <summary>Original Sybject</summary>
        public string? Subject { get; set; }

        /// <summary></summary>
        /// <returns></returns>
        public string ToPlainString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "________________________________   " );
            //sb.AppendLine( $"TAG Digital Studios   " );
            sb.AppendLine( $"From: {this.From}   " );
            sb.AppendLine( $"Subject: {this.Subject}   " );
            sb.AppendLine( $"Sent: {this.Date}   " );
            sb.AppendLine( $"To: {this.To}   " );
            sb.AppendLine( $"CC: {this.CC}   " );
            sb.AppendLine( "________________________________   " );
            sb.AppendLine( $"   " );

            return sb.ToString();
        }

        /// <summary></summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "<p>" );
            //sb.Append( $"TAG Digital Studios<br/>" );
            sb.Append( $"From: {WebUtility.HtmlEncode( this.From )}<br/>" );
            sb.Append( $"Subject: {this.Subject}<br/>" );
            sb.Append( $"Sent: {this.Date}<br/>" );
            sb.Append( $"To: {WebUtility.HtmlEncode( this.To )}<br/>" );
            sb.Append( $"CC: {WebUtility.HtmlEncode( this.CC )}<br/>" );
            sb.Append( $"</p>" );

            return sb.ToString();
        }
    }
}

