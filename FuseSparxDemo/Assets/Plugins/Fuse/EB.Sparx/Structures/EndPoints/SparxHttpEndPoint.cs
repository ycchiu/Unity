using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	// abstraction for a service endpoint
	// handles queuing of requests, ordering and sessioning
	public class HttpEndPoint : EndPoint
	{
		const string SessionKey = "stoken";
		
		private EB.Collections.Queue<Request> _queue = new EB.Collections.Queue<Request>();
		private Dictionary<string,Hashtable> _data = new Dictionary<string, Hashtable>();
		private Request _current = null;
		private int _nextId = 1;
		private int _doneId = 0;
		
		private object _interval;
		private int _lastActivity = -1;
		
		private Hmac _hmac;

		static System.DateTime _lastMemoryCheck = System.DateTime.Now;

		public HttpEndPoint( string endPoint, EndPointOptions options )  :
			base(endPoint,options)
		{
			if (options.Key != null)
			{
				_hmac = Hmac.Sha1(options.Key);
			}

			// add signing version
			AddData(string.Empty,"sver", 2);
		}
		
		public override void Dispose ()
		{			
			StopKeepAlive();
			
			base.Dispose ();
		}
		
		public override void StartKeepAlive ()
		{
			StopKeepAlive();
			if ( Options.KeepAlive && Options.KeepAliveInterval > 0 && !string.IsNullOrEmpty(Options.KeepAliveUrl))
			{
				_interval = Coroutines.SetInterval(OnInterval, (Options.KeepAliveInterval*1000)/4);
			}
		}
		
		public override void StopKeepAlive ()
		{
			Coroutines.ClearInterval(_interval);
			_interval = null;
			_lastActivity = -1;
		}
		
		private void OnInterval()
		{
			//EB.Debug.LogError("OnInterval " + _lastActivity + " " + (Time.Now-_lastActivity));
			if ( _lastActivity > 0 && (Time.Now-_lastActivity) > Options.KeepAliveInterval )
			{
				var keepAlive = Post(Options.KeepAliveUrl);
				Service(keepAlive, delegate(Response r){
					if (!r.sucessful && r.fatal)
					{
						Hub.Instance.FatalError(r.localizedError);	
					}
				});
			}
		}
		
		public override void AddData( string uri, string name, object value )
		{
			Hashtable ht = null;
			if (!_data.TryGetValue(uri, out ht))
			{
				ht = new Hashtable();
				_data[uri] = ht;
			}
			ht[name] = value;
		}
		
		public override object GetData (string uri, string key)
		{
			Hashtable ht = null;
			if (!_data.TryGetValue(uri, out ht))
			{
				return null;
			}
			return ht[key];
		}

				
		Request Prepare( Request r )
		{
			// get globals
			Hashtable ht = null;
			if (_data.TryGetValue(string.Empty, out ht))
			{
				foreach( DictionaryEntry e in ht )
				{
					r.AddQuery(e.Key.ToString(), e.Value);
				}
			}

			if (_data.TryGetValue(r.uri.Path, out ht))
			{
				foreach( DictionaryEntry e in ht )
				{
					r.AddQuery(e.Key.ToString(), e.Value);
				}
			}

			return r;
		}
		
		public override int Service( Request request, EB.Action<Response> callback )
		{
			// store the callback
			request.userData = callback;
			request.id = _nextId++;
			_queue.Enqueue(request);
			ServiceNext();
			return request.id;
		}
		
		public override bool IsDone( int requestId )
		{
			return _doneId >= requestId;
		}
		
		private void ServiceNext()
		{
			if ( _current != null)	
			{
				return;
			}
			
			if ( _queue.Count > 0 )
			{
				_current = _queue.Dequeue();
				Prepare(_current);
				EB.Coroutines.Run(Fetch(_current));
			}
		}
		
		string Sign( Request r, string method, string data )
		{
			if (Options.Key != null)
			{
				var sb = new System.Text.StringBuilder(2048);
				sb.Append(method);
				sb.Append("\n");
				sb.Append(r.uri.HostAndPort);
				sb.Append("\n");
				sb.Append(r.uri.Path);
				sb.Append("\n");
				sb.Append(data);
				sb.Append("\n");
				//EB.Debug.Log("String to sign: " + sb);
				var digest = _hmac.Hash(Encoding.GetBytes(sb.ToString()));
				return Encoding.ToBase64String(digest);
			}
			return string.Empty;
		}
		
	    private Net.WebRequest Generate( Request r, Response res, int retry )
	    {        
			// need to sign the request 
			// we use a method similar to AWS signature version 2 (with a few changes to better support JSON and not sort the qs)
			Net.WebRequest request = null;
			
			// timestamp
			if ( Time.Valid )
			{
				r.AddQuery("ts", Time.Now);
			}
			
			if ( retry > 0 )
			{
				r.AddQuery("retry", retry);
			}

			var data = "";
			r.query.Remove("sig");
			var signingData = QueryString.StringifySorted(Dot.Flatten(r.query));

	        if ( r.isPost )
	        {
	        	if( r.data.ContainsKey( "nonce" ) == false )
	        	{
					r.AddData( "nonce", EB.Sparx.Nonce.Generate() );
	        	}
	            // post as json		
				var json = JSON.Stringify(r.data);
				signingData+="\n"+json;

				byte[] body = Encoding.GetBytes(json);
				string sig	= Sign(r, "POST", signingData);
				r.AddQuery("sig", sig);
	
	            Hashtable headers = new Hashtable();
	            headers["Content-Type"] = "application/json";

				EB.Debug.Log("HttpRequest: POST ({0}): {1}\n{2}", r.id, r.url, json);		
				request = new Net.WebRequest(r.url, body, headers); 
	        }
	        else
	        {
				EB.Debug.Log("HttpRequest: GET ({0}): {1}", r.id, r.url);
				string sig = Sign(r,"GET", signingData);
				r.AddQuery("sig", sig);

	            request =  new Net.WebRequest(r.url);
	        }

			request.OnComplete = res.Parse;			
			request.Start();
			return request;
	    }
		
		IEnumerator Fetch( Request req) 
		{	
			var res = new Response(req);

			// check memory
			if ( (System.DateTime.Now -_lastMemoryCheck).TotalSeconds> 2) 
			{
				EB.Memory.Update(10000);
				_lastMemoryCheck = System.DateTime.Now;
			}
			
	        int kNumRetries = req.numRetries;
	        for (int i = 0; i <= kNumRetries; ++i)
	        {
				// JEH - moved this from down there, somewhere
				if (HasInternetConnectivity == false)
				{
					EB.Debug.Log("No internet connectivity");
					res.error = "ID_SPARX_ERROR_NOT_CONNECTED";
					res.fatal = true;
					break;
				}
				// wait and try again
				if (i>1) 
				{
	           		yield return new WaitForSeconds( Mathf.Pow(2.0f, i) );
				}
				
				// check for hacks
				if (SafeValue.Breach || Memory.Breach)
				{
					res.fatal = true;
					res.error = "ID_SPARX_ERROR_UNKNOWN";
					break;
				}
			
	   			var www = Generate(req, res, i);	

				while(www.isDone==false)
				{
					yield return 1;
				}

#if !UNITY_WEBPLAYER
				if (www.failure == EB.Net.NetworkFailure.None)
				{
					NewRelicPlugin.LogHttpRequest(www.url, www.statusCode, www.NRTimer, www.bytesSent, www.bytesRecieved, www.text);
				}
				else
				{
					NewRelicPlugin.LogHttpFailure(www.url, www.NRTimer, www.failure);;
				}
#endif

				EB.Debug.Log("HttpRequest: RESULT ({0}): {1}", req.id, res.text);

				if (res.ts != 0)
				{
					EB.Time.Now = res.ts;
				}
				_lastActivity = EB.Time.Now;

				if (res.sucessful || res.retry == false)
				{
					break;
				}

	        }
			
			OnComplete(res);
			
	    }
		
		private void PrintError( Request r, Net.WebRequest www )
		{
			EB.Debug.LogError("Url: " + r.url + " Error: " + (www.error ?? "null") + " Status: " + www.statusCode );
			if ( www.responseHeaders != null && www.responseHeaders.Count > 0 )
			{
				foreach( var header in www.responseHeaders )	
				{
					EB.Debug.LogError("Error Header: {0}:{1}", header.Key ?? "" , header.Value ?? "" );
				}
			}
			else
			{
				//EB.Debug.LogError("No response headers");
			}		
		}
		
		private void OnComplete( Response result )
		{
			var request = result.request;
			if (_current != result.request)
			{
				EB.Debug.LogError("Something went seriously wrong in the service queue");
			}
						
			var async = result.async;
			if (async != null )
			{
				foreach(Hashtable message in async)
				{
					SparxHub.Instance.PushManager.OnMessage(message);
				}
			}

			if ( request.userData != null && request.userData is EB.Action<Response> )
			{
				var cb = (EB.Action<Response>)request.userData;
				cb(result);
			}
			
			_doneId  = _current.id;
			_current = null;
			
			ServiceNext();
		}
				
	}
	
}
