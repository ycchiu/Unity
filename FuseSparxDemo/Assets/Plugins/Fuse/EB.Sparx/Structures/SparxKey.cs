using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class Key 
	{
		public byte[] Value { get;private set;}
		
		public Key( string secret )
		{
			Value = Crypto.CreateKey(secret);
		}
	}
}


