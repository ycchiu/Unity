using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class SodaAPI
	{
		private readonly int SodaAPIVersion = 1;
	
		EndPoint _api;
		
		public SodaAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void GenerateCertificate( Action<string,string> cb )
		{
			EB.Sparx.Request request = this._api.Post("/wske/cert");
			request.AddData("api", SodaAPIVersion );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.str);
				}
				else
				{
					EB.Debug.LogError( "GenerateCertificate Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
		
		public void RedeemLoyaltyReward( SparxSODA.Reward reward, Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/wske/redeem");
			request.AddData("api", SodaAPIVersion );
			request.AddData("signature", reward.Signature );
			request.AddData("receipt", reward.Receipt );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.LogError( "RedeemLoyaltyReward Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
	}
}
