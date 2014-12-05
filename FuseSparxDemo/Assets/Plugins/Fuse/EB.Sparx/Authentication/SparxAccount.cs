using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class AuthData
	{
		public Authenticator Authenticator {get;private set;}
		public string Id { get; private set; }
		public object Data { get; private set; }

		public AuthData(string name, object obj)
		{
			this.Authenticator = Hub.Instance.LoginManager.GetAuthenticator(name);
			this.Id = Dot.String("id", obj, string.Empty);
			this.Data = Dot.Object("data", obj, null);
		}
	}

	public class Account
	{
		public User User 					{get; private set;}
		public AuthData[] Authenticators 	{get; private set;}

		public AuthData GetAuthData( string name )
		{
			foreach( var auth in Authenticators )
			{
				if (auth.Authenticator.Name == name)
				{
					return auth;
				}
			}
			return null;
		}

		public Account( object obj )
		{
			var user = Dot.Object("user", obj, null);
			if (user != null)
			{
				this.User = Hub.Instance.UserManager.GetUser(user);
			}

			var auth = Dot.Object("auth", obj, null);
			if (auth != null)
			{
				var list = new List<AuthData>();
				foreach( DictionaryEntry entry in auth)
				{
					var authData = new AuthData(entry.Key.ToString(), entry.Value);
					list.Add(authData);
				}
				Authenticators = list.ToArray();
			}
			else
			{
				Authenticators = new AuthData[0]{};
			}

		}
	}

}
