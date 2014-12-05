using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class EventsConfig
	{
		public EventsConfig()
		{
			this.EventTypeMapping.Add( "basic", typeof( EventsEvent ) );
		}
	
		public Dictionary< string, System.Type > EventTypeMapping = new Dictionary<string, System.Type>();
	}
	
	public class EventsManager : SubSystem, Updatable
	{
		EventsConfig Config = new EventsConfig();
		EventsAPI Api = null;
		string CheckHash = string.Empty;
		private List<EventsEvent> Events = new List<EventsEvent>();
		
		public List<EventsEvent> GetEvents( EventsEvent.EventState type )
		{
			List<EventsEvent> events = new List<EventsEvent>();
			foreach( EventsEvent candidate in this.Events )
			{
				if( candidate.State == type )
				{
					events.Add( candidate );
				}
			}
			return events;
		}

		public EventsEvent GetEventById( string eventID )
		{
			foreach( EventsEvent candidate in this.Events )
			{
				if( candidate.Id == eventID )
				{
					return candidate;
				}
			}
			return null;
		}


		public List<T> GetAllEvents<T>() where T : EventsEvent
		{
			List<T> events = new List<T>();
			foreach( EventsEvent candidate in this.Events )
			{
				T evt = candidate as T;
				if( evt != null )
				{
					events.Add( evt );
				}
			}
			return events;
		}
		
		public void ClaimMilestone( EventsEvent.MilestoneInfo.Milestone milestone, EB.Action< bool > cb )
		{
			if( ( milestone != null ) && ( string.IsNullOrEmpty( milestone.ClaimUrl ) == false ) )
			{
				this.Api.ClaimAward( milestone.ClaimUrl, delegate( string err, bool success ) {
					if( string.IsNullOrEmpty( err ) == false )
					{
						EB.Debug.LogError( "Problem claiming {0}->{1}", milestone, err );
					}
					
					cb( success );
				});
			}
		}
		
		public void ClaimRanked( EventsEvent.RankedRangesInfo.RankedRange ranked, EB.Action< bool > cb )
		{
			if( ( ranked != null ) && ( string.IsNullOrEmpty( ranked.ClaimUrl ) == false ) )
			{
				this.Api.ClaimAward( ranked.ClaimUrl, delegate( string err, bool success ) {
					if( string.IsNullOrEmpty( err ) == false )
					{
						EB.Debug.LogError( "Problem claiming {0}->{1}", ranked, err );
					}
					
					cb( success );
				});
			}
		}
		
		public void Refresh( EB.Action<bool> cb )
		{
			this.Api.Refresh( this.CheckHash, delegate( string err, List<EventsEvent> events, string updatedCheckHash ) {
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( false );
				}
				else
				{
					if( this.CheckHash == updatedCheckHash )
					{
						cb( false );
					}
					else
					{
						this.Events = events;
						this.CheckHash = updatedCheckHash;
						cb( true );
					}
				}
			});
		}
		
		public void OnReport( Hashtable data, EB.Action< EventsEvent > cb )
		{
			EventsEvent evt = null;
			
			string eid = EB.Dot.String( "eid", data, string.Empty );
			if( string.IsNullOrEmpty( eid ) == false )
			{
				evt = this.Events.Find( delegate( EventsEvent candidate ) { return candidate.Id == eid; } );
				if( evt != null )
				{
					evt.UpdateReport( data );
				}	
			}
			
			cb( evt );
		}
		
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			this.Config = config.EventsConfig;
			this.Api = new EventsAPI( this.Config, Hub.ApiEndPoint );
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var eventsData = Dot.Object( "events", Hub.LoginManager.LoginData, null );
			if( eventsData != null )
			{
				this.Api.OnEventsData( eventsData, delegate( string err, List<EventsEvent> events, string updatedCheckHash ) {
					this.Events = events;
					this.CheckHash = updatedCheckHash;
					State = SubSystemState.Connected;
				});
			}
			else
			{
				this.Refresh( delegate( bool updated ) {
					State = SubSystemState.Connected;
				});
			}
		}

		public void Update ()
		{
		}

		public override void Disconnect (bool isLogout)
		{
		}
		#endregion
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
				case "report": {
					Hashtable data = payload as Hashtable;
					this.OnReport( data, delegate( EventsEvent evt ){
						if( evt != null )
						{
							EB.Debug.Log( "Updating {0}", evt.ServerName );
						}
					});
					break;
				}
				case "refresh":
				{
					this.Refresh( delegate( bool updated ) {
					});
					break;
				}
				default:
				{
					break;
				}
			}
		}
	}
}
