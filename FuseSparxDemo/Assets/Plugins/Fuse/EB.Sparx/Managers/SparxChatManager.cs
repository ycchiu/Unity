using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ChatConfig
	{
		public ChatListener Listener 		= new DefaultChatListener();
		public int 			ChatListRecent 	= 10;	// number of entries to pull on getmore/join
		public int 			ChatListPrune 	= 100;	// max number of entries in memory (prunes oldest)
	}

	public class ChatItem
	{
		public string 			id 			= string.Empty;
		public string 			channel		= string.Empty;
		public string 			text 		= string.Empty;
		public string 			filtered 	= string.Empty;
		public object 			attributes 	= null;
		public string 			locale		= string.Empty;
		public string 			name		= string.Empty;
		public Id				naid		= Id.Null;
		public int		 		ts 			= 0;

		public Id uid { get { return new Id(Dot.Find("uid",this.attributes)); } }

		public ChatItem(object item)
		{
			this.naid		= new Id( Dot.String("uid", item, string.Empty) );
			this.id 		= Dot.String("id", item, string.Empty);
			this.text 		= Dot.String("text", item, string.Empty);
			this.filtered 	= Dot.String("filtered", item, string.Empty);
			this.attributes = Dot.Object("attributes", item, new Hashtable() );
			this.channel	= Dot.String("channel", item, string.Empty);
			this.locale		= Dot.String("locale", item, string.Empty);
			this.name 		= Dot.String ("name", item, string.Empty);
			this.ts   		= Dot.Integer("ts",item, 0); 
		}
	}

	public class ChatList : List<ChatItem>
	{
		public ChatList(int capacity) :
			base(capacity)
		{

		}

		public ChatItem Find( string id )
		{
			return this.Find( delegate(EB.Sparx.ChatItem obj) {
				return obj.id == id;
			});
		}

		public new void Sort()
		{
			this.Sort(delegate(ChatItem x, ChatItem y) {
				return string.Compare(x.id,y.id);
			});
		}
	}

	public class ChatManager : SubSystem, Updatable
	{
		#region Updatable implementation

		public bool UpdateOffline 
		{
			get 
			{
				return true;
			}
		}

		#endregion

		Net.TalkWebSocket 				_socket;
		Deferred 						_deffered;
		int 							_retry = 0;
		ChatAPI 						_api;
		ChatConfig						_config;
		Dictionary<string,ChatList> 	_channels = new Dictionary<string, ChatList>();
		ChatList						_tmp;

		public string[] Channels 
		{
			get
			{
				var keys =_channels.Keys;
				var arr = new string[keys.Count];
				keys.CopyTo(arr,0);
				return arr;
			}
		}

		public string DefaultChannel 
		{
			get
			{
				var ch = this.Channels;
				return ch.Length > 0 ? ch[0] : string.Empty;
			}
		}

		public override void Initialize (Config config)
		{
			_config = config.ChatConfig;
			_deffered = new Deferred(4);

			_api = new ChatAPI(Hub.ApiEndPoint);
			_tmp = new ChatList(_config.ChatListRecent);

			_socket = new Net.TalkWebSocket();
			_socket.OnError += _OnError;
			_socket.OnConnect += _OnConnect;
			_socket.OnRPC += _OnRPC;
		}

		public override void Connect ()
		{
			_OnChatUrl(string.Empty, Dot.Object("chat", Hub.LoginManager.LoginData, null));
			State = SubSystemState.Connected;
		}

		public override void Disconnect (bool isLogout)
		{
			_socket.Reset();
		}

		public void Reconnect()
		{
			_api.GetChatToken(_OnChatUrl);
		}

		public override void Dispose ()
		{
			_socket.Dispose();
			base.Dispose ();
		}

		public ChatList GetChatList( string channel )
		{
			ChatList list = null;
			if (string.IsNullOrEmpty(channel)==false)
			{
				_channels.TryGetValue(channel, out list);
			}
			return list;
		}

		public void GetMore( string channel ) 
		{
			var list = GetChatList(channel);
			if (list != null && list.Count < _config.ChatListPrune )
			{
				var opts = new Hashtable();
				opts["limit"] = _config.ChatListRecent;

				if (list.Count > 0)
				{
					// get the oldest one
					opts["last"] = list[0].id;
				}
				
				var args = new ArrayList();
				args.Add(channel);
				args.Add(opts);

				if (_socket.State == EB.Net.WebSocketState.Open)
				{
					_socket.RPC("history", args, delegate(string err, object result) {
						_deffered.Defer( (Action)delegate(){
							_OnJoin(channel, err, result);
						});
					});
				}
			}
		}

		public override void Async (string message, object payload)
		{
			base.Async (message, payload);

			switch(message)
			{
			case "token" :
				{
					_OnChatUrl(null, payload);
				}
				break;
			}
		}

		public void Send( string channel, string text, Hashtable attributes = null, EB.Action<string,ChatItem> cb = null  ) 
		{
			var list = GetChatList(channel);
			if (list != null && !string.IsNullOrEmpty(text) )
			{
				attributes 			= attributes ?? new Hashtable();
				attributes["uid"] 	= Hub.LoginManager.LocalUserId;
				attributes["name"] 	= Hub.LoginManager.LocalUser.Name;

				var args = new ArrayList();
				args.Add(channel);
				args.Add(text);
				args.Add(attributes);
		
				if (_socket.State == EB.Net.WebSocketState.Open)
				{
					_socket.RPC("send", args, delegate(string err, object result) {
						_deffered.Defer( (Action)delegate(){
							// dont need the item as it will come through the rpc.
							if (cb != null){
								var item = new ChatItem(result);
								cb(err,item);
							}
						});
					});
				}
			}
			else
			{
				Debug.LogError("Failed to send channel ("+channel+"): room is not joined"); 
			}
		}

		public void Join( string channel )
		{
			//Debug.LogError("Join: " + channel);
			if (!_channels.ContainsKey(channel))
			{
				_channels[channel] = new ChatList(_config.ChatListPrune);
			}

			if (_socket.State == EB.Net.WebSocketState.Open)
			{
				var opts = new Hashtable();
				opts["history"] = true;
				opts["limit"] = _config.ChatListRecent;

				var args = new ArrayList();
				args.Add(channel);
				args.Add(opts);

				_socket.RPC("join", args, delegate(string err, object result) {
					_deffered.Defer( (Action)delegate(){
						_OnJoin(channel, err, result );
					});
				});
			}
		}

		void _OnJoin(string channel, string err, object result) 
		{
			Debug.Log("_OnJoin {0}, {1}, {2}", channel, err, result); 
			if (result is ArrayList)
			{
				var ar = (ArrayList)result;
				_tmp.Clear();
				foreach( var item in ar )
				{
					try 
					{
						var c = new ChatItem(item);
						_tmp.Add(c);
					}
					catch
					{

					}
				}
				_Add(channel, _tmp);
			}
		}

		void _OnChatUrl(string error, object result) 
		{
			var url = Dot.String("url", result, string.Empty);
			if (string.IsNullOrEmpty(url) == false)
			{
				var uri = new Uri(url);
				_socket.ConnectAsync(uri, "io.sparx.chat", null);

				var rooms = Dot.Array("rooms", result, new ArrayList() );
				if (rooms != null && rooms.Count > 0 )
				{
					foreach(var room in rooms )
					{
						if (room != null)
						{
							Join(room.ToString());		
						}
					}
				}
			}
			else
			{
				Debug.LogError("Chat url not defined, chat service disabled");
			}
		}

		void _OnRPC (string channel, ArrayList arg, Action<string, object> cb)
		{
			if (arg.Count >= 1)
			{
				try {
					var item = new ChatItem(arg[0]);
					_deffered.Defer((Action)delegate(){
						_tmp.Clear();
						_tmp.Add(item);
						_Add(channel, _tmp );
					});
				}
				catch (System.Exception ex) {
					Debug.LogError("Failed to add chat item: " + ex);
				}
			}
			cb(null,null);
		}

		void _Add(string channel, ChatList items)
		{
			var list = GetChatList(channel);
			if (list != null)
			{
				foreach( var item in items )
				{
					if ( list.Find(item.id) == null) 
					{
						list.Add(item);
					}
				}
				list.Sort();

				// prune
				if (list.Count > _config.ChatListPrune)
				{
					list.RemoveRange(0, list.Count-_config.ChatListPrune);
				}

				_config.Listener.OnUpdated(channel);
			}
		}

		void _OnConnect()
		{
			Debug.Log("Connect to chat service");

			// join 
			foreach( var channel in _channels.Keys )
			{
				Join(channel);
			}
		}

		void _OnError(string err) 
		{
			_retry++;
			_deffered.Defer( (Action)delegate(){
				Coroutines.SetTimeout(delegate(){
					EB.Debug.Log("Reconnecting to chat server: " + err);
					Reconnect();
				}, (int)Mathf.Pow(2,_retry)*1000 );
			});
		}

		public void Update ()
		{
			_deffered.Dispatch();
		}

	}
}

