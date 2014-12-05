using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class LoginAPI 
	{
		private EndPoint _endPoint;
		private Hashtable _deviceInfo;

		public const string LoginDataUri = "/account/data";

		public LoginAPI( EndPoint endpoint )
		{
			_endPoint = endpoint;
			_deviceInfo = Version.GetDeviceInfo();
			_deviceInfo["udid"] = Device.UniqueIdentifier;
		}

		void AddData( Request request )
		{
			request.AddData("platform", Device.MobilePlatform );
			request.AddData("device", _deviceInfo );
			request.AddData("version", Version.GetVersion() );
			request.AddData("locale", Version.GetLocale() );
			request.AddData("lang", Localizer.GetSparxLanguageCode(Hub.Instance.Config.Locale) );
			request.AddData("cellular", Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ? 1 : 0 );
			request.AddData("tz", Version.GetTimeZoneOffset() );
		}
		
		Request Post(string path) {
			var req = _endPoint.Post(path);
			AddData(req);
			return req;
		}

		public void Init( Action<string,object> callback )
		{
			var req = Post ("/auth/init");
			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					callback(null,res.result);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}

		public void Enumerate( object authData, Action<string,ArrayList> callback )
		{
			var req = Post ("/auth/enumerate");
			req.AddData("auth", authData);
			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					callback(null,res.arrayList);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}

		public void PreLogin( Action<string,object> callback )
		{
			var req = Post ("/auth/prelogin");
			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					callback(null,res.arrayList);
				}
				else {
					callback(res.localizedError,res.result);
				}
			});
		}

		public void Login(string authenticator, object credentials, Hashtable addData, Action<string,object> callback )
		{
			var req = Post ("/auth/login");
			req.AddData("authenticator", authenticator);
			req.AddData("credentials", credentials);

			if (addData != null)
			{
				req.AddData(addData);
			}

			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {

					var stoken = Dot.String("stoken", res.hashtable, null);
					
					if (!string.IsNullOrEmpty(stoken))
					{
						_endPoint.AddData(string.Empty, "stoken", stoken);
						BugReport.AddData("stoken", stoken);
					}

					callback(null,res.hashtable);
				}
				else {
					callback(res.localizedError,res.result);
				}
			});
		}

		public void Link(string authenticator, object credentials, Action<string,object> callback )
		{
			var req = Post ("/account/link");
			req.AddData("authenticator", authenticator);
			req.AddData("credentials", credentials);

			_endPoint.Service(req, delegate (Response res) {
				if (res.sucessful) {
					
					var stoken = Dot.String("stoken", res.hashtable, null);
					
					if (!string.IsNullOrEmpty(stoken))
					{
						_endPoint.AddData(string.Empty, "stoken", stoken);
						BugReport.AddData("stoken", stoken);
					}
					
					callback(null,res.hashtable);
				}
				else {
					callback(res.localizedError,res.result);
				}
			});
		}

		public void LoginData( Action<string,Hashtable> callback )
		{
			var req = _endPoint.Get(LoginDataUri);
			
			var apiVersions = new Hashtable();
			foreach( var manager in Hub.Instance.Managers )
			{
				if( manager.Version > 1 )
				{
					apiVersions[ manager.Name ] = manager.Version;
				}
			}
			req.AddData( "apiversions", apiVersions );
			
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					callback(null,res.hashtable);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}

		public void SetName( string name, Action<string, object> callback )
		{
			var req = _endPoint.Post("/account/name");
			req.AddData("name", name);
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					callback(null, res.hashtable);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}
		
		public void GetSupportUrl( Action<string, object> callback )
		{
			var req = _endPoint.Get("/account/support");
			_endPoint.Service(req, delegate(Response res) {
				if (res.sucessful) {
					callback(null, res.hashtable);
				}
				else {
					callback(res.localizedError,null);
				}
			});
		}
	}
}
