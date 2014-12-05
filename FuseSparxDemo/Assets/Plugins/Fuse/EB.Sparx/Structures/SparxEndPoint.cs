using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public struct EndPointOptions
	{
		public byte[] 		Key;
		public string 		Protocol;
		public bool 		KeepAlive;
		public string 		KeepAliveUrl;
		public int 			KeepAliveInterval;
		public int			ActivityTimeout;
	}
	
	// abstraction for a service endpoint
	// handles queuing of requests, ordering and sessioning
	public abstract class EndPoint : System.IDisposable
	{
		public EndPointOptions Options {get;private set;}
		public string Url {get;private set;}
		
		protected bool HasInternetConnectivity 
		{
			get
			{
				return Application.internetReachability != NetworkReachability.NotReachable;
			}
		}
				
		public EndPoint( string url, EndPointOptions options )
		{
			Url = url;
			Options = options;
			
			if (Hub.Instance != null)
			{
				Hub.Instance.OnUpdate += this.Update;
			}
		}
		
		public virtual void StartKeepAlive() {}
		public virtual void StopKeepAlive() {}
		
		public virtual void Dispose ()
		{
			try {
				if ( Hub.Instance != null ) {
					Hub.Instance.OnUpdate -= this.Update;
				}
			}
			catch {}
		}
		
		public virtual void RPC( string name, ArrayList args, EB.Action<string,object> callback )
		{
			throw new System.NotSupportedException();
		}
		
		public virtual Request Get( string path )
		{
			return new Request( Url+path, false);
		}
		
		public virtual Request Post( string path )
		{
			return new Request( Url+path, true);
		}
		
		public virtual 	void 	AddData(string uri, string key, object value) 			{}		
		public virtual object 	GetData(string uri, string key)						{ return null; }
		
		public virtual 	void Update()						  {}
		public abstract int Service( Request request, EB.Action<Response> callback );
		public abstract bool IsDone(int requestId);
		
		public Coroutine ServiceCoroutine( Request request, EB.Action<Response> callback)
		{
			var id = Service(request, callback);
			return Wait(id);
		}
		
		public Coroutine Wait (int requestId)
		{
			return Coroutines.Run(_Wait(requestId));
		}
		
		private IEnumerator _Wait(int requestId)
		{
			while( IsDone(requestId) == false )
			{
				yield return 1;
			}
		}
		
	}
	
	public static class EndPointFactory
	{
		public static EndPoint Create( string url, EndPointOptions options ) 
		{
			var uri = new EB.Uri(url);
			switch(uri.Scheme)
			{
			case "http":
			case "https":
				return new HttpEndPoint(url,options);
			case "ws":
			case "wss":
				return new WebSocketEndPoint(url,options);
			default:
				throw new System.ArgumentException("Unknown scheme " + uri.Scheme);
			}
		}
	}
	
}

