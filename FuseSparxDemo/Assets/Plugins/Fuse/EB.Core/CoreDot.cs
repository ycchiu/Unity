#define DOT_COLOUR

using System.Collections;

namespace EB
{
	// a wrapper for dot json notation
	// traverses the json struction via string code
	public static class Dot
	{
		public static object Find( string name, object obj )
		{
			if (string.IsNullOrEmpty(name) || obj == null)
			{
				return obj;
			}
			
			var variable = name;
			var next = string.Empty;
			int dot     = name.IndexOf('.');
			if ( dot != -1 )
			{
				variable = name.Substring(0,dot);
				next = name.Substring(dot+1);
			}
	
	        int index = -1;
	        int startBracket = variable.IndexOf('[');
	        if (startBracket != -1)
	        {
	            var endBracket = variable.IndexOf(']', startBracket + 1);
	            if ( endBracket != -1 )
	            {
	                var tmp = variable.Substring(startBracket + 1, endBracket - startBracket -1);
	                if ( !int.TryParse(tmp, out index) )
	                {
	                    index = -1;
	                    EB.Debug.LogError("invalid index: " + tmp);
	                    return null;
	                }
	                variable = variable.Substring(0, startBracket);
	            }
				
				// OLD path!
				if ( !string.IsNullOrEmpty(variable) )
	            {
	                if ( obj is Hashtable )	
				    {
						var ht = (Hashtable)obj;
						obj = ht[variable];
				    }
	            }
	
	            if ( index >= 0 && obj is ArrayList )
	            {
	                var list = (ArrayList)obj;
	                if ( index < list.Count )
	                {
	                    obj = list[index];
	                }
	                else
	                {
	                    obj = null;
	                }
	            }
				
	        }
			if ( !string.IsNullOrEmpty(variable) )
            {
                if ( obj is Hashtable )	
			    {
					var ht = (Hashtable)obj;
					obj = ht[variable];
			    }
				else if ( obj is ArrayList && int.TryParse(variable, out index) )
				{
					var arr = (ArrayList)obj;
					if ( index >=0 && index < arr.Count )
					{
						obj = arr[index];
					}
					else
					{
						EB.Debug.LogWarning("EB.Dot.Find > out of array bounds [index " + index + " vs. arr size " + arr.Count + "], stopping");
						return null;
					}
				}
            }
			
			return Find(next, obj);
		}
	
		public static object DeepCopy( object source, object dest )
		{
			if ( source is Hashtable )
			{
				var ht = (Hashtable)source;
				var dht = (Hashtable)dest;
				foreach( DictionaryEntry entry in ht )
				{
					var value = entry.Value;
					if (dht.ContainsKey(entry.Key))
					{
						dht[entry.Key] = DeepCopy(value,dht[entry.Key]);
					}
					else
					{
						dht.Add(entry.Key,entry.Value);
					}
				}
			}
			else 
			{
				dest = source;
			}
			
			return dest;
		}
		
		public static ArrayList Array( string name, object obj, ArrayList defaultValue )
		{
			var value = Find(name, obj);
			if ( value != null && value is ArrayList )
			{
				return (ArrayList)value;
			}
			return defaultValue;
		}
		
		public interface IDotListItem
		{
			bool IsValid { get; }
		}
		
		public static System.Collections.Generic.List<T> List<T>( string name, object obj, System.Collections.Generic.List<T> defaultValue ) where T : IDotListItem
		{
			ArrayList listData = EB.Dot.Array( name, obj, null );
			if( listData != null )
			{
				System.Collections.Generic.List<T> items = new System.Collections.Generic.List<T>();
				foreach( Hashtable itemData in listData )
				{
					T item = (T)System.Activator.CreateInstance( typeof(T), itemData );
					if( item.IsValid == true )
					{
						items.Add( item );
					}
				}
				return items;
			}
			else
			{
				return defaultValue;
			}
		}
		
		public static Hashtable Object( string name, object obj, Hashtable defaultValue )
		{
			var value = Find(name, obj);
			if ( value != null && value is Hashtable )
			{
				return (Hashtable)value;
			}
			return defaultValue;
		}
		
		public static string String( string name, object obj, string defaultValue )
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				return value.ToString();
			}
			return defaultValue;
		}
		
		public static T Enum<T>( string name, object obj, T defaultValue ) where T : struct, System.IConvertible
		{
			if (!typeof(T).IsEnum) throw new System.ArgumentException("T must be an enumerated type");
			
			var value = Find(name, obj);
			if ( value != null )
			{
				try
				{
					return (T)System.Enum.Parse( typeof( T ), value.ToString(), true );
				}
				catch
				{
					return defaultValue;
				}
			}
			return defaultValue;
		}
		
		public static int Integer( string name, object obj, int defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				double result = 0;
				if ( double.TryParse(value.ToString(), out result) )
				{
					return (int)result;
				}
			}
			return defaultValue;
		}
		
		public static uint UInteger( string name, object obj, uint defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				uint result = 0;
				if ( uint.TryParse(value.ToString(), out result) )
				{
					return result;
				}
			}
			return defaultValue;
		}
		
		public static long Long( string name, object obj, long defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				long result = 0;
				if ( long.TryParse(value.ToString(), out result) )
				{
					return result;
				}
			}
			return defaultValue;
		}
		
		public static float Single( string name, object obj, float defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				double result = 0;
				if ( double.TryParse(value.ToString(), out result) )
				{
					return (float)result;
				}
			}
			return defaultValue;
		}
		
		public static double Double( string name, object obj, float defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				double result = 0;
				if ( double.TryParse(value.ToString(), out result) )
				{
					return result;
				}
			}
			return defaultValue;
		}				
		
		public static bool Bool( string name, object obj, bool defaultValue ) 
		{
			var value = Find(name, obj);
			if ( value != null )
			{
				bool result = false;
				if ( bool.TryParse(value.ToString(), out result) )
				{
					return result;
				}
			}
			return defaultValue;
		}

		static void _FlattenInternal( EB.Collections.Stack<string> prefixes, Hashtable result, object obj )
		{
			if (obj is IDictionary) 
			{
				var enumerator = AOT.GetDictionaryEnumerator(obj);
				if (enumerator != null)
				{
					while(enumerator.MoveNext())
					{
						prefixes.Push(enumerator.Entry.Key.ToString());
						_FlattenInternal(prefixes, result, enumerator.Entry.Value);
						prefixes.Pop();
					}
				}
			}
			else if (obj is IList)
			{
				var index = 0;
				var enumerator = AOT.GetEnumerator(obj);
				if (enumerator != null)
				{
					while(enumerator.MoveNext())
					{
						prefixes.Push(index.ToString());
						_FlattenInternal(prefixes, result, enumerator.Current);
						prefixes.Pop();
						index++;
					}
				}
			}
			else if (obj != null)
			{
				if (prefixes.Count == 1)
				{
					result[prefixes[0]] = obj;
				}
				else
				{
					result[ArrayUtils.Join(prefixes,'.')] = obj;
				}
			}
		}

		public static Hashtable Flatten( object obj )
		{
			Hashtable result = new Hashtable();

			var prefixes = new EB.Collections.Stack<string>();
			_FlattenInternal(prefixes, result, obj);

			return result;
		}
		
	#if DOT_COLOUR || UNITY_WEBPLAYER
		public static UnityEngine.Color Colour( string name, object obj, UnityEngine.Color defaultValue )
		{
			var value = EB.Dot.Object( name, obj, null );
			if (value != null)
			{
				UnityEngine.Color c = UnityEngine.Color.white;
				c.r = EB.Dot.Integer("r", value, 255) / 255.0f;
				c.g = EB.Dot.Integer("g", value, 255) / 255.0f;
				c.b = EB.Dot.Integer("b", value, 255) / 255.0f;
				c.a = EB.Dot.Single("a", value, 1.0f);
				return c;
			}
			
			var colourString = EB.Dot.String( name, obj, null );
			if( ( colourString != null ) && ( colourString.StartsWith( "rgba(" ) == true ) && ( colourString.EndsWith( ")" ) == true ) )
			{
				string inner = colourString.Substring( 5, colourString.Length - 6 );
				var parts = inner.Split( ',' );
				if( parts.Length == 4 )
				{
					int r,g,b;
					float a;
					if( int.TryParse( parts[ 0 ], out r ) && int.TryParse( parts[ 1 ], out g ) && int.TryParse( parts[ 2 ], out b ) && float.TryParse( parts[ 3 ], out a ) )
					{
						UnityEngine.Color c = UnityEngine.Color.white;
						c.r = r / 255.0f;
						c.g = g / 255.0f;
						c.b = b / 255.0f;
						c.a = a;
						return c;
					}
				}
			}
			
			return defaultValue;
		}
	#endif
		
	}
}