using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GlobalSettings : MonoBehaviour 
{
	private static GameObject s_GlobalSettingsGameObject = null;
	private const string s_GameObjectName = "GlobalSettings";
	private bool m_AreMessageHandlersRegistered = false;
	private const string s_IsFullScreenPlayerPrefsKey = "IsFullScreen";
	private const string s_IsAudioMutedPlayerPrefsKey = "IsAudioMuted";
	private const string s_IsMusicMutedPlayerPrefsKey = "IsMusicMuted";
	private const bool s_DefaultIsFullScreenPlayerPref = false;	
	private const bool s_DefaultIsAudioMutedPlayerPref = false;
	private const bool s_DefaultIsMusicMutedPlayerPref = false;
		
	private int m_InitialScreenWidth = 0;
	private int m_InitialScreenHeight = 0;
	private Dictionary<string, bool> m_CachedPlayerPrefs = new Dictionary<string, bool>();

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void SetUpGlobalGameSettingsGameObject()
	{
		if (null == s_GlobalSettingsGameObject)
		{
			s_GlobalSettingsGameObject = new GameObject(s_GameObjectName);

			s_GlobalSettingsGameObject.AddComponent<Persistent>();
			GlobalSettings GlobalSettingsComponent = s_GlobalSettingsGameObject.AddComponent<GlobalSettings>();
			GlobalSettingsComponent.RegisterMessageHandlers();

			GlobalSettingsComponent.m_InitialScreenWidth = Screen.width;
			GlobalSettingsComponent.m_InitialScreenHeight = Screen.height;

			GlobalSettingsComponent.CachePlayerPrefs();
			
			GlobalSettingsComponent.SetFullScreen(GlobalSettingsComponent.GetCachedPlayerPref(s_IsFullScreenPlayerPrefsKey));			
			GlobalSettingsComponent.MuteAudio(GlobalSettingsComponent.GetCachedPlayerPref(s_IsAudioMutedPlayerPrefsKey));
			GlobalSettingsComponent.MuteMusic(GlobalSettingsComponent.GetCachedPlayerPref(s_IsMusicMutedPlayerPrefsKey));			
		}
	}	

	public static GameObject GetGlobalSettingsGameObject()
	{
		if (null == s_GlobalSettingsGameObject)
		{
			SetUpGlobalGameSettingsGameObject();
		}

		return (s_GlobalSettingsGameObject);
	}

	public void Awake()
	{		
	}

	public void OnDestroy()
	{
		UnregisterMessageHandlers();
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private void RegisterMessageHandlers()
	{
		if (false == m_AreMessageHandlersRegistered)
		{
			Messenger.AddListener(gameObject, "ToggleIsFullScreen", OnToggleIsFullScreen);
			Messenger<bool>.AddListener(gameObject, "GetIsFullScreen", OnGetIsFullScreen);
			Messenger<bool>.AddListener(gameObject, "SetIsFullScreen", OnSetIsFullScreen);
			Messenger.AddListener(gameObject, "ToggleIsAudioMuted", OnToggleIsAudioMuted);
			Messenger<bool>.AddListener(gameObject, "GetIsAudioMuted", OnGetIsAudioMuted);
			Messenger.AddListener(gameObject, "ToggleIsMusicMuted", OnToggleIsMusicMuted);
			Messenger<bool>.AddListener(gameObject, "GetIsMusicMuted", OnGetIsMusicMuted);

			m_AreMessageHandlersRegistered = true;
		}
	}

	private void UnregisterMessageHandlers()
	{
		if (true == m_AreMessageHandlersRegistered)
		{
			Messenger.RemoveListener(gameObject, "ToggleIsFullScreen", OnToggleIsFullScreen);
			Messenger<bool>.RemoveListener(gameObject, "GetIsFullScreen", OnGetIsFullScreen);
			Messenger<bool>.RemoveListener(gameObject, "SetIsFullScreen", OnSetIsFullScreen);
			Messenger.RemoveListener(gameObject, "ToggleIsAudioMuted", OnToggleIsAudioMuted);
			Messenger<bool>.RemoveListener(gameObject, "GetIsAudioMuted", OnGetIsAudioMuted);
			Messenger.RemoveListener(gameObject, "ToggleIsMusicMuted", OnToggleIsMusicMuted);
			Messenger<bool>.RemoveListener(gameObject, "GetIsMusicMuted", OnGetIsMusicMuted);

			m_AreMessageHandlersRegistered = false;
		}
	}

	private void OnToggleIsFullScreen()
	{
		SetFullScreen(!GetCachedPlayerPref(s_IsFullScreenPlayerPrefsKey));
	}

	private void OnGetIsFullScreen(ref bool IsFullScreen)
	{
		IsFullScreen = GetCachedPlayerPref(s_IsFullScreenPlayerPrefsKey);
	}

	private void OnSetIsFullScreen(ref bool DoIsFullScreen)
	{
		SetFullScreen(DoIsFullScreen);
	}
		
	private void OnToggleIsAudioMuted()
	{
		MuteAudio(!GetCachedPlayerPref(s_IsAudioMutedPlayerPrefsKey));	
	}

	private void OnGetIsAudioMuted(ref bool IsAudioMuted)
	{
		IsAudioMuted = GetCachedPlayerPref(s_IsAudioMutedPlayerPrefsKey);
	}

	private void OnToggleIsMusicMuted()
	{
		MuteMusic(!GetCachedPlayerPref(s_IsMusicMutedPlayerPrefsKey));
	}

	private void OnGetIsMusicMuted(ref bool IsMusicMuted)
	{
		IsMusicMuted = GetCachedPlayerPref(s_IsMusicMutedPlayerPrefsKey);
	}

	private void CachePlayerPrefs()
	{
		SetCachedPlayerPref(s_IsFullScreenPlayerPrefsKey, GetPlayerPref(s_IsFullScreenPlayerPrefsKey, s_DefaultIsFullScreenPlayerPref));
		SetCachedPlayerPref(s_IsAudioMutedPlayerPrefsKey, GetPlayerPref(s_IsAudioMutedPlayerPrefsKey, s_DefaultIsAudioMutedPlayerPref));
		SetCachedPlayerPref(s_IsMusicMutedPlayerPrefsKey, GetPlayerPref(s_IsMusicMutedPlayerPrefsKey, s_DefaultIsMusicMutedPlayerPref));	
	}

	private bool GetCachedPlayerPref(string Key)
	{
		return (m_CachedPlayerPrefs[Key]);
	}

	private void SetCachedPlayerPref(string Key, bool Value)
	{
		m_CachedPlayerPrefs[Key] = Value;
	}

	private bool GetPlayerPref(string Key, bool Default)
	{
		return ((1 == PlayerPrefs.GetInt(Key, ((true == Default) ? 1 : 0))) ? true : false);
	}

	private void SetPlayerPref(string Key, bool Pref)
	{
		if (Pref != GetCachedPlayerPref(Key))
		{
			PlayerPrefs.SetInt(Key, ((true == Pref) ? 1 : 0));
			SetCachedPlayerPref(Key, Pref);
		}
	}
	
	private void SetFullScreen(bool DoFullScreen)
	{
		if (DoFullScreen != Screen.fullScreen)
		{
			int ScreenWidth = 0;
			int ScreenHeight = 0;
			if (true == DoFullScreen)
			{
				ScreenWidth = Screen.resolutions[Screen.resolutions.Length - 1].width;
				ScreenHeight = Screen.resolutions[Screen.resolutions.Length - 1].height;
			}
			else
			{
				ScreenWidth = m_InitialScreenWidth;
				ScreenHeight = m_InitialScreenHeight;
			}

			Screen.SetResolution(ScreenWidth, ScreenHeight, DoFullScreen);			
		}

		SetPlayerPref(s_IsFullScreenPlayerPrefsKey, DoFullScreen);	
	}

	private void MuteAudio(bool DoMuteAudio)
	{
		if (true == DoMuteAudio)
		{
			AudioListener.volume = AudioConstants.s_VolumeScalarMin;
		}
		else
		{
			AudioListener.volume = AudioConstants.s_VolumeScalarMax;
		}

		SetPlayerPref(s_IsAudioMutedPlayerPrefsKey, DoMuteAudio);	
	}

	private void MuteMusic(bool DoMuteMusic)
	{
		bool OnlyIfIsForMusic = true;
		Messenger<bool, bool>.BroadcastToAllListeners("MuteAudioJukebox", ref DoMuteMusic, ref OnlyIfIsForMusic);
		
		SetPlayerPref(s_IsMusicMutedPlayerPrefsKey, DoMuteMusic);
	}
}
