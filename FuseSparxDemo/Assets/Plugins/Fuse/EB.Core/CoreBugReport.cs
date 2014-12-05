using UnityEngine;
using System.Collections;

namespace EB
{
	public static class BugReport
	{
		private static string 		_url;
		private static Hashtable	_data = new Hashtable();
		private static bool 		_sent;
		
		public static bool DidCrash {get { return _sent; }}
		
		public static event Action OnBugReport;
		
		public static void Init( string url)
		{
#if !UNITY_WEBPLAYER
			_url 		= url.Replace("https://", "http://");
#else
			_url 		= url;
#endif
			Application.RegisterLogCallback(LogCallback);	
			
			AddData("os", SystemInfo.operatingSystem );	
			AddData("sl", SystemInfo.graphicsShaderLevel );
			AddData("graphics", SystemInfo.graphicsDeviceName );
			AddData("unity", Application.unityVersion );
			AddData("version", Version.GetVersion() );
			AddData("changelist", Version.GetChangeList() );
			AddData("locale", Version.GetLocale() );
			
#if UNITY_IPHONE
			AddData("device", iPhone.generation.ToString() );
#elif UNITY_ANDROID
			AddData("device", SystemInfo.deviceModel );
			AddData("processor", SystemInfo.processorType );
#elif UNITY_WEBPLAYER		// moko: collect more info for webplayer
			AddData("device", SystemInfo.deviceModel);
			AddData("deviceType", SystemInfo.deviceType);
			AddData("processor", SystemInfo.processorType );
			AddData("gfx", SystemInfo.graphicsDeviceVersion );
#endif
		}
		
		public static void AddData( string key, object value )
		{
			_data[key] = value;
		}
		
		static IEnumerator SendBugReport()
		{
			yield return 1;

			UnityEngine.Debug.Log("Sending Bug Report: " + _url);			
			// add log
			Debug.Dump(_data);
			
			// screen shot
			yield return new WaitForEndOfFrame();
			TakeScreenShot();
			
			// call this after the screenshot so we don't see any error dialogs			
			try {
				if (OnBugReport != null)
				{
					OnBugReport();
				}
			}
			catch {}
			
			if (Application.isEditor)
			{
				yield break;
			}
			
			var json = JSON.Stringify(_data);
			var bytes = Encoding.GetBytes(json);
			
			var headers = new Hashtable();
			headers["Content-Type"] = "application/json";
			
			UnityEngine.Debug.Log("Sending...");
			var www = new WWW( _url, bytes, headers );
			yield return www;

			if (!string.IsNullOrEmpty(www.error))
			{
				UnityEngine.Debug.LogError("Error sending bug report: " + www.error);
			}
		}
		
		static void TakeScreenShot()
		{
			try
			{
				if ( Camera.main != null )
				{				
					var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
					tex.ReadPixels( new Rect(0,0,Screen.width,Screen.height), 0, 0);
					tex.Apply();
					
					var scale = 360.0f / (float)Screen.width;
					var encoder = new JPGEncoder(tex, 25, scale); 
					var bytes = encoder.GetBytes();
					
					Texture2D.Destroy(tex);
					AddData("screen", Encoding.ToBase64String(bytes) );
				}
			}
			catch (System.Exception ex)
			{
				UnityEngine.Debug.LogWarning("Failed to create screenshot " + ex);
			}
		}
		
		
		static void LogCallback (string condition, string stackTrace, LogType type)
		{
			if (_sent)
			{
				return;
			}
			
			if ( type == LogType.Exception )
			{
				_sent = true;
				AddData("type", "exception");
				AddData("condition", condition);
				AddData("stack", stackTrace);
				Coroutines.Run(SendBugReport());
			}
		}
		
		 
	}
	
	
}
