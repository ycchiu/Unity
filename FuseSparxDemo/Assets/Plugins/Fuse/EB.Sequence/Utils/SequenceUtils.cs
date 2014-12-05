using System.Collections.Generic;
using System.Reflection;
using EB.Sequence.Serialization;
using UnityEngine;

namespace EB.Sequence
{
	public static class Utils
	{
		static Assembly[] _assemblies = new Assembly[0];
		
		private static void Initialize() 
		{
			List<Assembly> assemblies = new List<Assembly>();
			foreach( var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				var name = assembly.FullName;
				if (name.StartsWith("Assembly-") && name.ToLower().Contains("editor")==false)
				{
					Debug.Log("Adding Assembly: " + name);
					assemblies.Add(assembly);
				}
			}
			_assemblies = assemblies.ToArray();
		}
		
		public static MenuItemAttribute GetMenuAttribute( System.Type type )
		{
			var items = type.GetCustomAttributes(typeof(MenuItemAttribute),false);
			if ( items.Length > 0 )
			{
				return items[0] as MenuItemAttribute;
			}
			return null;
		}

        public static PropertyAttribute GetPropertyAttribute(System.Type type, string name)
        {
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                var items = field.GetCustomAttributes(typeof(PropertyAttribute), true);
                if (items.Length > 0)
                {
                    return items[0] as PropertyAttribute;
                }
            }
            return null;
        }
		
		public static VariableAttribute GetVariableAttribute( System.Type type, string name )
		{
			FieldInfo field = type.GetField(name);
			if ( field != null )
			{
				var items = field.GetCustomAttributes(typeof(VariableAttribute),true);
				if ( items.Length > 0 )
				{
					return items[0] as VariableAttribute;
				}
			}
			return null;
		}
		
		public static TriggerAttribute GetTriggerAttribute( System.Type type, string name )
		{
			FieldInfo field = type.GetField(name);
			if ( field != null )
			{
				var items = field.GetCustomAttributes(typeof(TriggerAttribute),true);
				if ( items.Length > 0 )
				{
					return items[0] as TriggerAttribute;
				}
			}
			return null;
		}
		
		public static EntryAttribute GetEntryAttribute( System.Type type, string name )
		{
			MethodInfo method = type.GetMethod(name);
			if ( method != null )
			{
				var items = method.GetCustomAttributes(typeof(EntryAttribute),true);
				if ( items.Length > 0 )
				{
					return items[0] as EntryAttribute;
				}
			}
			return null;
		}
		
		public static System.Type[] GetAllTypes()
		{
			List<System.Type> items = new List<System.Type>();

			if (_assemblies.Length == 0)
			{
				Initialize();
			}
			
			foreach( var assembly in _assemblies ) 
			{
				foreach( var type in assembly.GetTypes() )
				{
					if ( type.GetCustomAttributes(typeof(MenuItemAttribute),false).Length > 0 )
					{
						items.Add(type);
					}
				}	
			}
			return items.ToArray();
		}
		
		public static NodeType GetNodeType( System.Type type ) 
		{
			if ( type.IsSubclassOf( typeof(Runtime.Event) )  )
			{
				return NodeType.Event;
			}
			else if ( type.IsSubclassOf( typeof(Runtime.Action) ) )
			{
				return NodeType.Action;
			}
			else if ( type.IsSubclassOf(typeof(Runtime.Condition)))
			{
				return NodeType.Condition;
			}
			else if ( type.IsSubclassOf(typeof(Runtime.Variable)))
			{
				return NodeType.Variable;
			}
			throw new System.Exception("Invalid node type of type: " + type.Name);
		}
		
		public static PropertyType GetPropertyType( System.Type type )
		{
			if ( type == typeof(int) )
			{
				return PropertyType.Int;
			}	
			else if ( type == typeof(float)) 
			{
				return PropertyType.Float;
			}
			else if ( type == typeof(string) )
			{
				return PropertyType.String;
			}
			else if ( type == typeof(UnityEngine.GameObject) )
			{
				return PropertyType.GameObject;
			}
			else if ( type == typeof(bool) )
			{
				return PropertyType.Boolean;
			}
			else if ( type == typeof(UnityEngine.Color) )
			{
				return PropertyType.Color;
			}
			else if ( type == typeof(UnityEngine.Vector2) )
			{
				return PropertyType.Vector2;
			}
			else if ( type == typeof(UnityEngine.Vector3) )
			{
				return PropertyType.Vector3;
			}
			else if ( type == typeof(UnityEngine.Vector4) )
			{
				return PropertyType.Vector4;
			}
			else if ( type == typeof(UnityEngine.AnimationClip) )
			{
				return PropertyType.AnimationClip;
			}
			
			throw new System.Exception("Invalid property of type: " + type.Name );
		}
		
		public static System.Type GetTypeFromName( string name )
		{
			if ( string.IsNullOrEmpty(name) ) return null;

			if (_assemblies.Length==0)
			{
				Initialize();
			}

			System.Type type = System.Type.GetType(name, false);
			if ( type == null )
			{
				foreach( var assembly in _assemblies )
				{
					type = assembly.GetType(name, false);
					if ( type != null )
					{
						return type;
					}
				}
			}
			return type;
		}
		
		public static Serialization.Node CreateSerializationNode( System.Type runtimeType, int id )
		{
			Serialization.Node node = new Serialization.Node();
			node.nodeType = GetNodeType(runtimeType);
			node.id = id;
			node.RuntimeType = runtimeType;
			
			UpdateSerializationNode(node);
			return node;
		}
		
		public static void UpdateSerializationNode( Serialization.Node node ) 
		{
			var type = node.RuntimeType;
			if ( type != null )
			{
				var obj = System.Activator.CreateInstance(type);

				foreach( var field in type.GetFields() )
				{
					PropertyAttribute[] attributes = (PropertyAttribute[])field.GetCustomAttributes( typeof(PropertyAttribute), true );
					if ( attributes.Length > 0 )
					{
                        if ( field.FieldType.IsArray )
                        {
							var array = node.GetPropertyArray(field.Name);
                            if (array == null)
                            {
                                array = new PropertyArray();
                                array.name = field.Name;
                                array.type = GetPropertyType(field.FieldType.GetElementType());
                                node.propertyArrays.Add(array);
                            }
							
							foreach( var property in array.items )
							{
								property.Value = property.Value;
							}
                        }
                        else
                        {
							var property = node.GetProperty(field.Name);
                            if (property == null)
                            {
                                property = new Property();
                                property.name = field.Name;
                                property.type = GetPropertyType(field.FieldType);
                                property.Value = field.GetValue(obj);
                                node.properties.Add(property);
                            }
							property.Value = property.Value;
                        }
					}
				}
				
				// remove old properties
				foreach( var property in node.properties.ToArray() )
				{
					var field = type.GetField(property.name);
					if ( field == null || field.GetCustomAttributes( typeof(PropertyAttribute), true ).Length == 0 )
					{
						node.properties.Remove(property);
					}
				}
				
				// remove old arrays
				foreach( var array in node.propertyArrays.ToArray() )
				{
					var field = type.GetField(array.name);
					if ( field == null || field.GetCustomAttributes( typeof(PropertyAttribute), true ).Length == 0 )
					{
						node.propertyArrays.Remove(array);
					}
				}
			}
		}
		
		
		
		
		// runtime helpers
		public static Runtime.Node CreateRuntimeNode( Node node, Component parent ) 
		{
			System.Type type = node.RuntimeType;
			if ( type == null )
			{
				EB.Debug.LogError("No type for class " + node.runtimeTypeName);
				return null;
			}
			
			Runtime.Node runtimeNode = (Runtime.Node)System.Activator.CreateInstance(type);
			runtimeNode.Parent = parent;
			runtimeNode.Id = node.id;
			
			foreach( var property in node.properties )
			{
				var field = type.GetField(property.name);
				if ( field == null || field.FieldType.IsArray )
				{
					//EB.Debug.LogWarning("Failed to find property:" + property.name);
				}
				else
				{
					field.SetValue(runtimeNode, property.Value );
				}
			}
			
			foreach( var array in node.propertyArrays )
			{
				var field = type.GetField(array.name);
				if ( field == null || field.FieldType.IsArray == false )
				{
					EB.Debug.LogWarning("Failed to find property:" + array.name);
				}
				else
				{
					System.Array items = System.Array.CreateInstance( field.FieldType.GetElementType(), array.items.Count );
					for ( int i = 0; i < array.items.Count; ++i )
					{
						items.SetValue(array.items[i].Value, i);
					}
					field.SetValue(runtimeNode, items);
				}
				
			}
			
			//EB.Debug.LogIf( parent.name == "sequence_questlevel1fnetting_master", "Creating node " +node.id ); 
			
			runtimeNode.Init();
			
			return runtimeNode;
		}
		
		public static void SetProperyValue( Serialization.Node node, string name, object value ) 
		{
			foreach( Property prop in node.properties )
			{
				if ( prop.name == name )
				{
					prop.Value = value;
				}
			}
		}
		
		private static char[] UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
		
		public static string EditorName( string name, System.Type type )
		{
			TriggerAttribute attribute = GetTriggerAttribute( type, name );
			if ( attribute != null && string.IsNullOrEmpty(attribute.EditorName) == false )
			{
				return attribute.EditorName;
			}
			return NiceName(name);
		}
		
		public static string NiceName( string name ) 
		{
			string[] parts = name.Split('_');
			name = parts[parts.Length-1];
			
			string tmp = string.Empty;
			
			while ( name.Length > 0 )
			{
				int index = name.IndexOfAny(UpperCase, 1);
				if (index > 0 )
				{
					tmp += name.Substring(0,index) + " ";
					name = name.Substring(index);
				}
				else
				{
					tmp += name;
					name = string.Empty;
				}
			}
			return tmp;
		}
		
		public enum ValidateLinkResult
		{
			Ok,
			InvalidType,
			InvalidLink
		}
		
		public static bool IsCompatableType( System.Type type1, System.Type type2 )
		{
			type1 = type1 ?? typeof(object);
			type2 = type2 ?? typeof(object);
			
			if ( type1 == type2 ) return true;
			
			if ( type1.IsArray )
			{
				return IsCompatableType(type1.GetElementType(), type2 );
			}
			
			return type1.IsSubclassOf(type2);
		}
		
		public static bool ValidateTrigger( Serialization.Node node, string name ) 
		{
			FieldInfo field = node.RuntimeType.GetField( FieldName(name) );
			if ( field != null )
			{
				return IsCompatableType(field.FieldType,typeof(Runtime.Trigger));
			}
			return false;
		}
		
		public static bool ValidateVariable( Serialization.Node node, string name )
		{
			FieldInfo field = node.RuntimeType.GetField( FieldName(name));
			if ( field != null )
			{
				return IsCompatableType(field.FieldType,typeof(Runtime.Variable));
			}
			else if ( node.nodeType == NodeType.Variable && name == Runtime.Variable.ValueLinkName )
			{
				return true;
			}
			return false;	
		}
		
		public static System.Type GetVariableType( Serialization.Node node, string feildName, out bool anyType )
		{
			System.Type variableType = null;
			anyType = false;
			switch( node.nodeType )
			{
				case NodeType.Variable:
				{
					MenuItemAttribute menuAttribute = GetMenuAttribute( node.RuntimeType );
					if( menuAttribute != null )
					{
						variableType = menuAttribute.VariableType;
						if( variableType == null )
						{
							anyType = true;
						}
					}
					break;
				}
				default:
				{
					VariableAttribute varAttribute = GetVariableAttribute(node.RuntimeType, FieldName(feildName) ); 
					if( varAttribute != null )
					{
						variableType = varAttribute.ExpectedType;
						if( variableType == null )
						{
							anyType = true;
						}
					}
					break;
				}
			}
			return variableType;
		}
		
		public static bool ValidateVariableTypes( Serialization.Node outNode, string outName, Serialization.Node inNode, string inName )
		{
			bool acceptAnyOut = false;
			System.Type outType = GetVariableType( outNode, outName, out acceptAnyOut );
			bool acceptAnyIn = false;
			System.Type inType = GetVariableType( inNode, inName, out acceptAnyIn );
			if( ( acceptAnyOut == true ) || ( acceptAnyIn == true ) )
			{
				return true;
			}
			if( ( outType != null ) && ( inType != null ) )
			{
				return IsCompatableType( outType, inType ); 
			}
			
			return false;
		}
		
		public static bool ValidateEntry( Serialization.Node node, string name )
		{
			EntryAttribute entry = GetEntryAttribute(node.RuntimeType, name);
			if ( entry != null )
			{
				return true;
			}
			return false;
		}
		
		public static T[] GetObjectList<T>( object value )
		{
			List<T> list = new List<T>();
			
			if ( value != null )
			{
				if ( value is T )
				{
					list.Add( (T)value );
				}
				else if ( value is System.Collections.IEnumerable )
				{
					System.Collections.IEnumerable enumerable = (System.Collections.IEnumerable)value;
					foreach( object sub in enumerable )
					{
						var tmp = GetObjectList<T>(sub);
						foreach( var tt in tmp )
						{
							list.Add(tt);
						}
					}
				}
			}
			
			return list.ToArray();
		}
		
		public static bool Filter<T>( Runtime.Variable variable, T obj )
		{
			if ( variable.IsNull ) return true;
			return Contains(variable.Value, obj );
		}
		
		public static bool Contains<T>( object value, T obj  )
		{
			if ( value != null )
			{
				if ( value is T )
				{
					return obj.Equals(value);	
				}
				else if ( value is System.Collections.IEnumerable )
				{
					System.Collections.IEnumerable enumerable = (System.Collections.IEnumerable)value;
					foreach( object sub in enumerable )
					{
						if ( Contains<T>(sub, obj) )
						{
							return true;
						}
					}
				}
			}
			
			return false;
		}
		
		
		public enum LinkType
		{
			None,
			Variable,
			Trigger,
			Entry
		}
		
		private static char[] kSplits = new char[]{ '[', ']' };
		
		public static Runtime.LinkFieldInfo ParseField( string name ) 
		{
			Runtime.LinkFieldInfo info;
			string[] parts = name.Split( kSplits, System.StringSplitOptions.RemoveEmptyEntries );
			if ( parts.Length == 1 )
			{
				info.field = name;
				info.index = -1;
			}
			else
			{
				info.field = parts[0];
				info.index = int.Parse(parts[1]);
			}
			return info;
		}
		
		public static string FieldName( string name )
		{
			int arrayBracket = name.IndexOf('[');
			if ( arrayBracket >= 0 )
			{
				return name.Substring(0,arrayBracket);
			}
			return name;
		}
		
		public static LinkType GetLinkType( Serialization.Node node, string name )
		{
			if ( node.nodeType == NodeType.Variable && (name == Runtime.Variable.ValueLinkName || name == string.Empty) )
			{
				return LinkType.Variable;
			}
			
			string fieldName = FieldName(name);
			
			VariableAttribute variable = GetVariableAttribute(node.RuntimeType, fieldName);
			if ( variable != null )
			{
				return LinkType.Variable;
			}
			
			// test for trigger
			TriggerAttribute trigger = GetTriggerAttribute( node.RuntimeType, fieldName );
			if ( trigger != null )
			{
				return LinkType.Trigger;
			}
			
			// entry
			EntryAttribute entry = GetEntryAttribute( node.RuntimeType, fieldName );
			if ( entry != null )
			{
				return LinkType.Entry;
			}
			
			return LinkType.None;
		}
		
		public static Direction GetLinkDirection( Serialization.Node node, string name )
		{
			if ( node.nodeType == NodeType.Variable && (name == Runtime.Variable.ValueLinkName || name == string.Empty) )
			{
				return Direction.InOut;
			}
			
			string fieldName = FieldName(name);
			
			VariableAttribute variable = GetVariableAttribute(node.RuntimeType, fieldName);
			if ( variable != null )
			{
				return variable.Direction;
			}
			
			// test for trigger
			TriggerAttribute trigger = GetTriggerAttribute( node.RuntimeType, fieldName );
			if ( trigger != null )
			{
				return Direction.Out;
			}
			
			// entry
			EntryAttribute entry = GetEntryAttribute( node.RuntimeType, fieldName );
			if ( entry != null )
			{
				return Direction.In;
			}
			
			return Direction.InOut;
		}
		
		// checks for invalid nodes
		public static bool InvalidNode(EB.Sequence.Serialization.Node node)
		{
			return node.RuntimeType == null;
		}
		
		// checks for invalid links
		public static bool InvalidLink(EB.Sequence.Component sequence, EB.Sequence.Serialization.Link link)
		{
			bool invalid = true;
			var nodeIn = sequence.FindById(link.inId);
			var nodeOut = sequence.FindById(link.outId);
			if( ( nodeIn != null ) && ( nodeOut != null ) )
			{
				invalid = EB.Sequence.Utils.ValidateLink( nodeOut, link.outName, nodeIn, link.inName) != Utils.ValidateLinkResult.Ok;
			}
			
			return invalid;
		}
		
		// checks for invalid links
		public static bool InvalidGroup(EB.Sequence.Component sequence, EB.Sequence.Serialization.Group group)
		{
			bool invalid = true;
			foreach( int id in group.Ids )
			{
				var node = sequence.FindById( id );
				if( node != null )
				{
					invalid = false;
				}
			}
			return invalid; 
		}
		
		public static ValidateLinkResult ValidateLink( Serialization.Node outNode, string outName, Serialization.Node inNode, string inName )
		{
			LinkType linkOut = GetLinkType(outNode, outName );
			LinkType linkIn = GetLinkType(inNode, inName);
			
			EB.Debug.Log( string.Format("ValidateLink: ({0}:{1} => {2}:{3})", linkOut, outName, linkIn, inName)  );
			
			switch( linkOut )
			{
			case LinkType.Trigger:
				{
					if ( ValidateTrigger(outNode, outName) )
					{
						if ( linkIn == Utils.LinkType.Entry && ValidateEntry(inNode, inName) ) 
						{
							return ValidateLinkResult.Ok;
						}
					}
				}
				
				break;
			case LinkType.Variable:
				{
					if ( ValidateVariable(outNode, outName) )			
					{
						if ( linkIn == Utils.LinkType.Variable && ValidateVariable(inNode, inName) )	
						{
							if( ValidateVariableTypes(outNode, outName, inNode, inName ) )
							{
								return ValidateLinkResult.Ok;
							}
							else 
							{
								EB.Debug.LogWarning("Invalid Variable Type: " + inName );
								return ValidateLinkResult.InvalidType;
							}
						}
					}
				}
				break;
			}
			
			return ValidateLinkResult.InvalidLink;
		}
		
	}
}
