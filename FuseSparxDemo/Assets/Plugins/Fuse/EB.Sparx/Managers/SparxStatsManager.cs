using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace EB.Sparx
{
	public class StatObject
	{
		public StatObject( Hashtable data = null )
		{
			if( data != null )
			{
				sid = EB.Dot.String( "sid", data, string.Empty );
				name = EB.Dot.String( "name", data, string.Empty );
			}
			else
			{
				sid = string.Empty;
				name = string.Empty;
			}
		}
		
		public string sid { get; private set; }
		public string name { get; private set; }
	}
	public class StatObjectDict : Dictionary<string, StatObject> {}
	
	public class StatsManager : SubSystem
	{
		StatsAPI _api;
		
		private Dictionary<string, int> _userStats;
		private StatObjectDict _statObjectDict;
		
		public Dictionary<string, int> UserStats { get; private set; }
		public StatObjectDict StatObjectDict { get; private set; }
	
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_api = new StatsAPI( Hub.ApiEndPoint );
			_userStats = new Dictionary<string, int>();
			_statObjectDict = new StatObjectDict();
			EB.Debug.LogWarning("Stats Manager Initialized");
		}
		
		public override void Connect ()
		{
			var statsData = EB.Dot.Object( "gamestats", Hub.LoginManager.LoginData, null );
			if( statsData != null )
			{
				this.OnStatsData( statsData, delegate( string statsErr ) {		
					State = EB.Sparx.SubSystemState.Connected;
					EB.Debug.Log("StatsManager.Connect: Data retrieved via login data");										
				});
			}
			else
			{
				this.Fetch( delegate( bool updated ) {
					State = EB.Sparx.SubSystemState.Connected;
					EB.Debug.Log("StatsManager.Fetch: We've fetched everything!...Success");										
				});
			}	
		}
		
		public override void Disconnect (bool isLogout)
		{
			if( isLogout )
			{
				ClearLocalUserData();
			}
		} 
		
		public void Fetch( EB.Action<bool> cb )
		{
			this._api.GetLoginData( delegate( string err, Hashtable statsData ) {
				if( !string.IsNullOrEmpty( err ) )
				{
					Debug.LogWarning ("StatsManager.Fetch: Failed");
					FatalError( err );
					return cb( false );
				}
				
				this.OnStatsData( statsData, delegate( string bcgErr ) {
					if( !string.IsNullOrEmpty( bcgErr ) )
					{
						FatalError( bcgErr );					
					}
					return cb( true );
				});
			});
		}
		
		public void OnStatsData( Hashtable statsData, EB.Action<string> cb ) {		
			_userStats.Clear();
			
			Hashtable setData = EB.Dot.Object( "setData", statsData, null );
			if( setData == null )
			{
				cb( "No Stats Set Data" );
				return;
			}
			foreach( DictionaryEntry stat in setData )
			{
				StatObject statObject = new StatObject( (Hashtable)stat.Value );
				_statObjectDict[stat.Key.ToString()] = statObject;		
			}
			
			Hashtable userStats = EB.Dot.Object( "userstats", statsData, null );
			if( userStats == null )
			{
				cb( "No Stats User Data" );
				return;
			}
			HandleUserStatsUpdate( userStats );
			
			cb( null );
		}
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
				case "stats-updated":
				{
					Hashtable data = payload as Hashtable;
					
					if( data != null )
					{
						Hashtable result = EB.Dot.Object( "result", data, null );
						HandleUserStatsUpdate( result );					
					}
					break;
				}
				default:
				{
					break;
				}
			}
		}
		
		#endregion		

		private void HandleUserStatsUpdate( Hashtable userStats )
		{
			foreach( DictionaryEntry stat in userStats )
			{
				string key = stat.Key.ToString();
				int value = EB.Dot.Integer( key, userStats, 0 );			
				_userStats[key] = value;
			}			
		}

		private void ClearLocalUserData()
		{
			_userStats.Clear();
		}
	}
}