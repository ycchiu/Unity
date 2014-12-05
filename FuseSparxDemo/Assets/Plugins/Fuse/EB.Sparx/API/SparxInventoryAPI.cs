using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class InventoryAPI
	{
		EndPoint _api;
		int		 _next = 0;
		int		 _done = 0;
		
		public InventoryAPI( EndPoint api )
		{
			_api = api;
		}
		
		public int Sync( Action<int, string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Sync(callback));
			return id;
		}
		
		public int Add( Hashtable items, Action<int, string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Call("add", items, -1, callback));
			return id;
		}
		
		public int Use( Hashtable items, Action<int, string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Call("use", items, -1, callback));
			return id;
		}
		
		public int Purchase( Hashtable items, int cost, Action<int, string,Hashtable> callback )
		{
			var id = _next;
			Coroutines.Run(_Call("purchase", items, cost, callback));
			return id;
		}
		
		public bool IsDone( int id )
		{
			return id < _done;
		}
		
		public Coroutine Wait( int id )
		{
			return Coroutines.Run(_Wait(id));
		}
		
		IEnumerator _Wait(int id)
		{
			while(IsDone(id)==false)	
					yield return 1;
		}
		
		IEnumerator _Sync( Action<int, string,Hashtable> callback )
		{
			var id = _next++;
			while (id != _done)
			{
				yield return 1;
			}			
			
			var request = _api.Get("/inventory");
			_api.Service( request, delegate(Response result){
				_done++;
				
				if (result.sucessful)
				{
					callback(result.id, null, result.hashtable);
				}
				else
				{
					callback(result.id, result.localizedError, null);
				}
			});
		}
		
		IEnumerator _Call( string type, Hashtable items, int cost, Action<int, string,Hashtable> callback ) 
		{
			var id = _next++;
			while (id != _done)
			{
				yield return 1;
			}
			
			var nonce = Nonce.Generate();
			
			var request = _api.Post("/inventory/"+type);
				
			if (cost >= 0)
			{
				request.AddData("cost", cost);
			}
			
			if (items != null)
			{
				request.AddData("items", items);	
			}
			
			request.AddData("nonce", nonce);
			
			_api.Service(request, delegate(Response result){
				_done++;
				if (result.sucessful){
					callback(id, null,result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf" )
				{
					callback(id, "nsf", null);
				}
				else {
					callback(id, result.localizedError, null);
				}
			});
			
		}
	}
}
