using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace EB.Sparx
{
	public class MatchManager : SubSystem
	{
		MatchAPI _api;
		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_api = new MatchAPI( Hub.ApiEndPoint );
			EB.Debug.LogWarning("Match Manager Initialized");
		}
		
		public override void Connect ()
		{
			State = SubSystemState.Connected;
			EB.Debug.LogWarning("Match Manager Connected");
		}
		
		public override void Disconnect (bool isLogout)
		{
			
		}
		#endregion		
		
		public void ActivateMatch<T>( string matchType, T matchData, EB.Action<EB.Sparx.Request, T> convertMatchData, EB.Action<string> onComplete )
		{
			_api.ActivateMatch( matchType, matchData, convertMatchData, delegate(string err, Hashtable result ) {
				return onComplete( err );
			});
		}
		
		public void ResolveMatch<T1, T2>( string matchType, string matchID, T1 matchResults, EB.Action<EB.Sparx.Request, T1> convertMatchResults, T2 matchStats, EB.Action<EB.Sparx.Request, T2 > converMatchStats, EB.Action<string> onComplete )
		{
			_api.ResolveMatch( matchType, matchID, matchResults, convertMatchResults, matchStats, converMatchStats, delegate(string err, Hashtable result ) {
				return onComplete( err );
			});
		}		
	}
	
}