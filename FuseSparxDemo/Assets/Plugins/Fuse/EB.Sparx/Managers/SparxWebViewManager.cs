using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class WebViewConfig
	{
		public bool UseSecureEndpoint = false;
	}

	public class WebViewManager : SubSystem, Updatable
	{
		WebViewConfig _config = null;
		WebViewAPI _api = null;
		Hashtable _tabConfig = null;
		
		
	
	#if UNITY_IPHONE && !UNITY_EDITOR
		[System.Runtime.InteropServices.DllImportAttribute("__Internal")]
		static extern void _WebViewOpenRect(string baseUrl, float x, float y, float w, float h);
		
		[System.Runtime.InteropServices.DllImportAttribute("__Internal")]
		static extern void _WebViewClose();
		
		[System.Runtime.InteropServices.DllImportAttribute("__Internal")]
		static extern void _ConnectShow(string activeUrl, string pageInfo);
	#endif

		public void GetWebviewTabConfig(EB.Action<Hashtable> cb)
		{
			cb( this._tabConfig );
		}
	
		public void OpenTabbedWebView( string activeTag )
		{
			//page = PageType.Messages;
			GetWebviewTabConfig( delegate( Hashtable tabConfig ) {
				if( tabConfig != null )
				{
					var tabs = EB.Dot.Array( "tabs", tabConfig, null );
					if( ( tabs != null ) && ( tabs.Count > 0 ) )
					{
						string activeUrl = string.Empty;
						var endPointUrl = SparxHub.Instance.ApiEndPoint.Url;
						if( this._config.UseSecureEndpoint == false )
						{
							endPointUrl = endPointUrl.Replace( "https://", "http://" );
						}
						var stoken = SparxHub.Instance.ApiEndPoint.GetData(string.Empty, "stoken") ?? string.Empty;
						foreach( Hashtable tab in tabs )
						{
							string baseUrl = EB.Dot.String( "baseurl", tab, null );
							string tag = EB.Dot.String( "tag", tab, string.Empty );
							if( string.IsNullOrEmpty( baseUrl ) == false )
							{
								string url = string.Format( "{0}/{1}?stoken={2}", endPointUrl, baseUrl, stoken );
								tab[ "url" ] = url;
								if( ( tag == activeTag ) || ( string.IsNullOrEmpty( activeUrl ) == true ) )
								{
									activeUrl = url;
								}
							}
						}
						
						
					#if UNITY_IPHONE && !UNITY_EDITOR
						_ConnectShow( activeUrl, EB.JSON.Stringify( tabConfig ) );
					#elif UNITY_ANDROID && !UNITY_EDITOR
						var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
						if( unityPlayerClass != null )
						{
							var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
							if( currentActivity != null )
							{
								bool success = currentActivity.Call<bool>( "WebViewShowFullscreen", activeUrl, EB.JSON.Stringify( tabConfig ) );
								if( success == true )
								{
									Debug.Log( "!*!*!*OpenWebView: Success" );
								}
								else
								{
									Debug.LogError( "!*!*!*OpenWebView Failed - OpenWebView returned false. Check FacebookIAP stream for more info" );
								}
							}
							else
							{
								Debug.LogError( "!*!*!*OpenWebView Init: Failed - Could not get the currentActivity");
							}
						}
						else
						{
							Debug.LogError( "!*!*!*OpenWebView Init: Failed - com.unity3d.player.UnityPlayer lookup failed") ;
						}
						
					#elif UNITY_EDITOR
						Application.OpenURL(activeUrl);
					#endif
					}
				}
			});
		}
	
		public void OpenWebPopup( string url, float x=0.0f, float y=0.0f, float width=1.0f, float height=1.0f )
		{
		#if UNITY_IPHONE && !UNITY_EDITOR
			_WebViewOpenRect(url, x, y, width, height);
		#elif UNITY_ANDROID && !UNITY_EDITOR
			var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			if( unityPlayerClass != null )
			{
				var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
				if( currentActivity != null )
				{
					bool success = currentActivity.Call<bool>( "WebViewPopup", url, 1024, x, y, width, height );
					if( success == true )
					{
						Debug.Log( "!*!*!*WebViewPopup: Success" );
					}
					else
					{
						Debug.LogError( "!*!*!*WebViewPopup Failed - WebViewPopup returned false. Check FacebookIAP stream for more info" );
					}
				}
				else
				{
					Debug.LogError( "!*!*!*WebViewPopup Init: Failed - Could not get the currentActivity");
				}
			}
			else
			{
				Debug.LogError( "!*!*!*WebViewPopup Init: Failed - com.unity3d.player.UnityPlayer lookup failed") ;
			}
		#elif UNITY_EDITOR
			Application.OpenURL(url);
		#endif
		}
		
		public void CloseWebPopup()
		{
		#if UNITY_IPHONE && !UNITY_EDITOR
			_WebViewClose();
		#elif UNITY_ANDROID && !UNITY_EDITOR
			var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			if( unityPlayerClass != null )
			{
				var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
				if( currentActivity != null )
				{
					bool success = currentActivity.Call<bool>( "WebViewPopupClose" );
					if( success == true )
					{
						EB.Debug.Log( "!*!*!*WebViewClose: Success" );
					}
					else
					{
						EB.Debug.LogError( "!*!*!*WebViewClose Failed - WebViewClose returned false. Check FacebookIAP stream for more info" );
					}
				}
				else
				{
					EB.Debug.LogError( "!*!*!*WebViewClose Init: Failed - Could not get the currentActivity");
				}
			}
			else
			{
				EB.Debug.LogError( "!*!*!*WebViewClose Init: Failed - com.unity3d.player.UnityPlayer lookup failed") ;
			}
		#endif
		}
		
		public void SyncTabConfiguration( Action<string, Hashtable> cb )
		{
			this._api.FetchTabConfiguration( delegate( string error, Hashtable data ){
				this.OnFetchTabConfiguration( error, data );
				cb( error, data );
			});
		}
		
		private void PreFetch( Hashtable container, string key )
		{
			string imagePath = EB.Dot.String( key, container, string.Empty );
			if( string.IsNullOrEmpty( imagePath ) == false )
			{
				EB.Cache.Precache( imagePath, delegate( string filePath ) {
					container[ key ] = filePath;
				});
			}
		}
		
		private void OnFetchTabConfiguration( string error, Hashtable data )
		{
			this._tabConfig = data;
			if( this._tabConfig != null )
			{
				//Convert all of the file paths appropriately
				Hashtable title = EB.Dot.Object( "title", this._tabConfig, null );
				if( title != null )
				{
					string[] titleImages = { "close_image", "badge_image", "background_image" };
					foreach( string titleImage in titleImages )
					{
						PreFetch( title, titleImage );
					}
				}
			
				ArrayList tabs = EB.Dot.Array( "tabs", this._tabConfig , new ArrayList() );
				foreach( Hashtable tab in tabs )
				{
					string[] tabImages = { "selected_image", "unselected_image" };
					foreach( string tabImage in tabImages )
					{
						PreFetch( tab, tabImage );
					}
				}
			}
		}

		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_config = config.WebViewConfig;
			_api = new WebViewAPI(Hub.ApiEndPoint);
			
			new GameObject("WebViewCallbacks", typeof(EB.Sparx.WebViewCallbacks));
			
		}

		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var webviewLoginData = Dot.Object( "webview_configure", Hub.LoginManager.LoginData, null );
			if( webviewLoginData != null )
			{
				this.OnFetchTabConfiguration( null, webviewLoginData );
				State = SubSystemState.Connected;
			}
			else
			{
				this.SyncTabConfiguration( delegate( string error, Hashtable data ) {
					State = SubSystemState.Connected;
				});
			}
		}

		public void Update ()
		{
		}

		public override void Disconnect (bool isLogout)
		{
		}
		#endregion
		
	
	}
}
