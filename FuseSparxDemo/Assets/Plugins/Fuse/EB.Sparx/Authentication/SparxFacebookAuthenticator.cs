using System.Collections;

namespace EB.Sparx
{
	public class FacebookAuthenticator : Authenticator
	{
		public string[] _scope = null;
		public FacebookManager _manager = null;

		#region Authenticator implementation
		public void Init (object initData, Action<string, bool> callback)
		{
			_scope = (string[])Dot.Array("scope", initData, new ArrayList() ).ToArray(typeof(string));

			_manager = Hub.Instance.FacebookManager;
			_manager.InitializeSDK( initData, callback ); 
		}

		Hashtable GetCredentials()
		{
			var cred = new Hashtable();
			cred["access_token"] = _manager.AccessToken;
			return cred;
		}

		public void Authenticate (bool silent, Action<string, object> callback)
		{
#if UNITY_EDITOR
			silent = false;
#endif

			if (silent) 
			{
				if (IsLoggedIn) 
				{
					callback(null, GetCredentials());
					return;
				}
				else 
				{
					callback(null,null);
					return;
				}
			}
			else {
				_manager.Login(_scope, delegate(string err) {
					if (!string.IsNullOrEmpty(err))
					{
						callback(err,null);

					}
					else if (IsLoggedIn) 
					{
						callback(null, GetCredentials());
						return;
					}
					else 
					{
						callback(null,null);
						return;
					}
				});

			}
		}

		public string Name {
			get {
				return "facebook";
			}
		}

		public bool IsLoggedIn {
			get {
				return _manager.IsLoggedIn;
			}
		}

		public AuthenticatorQuailty Quailty {
			get {
				return AuthenticatorQuailty.High;
			}
		}
		#endregion
	}

}

