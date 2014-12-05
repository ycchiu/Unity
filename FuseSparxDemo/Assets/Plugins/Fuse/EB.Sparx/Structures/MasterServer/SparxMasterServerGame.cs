using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class MasterServerGame
	{
		public long  Id 	{get;private set;}
		public int PlayerCount {get;private set;}
		public int MaxPlayers {get;private set;}
		public Hashtable Attributes {get;private set;}
		
		public MasterServerGame( object data )		
		{
			Id = Dot.Long("id", data, 0);
			PlayerCount = Dot.Integer("player_count", data, 0);
			MaxPlayers = Dot.Integer("max_players", data, 0);
			Attributes = Dot.Object("attributes", data, new Hashtable( ) ); 
		}
		
		public override string ToString ()
		{
			return Debug.Format ("[MasterServerGame: Id={0}, PlayerCount={1}, MaxPlayers={2}, Attributes={3}]", Id, PlayerCount, MaxPlayers, Attributes);
		}
	}
}
