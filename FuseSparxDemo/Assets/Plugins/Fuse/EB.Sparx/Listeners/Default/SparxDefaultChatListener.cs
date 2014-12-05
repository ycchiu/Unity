using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class DefaultChatListener : ChatListener
	{
		#region ChatListener implementation
		public void OnConnected ()
		{

		}
		public void OnDisconnected (string error)
		{

		}

		public void OnUpdated(string channel)
		{
			Debug.Log("Chat List updated: " + channel);
		}

		public void OnJoin (string channel)
		{
			Debug.Log("Chat List joined: " + channel);
		}

		public void OnLeave (string channel)
		{
			Debug.Log("Chat List left: " + channel);
		}
		#endregion
	}
		
	
}
