using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class BasicMessage : MessengerMessage
	{
		public BasicMessage( string cdn, Hashtable data = null )
			:
		base( cdn, data )
		{
			this.Image = string.Empty;
			this.Body = string.Empty;
			
			if( data != null )
			{
				this.Image = EB.Dot.String( "imageUrl", data, string.Empty );
				this.Body = EB.Dot.String( "body", data, string.Empty );
			}
		}
		
		public string Image { get; private set; }
		public string Body { get; private set; }
		
		public override string ToString ()
		{
			return string.Format( "{0} Image:{1} Body:{2} ", base.ToString(), this.Image, this.Body );
		}
	}
}
