#if UNITY_IPHONE && !UNITY_EDITOR
#define USE_CHARTBOOST_IOS
#elif UNITY_ANDROID && !UNITY_EDITOR
#define USE_CHARTBOOST_ANDROID
#endif
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class ChartboostPlugin : MonoBehaviour 
{	
#if USE_CHARTBOOST_IOS
	#region P/Invoke
	const string DLL_NAME = "__Internal";
		
	[DllImport(DLL_NAME)]
	static extern void _Chartboost_StartSession( string appId, string appSignature );
    
	[DllImport(DLL_NAME)]
    static extern void _Chartboost_ShowInterstitial();
    
	[DllImport(DLL_NAME)]
    static extern void _Chartboost_ShowInterstitial2(string location);
    
	[DllImport(DLL_NAME)]
    static extern void _Chartboost_CacheInterstitial();
    
	[DllImport(DLL_NAME)]
    static extern void _Chartboost_ShowMoreApps();
	
	[DllImport(DLL_NAME)]
    static extern void _Chartboost_CacheMoreApps();
	#endregion
#elif USE_CHARTBOOST_ANDROID
	private static AndroidJavaClass _chartboost_wrapper;
#endif
	
	public static ChartboostPlugin Instance {get;private set;}
	
	public string appId = string.Empty;
	public string appSignature = string.Empty;
	public bool showInterstitial = false;

	void Start()
	{
		Instance = this;
		
		DontDestroyOnLoad(gameObject);
		
		if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(appSignature) )
		{
#if USE_CHARTBOOST_IOS
			_Chartboost_StartSession(appId.Trim(), appSignature.Trim());
			
			if (showInterstitial)
			{
				_Chartboost_ShowInterstitial();
			}
#elif USE_CHARTBOOST_ANDROID
			ChartBoostAndroid.init( appId.Trim(), appSignature.Trim());
			ChartBoostAndroid.onStart();
#endif
		}
	}

	public void CacheInterstitial()
	{
#if USE_CHARTBOOST_IOS
		_Chartboost_CacheInterstitial();
#elif USE_CHARTBOOST_ANDROID
		ChartBoostAndroid.cacheInterstitial(null);
#endif		
	}			
	
	public void ShowInterstitial( string location )
	{
		EB.Debug.Log("ShowInterstitial: " + location);
		if (!string.IsNullOrEmpty(location)  )
		{
#if USE_CHARTBOOST_IOS
			_Chartboost_ShowInterstitial2(location);
#elif USE_CHARTBOOST_ANDROID
			ChartBoostAndroid.showInterstitial(location);
#endif		
		}
		else
		{
#if USE_CHARTBOOST_IOS
			_Chartboost_ShowInterstitial();
#elif USE_CHARTBOOST_ANDROID
			ChartBoostAndroid.showInterstitial(null);
#endif		
		}
	}
	
	public void ShowMoreApps()
	{
#if USE_CHARTBOOST_IOS
		_Chartboost_ShowMoreApps();
#elif USE_CHARTBOOST_ANDROID
		ChartBoostAndroid.showMoreApps();
#endif		
	}	
	
	public void CacheMoreApps()
	{
#if USE_CHARTBOOST_IOS
		_Chartboost_CacheMoreApps();
#elif USE_CHARTBOOST_ANDROID
		ChartBoostAndroid.cacheMoreApps();
#endif		
	}	
	
	bool _paused = false;
	
	void OnApplicationPause(bool pause)
	{
		if (!pause && _paused)
		{
			// start new session on comming back
			Start();
		}
		_paused = pause;
	}
}
