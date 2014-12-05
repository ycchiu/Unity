using UnityEngine;
using System;
using System.Collections;

namespace EB
{
	// secure player prefs (are signed)
	public static class SecurePrefs 
	{
		public static Hmac Key = Hmac.Sha1( Crypto.CreateKey("3498=32wcqeDS#!wfad93r2/3tf") );
		
		private static string GetKey( string key )
		{
#if UNITY_EDITOR
			return "editor-"+key;
#else
			return key;
#endif
		}
		
		private static byte[] Get( string key )
		{
			var data = PlayerPrefs.GetString( GetKey(key), string.Empty);
			if (!string.IsNullOrEmpty(data))
			{
				return SignedRequest.Parse(data, Key);
			}
			return null;
		}
		
		private static void Set( string key, byte[] data )
		{
			if (data != null)
			{
				PlayerPrefs.SetString( GetKey(key), SignedRequest.Stringify( data, Key ) ); 	
			}
			else
			{
				PlayerPrefs.DeleteKey( GetKey(key) );
			}
			
			Save();
		}
		
		public static void Save()
		{
			PlayerPrefs.Save();
		}
		
		public static void DeleteKey( params string[] keys )
		{
			foreach( var key in keys )
			{
				PlayerPrefs.DeleteKey( GetKey(key) );
			}
			Save();
		}
		
		public static int GetInt( string key, int defaultValue )
		{
			var data = Get(key);
			if ( data != null )
			{
				return BitConverter.ToInt32(data,0);
			}
			return defaultValue;
		}
		
		public static void SetInt(string key, int value)
		{
			Set(key, BitConverter.GetBytes(value) );
		}
		
			
		public static float GetFloat( string key, float defaultValue )
		{
			var data = Get(key);
			if ( data != null )
			{
				return BitConverter.ToSingle(data,0);
			}
			return defaultValue;
		}
		
		public static void SetFloat(string key, float value)
		{
			Set(key, BitConverter.GetBytes(value) );
		}
		
		
		public static string GetString( string key, string defaultValue )
		{
			var data = Get(key);
			if ( data != null )
			{
				return Encoding.GetString(data);
			}
			return defaultValue;	
		}
		
		public static void SetString( string key, string value )
		{
			Set(key, Encoding.GetBytes(value) );
		}
		
		public static object GetJSON( string key ) 
		{
			var json = GetString(key, string.Empty);
			if (!string.IsNullOrEmpty(json))
			{
				return JSON.Parse(json);
			}
			return null;
		}
		
		public static string SetJSON( string key, object json )
		{
			var value = JSON.Stringify(json);
			SetString( key, value );
			return value;
		}
		
		
	}
	
}