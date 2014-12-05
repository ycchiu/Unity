using UnityEngine;
using System.Collections;

namespace EB.Replication
{
	public class GameListener : EB.Sparx.GameListener
	{
		#region GameListener implementation
		public void OnJoinedGame (EB.Sparx.Game game)
		{
			Manager.SetGame(game);
			EB.Util.BroadcastMessage("OnJoinedGame");
		}

		public void OnLeaveGame (EB.Sparx.Game game, string reason)
		{
			Manager.ClearGame();
			EB.Util.BroadcastMessage("OnLeaveGame", reason);
		}
		
		public void OnUpdate( EB.Sparx.Game game)
		{
			Manager.Update();
		}

		public void OnPlayerJoined (EB.Sparx.Game game, EB.Sparx.Player player)
		{
			EB.Util.BroadcastMessage("OnPlayerJoined", player);
		}

		public void OnPlayerLeft (EB.Sparx.Game game, EB.Sparx.Player player)
		{
			Manager.OnPlayerLeft(game,player);
			
			EB.Util.BroadcastMessage("OnPlayerLeft", player);
		}

		public void OnAttributesUpdated (EB.Sparx.Game game)
		{
			EB.Util.BroadcastMessage("OnAttributesUpdated");
		}

		public void OnReceive (EB.Sparx.Game game, EB.Sparx.Player player, EB.Sparx.Packet packet)
		{
			Manager.Receive( player != null ? player.PlayerId : EB.Sparx.Network.HostId, packet.Data );  
		}

		public void OnGameStarted (EB.Sparx.Game game)
		{
			EB.Util.BroadcastMessage("OnGameStarted");
		}

		public void OnGameEnded (EB.Sparx.Game game)
		{
			EB.Util.BroadcastMessage("OnGameEnded");
		}

		public void OnJoinGameFailed (string err)
		{
			EB.Util.BroadcastMessage("OnJoinGameFailed", err);
		}
		#endregion
		
		
	}

}

