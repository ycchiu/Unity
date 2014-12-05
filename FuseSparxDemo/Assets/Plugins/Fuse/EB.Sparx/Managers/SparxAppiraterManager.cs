#if UNITY_IPHONE && !UNITY_EDITOR
#define USE_APPIRATER
#endif
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace EB.Sparx
{
	public class AppiraterConfig
	{
		public string 	AppId = string.Empty;
		public double 	DaysUntilPrompt = 1.0f;
		public int		UsesUntilPrompt = 10;
		public int		SignificantEventsUntilPrompt = 3;
		public double 	TimeBeforeReminding = 86400;
		public bool		CanPromptOnLaunched = false;
		public bool		CanPromptOnUse = false;
		public bool		CanPromptOnEvent = false;
		
		public string	Title = string.Empty;
		public string 	Message = string.Empty;
		public string 	RateButton = string.Empty;
		public string 	CancelButton = string.Empty;
		public string 	RemindButton = string.Empty;
		
		public AppiraterListener Listener = null; 
	}
	
	public class AppiraterManager : Manager
	{
#if USE_APPIRATER
		const string DLL_NAME="__Internal";
		
		bool _enabled = false;
		bool _stringsLoaded = false;
		AppiraterConfig _config = null;
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetAppId(string appId);
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_SetDaysUntilPrompt( double days );
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_SetUsesUntilPrompt( int uses );
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_SetSignificantEventsUntilPrompt( int events );
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_SetTimeBeforeReminding( double time);
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_SetDebug( bool debug);
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_AppLaunched(bool canprompt);
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_AppEnteredForeground(bool canprompt);
    
    	[DllImport(DLL_NAME)]
		static extern void _Appirater_UserDidSignificantEvent(bool canprompt);
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetTitle(string str);
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetMessage(string str);
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetCancelButton(string str);
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetRateButton(string str);
		
		[DllImport(DLL_NAME)]
		static extern void _Appirater_SetRemindButton(string str);

#endif
		public override void Initialize (Config config)
		{
#if USE_APPIRATER
			var cfg = config.AppiraterConfig;
			if (string.IsNullOrEmpty(cfg.AppId))
			{
				return;
			}
			
			_config = cfg;
			
			// setup the listener
			var listener = new GameObject("appirater_callbacks", typeof(SparxAppiraterManager)).GetComponent<SparxAppiraterManager>();
			listener.Listener = cfg.Listener;
			
			_Appirater_SetAppId(cfg.AppId);
			_Appirater_SetDaysUntilPrompt(cfg.DaysUntilPrompt);
			_Appirater_SetUsesUntilPrompt(cfg.UsesUntilPrompt);
			_Appirater_SetSignificantEventsUntilPrompt(cfg.SignificantEventsUntilPrompt);
			_Appirater_SetTimeBeforeReminding(cfg.TimeBeforeReminding);
			
#if ENABLE_PROFILER
			//_Appirater_SetDebug(true);
#endif
			
			// we don't count the use until we have the login event
			//_Appirater_AppLaunched(_config.CanPromptOnLaunched);			
			_enabled = true;
#endif
		}
		
		void LoadStrings()
		{
#if USE_APPIRATER
			if (_stringsLoaded)
				return;
			
			var cfg = _config;
			
			if (!string.IsNullOrEmpty(cfg.Title))
			{
				_Appirater_SetTitle( EB.Localizer.GetString(cfg.Title));
			}

			if (!string.IsNullOrEmpty(cfg.Message))
			{
				_Appirater_SetMessage(EB.Localizer.GetString(cfg.Message));
			}			
			
			if (!string.IsNullOrEmpty(cfg.RateButton))
			{
				_Appirater_SetRateButton(EB.Localizer.GetString(cfg.RateButton));
			}
			
			if (!string.IsNullOrEmpty(cfg.CancelButton))
			{
				_Appirater_SetCancelButton(EB.Localizer.GetString(cfg.CancelButton));
			}
			
			if (!string.IsNullOrEmpty(cfg.RemindButton))
			{
				_Appirater_SetRemindButton(EB.Localizer.GetString(cfg.RemindButton));
			}
			
			_stringsLoaded = true;
#endif
		}
		
		public void UserDidSignificantEvent()
		{
#if USE_APPIRATER
			if (_enabled)
			{
				_Appirater_UserDidSignificantEvent(_config.CanPromptOnEvent);
			}
#endif
		}
		
		public override void OnLoggedIn()
		{
#if USE_APPIRATER
			if (_enabled)
			{
				LoadStrings();
				_Appirater_AppEnteredForeground(_config.CanPromptOnUse);
			}
#endif			
		}
		
	}
	
}

// mono class to handle listner
public class SparxAppiraterManager : MonoBehaviour 
{
	public EB.Sparx.AppiraterListener Listener {get;set;}
	
	void Awake()			
	{
		DontDestroyOnLoad(gameObject);				
	}
				
	void OnDisplay(string i)
	{
		if (Listener != null)
		{
			Listener.OnDisplay();
		}
	}
	
	void OnDeclined(string i)
	{
		if (Listener != null)
		{
			Listener.OnDeclined();
		}
	}
	
	void OnRated(string i)
	{
		if (Listener != null)
		{
			Listener.OnRated();
		}
	}
	
	void OnRemind(string i)
	{
		if (Listener != null)
		{
			Listener.OnRemind();
		}
	}
	
	
}

