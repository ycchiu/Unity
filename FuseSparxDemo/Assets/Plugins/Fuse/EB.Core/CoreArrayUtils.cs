using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public static class ArrayUtils
	{
		public static string Join( object list, char separator )
		{
			var sb = new System.Text.StringBuilder();
			var add = false;
			var enumerator = AOT.GetEnumerator(list);
			if ( enumerator != null )
			{
				while(enumerator.MoveNext())
				{
					var obj = enumerator.Current;
					if ( obj != null )
					{
						if (add)
						{
							sb.Append(separator);
						}
						sb.Append(obj.ToString());
						add = true;
					}
				}
			}
			return sb.ToString();
		}
		
		public static List<T> Map<O,T>( List<O> list, Function<T,O> pred ) where T : class
		{
			List<T> mapped = new List<T>(list.Count);
			foreach( O obj in list  )
			{
				var t = pred(obj);
				if (t != null)
				{
					mapped.Add(t); 
				}
			}
			return mapped;
		}
		
		// Trampoline safe conversion to list.
		public static List<T> ToList<T>(object array)
		{
			List<T> list = new List<T>();
			
			var enumerator = AOT.GetEnumerator(array);
			if (enumerator!= null)
			{
				while(enumerator.MoveNext())
				{
					list.Add((T)enumerator.Current);
				}
			}
			
			return list;
		}
	}
	
	
}


