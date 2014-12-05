using System.Collections.Generic;

namespace EB.Sequence.Serialization
{
	[System.Serializable]
	public class Group
	{
		public string Description = string.Empty;
		public string Colour = "Yellow";
		public List<int> Ids = new List<int>();
	};


	[System.Serializable]
	public class Link
	{
		public int outId = 0;
		public string outName = string.Empty;
		public int inId = 0;
		public string inName = string.Empty;
	}
	
	[System.Serializable]
	public enum PropertyType
	{
		None,
		Int,
		Float,
		String,
		GameObject,
		Boolean,
		Color,
		Vector2,
		Vector3,
		Vector4,
		AnimationClip,
	};
	
	[System.Serializable]
	public class Property : System.ICloneable
	{
		public PropertyType type = PropertyType.None;
		public string name;
		public int intValue = 0;
		public float floatValue = 0;
		public string stringValue = string.Empty;
		public UnityEngine.GameObject gameObjectValue = null;
		public UnityEngine.Color colorValue = default(UnityEngine.Color);
		public UnityEngine.Vector4 vector4Value = UnityEngine.Vector4.zero;
		
		public UnityEngine.Vector2 vector2Value { get { return new UnityEngine.Vector2(vector4Value.x,vector4Value.y); } set { vector4Value = new UnityEngine.Vector4(value.x, value.y,0,0); }  }
		public UnityEngine.Vector3 vector3Value { get { return new UnityEngine.Vector3(vector4Value.x,vector4Value.y, vector4Value.z); } set { vector4Value = new UnityEngine.Vector4(value.x, value.y,value.z,0); }  }
		//public UnityEngine.Color colorValue { get { return new UnityEngine.Color(vector4Value.x,vector4Value.y, vector4Value.z, vector4Value.z); } set { vector4Value = new UnityEngine.Vector4(value.r,value.g,value.b,value.a); }  }
		
		public object Value
		{
			get
			{
				switch(type)
				{
				case PropertyType.Int: return intValue;
				case PropertyType.Float: return floatValue;
				case PropertyType.String: return stringValue;
				case PropertyType.GameObject: return gameObjectValue;
				case PropertyType.Boolean: return intValue != 0;
				case PropertyType.Color: return colorValue;
				case PropertyType.Vector2: return vector2Value;
				case PropertyType.Vector3: return vector3Value;
				case PropertyType.Vector4: return vector4Value;
				}
				return null;
			}
			set
			{
				switch(type)
				{
				case PropertyType.Int: intValue = (int)value; break;
				case PropertyType.Float: floatValue = (float)value; break;
				case PropertyType.String: stringValue = (string)value; break;
				case PropertyType.GameObject: gameObjectValue = (UnityEngine.GameObject)value; break;
				case PropertyType.Boolean: { bool b = (bool)value; intValue = b ? 1 : 0; } break;
				//case PropertyType.Color: colorValue = (UnityEngine.Color)value;	break;
				case PropertyType.Color: { var c = (UnityEngine.Color)value; vector4Value = new UnityEngine.Vector4(c.r,c.g,c.b,c.a); colorValue = c; } break;	
				case PropertyType.Vector2: vector2Value = (UnityEngine.Vector2)value; break;
				case PropertyType.Vector3: vector3Value = (UnityEngine.Vector3)value; break;
				case PropertyType.Vector4: vector4Value = (UnityEngine.Vector4)value; break;
				}		
			}
		}
		
		public object Clone()
	    {
	        return MemberwiseClone();
	    }
	}
	
	[System.Serializable]
	public enum NodeType
	{
		None,
		Event,
		Action,
		Condition,
		Variable,
	};

    [System.Serializable]
    public class PropertyArray : System.ICloneable
    {
        public string name = string.Empty;
        public PropertyType type = PropertyType.None;
        public List<Property> items = new List<Property>();

        public bool Resize( int count )
        {
            bool dirty = false;
            if ( count < items.Count )
            {
                items.RemoveRange(count, items.Count - count);
                dirty = true;
            }
            else 
            {
                while ( items.Count < count )
                {
                    var p = new Property();
                    p.name = string.Format("{0}[{1}]", name, items.Count);
                    p.type = type;
                    items.Add(p);
                    dirty = true;
                }
			}
            return dirty;
        }
		
		public object Clone()
	    {
			PropertyArray clone = (PropertyArray)MemberwiseClone();
			clone.items = new List<Property>();
			
			foreach( var p in items )
			{
				clone.items.Add(  (Property)p.Clone() );
			}
			
	        return clone;
	    }
    }

	[System.Serializable]
	public class Node : System.ICloneable
	{
		public NodeType nodeType = NodeType.None;
		public string comment = string.Empty;
		public UnityEngine.Rect rect = new UnityEngine.Rect(0,0,100,100);
		public int id = 0;
		public string runtimeTypeName = string.Empty;
		public List<Property> properties = new List<Property>();
        public List<PropertyArray> propertyArrays = new List<PropertyArray>();
		
		public System.Type RuntimeType
		{
			get
			{
				return Utils.GetTypeFromName(runtimeTypeName);
			}
			set
			{
				runtimeTypeName = value.Name;
			}
		}

        public int GetPropertyCount()
        {
            int count = properties.Count;
            foreach( var array in propertyArrays )
            {
                count += 1 + array.items.Count;
            }
            return count;
        }
		
		public Property FindByHint(string hint)
		{
			foreach( Property p in properties )
			{
				var attribute = GetPropertyAttribute(p.name);
				if ( attribute != null && attribute.Hint == hint )
				{
					return p;
				}
			}
			return null;			
		}

        public PropertyAttribute GetPropertyAttribute(string name)
        {
            try
            {
                return Utils.GetPropertyAttribute(RuntimeType, name);
            }
            catch// (System.Exception ex)
            {
            	
            }
            return null;
        }
		
		public Property GetOrAddProperty( string name )
		{
			foreach( Property p in properties )
			{
				if ( p.name == name )
				{
					return p;
				}
			}
			
			var prop = new Property();
			prop.name = name;
			properties.Add(prop);
			return prop;
		}
		
		public Property GetProperty( string name )
		{
			foreach( Property p in properties )
			{
				if ( p.name == name )
				{
					return p;
				}
			}
			return null;
		}

        public PropertyArray GetPropertyArray(string name)
        {
            foreach (PropertyArray p in propertyArrays)
            {
                if (p.name == name)
                {
                    return p;
                }
            }
            return null;
        }
		
		public object Clone()
	    {
	        Node n = (Node)MemberwiseClone();
			n.properties = new List<Property>();
			n.propertyArrays = new List<PropertyArray>();
			
			foreach( var p in properties )
			{
				n.properties.Add( (Property)p.Clone() );
			}
			
			foreach( var p in propertyArrays )
			{
				n.propertyArrays.Add( (PropertyArray)p.Clone() );
			}
			return n;
	    }
	}
	
}