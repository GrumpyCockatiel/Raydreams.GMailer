using System;

namespace Raydreams.GMailer
{
    /// <summary>Loads values from the App Settings JSON environment files</summary>
    public class AppConfig
    {
        private int _top = 1;

        /// <summary>What environment is this</summary>
        public string? Environment { get; set; }

        /// <summary>The GMail User</summary>
        public string? UserID { get; set; }

        /// <summary></summary>
        public string? ForwardTo { get; set; }

        /// <summary></summary>
        public string? ClientID { get; set; }

        /// <summary></summary>
        public string? ClientSecret { get; set; }

        /// <summary></summary>
        public int Top
        {
            set { Math.Clamp( value, 1, 500 ); }
            get => this._top;
        }
    }
}

