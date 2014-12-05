using UnityEngine;
using System.Collections;

namespace EB
{
	public static class Encoding
	{
		private static System.Text.Encoding _encoding = System.Text.Encoding.UTF8;
		
		public static byte[] GetBytes( string str )
		{
			return _encoding.GetBytes(str ?? string.Empty);
		}
		
		public static string GetString( byte[] bytes )
		{
			return _encoding.GetString(bytes);
		}
		
		public static string GetString( Buffer buffer )
		{
			var segment = buffer.ToArraySegment(true);
			return GetString( segment.Array, segment.Offset, segment.Count ); 
		}
		
		public static string GetString( byte[] bytes, int offset, int count )
		{
			return _encoding.GetString(bytes, offset, count);
		}
		
		public static string ToBase64String( byte[] bytes )
		{
			return System.Convert.ToBase64String(bytes);
		}
		
		public static byte[] FromBase64String( string base64 )
		{
			return System.Convert.FromBase64String( base64 );
		}
			
		public static string ToBase64Url( byte[] data )
		{
			var base64 = System.Convert.ToBase64String(data);
			base64 = base64.Replace("=","");
			base64 = base64.Replace("+","-");
			base64 = base64.Replace("/","_");
			return base64;
		}
		
		public static string ToBase64Url( string data )
		{
			return ToBase64Url( GetBytes(data) ); 
		}
		
		public static byte[] BytesFromBase64Url( string base64Url )
		{
			//
			var base64 = base64Url;
			base64 = base64.Replace("-","+");
			base64 = base64.Replace("_","/");
			
			// fix padding
			while ( (base64.Length % 4) != 0 )
			{
				base64 += '=';
			}
			return System.Convert.FromBase64String(base64);
		}
		
		public static string StringFromBase64Url( string base64Url )
		{
			return GetString( BytesFromBase64Url(base64Url) ); 
		}
	
		public static byte[] FromHexString( string str )
		{
			str = str.Replace(" ", string.Empty);
			
			if ( (str.Length&1) != 0 )
			{
				throw new System.Exception("str must be even length");
			}
			
			var len = str.Length/2;
			var bytes = new byte[len];
			
			for ( int i = 0; i < len; ++i )
			{
				bytes[i] = System.Convert.ToByte( str.Substring(i*2,2), 16 ); 
			}
			return bytes;
		}
		
		public static string ToHexString( byte[] bytes, int spacing = 0 )
		{
			return ToHexString(bytes, 0, bytes.Length, spacing);
		}
		
		public static string ToHexString( byte[] bytes, int offset, int count, int spacing = 0 )
		{
			var sb = new System.Text.StringBuilder( count * 2 );
			var space = "";
			for (int i = 0; i < spacing; ++i )
			{
				space += " ";
			}
			
			for (int i = 0; i < count; ++i)
			{
				sb.AppendFormat("{0:X2}", bytes[offset+i]);
				sb.Append(space);
			}
			return sb.ToString();
		}
		
	}
}

