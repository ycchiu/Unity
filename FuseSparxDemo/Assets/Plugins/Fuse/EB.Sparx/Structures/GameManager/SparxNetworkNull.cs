using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class NetworkNull : Network
	{
		private uint _connId;
		
		#region implemented abstract members of EB.Sparx.Network
		public override void Connect (string url, uint connId)
		{
			_connId = connId;
			DispatchConnect(); 
		}

		public override void Disconnect (bool now)
		{
			DispatchDisconnect(string.Empty);
		}

		public override void SendTo (uint playerId, Packet packet)
		{
			// check for game commands
			if ( playerId == HostId)
			{
				if ( packet.Channel == Channel.Manager_Reliable )
				{
					var text = packet.Utf8Data;
					var message = text;
					var index = text.IndexOf(':');
					object payload = null;
					if (index>=0)
					{
						message = text.Substring(0,index);
						
						try {
							payload = JSON.Parse( text.Substring(index+1) ); 
						}
						catch {}
					}
					HandleGameCommand(message, payload);					
				}
			}
			else if ( playerId == _connId )
			{
				DispatchReceive(playerId, packet);
			}
		}

		public override void Broadcast (Packet packet)
		{
			// nothing
		}

		public override void Update ()
		{
			// nothing
		}
		
		Hashtable CreateLocalPlayer()
		{
			var player = new Hashtable();
			player["uid"] = Hub.Instance.LoginManager.LocalUserId.ToString();
			player["id"] = _connId;
			return player;			
		}
		
		void HandleGameCommand( string message, object payload )
		{
			switch(message)
			{
			case "hello":
				{
					// send out fake roster
					var players = new ArrayList();
					players.Add(CreateLocalPlayer());
					ReturnGameCommand("roster", players);
				}
				break;
			case "start":
				{
					ReturnGameCommand(message, payload);
				}
				break;
			case "ai":
				{
					var players = new ArrayList();
					players.Add(CreateLocalPlayer());
					// KL: workaround for trampoline @#$%
					IEnumerator en = EB.AOT.GetEnumerator(payload);
					while (en.MoveNext())
					{
						players.Add (en.Current);
					}
					ReturnGameCommand("roster", players);
				}
				break;
			default:
				break;
			}
		}
		
		void ReturnGameCommand( string message, object payload )
		{
			var data = message;
			if ( payload != null )
			{
				data += ":" + JSON.Stringify(payload);
			}
			DispatchReceive( HostId, new Packet(Channel.Manager_Reliable, data) ); 
		}

		public override NetworkStats Stats 
		{
			get 
			{
				return new NetworkStats();
			}
		}
		
		public override bool IsLocal {
			get {
				return true;
			}
		}
		#endregion
		
	}
}


