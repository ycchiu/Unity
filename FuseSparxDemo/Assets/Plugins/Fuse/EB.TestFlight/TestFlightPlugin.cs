using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace EB.TestFlight
{
	public static class Plugin
	{
#if UNITY_IPHONE && !UNITY_EDITOR
		[DllImport("__Internal")]
		static extern void TestFlight_TakeOff( string apiToken );		
		
		[DllImport("__Internal")]
		static extern void TestFlight_SetUDID();
		
		[DllImport("__Internal")]
		static extern void TestFlight_Log(string l);
		
		[DllImport("__Internal")]
		static extern void TestFlight_Checkpoint(string l);
		
#else
		static void TestFlight_TakeOff(string apiToken) {}
		static void TestFlight_SetUDID() {}
		static void TestFlight_Log(string l) {}
		static void TestFlight_Checkpoint(string l) {}
#endif
		
		public static void TakeOff( string apiToken )
		{
			TestFlight_TakeOff(apiToken);
		}		
		
		public static void SetUDID()
		{
			TestFlight_SetUDID();
		}
		
		public static void Log( string message )
		{
			TestFlight_Log(message);
		}
		
		public static void Checkpoint( string message )
		{
			TestFlight_Checkpoint(message);
		}
			
	}
}

public class TestFlightPlugin : MonoBehaviour 
{
	public string ApiToken = string.Empty;

	// Use this for initialization
	void Start () 
	{
		if (!string.IsNullOrEmpty(ApiToken) && EB.Version.GetVersion() != "1.0.0")
		{
			EB.TestFlight.Plugin.SetUDID();
			EB.TestFlight.Plugin.TakeOff(ApiToken);
			Application.RegisterLogCallbackThreaded(this.Logger);
		}
		
		DontDestroyOnLoad(gameObject);
	}
	
	void Logger( string stack, string message, LogType type )
	{
		switch( type )
		{
		case LogType.Log:
			{
				EB.TestFlight.Plugin.Log("I: " + message + "\n" + stack);
			}
			break;
		case LogType.Assert:
			{
				EB.TestFlight.Plugin.Log("A: " + message + "\n" + stack);
			}
			break;	
		case LogType.Warning:
			{
				EB.TestFlight.Plugin.Log("W: " + message + "\n" + stack);
			}
			break;	
		case LogType.Exception:
			{
				EB.TestFlight.Plugin.Log("EX: " + message + "\n" + stack);
			}
			break;	
		case LogType.Error:
			{
				EB.TestFlight.Plugin.Log("E: " + message + "\n" + stack);
			}
			break;	
		}
	}
}
