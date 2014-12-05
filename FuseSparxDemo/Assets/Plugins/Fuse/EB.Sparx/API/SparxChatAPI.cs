using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class ChatAPI
	{
		EndPoint _api;
		
		public ChatAPI( EndPoint api )
		{
			_api = api;	
		}
		
		public void GetChatToken( Action<string,Hashtable> callback )
		{
			var request = _api.Post("/chat/token");
			_api.Service( request, delegate( Response result ){
				if ( result.sucessful)
				{
					callback(null, result.hashtable);
				}
				else
				{
					callback(result.localizedError,null);
				}
			});
		}
	
		
	}
}
