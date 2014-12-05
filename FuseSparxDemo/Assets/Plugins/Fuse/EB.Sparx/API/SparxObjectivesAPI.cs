using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ObjectivesAPI
	{
		private readonly int ObjectivesAPIVersion = 1;
	
		EndPoint _api;
		
		public ObjectivesAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void FetchObjectives( Action<string,Hashtable> callback )
		{
			EB.Sparx.Request request = this._api.Get("/objectives/fetch");
			request.AddData( "api", ObjectivesAPIVersion );
			this._api.Service( request, delegate( Response result ){
				if( result.sucessful == true )
				{
					callback( null, result.hashtable );
				}
				else
				{
					callback( result.localizedError, null );
				}
			});
		}

		public void ReportObjectives(List<ObjectiveReport> updates, Action<string,Hashtable> callback )
		{
			Hashtable data = new Hashtable();
			foreach(ObjectiveReport update in updates) 
			{
				if(update.IsValid) 
				{
					Hashtable u = new Hashtable();
					u["_id"] = update.Id;
					u["n"] = update.Increment;
					data[update.Category] = u;
				}
			}

			EB.Sparx.Request request = this._api.Post("/objectives/report");
			request.AddData( "api", ObjectivesAPIVersion );
			request.AddData( "updates", data );
			this._api.Service( request, delegate( Response result ){
				if( result.sucessful == true )
				{
					callback( null, result.hashtable );
				}
				else
				{
					callback( result.localizedError, null );
				}
			});
		}

		public void ResetObjectives( Action<string,Hashtable> callback )
		{
			EB.Sparx.Request request = this._api.Get("/objectives/reset");
			request.AddData( "api", ObjectivesAPIVersion );
			this._api.Service( request, delegate( Response result ){
				if( result.sucessful == true )
				{
					callback( null, result.hashtable );
				}
				else
				{
					callback( result.localizedError, null );
				}
			});
		}
	}
}
