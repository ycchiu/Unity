using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public abstract class MOTDTemplateA
	{
		public abstract bool IsValid { get; }
		
		public MOTDTemplateA( string cdn, Hashtable data = null )
		{
			this.CDN = cdn;
			if( data != null )
			{
				this.CDN = EB.Dot.String( "cdn", data, string.Empty );
			}
		}
		
		public string CDN { get; private set; }
		
		public override string ToString ()
		{
			return string.Format( "Type:{0} CDN:{1}", this.GetType(), this.CDN );
		}
	}

	public class MOTDAPI
	{
		private readonly int MOTDAPIVersion = 1;
	
		EndPoint _api;
		
		public MOTDAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void FetchStatus( bool templates, Action<string,Hashtable> callback )
		{
			EB.Sparx.Request statusRequest = this._api.Get("/motd/status");
			statusRequest.AddData( "api", MOTDAPIVersion );
			statusRequest.AddData( "templates", templates );
			this._api.Service( statusRequest, delegate( Response result ){
				if( result.sucessful == true )
				{
					callback( null, result.hashtable );
				}
				else
				{
					callback( result.localizedError, null );
				}
			});
		}
	}
}
