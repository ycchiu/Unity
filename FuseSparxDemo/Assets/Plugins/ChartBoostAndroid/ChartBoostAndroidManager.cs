using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class ChartBoostAndroidManager : MonoBehaviour
{
#if UNITY_ANDROID
	// Fired when the more apps screen fails to load
	public static event Action didFailToLoadMoreAppsEvent;

	// Fired when an interstitial is cached
	public static event Action<string> didCacheInterstitialEvent;

	// Fired when the more apps screen is cached
	public static event Action didCacheMoreAppsEvent;

	// Fired when an interstitial is finished. Possible reasons are 'dismiss', 'close' and 'click'
	public static event Action<string> didFinishInterstitialEvent;

	// Fired when the more apps screen is finished. Possible reasons are 'dismiss', 'close' and 'click'
	public static event Action<string> didFinishMoreAppsEvent;

	// Fired whent he more apps screen is closed
	public static event Action didCloseMoreAppsEvent;

	// Fired when an interstitial fails to load
	public static event Action<string> didFailToLoadInterstitialEvent;

	// Fired when an interstitial is shown
	public static event Action<string> didShowInterstitialEvent;

	// Fired when the more app screen is shown
	public static event Action didShowMoreAppsEvent;

	
	
	void Awake()
	{
		gameObject.name = "ChartBoostAndroidManager";
		DontDestroyOnLoad( gameObject );
	}


	public void didFailToLoadMoreApps( string empty )
	{
		if( didFailToLoadMoreAppsEvent != null )
			didFailToLoadMoreAppsEvent();
	}


	public void didCacheInterstitial( string location )
	{
		if( didCacheInterstitialEvent != null )
			didCacheInterstitialEvent( location );
	}


	public void didCacheMoreApps( string empty )
	{
		if( didCacheMoreAppsEvent != null )
			didCacheMoreAppsEvent();
	}


	public void didFinishInterstitial( string param )
	{
		if( didFinishInterstitialEvent != null )
			didFinishInterstitialEvent( param );
	}


	public void didFinishMoreApps( string param )
	{
		if( didFinishMoreAppsEvent != null )
			didFinishMoreAppsEvent( param );
	}


	public void didCloseMoreApps( string empty )
	{
		if( didCloseMoreAppsEvent != null )
			didCloseMoreAppsEvent();
	}


	public void didFailToLoadInterstitial( string location )
	{
		if( didFailToLoadInterstitialEvent != null )
			didFailToLoadInterstitialEvent( location );
	}


	public void didShowInterstitial( string location )
	{
		if( didShowInterstitialEvent != null )
			didShowInterstitialEvent( location );
	}


	public void didShowMoreApps( string empty )
	{
		if( didShowMoreAppsEvent != null )
			didShowMoreAppsEvent();
	}
	
#endif
}

