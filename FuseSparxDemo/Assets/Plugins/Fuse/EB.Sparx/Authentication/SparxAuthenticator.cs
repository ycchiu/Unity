

namespace EB.Sparx
{
	public enum AuthenticatorQuailty
	{
		Low = 0,	// low quailty: only good for a single device
		Med = 1,	// med quailty: only good for a single platform
		High = 2,	// high quality: good for cross platform support (ie facebook/g+)
	}

	public interface Authenticator 
	{
		string 					Name 		{get;}
		bool					IsLoggedIn 	{get;}
		AuthenticatorQuailty	Quailty		{get;}

		void 		Init( object initData, Action<string,bool> callback );
		void 		Authenticate( bool silent, Action<string,object> callback );
	}

}

