using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class WebSocketEndPoint : HttpEndPoint
	{
		private Net.TalkWebSocket _socket = null;
		private Deferred _deferred = new Deferred(8);
		private EB.Uri _wsUrl;

		// make a compatible url
		static string MakeHttpEndpoint( string endPoint )
		{
			return endPoint.Replace("wss://","https://").Replace("ws://","http://");
		}

		// token for the websocket
		public WebSocketEndPoint( string endPoint, EndPointOptions options )  :
			base(MakeHttpEndpoint(endPoint),options)
		{
			_wsUrl = new EB.Uri(endPoint);
			_socket = new Net.TalkWebSocket();
			_socket.ActivityTimeout = options.ActivityTimeout;
			_socket.OnError += OnError;

			if ( options.KeepAlive )
			{
				ConnectIfNeeded();
			}
		}

		public override void Dispose ()
		{
			base.Dispose();

			if (_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}

		}

		void OnError(string error)
		{
			if (Options.KeepAlive)
			{
				// reconnect if there's a error
				_deferred.Defer( (Action)delegate(){
					Coroutines.SetTimeout(delegate(){
						EB.Debug.Log("Reconnecting");
						ConnectIfNeeded();
					}, 1000);
				});
			}
		}

		void ConnectIfNeeded()
		{
			if ( _socket.State < Net.WebSocketState.Connecting )
			{
				_socket.ConnectAsync( _wsUrl, Options.Protocol, Options.Key );
			}
		}

		public override void Update ()
		{
			base.Update();

			// deferred callbacks
			_deferred.Dispatch();
		}

		#region implemented abstract members of EB.Sparx.EndPoint
		public override void RPC (string name, ArrayList args, EB.Action<string, object> callback)
		{
			ConnectIfNeeded();

			_socket.RPC(name, args, delegate(string err,object result){
				//EB.Debug.Log("RPC callback " + name );
				_deferred.Defer( callback, err, result );
			});
		}
		#endregion
	}
}
