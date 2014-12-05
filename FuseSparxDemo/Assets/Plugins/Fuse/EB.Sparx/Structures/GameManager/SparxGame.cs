using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class GameParams
	{
		public uint			ConnId;
		public long			GameId;
		public string 		Url;
		public GameListener Listener;
		
		public GameParams(object obj)
		{
			ConnId = (uint)Dot.Long("connId", obj, 0);
			GameId = Dot.Long("gameId", obj, 0);
			Url = Dot.String("url", obj, string.Empty);
		}
	};
		
	
	public class Game : System.IDisposable
	{ 
		private readonly GameParams _pars;
		private Network _network;
		
		private List<Player> _players = new List<Player>();
		private Player _local;
		
		private Hashtable _attributes = new Hashtable();
		private bool _sentJoin = false;
		private bool _sentStart = false;
		private bool _sentEnd = false;
		private bool _started = false;
		private string _disconnectMessage = "";
		private bool _wantDisconnect = false;
		
		private string _privateKey = null;
		
		public Player LocalPlayer {get{ return _local;}}
		public Player HostPlayer  {get{ return _players.Count > 0 ? _players[0] : null; } }
		public List<Player> Players  { get { return _players;} }
		public Hashtable Attributes {get { return _attributes;}}
		public bool Connected {get { return _sentJoin; }}
		public long Id { get{return _pars.GameId; }}
		public bool IsHost { get { return _local != null ? _local.IsHost : false; } }
		public bool Started { get {  return _started; } }
		
		public string PrivateKey  { get { return _privateKey; } }
		public bool IsPrivate { get {  return string.IsNullOrEmpty(_privateKey) == false; } }
		
		public NetworkStats Stats { get { return _network.Stats; } }
		public Network Network { get { return _network; }}
		
		
		
		public Game( GameParams pars )
		{
			_pars = pars;
			_network = NetworkFactory.Create(pars.Url);
			_network.OnConnect += OnNetworkConnect;
			_network.OnError += OnNetworkError;
			_network.OnReceive += OnNetworkReceive;
			_network.OnDisconnect += OnNetworkDisconnect;
			
			Screen.sleepTimeout = (int)SleepTimeout.NeverSleep;
		}
		
		public void Connect()
		{
			_network.Connect(_pars.Url,_pars.ConnId); 
		}
		
		public void Update()
		{
			_network.Update();
		}
		
		public void Disconnect(bool now)
		{
			_wantDisconnect = true;
			_network.Disconnect(now);
		}
		
		public void LeaveGame( string message )
		{
			_disconnectMessage = message;
			this.Disconnect(false);
		}
		
		public void Dispose()
		{
			_network.Dispose();
			
			Screen.sleepTimeout = (int)SleepTimeout.SystemSetting;
		}
		
		public virtual void Start()
		{
			if ( IsHost && !_started && !_sentStart )
			{
				_sentStart = true;
				SendGameCommand( 0, "start", null );
			}
		}
		
		public virtual void End()
		{
			if ( IsHost && _started && !_sentEnd )
			{
				_sentEnd = true;
				SendGameCommand( 0, "end", null );
			}
		}
		
		public Player GetPlayer( uint playerId )
		{
			foreach ( var player in _players )
			{
				if ( player.PlayerId == playerId )	
				{
					return player;
				}
			}
			return null;
		}
		
		public Player GetPlayer( Id userId )
		{
			foreach ( var player in _players )
			{
				if ( player.UserId == userId )	
				{
					return player;
				}
			}
			return null;
		}
		
		public void SetAttributes( Hashtable values ) 
		{
			if ( IsHost )
			{
				SendGameCommand( 0, "attributes", values );
				foreach( DictionaryEntry entry in  values )
				{
					_attributes[entry.Key] = entry.Value;		
				}
			}
		}
		
		public void UpdatePlayerAttributes( Hashtable values )
		{
			if ( LocalPlayer != null)
			{
				Dot.DeepCopy(values,LocalPlayer.Attributes);  
			}
			SendGameCommand(Network.HostId, "playerattributes", values );
		}
		
		public void SendTo( uint playerId, Channel channel, Buffer data )
		{
			var packet = new Packet(channel, data);
			_network.SendTo( playerId, packet); 
		}
		
		public void Broadcast( Channel channel, Buffer data )
		{
			var packet = new Packet(channel, data);
			_network.Broadcast( packet);
		}
		
		public void SendGameCommand( uint playerId, string command, object data ) 
		{
			var message = command;
			if ( data != null )
			{
				message += ":" + JSON.Stringify(data);
			}
			
			_network.SendTo( playerId, new Packet( Channel.Manager_Reliable, message) );  
		}
		
		public void KickPlayer( uint playerId )
		{
			if (IsHost)
			{
				SendGameCommand(Network.HostId, "kick", playerId);
			}
		}
		
		void OnReceiveGameCommand( uint playerId, string command, object data )
		{
			EB.Debug.Log("Got game command {0} {1}", command, data );
			switch(command)
			{
			case "private":	
				{
					_privateKey = data.ToString();
				}
				break;
			case "attributes":	
				{
					if ( data is Hashtable )	
					{
						EB.Debug.Log("Updating attributes");
						_attributes = new Hashtable((Hashtable)data); 
		
						if (_sentJoin)
						{
							_pars.Listener.OnAttributesUpdated(this);
						}
					}
				}
				break;
			case "roster":
				{
					var list 		= (ArrayList)data;
					var players 	= new List<Player>();
					var added 		= new List<Player>();
					var removed 	= new List<Player>();
				
					// see who has joined
					foreach (var p in list )
					{
						var tmp = new Player(p);
						var old = GetPlayer(tmp.PlayerId);
						if (old == null)
						{	
							added.Add(tmp);
						}
						else
						{
							old.Update(p);
							tmp = old;
						}
						players.Add(tmp);
					}
				
					// see who has been removed
					var oldList = _players;
					_players	= players;
				
					foreach (var player in oldList )
					{
						if (GetPlayer(player.PlayerId) == null)
						{
							removed.Add(player);
						}
					}
				
					for ( int i = 0; i < _players.Count; ++i )
					{
						_players[i].Index = i;
					}
				
					if ( _sentJoin )
					{
						foreach( var player in removed ) 
						{
							_pars.Listener.OnPlayerLeft( this, player );
						}
					
						foreach( var player in added )
						{
							_pars.Listener.OnPlayerJoined( this, player );
						}
					
					}
					else
					{	
						_sentJoin = true;
						_local = GetPlayer( _pars.ConnId ); 
						_pars.Listener.OnJoinedGame( this ); 
					}
				
				}
				break;
			case "start":
				{
					_sentEnd = false;
					_started = true;
					_pars.Listener.OnGameStarted(this);
				}
				break;
			case "end":
				{
					_sentStart = false;
					_started = false;
					_pars.Listener.OnGameEnded(this);
				}
				break;
			case "kick":
				{
					// we have been kicked
					_disconnectMessage = "ID_SPARX_ERROR_GAME_KICKED";
					this.Disconnect(false);
				}
				break;
			}
		}
		
		void OnNetworkConnect()
		{
			EB.Debug.Log("Network connected, sending hello");
			SendGameCommand(Network.HostId,"hello", null);
		}
		
		void OnNetworkReceive( uint playerId, Packet packet )
		{
			switch( packet.Channel )
			{
			case Channel.Manager_Reliable:
			case Channel.Manager_Unreliable:
				{
					string message = packet.Utf8Data;
					object payload = null;
					var index	   = message.IndexOf(':');
					if ( index > 0 )
					{
						payload = JSON.Parse( message.Substring(index+1) );
						message = message.Substring(0,index);
					}
					OnReceiveGameCommand( playerId, message, payload);
				}
				break;
			case Channel.Game_Reliable:
			case Channel.Game_UnReliable:
				{
					var player = GetPlayer(playerId);
					_pars.Listener.OnReceive( this, player, packet ); 
				}
				break;
			}
		}
		
		void OnNetworkDisconnect(string message)
		{
			EB.Debug.Log("OnNetworkDisconnect: " + message);
			if (string.IsNullOrEmpty(_disconnectMessage)==false)
			{
				message = Localizer.GetString(_disconnectMessage);
			}
			else if (!_wantDisconnect)
			{
				message = Localizer.GetString("ID_SPARX_ERROR_GAME_LOST_CONNECTION");
			}
			Hub.Instance.GameManager.DestroyGameSoon(message);
		}
		
		void OnNetworkError( string error )
		{
			EB.Debug.LogError("OnNetworkError: " + error);
			Hub.Instance.GameManager.DestroyGameSoon(error);
		}
		
	}
}
