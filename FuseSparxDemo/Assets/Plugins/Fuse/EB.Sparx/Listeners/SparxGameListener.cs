using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface GameListener 
	{
		void OnJoinedGame( Game game );
		void OnLeaveGame( Game game, string reason );
		void OnPlayerJoined( Game game, Player player );
		void OnPlayerLeft( Game game, Player player );
		void OnAttributesUpdated( Game game );
		void OnReceive( Game game, Player player, Packet packet );
		void OnGameStarted( Game game );
		void OnGameEnded( Game game );
		void OnJoinGameFailed( string err );
		void OnUpdate(Game game);
	}
}
