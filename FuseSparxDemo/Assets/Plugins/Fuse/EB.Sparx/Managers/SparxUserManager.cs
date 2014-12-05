using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class UserManager : Manager
	{
		private Dictionary<Id,User> _users = new Dictionary<Id,User>();
				
		public override void Initialize (Config config)
		{
		
		}
						
		public User this[Id id]
		{
			get
			{
				User u;
				_users.TryGetValue(id, out u);
				return u;
			}
		}
		
		public User GetUser( Hashtable data ) 
		{
			var id = new Id( Dot.Find("uid",data) );
			if ( id.Valid )
			{
				User user;
				if (!_users.TryGetValue(id, out user))
				{
					EB.Debug.Log("Created user : " + id + " {0}", data);
					user = new User(id);
					_users[user.Id] = user;
				}
				user.Update(data);
				return user;
			}
			return null;
		}
		
	}
	
}

