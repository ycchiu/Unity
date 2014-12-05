using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class TiltedSale : MOTDTemplateA
	{
		public TiltedSale( string cdn, Hashtable data = null )
			:
		base( cdn, data )
		{
		}
		
		public override bool IsValid{ get{ return true; } }
	}
}
