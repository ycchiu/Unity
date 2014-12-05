using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class GameManagerConfig
	{
		public GameListener Listener;
		public bool Enabled = true;
		public bool PingEnabled = true;
	}
	
	public class GameManager : SubSystem, Updatable
	{
		public MasterServer[] MasterServers {get;private set;}
		public MasterServer	  BestMasterServer
		{
			get
			{
				// sorted by ping
				if (MasterServers.Length >0)
				{
					return MasterServers[0];
				}
				return null;
			}
		}
		
		public Game Game { get; private set; }
		
		public struct CreateGameParams
		{
			public int 			MaxPlayers;
			public Hashtable 	Attributes;
			public Hashtable 	PlayerAttributes;
		}
		
		public struct LocalCreateGameParams
		{
			public Hashtable 	Attributes;
			public Hashtable 	PlayerAttributes;
			public ArrayList	AIPlayers;
			public Hashtable	CampaignAttributes;
			
			public void AddAIPlayer( Hashtable attributes ) 
			{
				if (AIPlayers == null)
				{
					AIPlayers = new ArrayList();
				}
				
				var player = new Hashtable();
				player["uid"] = Hub.Instance.LoginManager.LocalUserId;
				player["id"] = AIPlayers.Count+Network.NpcId;
				player["attributes"] = attributes;
			 	AIPlayers.Add(player);
			}			
		}
		
		public struct JoinGameParams
		{
			public long 		GameId;
			public Hashtable 	PlayerAttributes;
		}
		
		public struct FindGameParams
		{
			public Hashtable	SearchAttributes;
			public Hashtable 	PlayerAttributes;
		}
		
		public struct FindOrCreateGameParams
		{
			public int 			MaxPlayers;
			public Hashtable	SearchAttributes;
			public Hashtable 	ResetAttributes;
			public Hashtable 	PlayerAttributes;
		}
	
		
		class WalkServers
		{			
			private Action<string,object>	_onDone;
			private Action<WalkServers>  	_onNext;
			
			private MasterServer[] 	_servers;
			private int 			_index;
			
			public MasterServer Server { get { return _servers[_index]; } }
			
			public WalkServers( MasterServer[] servers, Action<string,object> onDone, Action<WalkServers> onNext )
			{
				_onDone = onDone;
				_onNext = onNext;
				_servers = servers;
				_index = 0;
				Next(string.Empty);
			}
			
			public void OnResult(string err, object result)
			{
				if (!string.IsNullOrEmpty(err))
				{
					_index++;
					Next(err);
				}
				else
				{
					_onDone(err,result);
				}
			}
			
			void Next(string err)
			{
				if ( _index < _servers.Length )
				{
					_onNext(this);
				}
				else
				{
					if (string.IsNullOrEmpty(err))
					{
						err = "ID_SPARX_ERROR_NOFREEGAMES";
					}
					_onDone(err,null);
				}
			}
		}
		
		public bool UpdateOffline { get { return false;} }
		
		GameManagerAPI _api;
		GameManagerConfig _config;
		int _pingsRemaining = 0;
		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize( Config config )
		{
			_config = config.GameManagerConfig;
			_api = new GameManagerAPI(Hub.ApiEndPoint);
			MasterServers = new MasterServer[0];
			
			NetworkFactory.Register("null", typeof(NetworkNull));
			NetworkFactory.Register("ws", typeof(NetworkWS));
			NetworkFactory.Register("wss", typeof(NetworkWS));
			
			BugReport.OnBugReport += this.OnCrash;
		}
		
		public override void Connect ()
		{
			this.State = SubSystemState.Connecting;
			
			var data = Dot.Array("master", Hub.LoginManager.LoginData, null);
			if ( data != null )
			{
				OnGetMasterServers(null, _api.ParseMasterServers(data));
			}
			else
			{
				RefreshMasterServerList();
			}
		}

		public void Update ()
		{
			if (Game != null)
			{
				Game.Update();
				
				if (_config.Listener != null)
				{
					_config.Listener.OnUpdate(Game);
				}
			}
		}

		public override void Disconnect (bool isLogout)
		{
			if (!isLogout)
			{
				if (Game != null && Game.Network.IsLocal)
				{
					return;
				}
			}
			
			DestroyGameInternal(string.Empty);
		}
		#endregion
		
		void OnCrash()
		{
			if (Game != null)
			{
				Game.Disconnect(true);
			}
		}
				
		public override void Dispose ()
		{
			_api.Dispose();
			
			if ( Game != null)
			{
				Game.Dispose();
			}
			
			base.Dispose ();
		}
		
		
		public MasterServer GetMasterServer( string name )
		{
			foreach( var server in MasterServers )
			{
				if ( server.Name == name )
				{
					return server;
				}
			}
			return null;
		}
		
		public void ListGames( MasterServer server, Hashtable attributes, EB.Action complete )
		{
			_api.ListGames( server, attributes, delegate(string err,MasterServerGame[] games){
				
				if (string.IsNullOrEmpty(err))
				{
					server.Games = games;
				}
				
				if (complete!=null)
				{
					complete();
				}
			});
		}
				
		public void FindGame( FindGameParams pars )
		{
			new WalkServers(MasterServers, OnJoinGameResult, delegate(WalkServers walk){
				pars.PlayerAttributes["ping"] = walk.Server.Ping;
				_api.FindGame(walk.Server, pars.SearchAttributes, pars.PlayerAttributes, walk.OnResult);
			});
		}		
		
		public void FindOrCreateGame( FindOrCreateGameParams pars )
		{
			new WalkServers(MasterServers, OnJoinGameResult, delegate(WalkServers walk){
				pars.PlayerAttributes["ping"] = walk.Server.Ping;
				_api.FindOrCreateGame(walk.Server, pars.SearchAttributes, pars.MaxPlayers, pars.ResetAttributes, pars.PlayerAttributes, walk.OnResult);
			});
		}
		
		public void DestroyGameSoon( string err )
		{
			Coroutines.NextFrame(delegate(){
				DestroyGameInternal(err);
			});
		}
		
		void DestroyGameInternal( string error )
		{
			if (Game != null)
			{
				_config.Listener.OnLeaveGame( Game, error );
				Game.Dispose();
				Game = null;
			}
		}
		
		public void LeaveGame()
		{
			// todo: something better than this
			if ( Game != null )
			{
				Game.Disconnect(false);
			}
			
		}
		
		public void RefreshMasterServerList()
		{
			_api.GetMasterServers(this.OnGetMasterServers);
		}
		
		public Game CreateLocalGame( LocalCreateGameParams pars )
		{
			OnJoinGameResult( string.Empty, new Hashtable(){ {"connId", 1}, {"gameId", 1 },{"url","null://locahost"} } );
			Game.SetAttributes(pars.Attributes);
			Game.SetAttributes(pars.CampaignAttributes);
			Game.HostPlayer.Update( new Hashtable(){ {"attributes", pars.PlayerAttributes} } ); 
			
			if (pars.AIPlayers != null)
			{
				Game.SendGameCommand(Network.HostId, "ai", pars.AIPlayers );
			}
			
			return Game;
		}
		
		public void CreateOrJoinPrivateGame( string key, CreateGameParams pars )
		{
			_api.FindPrivateGame(key, delegate( string error, Hashtable game )
			{
				if (string.IsNullOrEmpty(error)==false)
				{
					_config.Listener.OnJoinGameFailed(error);
					return;
				}
				
				var gameId = Dot.Long("gameId", game, 0);
				var master = Dot.String("master", game, string.Empty);
				
				if ( gameId > 0 )
				{
					var masterServer = GetMasterServer(master);
					if ( masterServer == null )
					{
						_config.Listener.OnJoinGameFailed( EB.Localizer.GetString("ID_SPARX_ERROR_MASTER_SERVER_UNKNOWN") ); 
						return;
					}
					
					_api.JoinGame(masterServer, gameId, pars.PlayerAttributes, OnJoinGameResult);  
					return;
				}
				else
				{
					// find a server to play on
					new WalkServers(MasterServers, OnJoinGameResult, delegate(WalkServers walk) {
						pars.PlayerAttributes["ping"] = walk.Server.Ping;
						_api.ResetServer( walk.Server, pars.MaxPlayers, pars.Attributes, pars.PlayerAttributes, key, walk.OnResult ); 
					});
				}
			});
		}
			
		
		public void CreateGame( MasterServer server, CreateGameParams pars )
		{
			new WalkServers(MasterServers, OnJoinGameResult, delegate(WalkServers obj) {
				_api.ResetServer( obj.Server, pars.MaxPlayers, pars.Attributes, pars.PlayerAttributes, null, obj.OnResult ); 
			});
		}
		
		public void JoinGame( MasterServer server, JoinGameParams pars )
		{
			_api.JoinGame( server, pars.GameId, pars.PlayerAttributes, OnJoinGameResult ); 
		}
		
		void OnJoinGameResult( string err, object result ) 
		{
			var gameId = Dot.Long("gameId", result, 0);
			if (!string.IsNullOrEmpty(err) || gameId == 0)
			{
				EB.Debug.LogError("Join game (" + gameId.ToString() + ") failed: " + err);
				if ( !Localizer.GetString(err, out err) )
				{
					err = Localizer.GetString("ID_SPARX_ERROR_JOIN_GAME");
				}
				
				_config.Listener.OnJoinGameFailed(err);
				return;
			}
			
			EB.Debug.Log("Connecting to game");
			var pars = new GameParams(result);
			pars.Listener = _config.Listener;
			Game = new Game(pars);
			Game.Connect();
			
			// disconnect masters
			_api.DisconnectEndPoints();
		}
		
		void OnGetMasterServers(string error, MasterServer[] list)	
		{
			if ( !string.IsNullOrEmpty(error) || list.Length == 0)
			{
				EB.Debug.LogError("Failed to get master server list!");
				FatalError(Localizer.GetString("ID_SPARX_ERROR_UNKNOWN"));
				return;
			}
			
			State = SubSystemState.Connected;
			
			if (_config.PingEnabled)
			{
				// ping the master servers
				MasterServers = list;
				_pingsRemaining = MasterServers.Length;
				foreach( var server in MasterServers )
				{
					_api.PingMasterServer( server, this.OnPingResult );
				}
			}
			else
			{
				State = SubSystemState.Connected;
			}
			

		}
		
		void OnPingResult( string error, MasterServer server ) 
		{
			EB.Debug.Log("Ping Result: " + server);
			_pingsRemaining--;
			
			if (_pingsRemaining == 0) 
			{
				EB.Debug.Log("Ping complete");
				System.Array.Sort(MasterServers,delegate(MasterServer m1, MasterServer m2){
					return m1.CompareTo(m2);
				});
				
				State = SubSystemState.Connected;
			}
		}
		
		void OnListGames( string error, MasterServerGame[] result )
		{
			EB.Debug.Log("OnListGames {0}, {1}", error, result);
		}
		
		public override void OnEnteredBackground ()
		{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
			if (Game != null && !Game.Network.IsLocal )
			{
				EB.Debug.LogError("Entered Background, disconnecting game");
				Game.Disconnect(true);
			}
#endif
		}
				
	}
}
