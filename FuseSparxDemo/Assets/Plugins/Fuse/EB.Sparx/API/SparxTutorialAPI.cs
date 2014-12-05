using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class TutorialAPI 
	{
		private readonly int TutorialAPIVersion = 1;
		
		private EndPoint _endPoint;
		
		public TutorialAPI( EndPoint endpoint )
		{
			_endPoint = endpoint;
		}
		
		void AddData( Request request )
		{
			request.AddData("api", TutorialAPIVersion );
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
		
		public void GetLoginData( EB.Action<string, Hashtable> cb )
		{
			var req = Post("/tutorial/get-login-data");
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					cb(null,res.hashtable);
				}
				else {
					cb(res.localizedError,null);
				}
			});
		}
		
		public void StartTutorial( string tutorialId, EB.Action<string, Hashtable> cb )
		{
			var req = Post("/tutorial/start-tutorial");
			req.AddData("tid", tutorialId);	
			
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					cb(null,res.hashtable);
				}
				else {
					cb(res.localizedError,null);
				}
			});
		}

		public void EarlyStartBranch( string tutorialId, string branchId, EB.Action<string, Hashtable> cb )
		{
			var req = Post("/tutorial/early-start-branch");
			req.AddData("tid", tutorialId);	
			req.AddData("bid", branchId);	
			
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					cb(null,res.hashtable);
				}
				else {
					cb(res.localizedError,null);
				}
			});
		} 
		
		public void StartBranch( string tutorialId, string branchId, EB.Action<string, Hashtable> cb )
		{
			var req = Post("/tutorial/start-branch");
			req.AddData("tid", tutorialId);	
			req.AddData("bid", branchId);	
			
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					cb(null,res.hashtable);
				}
				else {
					cb(res.localizedError,null);
				}
			});
		} 

		public void CompleteTutorial( string tutorialId, EB.Action<string, Hashtable> cb )
		{
			var req = Post("/tutorial/complete-tutorial");
			req.AddData("tid", tutorialId);	
			
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					cb(null,res.hashtable);
				}
				else {
					cb(res.localizedError,null);
				}
			});
		} 
	}
}		