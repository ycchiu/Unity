using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class WalletAPI
	{
		EndPoint _api;
		int		 _next = 0;
		int		 _done = 0;
		
		public WalletAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void Fetch( Action<string,Hashtable> callback ) 
		{
			Coroutines.Run(_Fetch(callback));
		}
		
		public int Credit( int value, string reason, Action<int,string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Call("credit",value,reason,callback));
			return id;
		}
		
		public int Debit( int value, string reason, Action<int,string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Call("debit",value,reason,callback));
			return id;
		}
		
		public void FetchPayouts( string platform, Action<string,Hashtable> callback )
		{
			var request = _api.Get("/store/payouts");
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
		
		public void FetchOfferUrl( string platform, string offerName, Action<string,string> callback )
		{
			var request = _api.Get("/store/offerurl");
			request.AddData("offer", offerName);
			request.AddData("platform", platform );
			_api.Service(request, delegate(Response result){
				if (result.sucessful)
				{
					callback(null,result.str);
				}
				else
				{
					callback(result.localizedError, null);
				}
			});
		}
		
		public void VerifyPayout( string platform, Hashtable data, Action<string,Hashtable> callback )
		{
			var request = _api.Post("/store/verify-payout");
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
		
		IEnumerator _Fetch( Action<string,Hashtable> callback )
		{
			var id = _next++;
			while (id != _done)
			{
				yield return 1;
			}
			
			var request = _api.Get("/wallet/balance");
			_api.Service(request, delegate(Response result){
					_done++;
					if (result.sucessful){
						callback(null,result.hashtable);
					}
					else if (result.error != null && result.error.ToString() == "nsf" )
					{
						callback("nsf", null);
					}
					else if (result.error != null && result.error.ToString() == "walletDisabled" )
					{
						callback("walletDisabled", null);
					}
					else {
						callback(result.localizedError, null);
					}
			});
		}
		
		IEnumerator _Call( string type, int value, string reason, Action<int, string,Hashtable> callback ) 
		{
			var id = _next++;
			while (id != _done)
			{
				yield return 1;
			}	
			
			var nonce = Nonce.Generate();
			
			var request = _api.Post("/wallet/"+type);
				
			if (value > 0)
			{
				request.AddData("value", value);
			}
			
			if (!string.IsNullOrEmpty(reason))
			{
				request.AddData("reason", reason);	
			}
			
			request.AddData("nonce", nonce);
			
			_api.Service(request, delegate(Response result){
				_done++;
				if (result.sucessful){
					callback(id, null,result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf" )
				{
					callback(id, "nsf", null);
				}
				else {
					callback(id, result.localizedError, null);
				}
			});
			
		}

		
	}
}
