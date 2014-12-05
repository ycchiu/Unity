using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class GameCenterAPI 
	{
		EndPoint _ep;
		
		public GameCenterAPI( EndPoint ep)		
		{
			_ep = ep;
		}
		
		public void SetGameCenterId( string gameCenterId, string gameCenterName, EB.Action<string> callback ) 
		{
			var request = _ep.Post("/gamecenter/id");
			request.AddData("gcid", gameCenterId);
			request.AddData("gcname", gameCenterName);
			_ep.Service(request, delegate(Response result){
				if (result.sucessful) {
					callback(null);
				}
				else {
					callback(result.localizedError);
				}
			});
		}
		
	}
	
}

