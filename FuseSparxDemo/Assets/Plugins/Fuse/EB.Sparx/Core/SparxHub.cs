using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class Config
	{
		public string 				ApiEndpoint			= string.Empty;
		public Key 					ApiKey				= null;
		public int 					ApiKeepAlive		= 45;
		public string 				CertStore			= "Certs";
		public Language				Locale 				= Language.English;
		public bool					LoadLocalizer		= true;
		public bool					BackgroundOnPause	= true;
		public bool					UsePush				= true;
		public SaveLoadManagerConfig SaveLoadConfig		= new SaveLoadManagerConfig();
		public PushManagerConfig	PushManagerConfig	= new PushManagerConfig();
		public LoginConfig			LoginConfig 		= new LoginConfig();
		public GameManagerConfig 	GameManagerConfig 	= new GameManagerConfig();
		public InventoryConfig		InventoryConfig 	= new InventoryConfig();
		public PaymentsConfig 		PaymentsConfig		= new PaymentsConfig();
		public GameCenterConfig		GameCenterConfig	= new GameCenterConfig();
		public GameStoreConfig		GameStoreConfig		= new GameStoreConfig();
		public AppiraterConfig		AppiraterConfig		= new AppiraterConfig();
		public GachaConfig			GachaConfig			= new GachaConfig();
		public PerformanceConfig	PerformanceConfig	= new PerformanceConfig();
		public MOTDConfig			MOTDConfig			= new MOTDConfig();
		public EventsConfig			EventsConfig		= new EventsConfig();
		public PrizesConfig			PrizesConfig		= new PrizesConfig();
		public WebViewConfig		WebViewConfig		= new WebViewConfig();
		public ChatConfig			ChatConfig			= new ChatConfig();
		public RedeemerConfig		RedeemerConfig		= new RedeemerConfig();
		public MessageConfig		MessageConfig		= new MessageConfig();

		// deprecated
		public WalletConfig			WalletConfig 		= new WalletConfig();


		public System.Type			HubType				= typeof(SparxHub);
		public List<System.Type>	GameComponents 		= new List<System.Type>();
	}
	
	public enum HubState
	{
		Idle  = 0,
		LogginIn,
		Connecting,
		Connected,
	}
	
	public class Hub : MonoBehaviour
	{
		public static Hub Instance { get;private set; }
		
		public Config Config { get;private set; }
		
		public HubState State {get;private set;}
				
		public EndPoint					ApiEndPoint	 			{get;private set;}
		public LoginManager 			LoginManager 			{get;private set;}
		public TosManager	 			TosManager 				{get;private set;}
		public UserManager 				UserManager				{get;private set;}
		public GameManager 				GameManager				{get;private set;}
		public DataManager 				DataManager				{get;private set;}
		public SaveLoadManager 			SaveLoadManager 		{get;private set;}
		public PushManager				PushManager				{get;private set;}
		public InventoryManager			InventoryManager		{get;private set;}
		public PaymentsManager			PaymentsManager			{get;private set;}
		public GameCenterManager		GameCenterManager 		{get;private set;}
		public GameStoreManager			GameStoreManager 		{get;private set;}
		public TelemetryManager			TelemetryManager 		{get;private set;}
		public LeaderboardManager		LeaderboardManager 		{get;private set;}
		public TuningManager			TuningManager			{get;private set;}
		public FacebookManager			FacebookManager			{get;private set;}
		public AppiraterManager			AppiraterManager		{get;private set;}
		public GachaManager				GachaManager			{get;private set;}
		public PerformanceManager		PerformanceManager		{get;private set;}
		public ObjectivesManager		ObjectivesManager		{get;private set;}
		public MOTDManager				MOTDManager				{get;private set;}
		public WebViewManager			WebViewManager			{get;private set;}
		public SodaManager				SodaManager				{get;private set;}
		public LoginRewardsManager		LoginRewardsManager		{get;private set;}
		public RedeemerManager			RedeemerManager			{get;private set;}
		public LevelRewardsManager		LevelRewardsManager		{get;private set;}
		public ResourcesManager			ResourcesManager		{get;private set;}
		public EventsManager			EventsManager			{get;private set;}
		public PrizesManager			PrizesManager			{get;private set;}
		public ChatManager				ChatManager				{get;private set;}
		public MatchManager				MatchManager			{get;private set;}
		public TutorialManager			TutorialManager			{get;private set;}
		public MessageManager			MessageManager 			{get;private set;}
		public StatsManager				StatsManager			{get;private set;}
		
		// deprecated
		public WalletManager			WalletManager			{get;private set;}
		
		public event EB.Action OnUpdate;
		
		private List<Manager> _managers = new List<Manager>();
		private List<SubSystem> _subsystems = new List<SubSystem>();
		private List<Updatable> _update = new List<Updatable>();
		
		public Manager[] Managers { get{ return _managers.ToArray(); } }
		
		private bool _wasLoggedIn;
		private int  _enteredBackground;
		
		public static Hub Create( Config config )
		{
			if ( Instance != null )
			{
				throw new System.ApplicationException("Sparx is already initialized");
			}
			
			// load certifcates
			EB.Net.TcpClientFactory.LoadCertStore(config.CertStore);
			
			new GameObject("sparxhub", config.HubType);
			
#if UNITY_WEBPLAYER
			Hashtable values = QueryString.Parse(Application.srcValue);
			if ( values.ContainsKey("endpoint") )
			{
				config.ApiEndpoint =  values["endpoint"].ToString();
				EB.Debug.Log("EndPoint: " + config.ApiEndpoint );
			}
#endif
		
			Instance.Initialize(config);
			
			return Instance;
		}
		
		void Initialize( Config config )
		{
			
			Config = config;
			
			// initialize the bug reporter
			BugReport.Init( config.ApiEndpoint + "/bugs" ); 
			

			// load strings
			if ( config.LoadLocalizer )
			{
				Localizer.LoadStrings(Config.Locale, "sparx");
				
			}			
			
			// load profanity filter
			var profanity = Assets.Load<TextAsset>("profanity");
			if (profanity != null)
			{
				ProfanityFilter.Init(profanity.text);
			}

			
			// initialize api service endpoint
			var options = new EndPointOptions{ Key = config.ApiKey.Value };
			if (config.ApiKeepAlive > 0 )
			{
				options.KeepAlive = true;
				options.KeepAliveUrl = "/util/ping";
				options.KeepAliveInterval = config.ApiKeepAlive;
			}
			ApiEndPoint = EndPointFactory.Create(config.ApiEndpoint, options ); 
			
			
			EB.Memory.OnBreach += delegate()
			{
				this.FatalError("ID_SPARX_ERROR_UNKNOWN");
			};
			
			InitializeComponents();
		}

		protected virtual void InitializeComponents( )
		{
			TuningManager	= AddManager<TuningManager>();
			TelemetryManager= AddManager<TelemetryManager>();
			UserManager  	= AddManager<UserManager>();
			LoginManager 	= AddManager<LoginManager>();
			TosManager 		= AddManager<TosManager>();
			DataManager	 	= AddManager<DataManager>();
			SaveLoadManager = AddManager<SaveLoadManager>();
			PaymentsManager	= AddManager<PaymentsManager>();
			LeaderboardManager= AddManager<LeaderboardManager>();
			FacebookManager = AddManager<FacebookManager>();
			AppiraterManager= AddManager<AppiraterManager>();
			GachaManager = AddManager<GachaManager>();
			GameStoreManager = AddManager<GameStoreManager>();
			PerformanceManager = AddManager<PerformanceManager>();
			ObjectivesManager= AddManager<ObjectivesManager>();
			MOTDManager = AddManager<MOTDManager>();
			WebViewManager = AddManager<WebViewManager>();
			SodaManager = AddManager<SodaManager>();
			LoginRewardsManager = AddManager<LoginRewardsManager>();
			RedeemerManager = AddManager<RedeemerManager>();
			LevelRewardsManager = AddManager<LevelRewardsManager>();
			ResourcesManager = AddManager<ResourcesManager>();
			EventsManager = AddManager<EventsManager>();
			PrizesManager = AddManager<PrizesManager>();
			ChatManager = AddManager<ChatManager>();
			MatchManager = AddManager<MatchManager>();
			TutorialManager = AddManager<TutorialManager>();
			MessageManager = AddManager<MessageManager>();
			StatsManager = AddManager<StatsManager>();
			
			if ( Config.GameCenterConfig.Enabled)
			{
				GameCenterManager = AddManager<GameCenterManager>();
			}
			
			if ( Config.GameManagerConfig.Enabled)
			{
				GameManager 	= AddManager<GameManager>();
			}
			
			if ( Config.UsePush )
			{
				PushManager		= AddManager<PushManager>();	
			}

			// deprecaeted
			if (Config.WalletConfig.Enabled)
			{
				WalletManager = AddManager<WalletManager>();
			}
			
			
			if (Config.InventoryConfig.Enabled)
			{
				InventoryManager= AddManager<InventoryManager>();
			}
			
			foreach( var type in Config.GameComponents )
			{
				AddManager<Manager>(type);
			}
		}
		
		protected T AddManager<T>() where T : Manager, new()
		{
			return AddManager<T>( typeof(T) );
		}
		
		protected T AddManager<T>( System.Type type ) where T : Manager
		{
			var manager = (T)System.Activator.CreateInstance(type);
			manager.Initialize(Config);
			AddManager(manager);
			return manager;
		}
		
		public Manager GetManager( string name )
		{
			var ebname = "EB.Sparx."+name;
			foreach( var manager in _managers )
			{
				var n = manager.GetType().Name;
				
				if ( n == name || n == ebname)
				{
					return manager;
				}
			}
			return null;
		}
		
		public Manager GetManager( System.Type type )
		{
			foreach( var manager in _managers )
			{
				if ( manager.GetType() == type || manager.GetType().IsSubclassOf(type) )				
				{
					return manager;
				}
			}
			return null;
		}
		
		public T GetManager<T>() where T : Manager
		{
			return (T)GetManager( typeof(T) );
		}
		
		void AddManager( Manager manager )
		{
			_managers.Add(manager);
			if ( manager is SubSystem )
			{
				_subsystems.Add( (SubSystem)manager); 
			}
			
			if ( manager is Updatable )
			{
				_update.Add( (Updatable)manager );
			}
			
		}
		
		void OnDestroy()
		{
			// destroy everything
			ApiEndPoint.Dispose();
			foreach( var manager in _managers )
			{
				manager.Dispose();
			}
		}
		
		void OnLoginStateChanged( LoginState state )
		{
			switch(state)
			{
			case LoginState.LoggedIn:
				{
					// start the subsystem connect
					SubSystemConnect();
				}
				break;
			}
		}
		
		public void FatalError( string error )
		{
			EB.Debug.LogError("Sparx Fatal Error: " + error);
			
			var wasConnected = State == HubState.Connected;
			
			State = HubState.Idle;
			
			if (Config.LoginConfig.Listener != null)
			{
				if (wasConnected)
				{
					Config.LoginConfig.Listener.OnDisconnected(error);
				}
				else
				{
					Config.LoginConfig.Listener.OnLoginFailed(error);
				}
			}
			
			// call this after so we know why eveything was in a error
			SubSystemDisconnect(false);
		}
		
		public void Disconnect(bool isLogout)
		{
			State = HubState.Idle;
			
			if (Config.LoginConfig.Listener != null)
			{
				Config.LoginConfig.Listener.OnDisconnected(string.Empty);
			}
			
			// call this after so we know why eveything was in a error
			SubSystemDisconnect(isLogout);		
		}
		
		void SubSystemConnect()
		{
			EB.Debug.Log("SparxHub: SubSystemConnect");
			
			State = HubState.Connecting;
			foreach( SubSystem system in _subsystems )
			{
				system.State = SubSystemState.Connecting;
				system.Connect();
			}
		}	
		
		void SubSystemConnecting()
		{
			var allConnected = true;
			foreach( SubSystem system in _subsystems )
			{			
				switch(system.State)
				{
				case SubSystemState.Error:
					{
						FatalError( Localizer.GetString("ID_SPARX_ERROR_UNKNOWN") ); 
						return;
					}
				case SubSystemState.Connecting:
				case SubSystemState.Disconnected:
					{
						allConnected = false;
					}
					break;
				case SubSystemState.Connected:
					{
					}
					break;
				}
			}
			
			if ( allConnected )
			{
				EB.Debug.Log("SparxHub: SubSystemConnected");
				State = HubState.Connected;
				
				// keep our connection alive
				ApiEndPoint.StartKeepAlive();
				
				// logged in!
				if (Config.LoginConfig.Listener != null)
				{
					Config.LoginConfig.Listener.OnLoggedIn();
				}
				
				foreach( var manager in _managers )
				{
					manager.OnLoggedIn();
				}
			}
		}
		
		void SubSystemUpdate()
		{
			bool online = State != HubState.Idle;
			foreach( Updatable system in _update )
			{
				if (system.UpdateOffline || online)
				{
					system.Update();
				}
			}
		}
		
		void SubSystemDisconnect(bool isLogout)
		{
			EB.Debug.Log("SparxHub: SubSystemDisconnect");
			foreach( SubSystem system in _subsystems )
			{
				system.Disconnect(isLogout);
				system.State = SubSystemState.Disconnected;
			}
			
			ApiEndPoint.StopKeepAlive();
		}
		
		void Update()
		{
			SubSystemUpdate();
			
			switch(State)
			{
			case HubState.Connecting:
				{
					SubSystemConnecting();
				}
				break;
			}
			
			if (OnUpdate!=null)
			{
				OnUpdate();
			}
		}
		
		void Awake()
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		
#if UNITY_IPHONE
		void OnEnteredBackground(bool willPause)
		{
			if (Config.BackgroundOnPause && willPause)
			{
			//	EB.Debug.LogError("SparxHub::OnEnteredBackground ");
				foreach( var manager in _managers)
				{
					manager.OnEnteredBackground();
				}
			}
			
			_wasLoggedIn = State == HubState.Connected;
			_enteredBackground = Time.Now;
			ApiEndPoint.StopKeepAlive();
		}
		
		static void OnEnteredBackgroundStatic(int willPause)
		{
			if (Instance != null)
			{
				Instance.OnEnteredBackground(willPause != 0);
			}
		}
		
		void OnEnteredForeground()
		{
			//Debug.LogError("SparxHub::OnEnteredForeground:");
			foreach( var manager in _managers)
			{
			//	EB.Debug.LogError(string.Format ("Manager {0}.OnEnteredForeground", manager));
				manager.OnEnteredForeground();
			}
			
			if (_enteredBackground == 0)
			{
				return;
			}
			
			var since = Time.Since(_enteredBackground);
			EB.Debug.Log("since: " + since);
			EB.Debug.Log("reachability: " + Application.internetReachability);
			EB.Debug.Log("_wasLoggedIn:" + _wasLoggedIn);
			
			if (_wasLoggedIn)
			{
				var ping = ApiEndPoint.Post("/util/ping");
				ApiEndPoint.Service(ping, delegate(Response r){
					if (r.sucessful) {
						EB.Debug.Log("ping is ok!");
						ApiEndPoint.StartKeepAlive();
					}
					else {
						// relogin
						EB.Debug.Log("Relogin");
						Disconnect(false);
						LoginManager.Relogin();
					}
				});
			}
			
			_enteredBackground = 0;
		}
		
		static void OnEnteredForegroundStatic()
		{
			//Debug.LogError("SparxHub received OnEnteredForegroundStatic()");
			if (Instance != null)
			{
				Instance.OnEnteredForeground();
			}
		}
		
#else
		void OnApplicationPause(bool pause)
		{
			//EB.Debug.LogError("SparxHub::OnApplicationPause:  "+ pause);
			if (Config.BackgroundOnPause && pause)
			{
				//EB.Debug.LogError("SparxHub::OnEnteredBackground ");
				foreach( var manager in _managers)
				{
					manager.OnEnteredBackground();
				}
				
				_wasLoggedIn = State == HubState.Connected;
				_enteredBackground = Time.Now;
				ApiEndPoint.StopKeepAlive();
			}
			else if (Config.BackgroundOnPause && !pause)
			{
				//Debug.LogError("SparxHub::OnEnteredForeground:");
				foreach( var manager in _managers)
				{
					//Debug.LogError(string.Format ("Manager {0}.OnEnteredForeground", manager));
					manager.OnEnteredForeground();
				}
				
				if (_enteredBackground == 0)
				{
					return;
				}
				
				var since = Time.Since(_enteredBackground);
				EB.Debug.Log("since: " + since);
				EB.Debug.Log("reachability: " + Application.internetReachability);
				EB.Debug.Log("_wasLoggedIn:" + _wasLoggedIn);

				if (_wasLoggedIn)
				{
					var ping = ApiEndPoint.Post("/util/ping");
					ApiEndPoint.Service(ping, delegate(Response r){
						if (r.sucessful) {
							EB.Debug.Log("ping is ok!");
							ApiEndPoint.StartKeepAlive();
						}
						else {
							// relogin
							EB.Debug.Log("Relogin");
							Disconnect(false);
							LoginManager.Relogin();	
						}
					});
				}
									 
				_enteredBackground = 0;
			}

		}
#endif
	}
	
}

public class SparxHub : EB.Sparx.Hub
{
	
}
