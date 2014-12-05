using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class MatchAPI
	{
		private readonly int APIVersion = 1;
		
		private EndPoint _endPoint;
		
		public MatchAPI( EndPoint endpoint )
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
		
		public void ActivateMatch<T>( string matchType, T matchData, EB.Action<Request, T> convertMatchData, EB.Action<string,Hashtable> callback )
		{
			var req = Post("/matches/activate-match");
			req.AddData("type", matchType );
			convertMatchData( req, matchData );
		
			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					callback(null,res.hashtable);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}
		
		public void ResolveMatch<T1, T2>( string matchType, string matchID, T1 matchResults, EB.Action<Request, T1> convertMatchResults, T2 matchStats, EB.Action<Request, T2> convertMatchStats, EB.Action<string,Hashtable> callback )
		{
			var req = Post("/matches/resolve-match");
			req.AddData("type", matchType );
			req.AddData("id", matchID );
			convertMatchResults( req, matchResults );
			convertMatchStats( req, matchStats );
						
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
