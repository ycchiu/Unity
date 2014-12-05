using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class LeaderboardManager : Manager
	{
		// update type for updating a score
	    public enum UpdateType
	    {
	        Set,
	        Max,
	        Min,
	        Inc,
	        Dec,
	    };
		
		EndPoint _ep;
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			_ep = Hub.ApiEndPoint;
		}

		#endregion
		
		public void ReportScore( int leaderboardId, string context, UpdateType update, int score, params string[] info ) 
		{		
			if (Hub.State != HubState.Connected )
			{
				if ( update != UpdateType.Inc && update != UpdateType.Dec )
				{
					if (Hub.GameCenterManager != null)
					{
						Hub.GameCenterManager.ReportScore(leaderboardId, score);
					}
				}
				return;
			}
			
			var request = _ep.Post("/leaderboards/"+leaderboardId);
			request.AddData("score", score);
			request.AddData("info", info);
	        request.AddData("update", update.ToString().ToLower());
			request.AddData("nonce", Nonce.Generate() );
	
	        if (!string.IsNullOrEmpty(context))
	        {
	            request.AddData("context", context);
	        }
			
			_ep.Service(request, delegate(Response result){
				if (result.sucessful)
				{
					// report to game center if its there
					score = Dot.Integer("global.score", result.hashtable, score);
					if (Hub.GameCenterManager != null)
					{
						Hub.GameCenterManager.ReportScore(leaderboardId, score);
					}
				}
				
			});
			
		}
		
	}
	
}
