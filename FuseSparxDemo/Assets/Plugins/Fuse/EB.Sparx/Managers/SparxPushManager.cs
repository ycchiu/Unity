using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class PushManagerConfig
	{
		public string Protocol = "io.sparx.push";
	}

	public class PushManager : SubSystem, Updatable
	{
		public Action OnDisconnected;
		public Action OnConnected;

		PushAPI 			_api;
		Net.WebSocket 		_socket;
		Deferred 			_deffered;
		EB.Uri 				_url;
		string 				_puid = string.Empty;
		bool 				_gotToken;


		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_api = new PushAPI(Hub.ApiEndPoint);
			_deffered = new Deferred(4);
			_url = null;

#if UNITY_IPHONE
			NotificationServices.ClearLocalNotifications();
			NotificationServices.ClearRemoteNotifications();
#endif

#if UNITY_ANDROID
			GCMReceiver._onRegistered = this.OnGCMRegistered;
#endif
		}
		
		public bool UpdateOffline { get { return false;} }

		public override void Connect ()
		{
			State = SubSystemState.Connecting;
			
			var push = Dot.Object("push", Hub.LoginManager.LoginData, null);
			if ( push != null )
			{
				OnGetPushToken(null, push);
			}
			else
			{
				_api.GetPushToken(OnGetPushToken);	
			}
			
#if UNITY_IPHONE
			// get the apple push token 
			_gotToken = false;
			EB.Debug.Log("registering for notification services");
			NotificationServices.RegisterForRemoteNotificationTypes( RemoteNotificationType.Alert | RemoteNotificationType.Badge | RemoteNotificationType.Sound );
#endif

		}

		public void Update ()
		{
			_deffered.Dispatch();

#if UNITY_IPHONE			
			if (!_gotToken)
			{
				var token = NotificationServices.deviceToken;
				if (token != null)
				{
					_gotToken = true;
					var tokenHex = Encoding.ToHexString(token);
					_api.SetApplePushToken(tokenHex, OnSentApplePushToken);

					OtherLevelsSDK.RegisterDevice( _puid, tokenHex );  
				}
				else if ( !string.IsNullOrEmpty(NotificationServices.registrationError) )
				{
					_gotToken = true;
					EB.Debug.LogWarning("failed to register for push token : " + NotificationServices.registrationError);
				}

			}
#endif

#if UNITY_ANDROID
			if (!_gotToken)
			{
				var token = PlayerPrefs.GetString("OL_AndroidToken", string.Empty);
				if (!string.IsNullOrEmpty(token))
				{
					_gotToken = true;
					this.OnGCMRegistered(token);
				}
			}
#endif

		}

#if UNITY_ANDROID
		void OnGCMRegistered( string token )
		{
			Debug.Log("OnGCMRegistered: {0},{1}", _puid, token);
			OtherLevelsSDK.RegisterDevice( _puid, token ); 
		}
#endif

		void OnSentApplePushToken(string error)
		{
			if (!string.IsNullOrEmpty(error))
			{
				EB.Debug.Log("failed to send push token  " + error);
			}
		}
		

		public override void Disconnect (bool isLogout)
		{
			State = SubSystemState.Disconnected;
			if (_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}
		}
		
		public override void Dispose ()
		{
			if (_socket != null)
			{
				_socket.Dispose();
			}
		}
		#endregion
		
		void SetupSocket()
		{
			if (_socket != null)
			{
				_socket.Dispose();
			}
			
			_socket = new Net.WebSocket();
			_socket.OnConnect += OnConnect;
			_socket.OnError += OnError;
			_socket.OnMessage += OnMessage;
		}
		
		void OnConnect()
		{
			_deffered.Defer( (Action)delegate(){
				if ( OnConnected != null )
				{
					OnConnected();
				}
			});
			//Debug.Log("Connected to push server");
		}
		
		public void SimpleRPC( string type, Hashtable args )
		{
			ArrayList data = new ArrayList();
			
			data.Add(type);
			
			if ( null != args )
			{
				data.Add(args);
			}

			string str = JSON.Stringify(data);

			// EB.Debug.Log("->SimpleN " + str);
			if (_socket != null)
			{
				_socket.SendUTF8(str);
			}
		}

		void HandleMessage( Hashtable obj )
		{
			//EB.Debug.Log("Got async message: " + data);
			var component = Dot.String("component", obj, string.Empty);
			var message = Dot.String("message", obj, string.Empty);
			var payload = Dot.Find("payload", obj);
			
			var manager	= Hub.GetManager(component);
			if ( manager != null )
			{
				manager.Async(message,payload);
			}
			else
			{
				EB.Debug.LogError("Failed to find manager: " + component);
			}
		}
		
		void OnMessage( string data )
		{
			_deffered.Defer( (Action)delegate(){
				try
				{
					var obj = (Hashtable) JSON.Parse(data);
					HandleMessage(obj);
				}
				catch (System.Exception ex)
				{
					EB.Debug.LogError("Failed to parse async notification " + ex);
				}
			});	
		}

		public void OnMessage( Hashtable obj )
		{
			HandleMessage(obj);
		}

		void OnError( string error )
		{
			_deffered.Defer( (Action)delegate(){
				EB.Debug.Log("Lost connect to push server: " + error);

				if ( OnDisconnected != null )
				{
					OnDisconnected();
				}

				Coroutines.SetTimeout(delegate(){
					ConnectWebsocket();
				}, 1000);
			});
		}
		
		void ConnectWebsocket()
		{
			if (State != SubSystemState.Connected)
			{
				return;
			}
			
			if ( _socket == null)
			{
				SetupSocket();
			}
			
			if ( _socket.State < Net.WebSocketState.Connecting )	
			{
				_socket.ConnectAsync( _url, Hub.Instance.Config.PushManagerConfig.Protocol , null );
			}
		}
		
		void OnGetPushToken( string error, Hashtable result )
		{
			_puid = Dot.String("puid", result, Hub.LoginManager.LocalUserId.ToString() );
			var websocket = Dot.String("websocket", result, string.Empty);
			if (!string.IsNullOrEmpty(websocket))
			{
				_url = new EB.Uri(websocket);
				State = SubSystemState.Connected;
				ConnectWebsocket();
			}
			else
			{
				State = SubSystemState.Error;
			}

			
		}
	}
}

