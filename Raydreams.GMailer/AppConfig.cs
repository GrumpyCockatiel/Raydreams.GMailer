using System;

namespace Raydreams.GMailer
{
    /// <summary>Loads config values from the App Settings JSON environment files</summary>
    public class AppConfig
    {
        private int _top = 1;

        private int _send = 1;

        private string _toName = "unknown";

        private string _file = "MySentEmails";

        /// <summary>What environment is this</summary>
        public string Environment { get; set; } = "DEV";

        /// <summary>The GMail User ID. The mailbox to read and forward from.</summary>
        public string? UserID { get; set; }

        /// <summary>The email address to forward to</summary>
        public string ForwardToAddress
        {
            get => !String.IsNullOrWhiteSpace( this._toName ) ? this._toName.Trim() : "unknown";
            set => this._toName = value;
        }

        /// <summary>The name of the person to forward to</summary>
        public string? ForwardToName { get; set; }

        /// <summary>GMail API Client ID</summary>
        public string? ClientID { get; set; }

        /// <summary>GMail API Secret</summary>
        public string? ClientSecret { get; set; }

        /// <summary>Base name of the file to use for recording sent emails IDs.</summary>
        public string SentFile
        {
            get => !String.IsNullOrWhiteSpace( this._file ) ? $"{this._file.Trim()}.txt" : "MySentEmails.txt";
            set => this._file = value;
        }

        /// <summary>The maximum number of emails to read from the source</summary>
        public int MaxRead
        {
            set => this._top = value;
            get => Math.Clamp( this._top, 1, 500 );
        }

        /// <summary>The maximum number of emails to forward in one single run</summary>
        public int MaxSend
        {
            set => this._send = value;
            get => Math.Clamp( this._send, 1, 500 );
        }
    }
}

