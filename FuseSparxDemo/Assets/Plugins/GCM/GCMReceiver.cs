/*Copyright 2013 Keisuke Kobayashi
This software is licensed under Apache License 2.0*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GCM receiver.
/// </summary>
#if UNITY_ANDROID
public class GCMReceiver : MonoBehaviour {
	
	public static System.Action<string> _onError = null;
	public static System.Action<string> _onMessage = null;
	public static System.Action<string> _onRegistered = null;
	public static System.Action<string> _onUnregistered = null;
	
	public static System.Action<int> _onDeleteMessages = null;
	
	void Awake() {
		// This receiver must not be destroyed on loading level
		DontDestroyOnLoad(transform.gameObject);
	}
	
	void OnError (string errorId) {
		Debug.Log ("Error: " + errorId);
		if (_onError != null) {
			_onError (errorId);
		}
	}
	
	void OnMessage (string message) {
		Debug.Log ("MetaData: " + message);
		// The metaData from the notification can be retrieved at this point
		if (_onMessage != null) {
			_onMessage (message);
		}
	}
	
	void pMessage (string message) {
		Debug.Log ("pMessage: " + message);
		// Track the Message Open
		OtherLevelsSDK.PushPhashForTracking(message);
		OtherLevelsSDK.TrackLastPhashOpen();
	}
	
	void OnRegistered (string registrationId) {
		Debug.Log ("Registered: " + registrationId);
		PlayerPrefs.SetString("OL_AndroidToken", registrationId);
		OtherLevelsSDK.RegisterDevice("", registrationId);
		if (_onRegistered != null) {
			_onRegistered (registrationId);
		}
	}
	
	void OnUnregistered (string registrationId) {
		Debug.Log ("Unregistered: " + registrationId);
		if (_onUnregistered != null) {
			_onUnregistered (registrationId);
		}
	}
	
	void OnDeleteMessages (string total) {
		Debug.Log ("DeleteMessages: " + total);
		if (_onDeleteMessages != null) {
			int totalCnt = System.Convert.ToInt32 (total);
			_onDeleteMessages (totalCnt);
		}
	}
}
#endif
