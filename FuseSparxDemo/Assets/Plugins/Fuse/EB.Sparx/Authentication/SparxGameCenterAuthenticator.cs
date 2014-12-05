#if UNITY_IPHONE && !UNITY_EDITOR
#define USE_GAMECENTER_AUTH 
#endif
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace EB.Sparx
{
	public class GameCenterAuthenticator : Authenticator
	{
		#region Authenticator implementation
		public static string AuthName = "gamecenter";

#if USE_GAMECENTER_AUTH
		[DllImport("__Internal")]
		static extern bool _GameCenterAuthenticationSupported();

		[DllImport("__Internal")]
		static extern bool _GameCenterAuthenticate();
#endif

		public static GameCenterAuthenticator Instance {get;private set;}
		Action<string,object> _authCallback;

		public void Init (object initData, Action<string, bool> callback)
		{
			bool supported = false;
#if USE_GAMECENTER_AUTH
			supported = _GameCenterAuthenticationSupported();
			if (supported)
			{
				Instance = this;
				new GameObject("gca_callbacks", typeof(SparxGameCenterAuthenticator));
			}
#endif
			callback(null,supported);
		}

		public void Authenticate (bool silent, Action<string, object> callback)
		{
#if USE_GAMECENTER_AUTH
			_authCallback = callback;
			_GameCenterAuthenticate();
#endif
		}

		public string Name {
			get {
				return AuthName;
			}
		}

		public bool IsLoggedIn {
			get {
#if USE_GAMECENTER_AUTH
				var local = Social.localUser;
				return local != null && local.authenticated;
#else
				return false;
#endif
			}
		}

		public AuthenticatorQuailty Quailty {
			get {
				return AuthenticatorQuailty.Med;
			}
		}

		public void OnAuthenticateError( string error )
		{
			Debug.Log("GameCenterAuthenticator: OnAuthenticateError: {0}", error);
			if (_authCallback != null)
			{
				_authCallback(null,null);
				_authCallback = null;
			}
		}

		public void OnAuthenticate( object data )
		{
			Debug.Log("GameCenterAuthenticator: OnAuthenticate: {0}", data);
			if (_authCallback != null)
			{
				_authCallback(null,data);
				_authCallback = null;
			}
		}

		#endregion

	}


}

public class SparxGameCenterAuthenticator : MonoBehaviour
{
	void Awake()
	{
		Debug.Log("Creating Game Center Authenticator");
		DontDestroyOnLoad(gameObject);
	}

	public void OnAuthenticateError( string error )
	{
		EB.Sparx.GameCenterAuthenticator.Instance.OnAuthenticateError(error);
	}
	
	public void OnAuthenticate( string json )
	{
		var data = EB.JSON.Parse(json);
		EB.Sparx.GameCenterAuthenticator.Instance.OnAuthenticate( data );
	}

}


