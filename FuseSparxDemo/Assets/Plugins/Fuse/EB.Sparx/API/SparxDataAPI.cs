using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class DataAPI
	{
		EndPoint _api;
		
		public DataAPI( EndPoint api )
		{
			_api = api;
		}
		
		public void Load( Id userId, string key, Action<string,Hashtable> callback ) 
		{
			var request = _api.Get( string.Format("/ds/{0}/{1}", userId, key) );
			_api.Service( request, delegate(Response result){
				if (result.sucessful)
				{
					callback(null,result.hashtable);
					return;
				}
				callback(result.localizedError, null);
			});
		}
		
		public void Save( Id userId, Hashtable data, Action<string> callback )
		{
			var request = _api.Post( string.Format("/ds/{0}", userId) );
			request.AddData("data", data);
			_api.Service( request, delegate(Response result){
				if (result.sucessful)
				{
					callback(null);
					return;
				}
				callback(result.localizedError);
			});
		}
		
	}
}

