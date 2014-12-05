using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class AdXTracking {
	
#if UNITY_IPHONE && !UNITY_EDITOR
	/* Public interface for use inside C# / JS code */
	
	[DllImport ("__Internal")]
	private static extern void _SendEvent (string clientID, string eventname, string valuestr, string currency);
	[DllImport ("__Internal")]
	private static extern void _reportAppOpen (string clientID, string iTunesID);
		
	public static void reportAppOpen(string clientID,string iTunesID)
	{
		// Call plugin only when running on real device
		if (Application.platform != RuntimePlatform.OSXEditor)
			_reportAppOpen(clientID,iTunesID);
	}	

	
	public static void SendAdXEvent(string clientID, string eventname, string valuestr, string currency)
	{
		// Call plugin only when running on real device
		if (Application.platform != RuntimePlatform.OSXEditor)
			_SendEvent(clientID, eventname,valuestr,currency);
	}	
	
#elif UNITY_ANDROID && !UNITY_EDITOR
    public static void reportAppOpen(string clientID, string unused) 
	{
        // Get Android context
		try 
		{
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject app = activity.Call<AndroidJavaObject>("getApplicationContext");
			
			AndroidJavaClass tracking = new AndroidJavaClass("com.AdX.Override.AdXOverride"); 
			tracking.CallStatic("sendInstall", app,activity);
		}
        catch (System.Exception ex)
		{
			Debug.LogError ("AdX reportAppOpen error: " + ex.ToString());
		}

    }
	
	public static void SendAdXEvent(string clientID, string eventname, string valuestr, string currency)
	{
		// Get Android context
		try 
		{
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        	AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject app = activity.Call<AndroidJavaObject>("getApplicationContext");

			AndroidJavaClass tracking = new AndroidJavaClass("com.AdX.Override.AdXOverride");
			tracking.CallStatic("sendEvent", app, activity, eventname, valuestr, currency);        
		}
		catch
		{
		}
	}
#else
	public static void reportAppOpen(string clientID, string unused) 
	{
	}
	public static void SendAdXEvent(string clientID, string eventname, string valuestr, string currency)
	{
	}
#endif

}


