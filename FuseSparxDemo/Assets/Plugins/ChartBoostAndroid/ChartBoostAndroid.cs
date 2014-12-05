using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_ANDROID
public class ChartBoostAndroid
{
	private static AndroidJavaObject _plugin;
	
		
	static ChartBoostAndroid()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;

		// find the plugin instance
		using( var pluginClass = new AndroidJavaClass( "com.chartboost.ChartBoostPlugin" ) )
			_plugin = pluginClass.CallStatic<AndroidJavaObject>( "instance" );
	}
	
	
	#region Activity Lifecycle
	
	public static void onStart()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "onStart" );
	}
	
	
	public static void onDestroy()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "onDestroy" );
	}
	
	
	public static void onStop()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "onStop" );
	}
	
	
	public static void onBackPressed()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "onBackPressed" );
	}
	
	#endregion
	

	// Starts up ChartBoost and records an app install
	public static void init( string appId, string appSignature )
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "init", appId, appSignature );
	}

	
	// Caches an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static void cacheInterstitial( string location )
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		if( location == null )
			location = string.Empty;

		_plugin.Call( "cacheInterstitial", location );
	}
	
	
	// Checks for a cached an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static bool hasCachedInterstitial( string location )
	{
		if( Application.platform != RuntimePlatform.Android )
			return false;
		
		if( location == null )
			location = string.Empty;

		return _plugin.Call<bool>( "hasCachedInterstitial", location );
	}
	
	
	// Loads an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static void showInterstitial( string location )
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		if( location == null )
			location = string.Empty;

		_plugin.Call( "showInterstitial", location );
	}

	
	// Caches the more apps screen
	public static void cacheMoreApps()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "cacheMoreApps" );
	}
	
	
	// Checks to see if the more apps screen is cached
	public static bool hasCachedMoreApps()
	{
		if( Application.platform != RuntimePlatform.Android )
			return false;
		
		return _plugin.Call<bool>( "hasCachedMoreApps" );
	}
	
	
	// Shows the more apps screen
	public static void showMoreApps()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		_plugin.Call( "showMoreApps" );
	}
	
}
#endif
