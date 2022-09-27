using Google.Apis.Gmail.v1.Data;

namespace Raydreams.GMailer
{
	/// <summary>The GMailer interface</summary>
	public interface IGMailer
	{
		/// <summary>Get a list of message headers</summary>
		IEnumerable<Message> ListMessages( int top );

		/// <summary></summary>
		Message? DownloadMessage( Message msg );

		/// <summary></summary>
		ForwardResults ForwardMessage( Message msg );
    }
}
