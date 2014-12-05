using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class WebViewAPI
	{
		private readonly int WebViewAPIVersion = 1;
	
		EndPoint _api;
		
		public WebViewAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void FetchTabConfiguration( Action<string,Hashtable> callback )
		{
			EB.Sparx.Request statusRequest = this._api.Get("/webview/configure");
			statusRequest.AddData( "api", WebViewAPIVersion );
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
