using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class PushAPI
	{
		EndPoint _api;
		
		public PushAPI( EndPoint api )
		{
			_api = api;	
		}
		
		public void GetPushToken( Action<string,Hashtable> callback )
		{
			var request = _api.Get("/push/token");
			_api.Service( request, delegate( Response result ){
				if ( result.sucessful)
				{
					callback(null, result.hashtable);
				}
				else
				{
					callback(result.localizedError,null);
				}
			});
		}
		
		public void SetApplePushToken( string token, Action<string> callback )
		{
			var request = _api.Post("/push/apple");
			request.AddData("token", token);
			_api.Service( request, delegate( Response result ){
				if ( result.sucessful)
				{
					callback(null);
				}
				else
				{
					callback(result.localizedError);
				}
			});
		}
		
	}
}
