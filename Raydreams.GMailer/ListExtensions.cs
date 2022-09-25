using System;

namespace Raydreams.GMailer
{
    /// <summary>Some extensions for Generic List</summary>
    public static class ListExtensions
    {
        /// <summary>Simple test for a null or Empty list</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        /// <example>if ( list.IsNotEmpty() )</example>
        public static bool IsNotEmpty<T>( this List<T> list )
        {
            return ( list != null && list.Count > 0 );
        }

        /// <summary>Simple test for a null or Empty list</summary>
        public static bool IsEmpty<T>( this List<T> list )
        {
            return ( list == null || list.Count < 1 );
        }
    }
}

