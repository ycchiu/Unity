using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class StatsAPI
	{
		private readonly int APIVersion = 1;
		
		private EndPoint _endPoint;
		
		public StatsAPI( EndPoint endpoint )
		{
			_endPoint = endpoint;
		}
		
		void AddData( Request request )
		{
			request.AddData("api", APIVersion );
		}
		
		Request Get(string path) 
		{
			var req = _endPoint.Get(path);
			AddData(req);
			return req;
		}
		
		Request Post(string path) 
		{
			var req = _endPoint.Post(path);
			AddData(req);
			return req;
		}
		
		public void GetLoginData( EB.Action<string,Hashtable> callback )
		{
			var req = Get("/gamestats/get-login-data");
			
			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					callback(null,res.hashtable);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}
	}
}
