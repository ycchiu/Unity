using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class FacebookManager : Manager
	{
		bool 			_sdkInitialized 	= false;
		object 			_me					= null;

		public bool IsLoggedIn 		{ get { return _sdkInitialized && FB.IsLoggedIn; } }
		public string AccessToken 	{ get { return _sdkInitialized ? FB.AccessToken : string.Empty; } }

		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
		}

		public void InitializeSDK( object options, Action<string, bool> callback ) 
		{
			if (_sdkInitialized) 
			{
				callback(null,true);
				return;
			}

			var appId 	= Dot.String("appId", options, string.Empty);

			if (string.IsNullOrEmpty(appId))
			{
				Debug.Log("Facebook disabled");
				callback(null,false);
				return;
			}

			var appIds = FBSettings.Instance.AppIds;
			var index = System.Array.IndexOf(appIds, appId);
			if (index < 0 ) {
				Debug.LogError("AppId {0} is missing from FBSettings. Please add to the settings object", appId);
				callback(null,false);
				return;
			}

			FBSettings.Instance.SetAppIndex(index);

			FBSettings.Logging		= Dot.Bool("logging", options, FBSettings.Logging);
			FBSettings.Xfbml  		= Dot.Bool("xfbml", options, FBSettings.Xfbml);
			FBSettings.Status		= Dot.Bool("status", options, FBSettings.Status);
			FBSettings.Cookie		= Dot.Bool("cookie", options, FBSettings.Cookie);
			FBSettings.IosURLSuffix	= Dot.String("suffix", options, FBSettings.IosURLSuffix);

			// Nanigans
			Nanigans.Init(appId);

			FB.Init(
				delegate() {
					_sdkInitialized = true;

					// make sure to publish our install
					try {
						FB.PublishInstall(delegate(FBResult response) {});
					}
					catch {

					}
					

					// try and do a silent login
					if (FB.IsLoggedIn) {
						DoMe( delegate (string err) {
							callback(null, true);		
						});	 
					}
					else {
						callback(null, true);
					}
				},
				this.OnHideUnity
			);
		}

		public void Login( string[] scope, Action<string> callback)
		{
			if (_sdkInitialized==false) {
				callback("ID_SPARX_ERROR_FACEBOOK_INIT");
				return;
			}

			DoCheckPermissionsAndLogin(scope, callback);	
		}

		void DoCheckPermissionsAndLogin( string[] scope, Action<string> callback )
		{
			if (IsLoggedIn) {
				DoMe( delegate (string err) {
					if (!string.IsNullOrEmpty(err)) {
						callback(err);
						return;
					}

					if (HasPermissions(scope)) {
						callback(null);
					}
					else {
						DoLogin(scope, callback);
					}
				});
			}
			else {
				DoLogin( scope, callback);
			}
		}

		bool HasPermissions( string[] scope )
		{
			var permissions = Dot.Object("permissions", _me, null);
			var ok = true;
			foreach( var s in scope )
			{
				ok &= Dot.Integer(s,permissions,0) == 1;
			}
			return ok;
		}

		void DoMe( Action<string> callback )
		{
			if (_me != null)
			{
				callback(null);
				return;
			}

			FB.API("/me?fields=first_name,last_name,name,email,birthday,name,permissions", Facebook.HttpMethod.GET, delegate(FBResult result) {
				if (!string.IsNullOrEmpty(result.Error)) {
					Debug.LogError("Failed to fetch me from facebook! " + result.Error);
					callback("ID_SPARX_ERROR_FACEBOOK");
					return;
				}
				_me = JSON.Parse(result.Text);
				Debug.Log("Me: {0}", _me);
				callback(null);
			});

		}

		void DoLogin( string[] scope, Action<string> callback)
		{
			FB.Login( ArrayUtils.Join(scope,','), delegate(FBResult result) {
				Debug.Log("Facebook login result: " + result.Text);

				if (!string.IsNullOrEmpty(result.Error))
				{
					Debug.LogError("Failed to login with facebook! " + result.Error);
					callback("ID_SPARX_ERROR_FACEBOOK");
					return;
				}
				else if ( FB.IsLoggedIn )
				{
					// clear me object
					_me = null;
					DoMe(callback);
				}
				else {
					Debug.Log("FBLogin canceled!");
					callback(null);
				}
			});

		}

		void OnHideUnity(bool isUnityShown)
		{

		}

		#endregion
	}	
}

