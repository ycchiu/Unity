using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class TelemetryManager : SubSystem, Updatable
	{
		static Collections.CircularBuffer _buffer 	= new Collections.CircularBuffer(256);
		static List<string> _filter 				= new List<string>();
		static float _flushInterval					= 60.0f; 
		static float _flushTimer					= 0.0f;
		EndPoint _ep						= null;
		
		public static int UserLevel { get; set;}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			UserLevel = 1;
			_ep = Hub.ApiEndPoint;
		}
	
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect ()
		{
			var data = Dot.Object("tele", Hub.LoginManager.LoginData, null);
			_flushInterval = Dot.Single("interval", data,_flushInterval);
			_flushTimer = _buffer.Count > 0 ? 0.0f : _flushInterval;
			
			// get the filter list
			var filter = Dot.Array("filter", data, null);
			if ( filter != null )
			{
				_filter.Clear();
				foreach( var name in filter)
				{
					_filter.Add(name.ToString());
				}
			}
			State = SubSystemState.Connected;
		}

		public void Update ()
		{
			_flushTimer -= Time.deltaTime;
			if ( _flushTimer < 0 )
			{
				Flush();
			}
		}

		public override void Disconnect (bool isLogout)
		{
			if (isLogout)
			{
				Flush();
			}
			_buffer.Clear();
		}
		#endregion
		
		static bool IsFiltered( string name)
		{
			foreach (var f in _filter)
			{
				if (name.StartsWith(f))
				{
					return true;
				}
			}
			return false;
		}
		
		public static void Event( string name, int value, object eventData )
		{
			EventInternal(Time.Now, name, value, eventData);
		}		
		
		static void EventInternal( int time, string name, int value, object eventData)
		{
			name = StringUtil.SafeKey(name);
			if (IsFiltered(name))
			{
				EB.Debug.Log("filtering out event " + name);;
				return;
			}
			EB.Debug.Log("Event: " + name + " " + value);
			_buffer.Push( new object[]{ time, name, value, UserLevel, eventData } ); 
			
			if ( _flushTimer < 0 )
			{
				_flushTimer = 0.5f;
			}
		}
		
		public void Flush()
		{
			if (_buffer.Count>0 && Hub.State == HubState.Connected )
			{
				var events = _buffer.ToArray();
				_buffer.Clear();
				
				var req = _ep.Post("/telemetry");
				req.AddData("events", events);
				_ep.Service(req, delegate(Response r){
						
				});
				
				_flushTimer = _flushInterval;
			}
			else
			{
				_flushTimer = 1.0f;
			}
			
		}
	}
}

// shortcut class
public static class Telemetry
{
	public static int UserLevel
	{
		set 
		{
			EB.Sparx.TelemetryManager.UserLevel = value;
		}
	}
	
	public static void Event(string name, int value = 1, object eventData = null)
	{
		EB.Sparx.TelemetryManager.Event(name, value, eventData);
	}
	
	public static void Flush()
	{		
		if (EB.Sparx.Hub.Instance != null)
		{
			EB.Sparx.Hub.Instance.TelemetryManager.Flush();
		}
	}
}