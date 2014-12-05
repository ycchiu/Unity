using System.Collections;

namespace EB.Sparx
{
	public class DeviceAuthenticator : Authenticator
	{
		public static string AuthName = "device";

		#region Authenticator implementation
		public void Init (object initData, Action<string, bool> callback)
		{
			callback(null,true);
		}

		public void Authenticate (bool silent, Action<string, object> callback)
		{
			var data = new Hashtable();
			data["udid"] = Device.UniqueIdentifier;
			callback(null,data);
		}

		public string Name {
			get {
				return AuthName;
			}
		}

		public bool IsLoggedIn {
			get {
				return true;
			}
		}

		public AuthenticatorQuailty Quailty {
			get {
				return AuthenticatorQuailty.Low;
			}
		}
		#endregion
	}

}

