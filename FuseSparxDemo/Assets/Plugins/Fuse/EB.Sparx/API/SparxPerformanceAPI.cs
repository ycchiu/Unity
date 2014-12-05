using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class PerformanceAPI
	{
		private readonly int PerformanceAPIVersion = 1;
	
		EndPoint _api;
		
		public PerformanceAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void Fetch( string device, string CPU, string GPU, int platform, Action<string,object> callback )
		{
			EB.Sparx.Request request = this._api.Get("/performance/profile");
			request.AddData("device", device);
			request.AddData("cpu", CPU);
			request.AddData("gpu", GPU);
			request.AddData("platform", platform);
			
			this._api.Service( request, delegate( Response res ){
				if( res.sucessful == true )
				{
					callback( null, res.result );
				}
				else
				{
					callback( res.localizedError, null );
				}
			});
		}
	}
}
