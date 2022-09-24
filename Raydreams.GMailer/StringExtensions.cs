using System;

namespace Raydreams.GMailer
{
    public static class StringExtensions
    {
        /// <summary>Encodes a byte array as BASE64 URL encoded</summary>
        /// <remarks>This needs to move to Array Extensions</remarks>
        public static string BASE64UrlEncode( this byte[] arg )
        {
            string s = Convert.ToBase64String( arg ); // Regular base64 encoder
            //s = s.Split( '=' )[0];
            s = s.TrimEnd( '=' ); // remove any trailing =
            s = s.Replace( '+', '-' ); // 62nd char of encoding
            s = s.Replace( '/', '_' ); // 63rd char of encoding
            return s;
        }

        /// <summary>Decodes a BASE64 URL encoded string back to its original bytes</summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static byte[] BASE64UrlDecode( this string str )
        {
            str = str.Replace( '-', '+' ); // 62nd char of encoding
            str = str.Replace( '_', '/' ); // 63rd char of encoding

            // the modulus of the length by 4 can not be remainder 1
            switch ( str.Length % 4 )
            {
                // no padding necessary if it divides evenly
                case 0:
                    break;
                // pad with two =
                case 2:
                    str += "==";
                    break;
                // pad once
                case 3:
                    str += "=";
                    break;
                // hopefully this does not happen
                default:
                    throw new System.Exception( "Illegal BASE64URL string!" );
            }

            return Convert.FromBase64String( str ); // Standard base64 decoder
        }
    }
}

