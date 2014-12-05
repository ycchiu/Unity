using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class TosManager : SubSystem
	{
		LoginListener _loginListener;
		Hashtable _data = null;
		#region implemented abstract members of Manager

		public override void Initialize (Config config)
		{
			_loginListener = config.LoginConfig.Listener;
		}

		#endregion

		#region implemented abstract members of SubSystem

		public override void Connect ()
		{
			_data = Dot.Object("tos", Hub.LoginManager.LoginData, null);
			if (_data != null )
			{
				_loginListener.OnTermsOfService(_data);
			}
			else
			{
				State = SubSystemState.Connected;
			}
		}

		public void GetToS(EB.Action<string,object> cb)
		{
			var req = Hub.ApiEndPoint.Get("/tos");
			Hub.ApiEndPoint.Service(req, delegate(Response obj) {
				if (obj.sucessful)
				{
					cb(null,obj.result);
				}
				else
				{
					cb(obj.localizedError,null);
				}
			});
		}

		public void Accept()
		{
			var req = Hub.ApiEndPoint.Post("/tos/accept");
			req.AddData( "version", Dot.Integer("version", _data, 0) ); 
			Hub.ApiEndPoint.Service(req, delegate(Response obj) {
				if (obj.sucessful)
				{
					State = SubSystemState.Connected;
				}
				else
				{
					FatalError(obj.localizedError);
				}
			});
		}

		public override void Disconnect (bool isLogout)
		{
		}

		#endregion


	}

}
