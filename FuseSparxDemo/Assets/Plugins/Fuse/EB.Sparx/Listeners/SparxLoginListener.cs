using UnityEngine;
using System.Collections;

namespace EB.Sparx
{	
	public interface LoginListener 
	{
		// user is logged in
		void OnEnumerate( Account[] accounts ); 
		void OnLoggedIn();
		void OnLoginFailed(string error);
		void OnLinkFailed(string error);
		void OnLinkSuccess(string authenticatorName);
		void OnDisconnected(string error);
		void OnUpdateRequired(string storeUrl);
		void OnTermsOfService(object data);
	}
}


