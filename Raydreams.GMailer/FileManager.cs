using System;
using System.IO;
using System.Text;

namespace Raydreams.GMailer
{
    public class FileManager
    {
        /// <summary>Path to the user's desktop folder</summary>
        public static readonly string DesktopPath = Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory );

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

        /// <summary></summary>
        protected FileInfo SentFile { get; private set; }

        /// <summary></summary>
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

        /// <summary></summary>
        public int AppendIDs( IEnumerable<string> ids )
        {
            try
            {
                File.AppendAllLines( this.SentFile.FullName, ids );
                return ids.Count();
            }
            catch (System.Exception )
            {
                return 0;
            }
        }

    }
}

