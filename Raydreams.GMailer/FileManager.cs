using System.Reflection;
using System.Text;

namespace Raydreams.GMailer
{
    /// <summary>Handles simple file IO with the sent emails data file</summary>
    public class FileManager
    {
        /// <summary>file lock</summary>
		private readonly object _fileLock = new object();

        /// <summary>Path to the user's desktop folder</summary>
        public static readonly string DesktopPath = Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory );

        /// <summary>Gets the folder of the executing exe or program BUT only if this file is in the primary assembly.</summary>
		public static string? AppRoot => Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

		/// <summary>local app storage page</summary>
#if MACOS
		public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath( Environment.SpecialFolder.UserProfile), "Applications" );
#else
		public static readonly string AppDataPath = Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData );
#endif

        /// <summary></summary>
        /// <param name="sentFileName"></param>
        public FileManager( string sentFileName )
        {
            string path = Path.Combine( DesktopPath, sentFileName );

            this.SentFile = new FileInfo( path );
        }

        /// <summary>The file to use to record sent emails</summary>
        protected FileInfo SentFile { get; private set; }

        /// <summary>Load all the IDs</summary>
        public IEnumerable<string> LoadIDs()
        {
            if ( !this.SentFile.Exists )
                return new string[0];

            try
            {
                string[] file = File.ReadAllLines( this.SentFile.FullName, Encoding.UTF8 );
                return file;
            }
            catch ( System.Exception )
            {
                return new string[0];
            }
        }

        /// <summary>Append new IDs to the file</summary>
        public int AppendIDs( IEnumerable<string> ids )
        {
            try
            {
                lock ( _fileLock )
                {
                    File.AppendAllLines( this.SentFile.FullName, ids );
                }

                return ids.Count();
            }
            catch (System.Exception )
            {
                return 0;
            }
        }

    }
}

