using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public enum LoginState
	{
		Disconnected,
		Initializing,
		Initialized,
		Authenticating,
		LoggedIn,
	}
	
	// login configuration
	public class LoginConfig
	{
		public LoginListener Listener = new DefaultLoginListener();
		public System.Type[] Authenticators = new System.Type[]{ typeof(DeviceAuthenticator), typeof(GameCenterAuthenticator), typeof(FacebookAuthenticator) };
	}
	
	public class LoginManager : SubSystem
	{
		protected LoginConfig 			_config; 
		protected LoginAPI				_api;
		protected User 					_user;
		protected Hashtable				_loginData;
		protected bool					_initialized;
		protected string 				_lastAuthenticator = string.Empty;
		protected List<Authenticator> 	_authenticators = new List<Authenticator>();
		
		public User LocalUser 					{get { return _user; }  }
		public Id LocalUserId 					{get { if (LocalUser!=null) return LocalUser.Id; return Id.Null; }}
		public LoginState LoginState 			{get; private set;}
		public Hashtable LoginData				{get {return _loginData; }}
		public Authenticator[] Authenticators	{get { return _authenticators.ToArray(); } }
		public string AuthenticatorName			{get { return  _lastAuthenticator; } }

		public override void Initialize (Config config)
		{
			_config = config.LoginConfig;
			LoginState = LoginState.Disconnected;
			
			if ( _config.Listener == null )
			{
				throw new System.ApplicationException("Missing Login Listener");
			}

			_api = new LoginAPI( Hub.ApiEndPoint ); 

			_loginData = new Hashtable();
		}	

		public Authenticator GetAuthenticator( string name )
		{
			foreach( var authenticator in _authenticators)
			{
				if (authenticator.Name == name) {
					return authenticator;
				}
			}
			return null;
		}

		public void Enumerate()
		{
			Init (delegate {
				Coroutines.Run(_Authorize(true, delegate(string err, object authData) {
					if (!string.IsNullOrEmpty(err)){
						_LoginFailed(err);
						return;
					}

					_api.Enumerate( authData, delegate(string enumErr, ArrayList list) {
						if (!string.IsNullOrEmpty(err)){
							_LoginFailed(enumErr);
							return;
						}

						var accounts = new List<Account>();
						if (list != null )
						{
							foreach ( var obj in list )
							{
								var account = new Account(obj);
								accounts.Add(account);
							}
						}

						_config.Listener.OnEnumerate(accounts.ToArray());

					});

				}));
			});
		}

		public void SetName( string name, Action<string> callback )
		{
			if (_user == null)
			{
				Debug.LogError("Must be logged in before setting name!");
			 	callback("ID_SPARX_ERROR_UNKNOWN");
				return;
			}

			_api.SetName( name, delegate(string err, object result){
				if (!string.IsNullOrEmpty(err)) 
				{
					callback(err);
					return;
				}

				var user = Dot.Object("user", result, null);
				if (user == null)
				{
					Debug.LogError("Missing user object on set name!!!");
					callback("ID_SPARX_ERROR_UNKNOWN");
					return;
				}

				_user.Update(user);

				callback(null);
			});

		}

		public void Login( Authenticator authenticator )
		{
			if (authenticator == null)
			{
				this._LoginFailed("ID_SPARX_ERROR_UNKNOWN");
				return;
			}

			this.Login(authenticator.Name);
		}

		public void Login( string authenticatorName )
		{
			Init (delegate {
				var authenticator = GetAuthenticator(authenticatorName);
				if (authenticator == null) {
					_LoginFailed("ID_SPARX_ERROR_UNKNOWN");
					return;
				}

				SetState(LoginState.Authenticating);

				authenticator.Authenticate(false, delegate(string err, object data) {
					if (!string.IsNullOrEmpty(err)) {
						_LoginFailed(err);
						return;
					}
					else if (data == null) {
						_LoginFailed(null);
						return;
					}

					_Login( authenticator.Name, data ); 

				});

			});
		}

		public void Link( string authenticatorName )
		{
			if (LoginState == EB.Sparx.LoginState.LoggedIn)
			{
				var authenticator = GetAuthenticator(authenticatorName);
				if (authenticator == null) {
					_LinkFailed("ID_SPARX_ERROR_UNKNOWN");
					return;
				}

				authenticator.Authenticate(false, delegate(string err, object data) {
					if (!string.IsNullOrEmpty(err)) {
						_LinkFailed(err);
						return;
					}
					else if (data == null) {
						_LinkFailed(null);
						return;
					}
					
					_Link( authenticator.Name, data ); 
				});
			}
			else 
			{
				_LinkFailed("ID_SPARX_ERROR_LINK_NOT_SIGNED_IN");
			}
		}

		public void Relogin()
		{
			if (!string.IsNullOrEmpty(_lastAuthenticator))
			{
				Login(_lastAuthenticator);
			}
		}
		
		public void GetSupportUrl( Action<string,string> cb )
		{
			_api.GetSupportUrl( delegate(string err, object result) {
				cb( err, EB.Dot.String( "url", result, string.Empty ) );
			});
		}

		private void Init( Action callback ) 
		{
			if (_initialized) {
				SetState(LoginState.Initialized);
				callback();
				return;
			}

			SetState(LoginState.Initializing);

			_api.Init(delegate(string err, object data) {
				if (!string.IsNullOrEmpty(err)) {
					_LoginFailed(err);
					return;
				}
				Coroutines.Run(_PostInit(data, callback));
			});

		}

		void _Login( string authenticatorName, object authData ) 
		{
			_PreLogin(delegate(Hashtable secData) {

				_api.Login( authenticatorName, authData, secData, delegate(string err, object obj) {
					if (!string.IsNullOrEmpty(err)){
						_LoginFailed(err);
						return;
					}

					var user = Dot.Object("user", obj, null);
					_user = Hub.UserManager.GetUser(user);

					// add to bug reporting
					BugReport.AddData("uid", _user.Id.ToString() );

					// set the last authenticator
					_lastAuthenticator = authenticatorName;


					// track install
					var install = Dot.Bool("install", obj, false);
					if (install)
					{
						Nanigans.Install(_user.Id.ToString());
					}
					else
					{
						Nanigans.Visit(_user.Id.ToString());
					}

					// post login flow
					_PostLogin();
				}); 

			});
		}

		void _Link( string authenticatorName, object authData ) 
		{
			_api.Link( authenticatorName, authData, delegate(string err, object obj) {
				if (!string.IsNullOrEmpty(err)){
					_LinkFailed(err);
					return;
				}

				// update user
				var user = Dot.Object("user", obj, null);
				_user = Hub.UserManager.GetUser(user);
				
				// set the last authenticator
				_lastAuthenticator = authenticatorName;
				
				// let client know
				_config.Listener.OnLinkSuccess(authenticatorName);
	
			}); 
		}

		void _PreLogin( Action<Hashtable> callback ) 
		{
			_api.PreLogin( delegate(string err, object data) {
				if (!string.IsNullOrEmpty(err)) 
				{
					var url = Dot.String("url", data, null);
					if (!string.IsNullOrEmpty(url))
					{
						_UpdateRequired(url);
						return;
					}
					else 
					{
						_LoginFailed(url);
						return;
					}
				}

				var salt = Dot.String("salt", data, string.Empty);
				Coroutines.Run( Protect.CalculateHmac(salt, delegate(string challenge){

					var securityData = new Hashtable();
					securityData["salt"] = salt;
					securityData["chal"] = challenge;

					callback(securityData);

				}));

			});

		}

		void _PostLogin()
		{
			_api.LoginData( delegate(string err, Hashtable data) {
				if (!string.IsNullOrEmpty(err)) 
				{
					_LoginFailed(err);
					return;
				}

				_loginData = data ?? _loginData;

				// we are logged in
				SetState(LoginState.LoggedIn);
			});	 
		}

		IEnumerator _Authorize( bool silent, Action<string,object> callback )
		{
			var result = new Hashtable();
			foreach( var authenticator in _authenticators)
			{
				var done 		= false;
				var err 		= string.Empty;
				var	data		= default(object);

				authenticator.Authenticate(silent, delegate(string authErr, object authData) {
					err = authErr;
					data = authData;
					done = true;
				});

				while (!done) yield return 1;

				if (!string.IsNullOrEmpty(err)) {
					_LoginFailed(err);
					yield break;
				}
				else if (data != null) {
					result[authenticator.Name] = data;
				}
			}

			callback(null,result);
		}

		void _LoginFailed(string error)
		{
			Debug.LogError("LoginManager: Login Failed!: {0}",error);
			_user = null;
			_config.Listener.OnLoginFailed(error);
			SetState(LoginState.Disconnected);
		}

		void _LinkFailed(string error)
		{
			_config.Listener.OnLinkFailed(error);
		}

		void _UpdateRequired(string url)
		{
			_user = null;
			_config.Listener.OnUpdateRequired(url);
			SetState(LoginState.Disconnected);
		}

		IEnumerator _PostInit( object data, Action callback ) 
		{
			Debug.Log("Post init");

			// initialize new relic
			NewRelicPlugin.Init( Dot.String("newrelic", data, string.Empty) ); 

			// initialize adx
			var adx = Dot.Object("adx", data, null);
			if ( adx != null)
			{
				AdXTracking.reportAppOpen( Dot.String("client_id", adx, string.Empty), Dot.String("store_id", adx, string.Empty) ); 
			}

			var mat = Dot.Object("mat", data, null );
			if( mat != null )
			{
				Debug.Log("Initializing MAT");
#if ENABLE_PROFILER && MAT_DEBUG
				MATBinding.SetDebugMode(true);
#endif
				MATBinding.Init(Dot.String( "adId", mat, string.Empty ), Dot.String( "adKey", mat, string.Empty ) );

				Debug.Log("MeasureSession");
				MATBinding.MeasureSession();
			}


			// create all the authenticators
			var list = new List<Authenticator>();
			var empty = new Hashtable();
			foreach( System.Type type in _config.Authenticators )
			{
				var authenticator = (Authenticator)System.Activator.CreateInstance(type);
				var done = false;
				var err = string.Empty;
				var valid = false;

				authenticator.Init( Dot.Object(authenticator.Name, data, empty), delegate(string errAuth, bool validAuth){
					err = errAuth;
					valid = validAuth;
					done = true;
				});

				while(!done) yield return 1;

				if (!string.IsNullOrEmpty(err)){
					_LoginFailed(err);
					yield break;
				}

				if (valid) {
					Debug.Log("LoginManager: adding authenticator: " + authenticator.Name);
					list.Add(authenticator);
				}
			}

			if (list.Count == 0 ) 
			{
				Debug.LogError("LoginManager: no valid authenticators found!");
				_LoginFailed("ID_SPARX_ERROR_UNKNOWN");
				yield break;
			}
		
			_authenticators = list;
			_initialized = true;
			SetState(LoginState.Initialized);

			// wait a frame to make sure objects for callbacks are created.
			yield return 1;

			callback();
		}

		protected void SetState( LoginState state ) 
		{
			var prev = LoginState;
			LoginState = state;
			if ( prev != state || state == LoginState.LoggedIn )
			{
				Hub.SendMessage("OnLoginStateChanged", state );
			}
		}
	
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
			case "logout":
				{
					Debug.LogError("Got forced logout from the server {0}", payload);
					string error = "ID_SPARX_ERROR_UNKNOWN";
					if (payload != null)
					{
						var str = payload.ToString();
						if ( Localizer.HasString(str) )
						{
							error = str;
						}
					}
					FatalError( Localizer.GetString(error) );
				}
				break;
			}
			
		}

		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Connect ()
		{
			State = SubSystemState.Connected;
		}

		public override void Disconnect (bool isLogout)
		{
			_user = null;
			LoginState = LoginState.Disconnected;
		}
		#endregion
	}
}

