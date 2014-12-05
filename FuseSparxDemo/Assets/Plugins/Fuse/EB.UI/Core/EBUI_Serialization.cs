using System.Collections.Generic;

namespace EBUI
{
	public class UIPropertyAttribute : System.Attribute
	{
		public string Description = string.Empty;

        public System.Type MapTo = null;
		public string Hint = string.Empty;
		public bool NonEditable = false;
	}
	   
	[System.Serializable]
	public enum UIPropertyType
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
		UIWidgetPivot
	};
	
	[System.Serializable]
	public class UIProperty
#if UNITY_EDITOR		
		: System.ICloneable
#endif
	{
		public UIPropertyType type = UIPropertyType.None;
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
		public UIWidget.Pivot pivotValue = UIWidget.Pivot.Center;
		
		public object Value
		{
			get
			{
				switch(type)
				{
				case UIPropertyType.Int: return intValue;
				case UIPropertyType.Float: return floatValue;
				case UIPropertyType.String: return stringValue;
				case UIPropertyType.GameObject: return gameObjectValue;
				case UIPropertyType.Boolean: return intValue != 0;
				case UIPropertyType.Color: return colorValue;
				case UIPropertyType.Vector2: return vector2Value;
				case UIPropertyType.Vector3: return vector3Value;
				case UIPropertyType.Vector4: return vector4Value;
				case UIPropertyType.UIWidgetPivot: return pivotValue;
				}
				return null;
			}
			set
			{
				switch(type)
				{
				case UIPropertyType.Int: intValue = (int)value; break;
				case UIPropertyType.Float: floatValue = (float)value; break;
				case UIPropertyType.String: stringValue = (string)value; break;
				case UIPropertyType.GameObject: gameObjectValue = (UnityEngine.GameObject)value; break;
				case UIPropertyType.Boolean: { bool b = (bool)value; intValue = b ? 1 : 0; } break;
				//case PropertyType.Color: colorValue = (UnityEngine.Color)value;	break;
				case UIPropertyType.Color: { var c = (UnityEngine.Color)value; vector4Value = new UnityEngine.Vector4(c.r,c.g,c.b,c.a); colorValue = c; } break;	
				case UIPropertyType.Vector2: vector2Value = (UnityEngine.Vector2)value; break;
				case UIPropertyType.Vector3: vector3Value = (UnityEngine.Vector3)value; break;
				case UIPropertyType.Vector4: vector4Value = (UnityEngine.Vector4)value; break;
				case UIPropertyType.UIWidgetPivot: pivotValue = (UIWidget.Pivot)value; break;
				}		
			}
		}
		
#if UNITY_EDITOR	
		public object Clone()
	    {
	        return MemberwiseClone();
	    }
#endif
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
    public class UIPropertyArray
#if UNITY_EDITOR		
		: System.ICloneable
#endif
    {
        public string name = string.Empty;
        public UIPropertyType type = UIPropertyType.None;
        public List<UIProperty> items = new List<UIProperty>();

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
                    var p = new UIProperty();
                    p.name = string.Format("{0}[{1}]", name, items.Count);
                    p.type = type;
                    items.Add(p);
                    dirty = true;
                }
			}
            return dirty;
        }
		
#if UNITY_EDITOR	
		public object Clone()
	    {
			UIPropertyArray clone = (UIPropertyArray)MemberwiseClone();
			clone.items = new List<UIProperty>();
			
			foreach( var p in items )
			{
				clone.items.Add(  (UIProperty)p.Clone() );
			}
			
	        return clone;
	    }
#endif
    }
	
	/*
	[System.Serializable]
	public class Node 
#if UNITY_EDITOR
		: System.ICloneable
#endif
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
				return SequenceUtils.GetTypeFromName(runtimeTypeName);
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

#if UNITY_EDITOR		
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
                return SequenceUtils.GetPropertyAttribute(RuntimeType, name);
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
#endif
		
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

#if UNITY_EDITOR		
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
#endif
	}
	*/	
}