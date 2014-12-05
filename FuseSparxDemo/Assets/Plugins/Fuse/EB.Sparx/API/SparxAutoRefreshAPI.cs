using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class AutoRefreshAPI
	{
		private readonly int kNoRefreshTime = -2;
	
		private EndPoint EndPoint = null;

			
		public AutoRefreshAPI( EndPoint endpoint )		
		{
			this.EndPoint = endpoint;
		}
		
		
		
		public void Refresh( string name, int api, string checkHash, EB.Action< string, string, int, Hashtable > cb )
		{
			string url = "/autorefresh/" + name + "/refresh";
			EB.Sparx.Request request = this.EndPoint.Get( url );
			request.AddData( "api", api );
			request.AddData( "hash", checkHash );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnRefreshData( name, response.hashtable, delegate( string err, string updatedCheck, int nextRefresh, Hashtable data ) {
						cb( err, updatedCheck, nextRefresh, data );
					});
				}
				else
				{
					cb( response.error.ToString(), string.Empty, kNoRefreshTime, new Hashtable() );
				}
			});
		}
		
		public void OnRefreshData( string name, Hashtable data, EB.Action< string, string, int, Hashtable > cb )
		{		
			string updatedCheck = EB.Dot.String( "check", data, null );
			if( string.IsNullOrEmpty( updatedCheck ) == true )
			{
				cb( "nocheck", string.Empty, kNoRefreshTime, new Hashtable() );
				return;
			}
			
			int nextRefresh = EB.Dot.Integer( "refresh", data, kNoRefreshTime );
			if( nextRefresh == kNoRefreshTime )
			{
				cb( "norefresh", string.Empty, kNoRefreshTime, new Hashtable() );
				return;
			}
			
			Hashtable info = EB.Dot.Object( name, data, null );	
			cb( null, updatedCheck, nextRefresh, info );
		}
	}
}
