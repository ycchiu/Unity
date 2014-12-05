using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class LevelRewardsAPI
	{
		private readonly int LevelRewardsAPIVersion = 1;
		
		EndPoint _api;
		
		public LevelRewardsAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void FetchStatus( Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/levelrewards/fetch");
			request.AddData("api", LevelRewardsAPIVersion );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ExecuteLevelRewardsFetchStatus Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
	}
}

