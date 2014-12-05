using UnityEngine;
using System.Collections;

namespace EB
{
	// signed request is a signed string that can hold content.
	// uses base64 url encoding so it doesn't need to be escaped 
	public static class SignedRequest
	{
		public static byte[] Parse( string contents, Hmac hmac ) 
		{
			var parts = contents.Split('.');
			if ( parts.Length != 2 )
			{
				return null;
			}
			var sig 			= parts[0];
			var payloadString 	= parts[1];
			
			hmac.Update( Encoding.GetBytes(payloadString) );
			var test = Encoding.ToBase64Url(hmac.Final());
			if ( test != sig )
			{
				return null;
			}
			// decode
			return Encoding.BytesFromBase64Url( payloadString );			
		}
		
		public static string Stringify( byte[] payload, Hmac hmac )
		{
			var payloadString = Encoding.ToBase64Url(payload);
			hmac.Update( Encoding.GetBytes(payloadString) );
			var digest = hmac.Final();
			return Encoding.ToBase64Url(digest) + "." + payloadString;
		}
	}
}