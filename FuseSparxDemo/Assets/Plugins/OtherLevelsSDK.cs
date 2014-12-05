using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;


public static class OtherLevelsSDK 
{
	public const string VERSION_NUMBER = "1.3.1";
#if UNITY_IPHONE
	#region ios_bindings	
	[DllImport("__Internal")]
	extern static void OtherLevels_SetTrackingID(string trackingId);

	[DllImport("__Internal")]
	extern static void OtherLevels_PushPhashForTracking(string phash);

	[DllImport("__Internal")]
	extern static void OtherLevels_TrackLastPhashOpen();

	[DllImport("__Internal")]
	extern static void OtherLevels_RegisterEvent(string eventType, string eventLabel);

	[DllImport("__Internal")]
	extern static void OtherLevels_RegisterEventWithPhash(string eventType, string eventLabel, string phash);

	[DllImport("__Internal")]
	extern static void OtherLevels_ClearLocalNotificationsPending();

	[DllImport("__Internal")]
	extern static void OtherLevels_ScheduleLocalNotification(string notification, int badge, string campaign, double secondsFromNow);

	[DllImport("__Internal")]
	extern static void OtherLevels_ScheduleLocalNotificationEx(string notification, int badge, string action, string campaign, double secondsFromNow);

	[DllImport("__Internal")]
	extern static void OtherLevels_ScheduleLocalNotificationWithMetadata(string notification, int badge, string campaign, double secondsFromNow, string userInfo);
	
	[DllImport("__Internal")]
	extern static void OtherLevels_ScheduleLocalNotificationExWithMetadata(string notification, int badge, string action, string campaign, double secondsFromNow,  string userInfo);

	[DllImport("__Internal")]
	extern static void OtherLevels_RegisterDevice(string deviceToken, string trackingId);

	[DllImport("__Internal")]
	extern static void OtherLevels_UnregisterDevice(string deviceToken);

	[DllImport("__Internal")]
	extern static void OtherLevels_SetTagValue(string trackingId, string tagName, string tagValue, string tagType);
	#endregion

	/**
 	* Associate a trackingId with a device. This allows the devices to be tracked on an individual basis and still hold a reference for retargeting
 	* @param{trackingId} The trackingId of the user, usually and email, accountId or account hash to help send retargeted messages to a device
 	*/ 
	public static void SetTrackingID(string trackingId)
	{OtherLevels_SetTrackingID(trackingId);}

	/**
    * Register a phash assigned to an in App alert or interstitial
    * @param{phash} The phash from the split associated with the message or nil if phash failed
    */	
	public static void PushPhashForTracking(string phash)
	{OtherLevels_PushPhashForTracking(phash);}
	
	/**
    * Track a message open from an in App alert or interstitial, uses the last phash pushed into the tracking list
    */
	public static void TrackLastPhashOpen()
	{OtherLevels_TrackLastPhashOpen();}
	
	/**
    * Register an event for the session
    * @param{eventType} The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
    * @param{eventLabel} The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
    */	
	public static void RegisterEvent(string eventType, string eventLabel)
	{OtherLevels_RegisterEvent(eventType, eventLabel);}

	/**
 	* Register an event for the session with phash
 	* @param{eventType} The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
 	* @param{eventLabel} The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
 	* @param{phash} The phash passed in separately with the event call
 	*/
	public static void RegisterEventWithPhash(string eventType, string eventLabel, string phash)
	{OtherLevels_RegisterEventWithPhash(eventType, eventLabel, phash);}

	/**
    * Clear all local notifications that haven't been been delivered yet
    */
	public static void ClearLocalNotificationsPending()
	{OtherLevels_ClearLocalNotificationsPending();}
	
	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{badge} The badge to set the app to
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    */	
	public static void ScheduleLocalNotification(string notification, int badge, string campaign, double secondsFromNow)
	{OtherLevels_ScheduleLocalNotification(notification, badge,campaign, secondsFromNow);}
	
	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{badge} The badge to set the app to
    * @param{action} The name of the action button to show
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    */
	public static void ScheduleLocalNotificationEx(string notification, int badge, string action, string campaign, double secondsFromNow)
	{OtherLevels_ScheduleLocalNotificationEx(notification, badge, action, campaign, secondsFromNow);}

	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{badge} The badge to set the app to
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    * @param{metaData} A string with(key-value pairs) for passing custom information in the notification to the application. The string
    * has to delimit the key:value pairs with a colon(:) and and each pair is also delimited by a comma(,). Example: "LeaderList:True,showGraph:no,winner:Test"
    */	
	public static void ScheduleLocalNotificationWithMetadata(string notification, int badge, string campaign, double secondsFromNow, string metaData)
	{OtherLevels_ScheduleLocalNotificationWithMetadata(notification, badge,campaign, secondsFromNow, metaData);}
	
	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{badge} The badge to set the app to
    * @param{action} The name of the action button to show
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    * @param{metaData} A string with(key-value pairs) for passing custom information in the notification to the application. The string
    * has to delimit the key:value pairs with a colon(:) and and each pair is also delimited by a comma(,). Example: "LeaderList:True,showGraph:no,winner:Test"
    */
	public static void ScheduleLocalNotificationExWithMetadata(string notification, int badge, string action, string campaign, double secondsFromNow,  string metaData)
	{OtherLevels_ScheduleLocalNotificationExWithMetadata(notification, badge, action, campaign, secondsFromNow, metaData);}
	
	/**
 	* Register a device with OtherLevels push service
 	* @param{trackingId} A publishers userId. This should be a unique identifier of the user (ie. email, phone no.) or nil for an anonymous user
 	* @param{deviceToken} The deviceToken of the device
 	*/
	public static void RegisterDevice(string trackingId, string deviceToken)
	{OtherLevels_RegisterDevice(deviceToken, trackingId);}
		
	/**
 	* UnRegister a device from OtherLevels push service, this puid will no longer receive pushes for that puid, nil puids will no longer receive broadcasts
 	* @param{deviceToken} The deviceToken of the device
 	*/
	public static void UnregisterDevice(string deviceToken)
	{OtherLevels_UnregisterDevice(deviceToken);}

	/**
 	* Set the tag value for a tag name associated with a trackingId
 	* @param{trackingId} The trackingId that was linked to the device
 	* @param{tagName} The tag name associated with the trackingId
 	* @param{tagValue} The tag Value that is set
 	* @param{tagType} The datatype of the Value that is set (send as "numeric" OR "string" OR "timestamp" only depending on your value)
   	Example1: To pass in tagName:Age, send in tagValue:25, send in tagType:numeric (all passed in as strings)
  	Example2: To pass in tagName:City, send in tagValue:London, send in tagType:string (all passed in as strings)
   	Example3: To pass in tagName:Time, send in tagValue:1356998412000 (Needs to be UnixTimeStamp in milliseconds - send in tagType:timestamp (all passed in as strings)
 	*/
	public static void SetTagValue(string trackingId, string tagName, string tagValue, string tagType)
	{OtherLevels_SetTagValue(trackingId, tagName, tagValue, tagType);}

#elif UNITY_ANDROID
	public static string appKey = "DefaultAppKey";

	/**
 	* Associate a trackingId with a device. This allows the devices to be tracked on an individual basis and still hold a reference for retargeting
 	* @param{trackingId} The trackingId of the user, usually and email, accountId or account hash to help send retargeted messages to a device
 	*/ 
	public static void SetTrackingID(string trackingId)
	{
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 

		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("setTrackingID", new object[] { trackingId, context});	
	}
	
	/**
    * Register a phash assigned to an in App alert or interstitial
    * @param{phash} The phash from the split associated with the message or nil if phash failed
    */	
	public static void PushPhashForTracking(string phash) 
	{
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("pushPhashForTracking", new object[] { phash });
	}
	
	/**
    * Track a message open from an in App alert or interstitial, uses the last phash pushed into the tracking list
    */
	public static void TrackLastPhashOpen() 
	{
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("trackLastPhashOpen");		
	}
	
	/**
    * Register an event for the session
    * @param{eventType} The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
    * @param{eventLabel} The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
    */	
	public static void RegisterEvent(string eventType, string eventLabel) 
	{
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 

		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("registerAppEvent", new object[] { appKey, eventType, eventLabel, context});		
	}

	/**
    * Register an event for the session
    * @param{eventType} The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
    * @param{eventLabel} The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
    * @param{phash} The phash passed in separately with the event call
    */	
	public static void RegisterEventWithPhash(string eventType, string eventLabel, string phash) 
	{
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("registerAppEvent", new object[] { appKey, eventType, eventLabel, context, phash});		
	}
	
	/**
    * Clear all local notifications that haven't been been delivered yet
    */
	public static void ClearLocalNotificationsPending() 
	{
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
 		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("clearAllPendingNotification", new object[] { context });
	}
	
	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    */	
	public static void ScheduleLocalNotification(string notification, string campaign, double secondsFromNow) 
	{
		ScheduleLocalNotificationWithMetadata(notification, campaign, secondsFromNow, "");
	}

	/**
    * Perform a split test and schedule a local notification
    * @param{notification} The message to perform a split test on
    * @param{campaign} The campaignToken to track the push under
    * @param{secondsFromNow} The number of seconds in the future from the current time to show the notification
    * @param{metaData} The JSON string of the key/value pairs for passing custom information in the notification to the application.
    * Example - "{\"LeaderList\":\"True\",\"ShowGraph\":\"no\"}"
    */	
	public static void ScheduleLocalNotificationWithMetadata(string notification, string campaign, double secondsFromNow, string metaData) 
	{
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		
		// note, (arg)=>'s are linq callbacks (inline functions)
		// you can pass in any void(Dictionary<string,string>) function as the two parameters as callback
		String phash = "";
		String messagetext = "";
		String messagecontent = "";
		long millis = (int)(secondsFromNow*1000);
		String mData = metaData;
		SplitTestNotification.Start(notification, campaign, (obj) =>{ 
			Debug.Log("OK");
			if (obj.ContainsKey("phash"))
			{
				phash = obj["phash"];
			}
			if (obj.ContainsKey("messagetext"))
			{
				messagetext =  WWW.UnEscapeURL(obj["messagetext"]);
			}
			if (obj.ContainsKey("messagecontent"))
			{
				messagecontent = obj["messagecontent"];
			}
			if (Application.platform == RuntimePlatform.Android) {
				
				using (AndroidJavaClass cls = new AndroidJavaClass ("com.otherlevels.androidportal.UnityGCMRegister")) {
					cls.CallStatic ("setupLocalNotification", phash, messagetext, millis, mData, context);
				}
			}
		}, (obj) => Debug.Log("Error"));
	}
	
	/**
    * Register a device with OtherLevels push service
    * @param{trackingId} A publishers userId. This should be a unique identifier of the user (ie. email, phone no.) or nil for an anonymous user
    * @param{deviceToken} The deviceToken of the device
    */
	public static void RegisterDevice(string trackingId, string deviceToken) 
	{
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("registerUserAndDeviceToken", new object[] { appKey, trackingId, deviceToken });
	}

	/**
 	* UnRegister a device from OtherLevels push service, this puid will no longer receive pushes for that puid, nil puids will no longer receive broadcasts
 	* @param{deviceToken} The deviceToken of the device
 	*/
	public static void UnregisterDevice(string deviceToken) 
	{
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("unregisterUser", new object[] { appKey, deviceToken });
	}
	
	/**
 	* Set the tag value for a tag name associated with a trackingId
 	* @param{trackingId} The trackingId that was linked to the device
 	* @param{tagName The tag name associated with the trackingId
 	* @param{tagValue} The tag Value that is set
 	* @param{tagType} The datatype of the Value that is set (send as "numeric" OR "string" OR "timestamp" only depending on your value)
   	  Example1: To pass in tagName:Age, send in tagValue:25, send in tagType:numeric (all passed in as strings)
   	  Example2: To pass in tagName:City, send in tagValue:London, send in tagType:string (all passed in as strings)
   	  Example3: To pass in tagName:Time, send in tagValue:1356998412000 (Needs to be UnixTimeStamp in milliseconds, send in tagType:timestamp (all passed in as strings)
 	*/
	public static void SetTagValue(string trackingId, string tagName, string tagValue, string tagType)
	{
		AndroidJavaClass jc = new AndroidJavaClass("com.otherlevels.android.library.OlAndroidLibrary");
		var inst = jc.CallStatic<AndroidJavaObject>("getInstance");
		inst.Call("setTagValue", new object[] { appKey, trackingId, tagName, tagValue, tagType });
	}

	/**
 	* Get the App's default trackingId stored by the OtherLevels Android Library
 	*/
	public static string GetTrackingID()
	{
		string puid = "";
		AndroidJavaClass playerJc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject context = playerJc.GetStatic<AndroidJavaObject>("currentActivity"); 
		if (Application.platform == RuntimePlatform.Android) {
			
			using (AndroidJavaClass cls = new AndroidJavaClass ("com.otherlevels.androidportal.UnityGCMRegister")) {
				var jo = new AndroidJavaObject("java.lang.String");
				puid = cls.CallStatic<String>("getTrackingId", context);
			}
		}
		return puid;
	}
	
#else
	public static void SetTrackingID(string trackingId)	{}
	public static void PushPhashForTracking(string phash) {}
	public static void TrackLastPhashOpen() {}
	public static void RegisterEvent(string eventType, string eventLabel) {}
	public static void RegisterEventWithPhash(string eventType, string eventLabel, string phash) {}
	public static void ClearLocalNotificationsPending() {}
	public static void ScheduleLocalNotification(string notification, int badge, string campaign, double secondsFromNow) {}
	public static void ScheduleLocalNotificationEx(string notification, int badge, string action, string campaign, double secondsFromNow) {}
	public static void ScheduleLocalNotificationWithMetadata(string notification, int badge, string campaign, double secondsFromNow, string userInfo) {}
	public static void ScheduleLocalNotificationExWithMetadata(string notification, int badge, string action, string campaign, double secondsFromNow, string userInfo) {}
	public static void RegisterDevice(string deviceToken, string trackingId) {}
	public static void UnregisterDevice(string deviceToken) {}
	public static void SetTagValue(string trackingId, string tagName, string tagValue, string tagType) {}
#endif

}

// needs to be a gameobject on a monobehaviour to use coroutines
public class SplitTestNotification : MonoBehaviour
{
	// static function to launch a new post split (note the two void(Dictionary<string,string>) callbacks)
	public static void Start(String pushtext,  String campaign, System.Action<Dictionary<string, string>> onSuccess, System.Action<Dictionary<string, string>> onFailure)
	{
		var obj = new GameObject();									// new object (we'll nuke this when we're done)
		var launch = obj.AddComponent<SplitTestNotification>();					// add this behaviour to it (we need an object to do coroutines)
		launch.StartCoroutine(launch.Launch(pushtext, campaign, onSuccess, onFailure));	// start a delayed function to make the post
	}
		
	// internal coroutine for handling the post event. 
	IEnumerator Launch(String pushtext,  String campaign, System.Action<Dictionary<string, string>> onSuccess, System.Action<Dictionary<string, string>> onFailure) 
	{
		var form = new WWWForm();
		form.AddField("campaigntoken", campaign); 
		form.AddField("responsetype", "json"); 
		form.AddField("pushtext", pushtext);
		
		var post = new WWW("https://mdn.otherlevels.com/message/analytics", form);
		yield return post;
		
		var regex = new System.Text.RegularExpressions.Regex("\"([^\"]+)\"\\:\"?([^\",\\}]+)");
		
		if(post.error!=null)
		{
			Debug.Log("Split test error: "+post.error);
			var matches = regex.Matches(post.error);
			var values = new Dictionary<string, string>();
			foreach(System.Text.RegularExpressions.Match i in matches) values.Add(i.Groups[1].Value, i.Groups[2].Value);
			onFailure(values);
		}
		else 
		{
			Debug.Log("Split test success: "+post.text);
			var matches = regex.Matches(post.text);
			var values = new Dictionary<string, string>();
			foreach(System.Text.RegularExpressions.Match i in matches) values.Add(i.Groups[1].Value, i.Groups[2].Value);			
			onSuccess(values);
		}

		Destroy(gameObject); // nuke the object, we're done with it
		
	}
}

// needs to be a gameobject on a monobehaviour to use coroutines
public class GetTagValue : MonoBehaviour
{
	// static function to launch a new post split (note the two void(Dictionary<string,string>) callbacks)
	public static void Get(String appKey, String trackingId,  String tagName, System.Action<Dictionary<string, string>> onSuccess, System.Action<Dictionary<string, string>> onFailure)
	{
		var obj = new GameObject();									// new object (we'll nuke this when we're done)
		var launch = obj.AddComponent<GetTagValue>();					// add this behaviour to it (we need an object to do coroutines)
		launch.StartCoroutine(launch.Launch(appKey, trackingId, tagName, onSuccess, onFailure));	// start a delayed function to make the post
	}
		
	// internal coroutine for handling the post event. 
	IEnumerator Launch(String appKey, String trackingId,  String tagName, System.Action<Dictionary<string, string>> onSuccess, System.Action<Dictionary<string, string>> onFailure) 
	{		
		var getTag = new WWW("https://tags.otherlevels.com/api/apps/"+appKey+"/tracking/"+trackingId+"/tag/"+tagName);
		yield return getTag;
		
		var regex = new System.Text.RegularExpressions.Regex("\"([^\"]+)\"\\:\"?([^\",\\}]+)");
		
		if(getTag.error!=null)
		{
			Debug.Log("Unable to find TrackingId or AppKey, OR There is no Internet Connection");
			Debug.Log("Get TagValue error: "+getTag.error);
			var matches = regex.Matches(getTag.error);
			var values = new Dictionary<string, string>();
			foreach(System.Text.RegularExpressions.Match i in matches) values.Add(i.Groups[1].Value, i.Groups[2].Value);
			onFailure(values);
		}
		else 
		{
			Debug.Log("Get TagValue success: "+getTag.text);
			var matches = regex.Matches(getTag.text);
			var values = new Dictionary<string, string>();
			foreach(System.Text.RegularExpressions.Match i in matches) values.Add(i.Groups[1].Value, i.Groups[2].Value);			
			onSuccess(values);
		}

		Destroy(gameObject); // nuke the object, we're done with it
		
	}
}
