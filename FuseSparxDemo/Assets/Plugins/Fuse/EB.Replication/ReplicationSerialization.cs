using UnityEngine;
using System.Collections;

namespace EB
{
	public partial class BitStream
	{
		public void Serialize( ref Replication.ViewId data )
		{
			Serialize( ref data.p );
			SerializeUInt24( ref data.n );
		}
	}
}

