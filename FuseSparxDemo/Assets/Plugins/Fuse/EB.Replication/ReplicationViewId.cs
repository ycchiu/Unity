using UnityEngine;
using System.Collections;

namespace EB.Replication
{
	public struct ViewId : ISerializable
	{
		public uint p;	// playerId
		public uint n;	// id
		public bool s;  // spawned in the scene;
		
		public override bool Equals(object other)
		{
			if (other is ViewId)
			{
				var o = (ViewId)other;
				return o.p == p && o.n == n;
			}
			return false;
		}
		
		public int CompareTo( ViewId other )
		{
			if ( p == other.p )
			{
				return n.CompareTo(other.n);
			}
			return p.CompareTo(other.p);
		}
		
		public override int GetHashCode ()
		{
			return (int)(p ^ n);
		}
		
		public override string ToString ()
		{
			return string.Format ("[ViewId] p:{0}, n:{1}", p, n);
		}
		
		public static bool operator==( ViewId a, ViewId b ) 
		{
			return a.p==b.p && a.n==b.n;
		}
		
		public static bool operator!=( ViewId a, ViewId b ) 
		{
			return a.p!=b.p || a.n!=b.n;
		}

		public void Serialize(BitStream bs)
		{
			bs.Serialize(ref p);
			bs.SerializeUInt24(ref n);
		}
	}
}