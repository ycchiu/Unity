using UnityEngine;
using System.Collections;

namespace EB
{
	public static class StringUtil
	{
		public static string Possesive(string value)
        {
            if ( value.EndsWith("s") || value.EndsWith("S") )
            {
                return value + "'";
            }
            return value + "'s";
        }
		
		static char[] valid = "abcdefghijklmnopqrstuvwxyz0123456789/_".ToCharArray();
		
		public static string SafeKey( string src )
		{
			// remove dots and spaces
			src = src.ToLower();
			for (var i = 0; i < src.Length; )
			{
				if ( System.Array.IndexOf(valid, src[i]) < 0 ) 
				{
					// invalid
					src = src.Remove(i, 1);
				}
				else
				{
					++i;
				}
			}
			return src;
		}
	}
}
