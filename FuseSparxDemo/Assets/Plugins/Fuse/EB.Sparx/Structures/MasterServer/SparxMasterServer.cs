using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class MasterServer 
	{		
		public string EndPoint { get; private set;  }
		public string Token { get; private set;  }
		public string Name { get; private set;  }
		public string Hostname{ get; private set;  }
		public int 	  PingPort {get;set;}
		public int 	  Ping {get;set;}
		public bool   Down { get { return Ping < 0; } }
		
		public MasterServerGame[] Games {get;set;}
		
		
		public int GameCount 	= 0;
		public int PlayerCount	= 0;
		
		public MasterServer( object data )
		{
			EndPoint = Dot.String("endpoint", data, string.Empty);
			Token = Dot.String("token", data, string.Empty);
			Name= Dot.String("name", data, string.Empty);
			PingPort = Dot.Integer("pingPort", data, 0);
			Ping = 0; // unknown
			
			if (!string.IsNullOrEmpty(EndPoint))
			{
				Hostname = new EB.Uri(EndPoint).Host;
			}
			
			Games = new MasterServerGame[0];
		}
		
		public EndPoint CreateEndPoint()
		{
			return EndPointFactory.Create(EndPoint, new EndPointOptions{ Key=Encoding.GetBytes(Token), Protocol="io.sparx.master-client", ActivityTimeout=30*1000 });
		}
		
		public override bool Equals (object obj)
		{
			if ( obj is MasterServer)
			{
				var other = (MasterServer)obj;
				if (other.EndPoint == EndPoint)
				{
					return true;
				}
			}
			return false;
		}
		
		public int CompareTo( MasterServer other )
		{
			if ( Down != other.Down )
			{
				return Down ? 1 : -1;
			}
			
			if ( Ping != other.Ping )
			{
				return Ping < other.Ping ? -1 : 1;
			}
			return 0;
		}
		
		public override int GetHashCode ()
		{
			return EndPoint.GetHashCode();
		}
		
		public override string ToString ()
		{
			return Debug.Format ("[MasterServer: EndPoint={0}, Players={1}, Name={2}, Ping={3}]", EndPoint, PlayerCount, Name, Ping);
		}
		
	}
}

