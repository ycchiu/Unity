using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ResourcesAPI
	{
		private readonly int ResourcesAPIVersion = 1;
		
		EndPoint _api;
		
		public ResourcesAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void FetchStatus( Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/resources/fetch");
			request.AddData("api", ResourcesAPIVersion );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ExecuteResourcesFetchStatus Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
		
		public void DebugAddResource( string type, int amount, Action<string,Hashtable> cb )
		{
			EB.Sparx.Request request = this._api.Post("/resources/unittest-add-resource");
			request.AddData("api", ResourcesAPIVersion );
			request.AddData( "type", type );
			request.AddData( "amount", amount );
			request.AddData( "reason", "game|debug" );
			
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ExecuteResourcesFetchStatus Error: {0}", result.localizedError );
					cb(result.localizedError, null);
				}
			});
		}
	}
}

