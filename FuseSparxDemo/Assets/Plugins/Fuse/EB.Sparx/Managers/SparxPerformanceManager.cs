using UnityEngine;
using System;
using System.Collections;

namespace EB.Sparx
{
	public class PerformanceConfig
	{
		public delegate void DataLoaded(object data);
		public DataLoaded DataLoadedHandler = null;

		public delegate int GetPlatform();
		public GetPlatform GetPlatformHandler = null;
	}

	public class PerformanceManager : SubSystem
	{
		PerformanceConfig _config = new PerformanceConfig();
		PerformanceAPI _api = null;

		public object Info { get; private set; }
		
		public override void Initialize (EB.Sparx.Config config)
		{
			this._config = config.PerformanceConfig;
			_api = new PerformanceAPI(Hub.ApiEndPoint);
		}
		
		public override void Connect ()
		{
			var performanceData = Dot.Object( "performance", Hub.LoginManager.LoginData, null );
			if( performanceData != null )
			{
				OnFetch(null, performanceData);
			}
			else
			{
				Fetch();
			}
			State = EB.Sparx.SubSystemState.Connected;
		}
				
		public override void Disconnect (bool isLogout)
		{
		} 

		void OnFetch( string err, object data )
		{
			Info = null;
			if (data == null)
			{
				EB.Debug.LogError("PerformanceManager: fetched null data : " + err);
				return;
			}
			if (_config.DataLoadedHandler != null)
			{
				_config.DataLoadedHandler(data);
			}
		}
		
		public void Fetch(EB.Action cb = null, bool force = false, string forceDevice = "", string forceCPU = "", string forceGPU = "")
		{
			//only fetch once
			if (Info != null && !force)
			{
				if (cb != null)
				{
					cb();
				}
				return;
			}

			#if UNITY_EDITOR || UNITY_STANDALONE
				string device = (forceDevice.Length == 0) ? "Unity" : forceDevice;
				string CPU = (forceCPU.Length == 0) ? "Unity" : forceCPU;
				string GPU = (forceGPU.Length == 0) ? "Unity" : forceGPU;
			#else
				string device = (forceDevice.Length == 0) ? SystemInfo.deviceModel : forceDevice;
				string CPU = (forceCPU.Length == 0) ? SystemInfo.processorType : forceCPU;
				string GPU = (forceGPU.Length == 0) ? SystemInfo.graphicsDeviceName : forceGPU;
			#endif

			int platform = -1;
			if (_config.GetPlatformHandler != null)
			{
				platform = _config.GetPlatformHandler();
			}

			_api.Fetch(device, CPU, GPU, platform, delegate(string err, object result)
			{
				if (string.IsNullOrEmpty(err)) 
				{
					OnFetch(string.Empty, result);
				}	
				else
				{
					OnFetch(err, null);
				}
				if (cb != null)
				{
					cb();
				}
			});
		}
		
		public void ReportStats(int platform, string scene, string profile, float fps)
		{
			var eventData = new Hashtable();
			eventData["plt"] = platform;
			eventData["scn"] = scene;
			eventData["prf"] = profile;
			eventData["fps"] = fps;
			#if UNITY_EDITOR || UNITY_STANDALONE
				eventData["dev"] = "Unity";
				eventData["cpu"] = "Unity";
				eventData["gpu"] = "Unity";
			#else
				eventData["dev"] = SystemInfo.deviceModel;
				eventData["cpu"] = SystemInfo.processorType;
				eventData["gpu"] = SystemInfo.graphicsDeviceName;
			#endif
			
			EB.Debug.Log("PerformanceManager: ReportStats");
			Telemetry.Event("perf/fps", 1, eventData);
		}
	}
}

