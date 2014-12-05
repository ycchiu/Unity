using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class LoginRewardsAPI
	{
		private readonly int LoginRewardsAPIVersion = 1;
	
		EndPoint _api;
		
		public LoginRewardsAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void ClaimReward( int x, int y, Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/loginrewards/claim");
			request.AddData("api", LoginRewardsAPIVersion );
			request.AddData("x", LoginRewardsAPIVersion );
			request.AddData("y", LoginRewardsAPIVersion );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ExecuteLoginRewardStep Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
		
		public void FetchLoginRewardsStatus( Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/loginrewards/fetch");
			request.AddData("api", LoginRewardsAPIVersion );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ExecuteLoginRewardStep Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
	}
}
