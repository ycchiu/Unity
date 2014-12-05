using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	
	
	public class Request
	{			
	    private Hashtable   _data;
		private Hashtable   _query;
	    private EB.Uri	_url;
		private bool        _isPost;
		
		public object		userData		{get;set;}
		public int			id				{get;set;}
		
#if !UNITY_WEBPLAYER
		public Action<byte[]> dataCallback	{get;set;}
		public Action<Net.WebRequest> headersCallback {get;set;}
#endif

		public Hashtable	query			{get{return _query;}}
		public Hashtable	data			{get{return _data;}}
		public bool			isPost			{get{return _isPost;}}
		public EB.Uri	uri					{get{return _url;}}
		
		public int 			numRetries		{get;set;}
		
		public string 		url
		{
			get
			{
				var url = _url.Scheme + "://" + _url.HostAndPort + _url.Path + "?" + QueryString.Stringify(Dot.Flatten(_query));
				return url;
			}
		}
			
	    public Request(string url, bool isPost)
	    {
	        _url 	= new EB.Uri(url);
	        _isPost = isPost;
			numRetries = 5;
			
			_query  = QueryString.Parse(_url.Query);
			if (isPost)
			{
				_data = new Hashtable();
			}
			else
			{
				_data = _query;
			}
	    }
		
		public void AddQuery( string key, object value )
	    {
	        _query[key] = value;
	    }
			
	    public void AddData( string key, object value )
	    {
			_data[key] = value;
	    }	

		public void AddData( Hashtable data )
		{
			foreach( DictionaryEntry entry in data )
			{
				AddData(entry.Key.ToString(), entry.Value);
			}
		}
	}	
	
	public class Response
	{
		public Request		request			{get;private set;}
		public bool         sucessful 		{get;private set;}
		public int			timeTaken		{get;private set;} // time in miliseconds
		public string       text 			{get;private set;}
		public int			ts				{get;private set;}
		public object       result 			{get;private set;}
		public ArrayList	async			{get;private set;}
		public object       error 			{get; set;}
		public bool			fatal			{get; set;}
		public bool			retry			{get;private set;}
		public bool			sessionError	{get;private set;}


	    public Hashtable    hashtable 		
		{
			get 
			{
				if (result is Hashtable)
				{
					return (Hashtable)result;
				}
				return default(Hashtable);
			}
		}

	    public ArrayList    arrayList 
		{
			get 
			{
				if (result is ArrayList)
				{
					return (ArrayList)result;
				}
				return default(ArrayList);
			}
		}

	    public double       number 	
		{
			get 
			{
				if (result is double)
				{
					return (double)result;
				}
				return default(double);
			}
		}

		public string		str	
		{
			get 
			{
				if (result is string)
				{
					return (string)result;
				}
				return default(string);
			}

		}
		
		public string		localizedError
		{
			get
			{
				if ( error != null)
				{
					var tmp = error.ToString();
					var localized = string.Empty;
					if (Localizer.GetString(tmp,out localized))
					{
						return localized;
					}
					else if (tmp.StartsWith("^"))
					{
						// pre localized
						return tmp.Substring(1);
					}

					EB.Debug.LogError("Unknown error: " + tmp );
					
					// fallback
					return Localizer.GetString("ID_SPARX_ERROR_UNKNOWN");
				}
				return string.Empty;
			}
		}
		
		public int id				
		{
			get
			{
				return request.id;	
			}
		}
		
		public string url
		{
			get
			{
				return request.url;	
			}
		}
		
		public Response(Request r) 
		{
			request = r;
		}

		public void Parse( Net.WebRequest www ) 
		{
			var res = this;

			res.timeTaken = www.responseTime;

			if ( string.IsNullOrEmpty(www.error) && !string.IsNullOrEmpty(www.text) )
			{
				res.text = www.text ?? string.Empty;
				res.result = null;
				res.error = null;
				
				try
				{
					object obj = JSON.Parse(www.text);
					if ( obj == null )
					{
						res.error = "Failed to decode json: " + www.text;
					}
					else  
					{
						if ( obj is Hashtable )
						{
							Hashtable response =  obj as Hashtable;
							res.result = response["result"];
							res.async = (ArrayList)response["async"];
							res.error = response["err"];
							res.ts = Dot.Integer("ts", response, 0);
						}
						else
						{
							res.error = "Obj is " + obj.ToString();
						}	
					}                    
				}
				catch (System.Exception ex)
				{
					EB.Debug.LogWarning("failed to decode www " + ex);
					res.error = ex;
				}
								
				if ( res.error == null )
				{
					res.sucessful = true;
					res.fatal = false;
					res.sessionError = false;
					return;
				}
				
				res.fatal 		= Dot.Bool("fatal", res.hashtable, true);
				res.sessionError= Dot.Bool("invalid_session", res.hashtable, false);
				res.retry 		= Dot.Bool("retry",res.hashtable, true);
			}
			else
			{
				res.error = www.error ?? "unknown";
				res.fatal = true;
				res.retry = true;
				
				// if this is a client error, then don't retry
				if (www.statusCode >= 400 && www.statusCode <= 499)  
				{
					res.retry = false;
				}

			}
		}

	}
}


