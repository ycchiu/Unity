using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public static class QueryString
	{
		public static Hashtable Parse( string query )
		{
			Hashtable pars = new Hashtable();
			
			int queryIndex = query.IndexOf('?');
			if ( queryIndex >= 0 )
			{
				query = query.Substring(queryIndex + 1 );
			}
			
			foreach( string pair in query.Split(new char[]{'&'}, System.StringSplitOptions.RemoveEmptyEntries) )
			{
				string[] parts = pair.Split('=');
				var key = WWW.UnEscapeURL(parts[0]);
				if ( parts.Length == 1 )
				{
					pars[key] = string.Empty;
				}
				else if ( parts.Length ==2 )
				{
					pars[key] =  WWW.UnEscapeURL(parts[1]);
				}
			}
			return pars;
		}
		
		public static string Escape( string str )
		{
			string r = "";
			for ( int i = 0; i < str.Length; ++i )
			{
				char c = str[i];
				if ( (c >= 'A' && c <='Z' ) || (c>='a' && c <='z') || (c >= '0' && c <= '9') || (c=='-') || (c=='_') || (c=='.') || (c=='!') || (c=='~') || (c=='*') || (c=='\'') || (c=='(') || (c==')') )
				{
					r += c;
				}
				else
				{
					//Debug.Log("escaping " + c + " " + string.Format("{0:X2}", (int)c) );
					r += string.Format("%{0:X2}", (int)c);
				}
			}
			return r;
		}
		
		public static string Stringify( Hashtable data )
		{
			var sb = new System.Text.StringBuilder(2048);
			var first = true;
			
			foreach( DictionaryEntry entry in data )
			{
				var str = Escape(entry.Key.ToString())+'='+Escape(entry.Value.ToString());
				if ( !first )
				{
					sb.Append('&');
				}
				first = false;
				sb.Append(str);
			}
			
			return sb.ToString();
		}
		
		public static string StringifySorted( Hashtable data )
		{
			var sb = new System.Text.StringBuilder(2048);
			var first = true;
			
			List<string> tmp = new List<string>(data.Keys.Count);
			foreach( var key in data.Keys )
			{
				tmp.Add(key.ToString());
			}
			tmp.Sort();
			
			foreach( string key in tmp)
			{
				string value = data[key].ToString();
				var str = Escape(key)+'='+Escape(value);
				if ( !first )
				{
					sb.Append('&');
				}
				first = false;
				sb.Append(str);
			}
			
			return sb.ToString();
		}
	}
}

