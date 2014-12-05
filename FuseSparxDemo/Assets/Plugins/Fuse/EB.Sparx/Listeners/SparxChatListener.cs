using UnityEngine;
using System.Collections;

namespace EB.Sparx
{	
	public interface ChatListener 
	{
		// user is logged in
		void OnConnected();
		void OnDisconnected(string error);
		void OnUpdated(string channel);
		void OnJoin(string channel);
		void OnLeave(string channel);
	}
}


