using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class Player
	{
		public Id UserId {get;private set;}
		public uint PlayerId {get;private set;}
		public Hashtable Attributes {get;private set;}
		public int Index {get;set;}
		
		public bool IsHost { get { return Index == 0; } }
		
		public Player( Id uid, uint connId ) 	
		{
			this.UserId = uid;
			this.PlayerId = connId;
			this.Attributes = new Hashtable();
		}
		
		public Player( object obj )
		{
			this.Attributes = new Hashtable();
			this.UserId		= new Id( Dot.Find("uid", obj) );
			this.PlayerId 	= Dot.UInteger("id", obj, PlayerId);
			
			Update(obj);
		}
		
		public void Update( object obj ) 
		{
			this.Attributes = Dot.Object("attributes", obj, this.Attributes );
		}
		
		public override string ToString ()
		{
			return Debug.Format ("[Player: Id={0}, ConnId={1}, Index={2}, Attributes={3}]", UserId, PlayerId, Index, Attributes);
		}
	}
	
}