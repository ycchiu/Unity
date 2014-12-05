using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EB
{
	public class UncheckedAttribute : System.Attribute
	{
		
	}
	
	public static class Memory
	{
		class TypeInfo
		{
			public List<FieldInfo> fields = new List<FieldInfo>();
			
			public TypeInfo( System.Type type )
			{
				var potential = type.GetFields( BindingFlags.Instance | BindingFlags.Public);
				foreach ( var field in potential )
				{
					if (field.GetCustomAttributes(typeof(UncheckedAttribute), true).Length == 0)
					{
						fields.Add(field);
					}
				}
			}
			
			void Hash ( Digest digest, object value, bool deep )
			{
				if ( value != null )
				{
					var type = value.GetType();
					if ( type.IsArray || (value is IList) )
					{
						var enumerator = AOT.GetEnumerator(value);
						if ( enumerator != null )
						{
							while (enumerator.MoveNext())
							{
								Hash(digest, enumerator.Current, deep);
							}
						}
					}
					else if ( value is IDictionary )
					{
						var enumerator = AOT.GetDictionaryEnumerator(value);
						if ( enumerator != null )
						{
							while (enumerator.MoveNext())
							{
								Hash(digest, enumerator.Key, deep);
								Hash(digest, enumerator.Value, deep);
							}
						}
					}
					else if ( type == typeof(int) )
					{
						digest.Update( System.BitConverter.GetBytes( (int)value ));
					}
					else if ( type == typeof(uint) )
					{
						digest.Update( System.BitConverter.GetBytes( (uint)value ));
					}
					else if ( type == typeof(bool) )
					{
						digest.Update( System.BitConverter.GetBytes( (bool)value ));
					}
					else if ( type == typeof(ushort) )
					{
						digest.Update( System.BitConverter.GetBytes( (ushort)value ));
					}
					else if ( type == typeof(short) )
					{
						digest.Update( System.BitConverter.GetBytes( (short)value ));
					}
					else if ( type == typeof(float) )
					{
						digest.Update( System.BitConverter.GetBytes( (float)value ));
					}
					else if ( type == typeof(double) )
					{
						digest.Update( System.BitConverter.GetBytes( (double)value ));
					}
					else if ( type == typeof(ulong) )
					{
						digest.Update( System.BitConverter.GetBytes( (ulong)value ));
					}
					else if ( type == typeof(long) )
					{
						digest.Update( System.BitConverter.GetBytes( (uint)value ));
					}
					else if ( type == typeof(string) )
					{
						digest.Update( Encoding.GetBytes( (string)value) );
					}
					else if ( type.IsEnum ) 
					{
						digest.Update( System.BitConverter.GetBytes(System.Convert.ToInt32(value)));
					}
					else if ( type.IsClass && deep )
					{
						var info = GetTypeInfo(type);
						info._GetHash( digest, value, deep);
					}
					
				}
			}
			
			void _GetHash( Digest digest, object instance, bool deep)
			{				
				foreach( var field in fields )
				{
					var value = field.GetValue(instance);
					Hash( digest, value, deep);
				}
			}
			
			public byte[] GetHash(object instance, bool deep)
			{
				var digest = Digest.FNV64();
				_GetHash(digest, instance, deep);
				return digest.Final();
			}
		}
		
		static Dictionary<System.Type, TypeInfo> _types = new Dictionary<System.Type, TypeInfo>();
		static List<Locked> _locked = new List<Locked>();
		static int _index = 0;
		public static SafeBool Breach = false;
		public static event EB.Action OnBreach;
		
		class Locked
		{
			public object 		instance;
			public TypeInfo		info;
			public byte[] 		hash;
			public bool			deep;
		}
		
		static TypeInfo GetTypeInfo( System.Type type )
		{
			TypeInfo info;
			lock(_types)
			{
				if (!_types.TryGetValue(type, out info)) 
				{
					info = new TypeInfo(type);
					_types[type] = info;
				}	
			}
			
			return info;
		}
		
		public static void Lock( object instance )
		{
			Lock(instance, false);
		}
		
		public static void Lock( object instance, bool deep )
		{
			if (instance == null)
			{
				return;
			}
			
			var locked = new Locked();
			locked.instance = instance;
			locked.info = GetTypeInfo(instance.GetType());
			locked.deep = deep;
			locked.hash = locked.info.GetHash(instance,locked.deep);
			lock(_locked)
			{
				_locked.Add(locked);
			}
		}
		
		public static void Unlock( object instance )
		{
			lock(_locked)
			{
				_locked.RemoveAll(delegate(Locked obj) {
					return obj.instance == instance;
				});
			}
		}
		
		public static void Update( int maxItems )
		{
			if (Breach)
			{
				return;
			}
			
			lock(_locked)
			{
				maxItems = Mathf.Min(maxItems, _locked.Count);
				for ( int i = 0; i < maxItems; ++i )
				{
					_index = (_index+1) % _locked.Count;
					var locked = _locked[_index];
					if (locked.instance == null)
					{
						// remove (use pop back)
						_locked[_index] = _locked[_locked.Count-1];
						_locked.RemoveAt(_locked.Count-1);
						continue;
					}
					else
					{
						var hash = locked.info.GetHash(locked.instance, locked.deep);
						for ( int j = 0; j < hash.Length; ++j )
						{
							if (hash[j] != locked.hash[j] )
							{
								Breach = true;
								EB.Debug.LogError("Breach detected! for object " + locked.instance + " " + EB.Encoding.ToHexString(hash) + " != " + EB.Encoding.ToHexString(locked.hash) );
								
								if (OnBreach != null)
								{
									OnBreach();
								}
								
								return;
							}
						}
					}
				}
				
			}
		}
		
	}
	
}
