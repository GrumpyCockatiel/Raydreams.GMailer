using System;

namespace Raydreams.GMailer
{
    /// <summary></summary>
    public struct ForwardResults
    {
        /// <summary>Original ID</summary>
        public string? OriginalID { get; set; }

        /// <summary>Original Subject</summary>
        public string? OriginalSubject { get; set; }

        public bool IsSuccess => !String.IsNullOrWhiteSpace( this.OriginalID );
    }
}

