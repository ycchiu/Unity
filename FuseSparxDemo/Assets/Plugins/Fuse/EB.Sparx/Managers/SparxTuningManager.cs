using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class TuningManager : SubSystem
	{
		Hashtable _data = new Hashtable();
		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			
		}
	
		public override void Connect ()
		{
			Refresh();
		}

		public override void Disconnect (bool isLogout)
		{
			
		}
		
		public void Refresh()
		{
			var req = Hub.ApiEndPoint.Get("/tuning");
			Hub.ApiEndPoint.Service( req, delegate( Response res ){
				if ( res.sucessful )
				{
					OnRefresh(string.Empty, res.arrayList);
				}
				else 
				{
					OnRefresh(res.localizedError,null);
				}
			}); 
		}
		
		void OnRefresh( string error, ArrayList data )
		{
			if (!string.IsNullOrEmpty(error))
			{
				FatalError(error);
				return;
			}
			
			State = SubSystemState.Connected;
		
			_data.Clear();
			
			foreach( Hashtable obj in data )
			{
				Dot.DeepCopy(obj, _data);
			}
			
			EB.Debug.Log("Got data: " + JSON.Stringify(_data));
		}
		
		public void Apply( object instance, System.Reflection.FieldInfo field, string name ) 
		{
			//Debug.Log("Apply {0}, {1}, {2}", instance, field, name);
			if ( field.FieldType.IsPrimitive || field.FieldType == typeof(string) )
			{
				var value = Dot.Find(name, _data);
				if ( value != null )
				{
					value = System.Convert.ChangeType(value, field.FieldType);
					if ( value != null )
					{
						//Debug.LogWarning("Setting value {0} on {1}.{2}", value, instance, field.Name);
						field.SetValue(instance, value);
					}
				}
				else
				{
					//Debug.Log("Failed to find " + name);
				}
			}
			else if ( field.FieldType.IsEnum )
			{
				var value = Dot.Find(name, _data);
				if ( value != null )
				{
					value = System.Enum.Parse( field.FieldType, value.ToString(), true );
					if ( value != null )
					{
						field.SetValue(instance, value);
					}
				}
			}
			else if( field.FieldType == typeof( Color ) )
			{			
				Hashtable value = Dot.Find( name, _data ) as Hashtable;
				Color newColor = new Color();
				newColor.r = EB.Dot.Single( "r", value, 0.0f );
				newColor.b = EB.Dot.Single( "b", value, 0.0f );
				newColor.g = EB.Dot.Single( "g", value, 0.0f );
				newColor.a = EB.Dot.Single( "a", value, 0.0f );
				field.SetValue( instance, newColor );
			}
			else if ( field.FieldType.IsArray || field.FieldType.IsSubclassOf(typeof(IList)) )
			{
				var array = (IList)field.GetValue(instance);
				if ( array != null )
				{
					for( var i = 0; i < array.Count; ++i )
					{
						var key = name +'.'+i;
						var value = Dot.Find(key, _data);
						if ( value != null )
						{
							var current = array[i];
							if ( current != null )
							{
								if ( current.GetType() == typeof(string))
								{
									array[i] = value.ToString();
								}
								else if ( current.GetType().IsPrimitive )
								{
									value = System.Convert.ChangeType(value, current.GetType() );
									if ( value != null )
									{
										array[i] = value;
										//array.SetValue(value, i);
									}
								}
								else
								{
									Apply(current, key);
								}
							}
						}
					}
				}
			}
			else if ( field.FieldType.IsClass )
			{
				var info = EB.Serialization.GetClassInfo(field.FieldType);
				if ( info != null )
				{
					var value = field.GetValue(instance);
					if ( value != null )
					{
						Apply(value, name + "." + StringUtil.SafeKey(field.Name) );
					}
				}
			}
			else if ( instance != null )
			{
				var info = Serialization.GetClassInfo(instance.GetType());
				if ( info != null ) 
				{
					Apply(instance, name);
				}
			}
		}
		
		public void Apply( object instance, string name )
		{
			if ( instance == null )
			{
				return;
			}
			
			var info = EB.Serialization.GetClassInfo(instance.GetType());
			if (info != null)
			{
				//Debug.LogWarning("type: " + instance.GetType() + " " + name);
				foreach( var kvp in info.fields )
				{
					var key = name+"."+StringUtil.SafeKey(kvp.Key);
					Apply(instance, kvp.Value, key); 
				}
			}
		}
		
		public string GetString( string name, string defaultValue )
		{
			return  Dot.String(name,_data, defaultValue);
		}
		
		public int GetInt( string name, int defaultValue )
		{
			return  Dot.Integer(name,_data, defaultValue);
		}
		
		public bool GetBool( string name, bool defaultValue )
		{
			return  Dot.Bool(name,_data, defaultValue);
		}
		
		public float GetFloat( string name, float defaultValue )
		{
			return  Dot.Single(name,_data, defaultValue);
		}
		
		#endregion
				
	}
}

