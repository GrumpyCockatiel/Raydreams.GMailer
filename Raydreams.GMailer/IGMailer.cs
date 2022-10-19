using Google.Apis.Gmail.v1.Data;

namespace Raydreams.GMailer
{
	/// <summary>The GMailer interface</summary>
	public interface IGMailer
	{
		/// <summary>Get a list of message headers</summary>
		IEnumerable<Message> ListMessages( int top );

		/// <summary>Get a single full message</summary>
		Message? DownloadMessage( Message msg );

		/// <summary>Send a Message formatted as a forward</summary>
		ForwardResults ForwardMessage( Message msg );

		/// <summary>Get all of a users GMail Labels</summary>
		List<Label> ListLabels();
    }
}
