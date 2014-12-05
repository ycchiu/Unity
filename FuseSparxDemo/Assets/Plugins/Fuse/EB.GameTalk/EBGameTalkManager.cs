using UnityEngine;
using System.Collections;
using System.Reflection;

namespace EB.GameTalk
{
	public class Config
	{
		public string serverUrl = "wss://gametalk.sparx.io:30443";
		public string serverKey = "GameTalk-Kabam"; 
		public string game = "";
	}
	
	public class GameTalkRPC : System.Attribute
	{
		
	}
	
	public class Manager : System.IDisposable
	{
		private readonly bool Enabled = false;
		private Config _config;
		private EB.Net.TalkWebSocket _socket;
		private Hashtable _rpcs;
		private EB.Uri _wsUrl;
		private EB.Deferred _deferred;
		private object _updateHandle;
		
		public Manager( Config config )
		{
			if( Enabled == true )
			{
				_config = config;
				_rpcs = new Hashtable();	
				_deferred = new EB.Deferred(8);
				_updateHandle = EB.Coroutines.SetUpdate(_deferred.Dispatch);
				
				// load certs
				EB.Net.TcpClientFactory.LoadCertStore("Certs");
				
				// make the signed request
				Hashtable data = new Hashtable();
				data["_id"] = EB.Version.GetMACAddress();
				data["type"]= Application.platform.ToString();
				data["game"]= _config.game;
				data["last"]= EB.Time.Now;
				data["algorithm"] = "hmac-sha1";
				var json = EB.JSON.Stringify(data);
				
				var key = SignedRequest.Stringify( Encoding.GetBytes(json), Hmac.Sha1(Encoding.GetBytes(_config.serverKey)));
				
				var url = _config.serverUrl+"?access_token="+key;
				_wsUrl = new EB.Uri(url);
				Debug.Log("GameTalk Url: " + url);
				
				_socket = new EB.Net.TalkWebSocket();
				_socket.OnError += this.OnError;
				_socket.OnRPC += this.OnRPC;
							
				Bind(string.Empty, new RPC.Global());
				
				ConnectIfNeeded();
			}
		}
		
		void ConnectIfNeeded()
		{		
			if ( _socket.State < Net.WebSocketState.Connecting )	
			{
				_socket.ConnectAsync( _wsUrl,"io.sparx.gametalk",null );
			}
		}
		
		void OnError(string error)
		{
			// reconnect if there's a error
			//_deferred.Defer( (Action)delegate(){
			//	Coroutines.SetTimeout(delegate(){
			//	Debug.Log("Reconnecting");
			//	ConnectIfNeeded();	
			//	}, 1000);
			//});
		}
		
		void OnRPC( string rpcName, ArrayList parameters, Action<string,object> callback )
		{
			_deferred.Defer((Action)delegate(){
				var handler = (Action<ArrayList,Action<string,object>>)_rpcs[rpcName];
				if (handler != null)	
				{
					handler(parameters, callback);
				}
				else 
				{
					Debug.LogWarning("Missing RPC callback for RPC " + rpcName + " on socket " + this._wsUrl );
				}
			});
		}
		
		public void RPC (string name, ArrayList args, EB.Action<string, object> callback)
		{
			ConnectIfNeeded();
			
			_socket.RPC(name, args, delegate(string err,object result){
				//EB.Debug.Log("RPC callback " + name );
				_deferred.Defer( callback, err, result ); 
			});
		}
		
		public void Bind( string rpcName, Action<ArrayList,Action<string,object>> callback )
		{
			_rpcs[rpcName] = callback;
		}
		
		public void Bind( string ns, object instance )
		{
			if (string.IsNullOrEmpty(ns))
			{
				ns = "";
			}
			else
			{
				ns += ".";
			}
			
			var methods = instance.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
			foreach( var method in methods )
			{
				if (method.GetCustomAttributes(typeof(GameTalkRPC),true).Length > 0)
				{
					this.Bind(ns+method.Name, delegate(ArrayList obj, Action<string, object> obj1) {
						instance.GetType().InvokeMember(method.Name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.InvokeMethod, null, instance, new object[]{obj, obj1});
						//method.Invoke( instance, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.InvokeMethod, null, , null);
					});
				}
			}
		}
		
		#region IDisposable implementation
		void System.IDisposable.Dispose ()
		{
			if (_updateHandle != null)
			{
				EB.Coroutines.ClearUpdate(_updateHandle);
				_updateHandle = null;
			}
			
			if (_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}
		}
		#endregion
		
	}

}

