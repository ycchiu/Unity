using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class DefaultLoginListener : LoginListener
	{
		#region LoginListener implementation

		bool _linkGameCenter = false; // should we try to link game center on login

		protected Authenticator GetBest(Account account, Authenticator bestAuthenticator = null )
		{
			foreach( var authData in account.Authenticators )
			{
				if ( bestAuthenticator == null || (int)authData.Authenticator.Quailty > (int)bestAuthenticator.Quailty )
				{
					bestAuthenticator = authData.Authenticator;
				}
			}
			return bestAuthenticator;
		}

		protected Authenticator GetBest(Authenticator bestAuthenticator)
		{
			foreach( var authenticator in Hub.Instance.LoginManager.Authenticators )
			{
				if ( authenticator.IsLoggedIn && (int)authenticator.Quailty > (int)bestAuthenticator.Quailty )
				{
					bestAuthenticator = authenticator;
				}
			}
			return bestAuthenticator;
		}

		protected Authenticator FindAuthenticator( Account[] accounts, string name)
		{
			foreach( var account in accounts)
			{
				var auth = account.GetAuthData(name);
				if (auth != null)
				{
					return auth.Authenticator;
				}
			}
			return null;
		}

		protected Authenticator GetAuthenticator( string name )
		{
			return Hub.Instance.LoginManager.GetAuthenticator(name);
		}

		public virtual void SelectNewOrContinue( Account account ) 
		{
			// by default continue 
			var bestAuthenticator = GetBest(account, null);
			Hub.Instance.LoginManager.Login(bestAuthenticator);
		}

		public virtual void SelectAccount( Account[] accounts )
		{
			// by default, select the last logged in account
			var last = accounts[0];
			for ( int i = 1; i < accounts.Length; ++i )
			{
				if (accounts[i].User.LoginDate > last.User.LoginDate)
				{
					last = accounts[i];
				}
			}

			var best = GetBest(last);
			Login(best);
		}

		protected virtual void Login(Authenticator auth)
		{
			if (auth == null)
			{
				Debug.LogError("Error logging in with NULL authenticator, defaulting to device");
				auth = GetAuthenticator(DeviceAuthenticator.AuthName);
			}
			Hub.Instance.LoginManager.Login(auth);
		}

		public virtual void OnEnumerate (Account[] accounts)
		{
			// setup the gc link flag
			_linkGameCenter = false;
			if (FindAuthenticator(accounts,GameCenterAuthenticator.AuthName) == null)
			{
				var gcAuth = GetAuthenticator(GameCenterAuthenticator.AuthName);
				if (gcAuth != null && gcAuth.IsLoggedIn)
				{
					_linkGameCenter = true;
				}
			}


			var bestAuthenticator = GetAuthenticator(DeviceAuthenticator.AuthName);

			if (accounts.Length == 0)
			{
				// no accounts, login with the best one
				bestAuthenticator = GetBest(bestAuthenticator);
				Hub.Instance.LoginManager.Login(bestAuthenticator);
			}
			else if (accounts.Length == 1)
			{
				// check to see if we have played on this device before
				var deviceAuth = accounts[0].GetAuthData(DeviceAuthenticator.AuthName);
				if (deviceAuth != null)
				{
					// already played on this device, continue
					bestAuthenticator = GetBest(accounts[0], bestAuthenticator);
					Hub.Instance.LoginManager.Login(bestAuthenticator);
				}
				else
				{
					// select new or continue
					SelectNewOrContinue(accounts[0]);
				}
			}
			else
			{
				// who knows? let the user select
				SelectAccount(accounts);
			}
		}

		public virtual void OnLoggedIn ()
		{
			EB.Util.BroadcastMessage("OnLoggedIn");

			if (_linkGameCenter)
			{
				_linkGameCenter = false;
				Debug.Log("Trying to link account to game center");
				Hub.Instance.LoginManager.Link(GameCenterAuthenticator.AuthName);
			}

		}

		public virtual void OnLoginFailed (string error)
		{
			EB.Util.BroadcastMessage("OnLoginFailed", error);
		}

		public virtual void OnDisconnected (string error)
		{
			EB.Util.BroadcastMessage("OnDisconnected", error);
		}

		public virtual void OnUpdateRequired(string storeUrl)
		{
			EB.Util.BroadcastMessage("OnUpdateRequire", storeUrl);
		}

		public virtual void OnLinkFailed(string error)
		{
			EB.Util.BroadcastMessage("OnLinkFailed", error);
		}

		public virtual void OnLinkSuccess(string authenticatorName)
		{
			EB.Util.BroadcastMessage("OnLinkSuccess", authenticatorName);
		}

		public virtual void OnTermsOfService( object data )
		{
			EB.Debug.LogError("Warning auto-accepting terms of service {0}", data);
			SparxHub.Instance.TosManager.Accept();
		}
		
		
		#endregion
	}
		
	
}
