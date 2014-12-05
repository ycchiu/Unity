using UnityEngine;
using System.Collections;

namespace EB
{
	// helper class to get around trampoline problems when using AOT (ahead of time) compliation
	public static class AOT
	{
		public static IEnumerator GetEnumerator( object obj )
		{
			if (obj == null)
			{
				return null;
			}
			
			var interfaces = obj.GetType().GetInterfaces();
			var listType = System.Array.Find<System.Type>(interfaces,delegate(System.Type x){
				return x.IsGenericType==false && x == typeof(IEnumerable);	
			});
		
			if (listType == null)
			{
				EB.Debug.LogError("Object does not implement IEnumerable interface: " + obj.GetType());
				return null;
			}
			
			var method = listType.GetMethod("GetEnumerator");
			if (method == null)
			{
				EB.Debug.LogError("Failed to get method on: " + obj.GetType());
				return null;
			}
			
			IEnumerator enumerator = null;
			try
			{
				enumerator = (IEnumerator)method.Invoke(obj, null);
			}
			catch {}
			
			return enumerator;
		}
		
		public static IDictionaryEnumerator GetDictionaryEnumerator( object obj )
		{
			if (obj == null)
			{
				return null;
			}
			
			var interfaces = obj.GetType().GetInterfaces();
			var listType = System.Array.Find<System.Type>(interfaces,delegate(System.Type x){
				return x.IsGenericType==false && x == typeof(IDictionary);	
			});
		
			if (listType == null)
			{
				EB.Debug.LogError("Object does not implement IEnumerable interface: " + obj.GetType());
				return null;
			}
			
			var method = listType.GetMethod("GetEnumerator");
			if (method == null)
			{
				EB.Debug.LogError("Failed to get method on: " + obj.GetType());
				return null;
			}
			
			IDictionaryEnumerator enumerator = null;
			try
			{
				enumerator = (IDictionaryEnumerator)method.Invoke(obj, null);
			}
			catch {}
			
			return enumerator;	
		}
		
		
	}
}

