using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public static class Nonce
	{
		static short inc = 0;
		static short pid = 0;
		static int 	 device = 0;
		
		// generate a nonce (in this case its just a mongo objectId)
		public static string Generate()
		{
			var buffer = new Buffer(12);
			
			buffer.WriteInt32BE( Time.Now );
			
			if (device == 0)
			{
				device = Hash.FNV32(Device.UniqueIdentifier);
			}
			buffer.WriteInt32BE( (int)device ); 
			
			if (pid == 0)
			{
				pid = (short)Random.Range(1,short.MaxValue);
			}
			buffer.WriteInt16BE(pid);
			
			// write inc
			buffer.WriteInt16BE(inc++);
			
			var hex = buffer.ToHexString().ToLower();
			return hex;
		}
	}
}

