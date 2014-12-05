using UnityEngine;
using System.Collections;
using System.Linq;

public class OtherLevelsAndroidProxy : MonoBehaviour 
{
#if UNITY_ANDROID && UNITY_EDITOR
	[UnityEditor.Callbacks.PostProcessSceneAttribute]
	static void BuildIntoSceneZero()
	{
		OtherLevelsPreferences preferences = OtherLevelsPreferences.Load();
		if(preferences==null || !preferences.enabled)
		{
		}
		else if(GameObject.FindObjectOfType(typeof(OtherLevelsAndroidProxy)) == null)
		{
			var obj = new GameObject("_OtherLevelsAndroidProxy");
			var proxy = obj.AddComponent<OtherLevelsAndroidProxy>();
			proxy.enabled = true;
			
			var prefs = OtherLevelsPreferences.Load();
			proxy.push_enabled = prefs.push_enabled;
			proxy.appKey = prefs.appKey;	
		}
		
		AddPermissionsToManifest();
	}
	
	static void AddPermissionsToManifest()
	{
		var bundleId = UnityEditor.PlayerSettings.bundleIdentifier;
		
		var prefs = OtherLevelsPreferences.Load();
		string path = @"Assets/Plugins/Android/AndroidManifest.xml";
		string manifest = System.IO.File.ReadAllText(path);
		Debug.Log("Old Manifest: "+manifest);
		
		manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"<!-- OtherLevels permissions  -->((.|\n)*?)<!-- OtherLevels permissions end -->\n*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
		manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"<!-- GCM Receiver-->((.|\n)*?)<!-- GCM Receiver end-->\n*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
		
		string appendPermissions = "<!-- OtherLevels permissions  -->\n" +
				"<uses-permission android:name=\"android.permission.GET_ACCOUNTS\" />\n" +
				"<uses-permission android:name=\"android.permission.WAKE_LOCK\" />\n"+
				"<uses-permission android:name=\"android.permission.INTERNET\" />\n"+
				"<uses-permission android:name=\"android.permission.VIBRATE\" />\n"+	    		
	    		"<!-- OtherLevels permissions end -->";
		
		string appendPermissionsWithGCM = "<!-- OtherLevels permissions  -->\n" +
				"<permission android:name=\""+bundleId+".permission.C2D_MESSAGE\" android:protectionLevel=\"signature\" />\n" +
				"<uses-permission android:name=\""+bundleId+".permission.C2D_MESSAGE\" />\n" +
				"<uses-permission android:name=\"com.google.android.c2dm.permission.RECEIVE\" />\n"+
				"<uses-permission android:name=\"android.permission.GET_ACCOUNTS\" />\n" +
				"<uses-permission android:name=\"android.permission.WAKE_LOCK\" />\n"+
				"<uses-permission android:name=\"android.permission.INTERNET\" />\n"+
				"<uses-permission android:name=\"android.permission.VIBRATE\" />\n"+	    		
	    		"<!-- OtherLevels permissions end -->";
		
		if(prefs.push_enabled)
		{
			manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"(\<manifest[^\>]+\>)", "$0\n"+appendPermissionsWithGCM+"\n");
		}else{
			manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"(\<manifest[^\>]+\>)", "$0\n"+appendPermissions+"\n");
		}
		
		string appendReciever = "<!-- GCM Receiver-->\n" +
		        "<receiver android:name=\"com.otherlevels.androidportal.LocalNotificationReceiver\">\n"+
		        "</receiver>\n" +
				"<!-- GCM Receiver end-->";
		
		string appendGCMReciever = "<!-- GCM Receiver-->\n"+
        		"<receiver android:name=\"com.otherlevels.androidportal.UnityGCMBroadcastReceiver\" android:permission=\"com.google.android.c2dm.permission.SEND\">\n" +
            		"<intent-filter>\n" +
                		"<action android:name=\"com.google.android.c2dm.intent.RECEIVE\" />\n" +
                		"<action android:name=\"com.google.android.c2dm.intent.REGISTRATION\" />\n" +
							"<category android:name=\""+bundleId+"\" />\n" +
            		"</intent-filter>\n" +
        		"</receiver>\n" +
        		"<service android:name=\"com.otherlevels.androidportal.UnityGCMIntentService\" />\n" +
				"<activity android:name=\"com.otherlevels.androidportal.NotificationOpenActivity\" android:launchMode=\"singleTask\" android:exported=\"true\" android:excludeFromRecents=\"true\">\n" +
        		"</activity>\n" + 
				"<!-- GCM Receiver end-->";
		
		if(prefs.push_enabled)
		{
			manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"(\<application[^\>]+\>)", "$0\n"+appendReciever+"\n"+appendGCMReciever+"\n");
		}else{
			manifest = System.Text.RegularExpressions.Regex.Replace(manifest, @"(\<application[^\>]+\>)", "$0\n"+appendReciever+"\n");
		}
		
		Debug.Log("New Manifest: "+manifest);
		System.IO.File.Delete(path);
		System.IO.File.WriteAllText(path, manifest);		
	}
#endif
	
	
#if UNITY_ANDROID
	public string appKey = "acb4c5a8866b915202bd20222bdb325d";
	public bool push_enabled = false;
	bool lostFocus = false;
	bool isPaused = false;
	bool firstStart = true;
	
	// Project Number on Google API Console
	public static string[] SENDER_IDS = {"788379865987"};

	//  Fully qualified class name of the main launch activity. By default it is set to com.unity3d.player.UnityPlayerNativeActivity.
	//  Replace the default value with class name of your main launch activity.
	public string mainActivity = "com.unity3d.player.UnityPlayerNativeActivity";
			
	void Awake()
	{		
		if(System.Array.Exists(FindObjectsOfType(typeof(OtherLevelsAndroidProxy)), (obj) => obj!=this))
		{
			DestroyImmediate(this.gameObject);
			return;
		}
		
		OtherLevelsSDK.appKey = appKey;

		Debug.Log("Proxy:Create");

#if !UNITY_EDITOR
		// send activate
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("registerCreate", new object[] { OtherLevelsSDK.appKey, context});
#endif

		// don't destroy this object (we want it in all scenes)
		DontDestroyOnLoad(gameObject);
	}
	
	void OnApplicationFocus(bool focus) 
	{
#if !UNITY_EDITOR
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
#endif
		if(!focus)
		{
			Debug.Log("Proxy:Stop");
#if !UNITY_EDITOR
			inst.Call("registerStop", OtherLevelsSDK.appKey, context);
#endif
			lostFocus = true;
		}
		else 
		{
			if(lostFocus)
			{
				Debug.Log("Proxy:Restart");
#if !UNITY_EDITOR
				inst.Call("registerRestart", OtherLevelsSDK.appKey, context);
#endif
				lostFocus = false;
			}
			Debug.Log("Proxy:Start");
#if !UNITY_EDITOR
			inst.Call("registerStart", OtherLevelsSDK.appKey, context);
#endif
			
#if !UNITY_EDITOR 
			if(firstStart)
			{
				Debug.Log("Proxy:Resume");
				inst.Call("registerResume", OtherLevelsSDK.appKey, context);
				firstStart = false;
				
				// Create receiver game object
				if(push_enabled){
				GCM.Initialize ();
				GCM.Register (SENDER_IDS);
				}
				GCM.SetupLaunchActivity(mainActivity);
			}
#endif
		}
	}
	
	void OnApplicationPause(bool paused) 
	{
#if !UNITY_EDITOR
		// send pause
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
#endif
		if(paused)
		{
			Debug.Log("Proxy:Pause");
#if !UNITY_EDITOR
			inst.Call("registerPause", OtherLevelsSDK.appKey, context);
#endif
		}
		else 
		{
			Debug.Log("Proxy:Resume");
#if !UNITY_EDITOR
			inst.Call("registerResume", OtherLevelsSDK.appKey, context);
#endif
		}
		isPaused = paused;
	}
	
	void OnApplicationQuit()  
	{
#if !UNITY_EDITOR
		// send close
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
#endif
		if(!isPaused)
		{
			Debug.Log("Proxy:Pause");
#if !UNITY_EDITOR
			inst.Call("registerPause", OtherLevelsSDK.appKey, context);		
#endif
		}
		Debug.Log("Proxy:Stop");
#if !UNITY_EDITOR
		inst.Call("registerStop", OtherLevelsSDK.appKey, context);
#endif
		Debug.Log("Proxy:Destroy");
#if !UNITY_EDITOR
		inst.Call("registerDestroy", OtherLevelsSDK.appKey, context);
#endif
	}
#endif
}

