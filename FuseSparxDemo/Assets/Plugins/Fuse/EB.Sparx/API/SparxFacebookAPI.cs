using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	
	public class FacebookAPI
	{
		private string _accessToken;
		const int 		_numRetries = 5;
		
		public FacebookAPI( string access_token )
		{
			_accessToken = access_token;
		}
		
		public Coroutine Post( string uri, Hashtable parameters, Action<string,object> callback )
		{
			return Coroutines.Run(_Graph("POST",uri, parameters, callback));
		}
		
		public Coroutine Get( string uri, Hashtable parameters, Action<string,object> callback )
		{
			return Coroutines.Run(_Graph("GET",uri, parameters, callback));
		}
		
		int GetStatusCode( WWW www )
		{
			string header;
			if (www.responseHeaders.TryGetValue("STATUS", out header) )
			{
				var parts = header.Split(' ');
				if ( parts.Length > 2 )
				{
					int value = 0;
					int.TryParse(parts[1], out value);
					return value;
				}
			}
			return 0;
		}
		
		private static WWWForm BuildForm( Hashtable options ) 
		{
			WWWForm form = new WWWForm();
			
			options.Remove("access_token");
			foreach( DictionaryEntry entry in options )
			{
				var key = entry.Key;
				var value = entry.Value;
				var valueStr = value.ToString();
				
				if ( value is byte[] )
				{
					form.AddBinaryData( key.ToString(), (byte[])value );
					continue;
				}
				else if ( value is ICollection )
				{
					valueStr = JSON.Stringify(value);
				}
			
				form.AddField( key.ToString(), valueStr ); 
			}
			
			return form;
		}
		
		IEnumerator _Graph( string method, string uri, Hashtable parameters, Action<string,object> callback) 
		{
			var url = "https://graph.facebook.com"+uri;
			
			if (parameters == null)
			{
				parameters = new Hashtable();
			}
			
			
			string lastError = string.Empty;
			for (int i = 0; i < _numRetries; ++i )
			{
				WWW www = null;
				if (method == "POST")
				{
					var form = BuildForm(parameters);
					www = new WWW(url + "?access_token="+_accessToken, form);
				}
				else
				{
					parameters["access_token"] = _accessToken;
					www = new WWW(url + "?" + QueryString.Stringify(parameters));
				}
				
				EB.Debug.Log("url: " + www.url);
				
				yield return www;
				
				var status = GetStatusCode(www);
				
				if (string.IsNullOrEmpty(www.error))
				{
					var text = www.text;
					try {
						EB.Debug.Log("FbResult: " + text);
						var result = JSON.Parse(text);
						callback(null,result);
						yield break;
					}
					catch {
						
					}
				}
				else if ( status >= 400 && status <= 499 )
				{
					// client error
					lastError = www.error;
					break;
				}
				else 
				{
					EB.Debug.Log("Error headers: {0}", www.responseHeaders );
					// facebook error
					lastError = www.error;
					yield return new WaitForSeconds(0.5f);
				}
			}
			
			callback(lastError,null);
			
		}
		
	}
	
}
