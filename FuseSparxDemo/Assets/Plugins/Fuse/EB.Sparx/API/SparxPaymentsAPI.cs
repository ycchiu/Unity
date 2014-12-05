using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class PaymentsAPI
	{
		EndPoint _api;

		public PaymentsAPI( EndPoint api )		
		{
			_api = api;
		}
	
		public void FetchPayouts( string platform, Action<string,Hashtable> callback )
		{
			var request = _api.Get("/payments/payouts");
			request.AddData("platform", platform);
			request.AddData("version", EB.Version.GetVersion() );
			_api.Service(request, delegate(Response result){
				if (result.sucessful)
				{
					callback(null,result.hashtable);
				}
				else
				{
					callback(result.localizedError, null);
				}
			});
		}

		public void VerifyPayout( string platform, Hashtable data, Action<string,Hashtable> callback )
		{
			var request = _api.Post("/payments/verify-payout");
			request.AddData("data", data);
			request.AddData("platform", platform);
			_api.Service(request, delegate(Response result){
				if (result.sucessful)
				{
					callback(null,result.hashtable);
				}
				else
				{
					callback(result.localizedError, null);
				}
			});
		}

	}
}
