using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
    public class SerializableAttribute : System.Attribute
    {
        public string Tag = string.Empty;
    }

    public class NoSerializeAttribute : System.Attribute
    {

    }

    public static class Serialization
    {
        public delegate object SerializeDelagate(object obj);
        public delegate object DeserializeDelagate(object value);

        private struct SerializationCallbacks
        {
            public SerializeDelagate serialize;
            public DeserializeDelagate deserialize;
        }

        public class CachedClassInfo
        {
            public System.Type type;
            public SerializableAttribute main;
            public Dictionary<string, System.Reflection.FieldInfo> fields;
        }

        private static Dictionary<System.Type, SerializationCallbacks> _callbacks = new Dictionary<System.Type, SerializationCallbacks>();
        private static Dictionary<System.Type, object> _defaults = new Dictionary<System.Type, object>();
        private static Dictionary<System.Type, CachedClassInfo> _classInfo = new Dictionary<System.Type, CachedClassInfo>();

        static Serialization()
        {
            EB.Serialization.RegisterCallbacks(typeof(Vector2), SerializeVector2, DeserializeVector2);
            EB.Serialization.RegisterCallbacks(typeof(Vector3), SerializeVector3, DeserializeVector3);
			EB.Serialization.RegisterCallbacks(typeof(Vector4), SerializeVector4, DeserializeVector4);
            EB.Serialization.RegisterCallbacks(typeof(Color), SerializeColor, DeserializeColor);
        }

        #region Color Serialization
        private static object SerializeColor(object obj)
        {
            Color c = (Color)obj;
            Hashtable o = new Hashtable();
            o["r"] = Mathf.RoundToInt(c.r * 255f);
            o["g"] = Mathf.RoundToInt(c.g * 255f);
            o["b"] = Mathf.RoundToInt(c.b * 255f);
            o["a"] = Mathf.RoundToInt(c.a * 255f);
            return o;
        }
        
        private static object DeserializeColor(object obj)
        {
            Color c = Color.white;
            
            c.r = EB.Dot.Integer("r", obj, 255) / 255f;
            c.g = EB.Dot.Integer("g", obj, 255) / 255f;
            c.b = EB.Dot.Integer("b", obj, 255) / 255f;
            c.a = EB.Dot.Integer("a", obj, 255) / 255f;
            
            return c;
        }
        #endregion
        
		#region Vector4 Serialization
		private static object SerializeVector4(object obj)
		{
			Vector4 v = (Vector4)obj;
			Hashtable o = new Hashtable();
			o["x"] = v.x;
			o["y"] = v.y;
			o["z"] = v.z;
			o["w"] = v.w;
			return o;
		}
		
		private static object DeserializeVector4(object obj)
		{
			Vector4 v = Vector4.zero;
			v.x = EB.Dot.Single("x", obj, 0f);
			v.y = EB.Dot.Single("y", obj, 0f);
			v.z = EB.Dot.Single("z", obj, 0f);
			v.w = EB.Dot.Single("w", obj, 0f);
			
			return v;
		}
		#endregion
		
        #region Vector3 Serialization
        private static object SerializeVector3(object obj)
        {
            Vector3 v = (Vector3)obj;
            Hashtable o = new Hashtable();
            o["x"] = v.x;
            o["y"] = v.y;
            o["z"] = v.z;
            return o;
        }
        
        private static object DeserializeVector3(object obj)
        {
            Vector3 v = Vector3.zero;
            
            v.x = EB.Dot.Single("x", obj, 0f);
            v.y = EB.Dot.Single("y", obj, 0f);
            v.z = EB.Dot.Single("z", obj, 0f);
            
            return v;
        }
        #endregion

        #region Vector2 Serialization
        private static object SerializeVector2(object obj)
        {
            Vector2 v = (Vector2)obj;
            Hashtable o = new Hashtable();
            o["x"] = v.x;
            o["y"] = v.y;
            return o;
        }
        
        private static object DeserializeVector2(object obj)
        {
            Vector2 v = Vector2.zero;
            
            v.x = EB.Dot.Single("x", obj, 0f);
            v.y = EB.Dot.Single("y", obj, 0f);
            
            return v;
        }
        #endregion

        public static bool CanSerializeType(System.Type type)
        {
            if (type == typeof(string))
            {
                return true;
            }
            else if (type == typeof(bool))
            {
                return true;
            }
			else if (type.IsEnum)
			{
				return true;
			}
            else if (type.IsPrimitive)
            {
                return true;
            }
            else
            {
                return _callbacks.ContainsKey(type);
            }
        }

        public static void RegisterCallbacks(System.Type type, SerializeDelagate serialize, DeserializeDelagate deserialize)
        { 
            SerializationCallbacks cb = new SerializationCallbacks();
            cb.serialize = serialize;
            cb.deserialize = deserialize;
            _callbacks[type] = cb;
        }

        public static void RegisterDefault(object value)
        {
            RegisterDefault(value.GetType(), value);
        }

        public static void RegisterDefault(System.Type type, object value)
        {
            _defaults[type] = value;
        }
		
		public static bool DeserializeUntyped(string name, IDictionary hashtable, ref object result)
        {
            try
            {
				object value = hashtable[name];
				if ( value != null && result != null )
				{
					result = InternalDeserialize(value, result.GetType());
				}
				else
				{
					return false;
				}                
            }
            catch (System.Exception e )
            {
                EB.Debug.Log("Failed to deserialize: " + e.ToString());
                return false;
            }
            
            return result != null;
        }
		
        public static bool Deserialize<T>(string name, IDictionary hashtable, ref T result)
        {
            try
            {
				object value = hashtable[name];
				if ( value != null )
				{
					result = (T)InternalDeserialize(value, typeof(T));
				}
				else
				{
					return false;
				}                
            }
            catch (System.Exception e )
            {
                EB.Debug.Log("Failed to deserialize: " + e.ToString());
                return false;
            }
            
            return result != null;
        }
		
		public static bool Deserialize<T>(object value, ref T result)
        {
            try
            {
				if ( value != null )
				{
					result = (T)InternalDeserialize(value, typeof(T));
				}
				else
				{
					return false;
				}                
            }
            catch (System.Exception e )
            {
                EB.Debug.Log("Failed to deserialize: " + e.ToString());
                return false;
            }
            
            return result != null;
        }

        public static CachedClassInfo GetClassInfo( System.Type type )
        {
            CachedClassInfo info = null;
            if ( !_classInfo.TryGetValue(type, out info ) )
            {
                object[] attributes = type.GetCustomAttributes(typeof(EB.SerializableAttribute), true);
                if (attributes.Length > 0)
                {
                    info = new CachedClassInfo();
                    info.type = type;
                    info.main = (SerializableAttribute)attributes[0];
                    info.fields = new Dictionary<string, System.Reflection.FieldInfo>();

                    System.Reflection.FieldInfo[] fields = type.GetFields();
                    foreach (System.Reflection.FieldInfo field in fields)
                    {
                        if (field.GetCustomAttributes(typeof(NoSerializeAttribute), true).Length > 0 || field.IsPublic == false)
                        {
                            continue;
                        }

                        string fieldname = GetFieldName(field);

                        if ( info.fields.ContainsKey(fieldname) )
                        {
                            EB.Debug.LogError("Error: Type {0} contains conflicting field name {1}", type.Name, fieldname);
                        }

                        info.fields[fieldname] = field;
                    }

                    _classInfo[type] = info;
                }
            }
            return info;
        }

        private static object CopyArray(List<object> items, System.Type arrayType, ArrayList counts )
        {
            if (arrayType.IsArray)
            {
                if (arrayType.GetArrayRank() == 2)
                {
                    int rank0 = int.Parse(counts[0].ToString());
                    int rank1 = int.Parse(counts[1].ToString());

                    System.Array list = System.Array.CreateInstance(arrayType.GetElementType(), rank0, rank1);

                    int size = rank0 * rank1;
                    if (size != items.Count)
                    {
                        EB.Debug.Log("Bad!!! size= " + size + " count=" + items.Count);
                    }

                    int x = 0;
                    int y = 0;

                    for (int i=0; i < items.Count; ++i)
                    {
                        list.SetValue(items[i], x, y);

                        ++y;
                        if (y == rank1)
                        {
                            y = 0;
                            ++x;
                        }
                    }
                    return list;
                }
                else
                {
                    System.Array list = System.Array.CreateInstance(arrayType.GetElementType(), items.Count);
                    System.Array.Copy(items.ToArray(), list, items.Count); 
                    return list;
                }
            }
            else
            {
                IList list = (IList)System.Activator.CreateInstance(arrayType);
                foreach (object item in items)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        private static System.Type GetType(object name)
        {
            if (name == null)
            {
                return null;
            }

            System.Type type = System.Type.GetType(name.ToString(), false);
            if (type == null)
            {
                string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                type = System.Type.GetType(string.Format("{0}, {1}", name.ToString(), assemblyName));
            }
            return type;
        }

        private static System.Type GetElementType(System.Type type)
        {
            if ( type.IsArray )
            {
                return type.GetElementType();
            }
            else if ( type.IsGenericType )
            {
                System.Type[] args = type.GetGenericArguments();
                if( args.Length > 0)
                {
                    return args[0];
                }
            }
            return null;
        }

        private static object InternalDeserializeArray(object value, System.Type type)
        {
            System.Type elementType = GetElementType(type);
            if ( elementType == null )
            {
                EB.Debug.LogError("failed to determine element type of :"+ type.ToString());
                return null;
            }

            ArrayList items = null;
            ArrayList counts = new ArrayList();

            //EB.Debug.Log("deserialize array" + type.FullName);
            if ( value is ArrayList )
            {
                items = (ArrayList)value;
                counts = new ArrayList();
                counts.Add(items.Count);
            }
            else if ( value is Hashtable )
            {
                Hashtable data = (Hashtable)value;
                items = data["items"] as ArrayList;
                counts = data["ranks"] as ArrayList;
            }

            if ( items != null )
            {
                List<object> result = new List<object>();
                foreach (object item in items)
                {
                    object itemResult = InternalDeserialize(item, elementType);
                    if (itemResult != null)
                    {
                        result.Add(itemResult);
                    }
                }

                return CopyArray(result, type, counts);
            }

            EB.Debug.LogError("Failed to deserialize array of type:" + type.ToString());

            return null;
        }

        private static object InternalDeserialize(object value, System.Type type)
        {
            if (value == null || value.ToString() == "null")
            {
                return null;
            }

//            EB.Debug.Log("deserialize " + type.FullName);

            // check for custom serialization
            if (_callbacks.ContainsKey(type))
            {
                return _callbacks[type].deserialize(value);
            }

            if (IsArray(type))
            {
                return InternalDeserializeArray(value, type);
            }
            else if (type.IsEnum)
            {
                try
                {
                    return System.Enum.Parse(type, value.ToString(), true);
                }
                catch
                {
                    return null;	
                }
            }
            else if (type == typeof(string))
            {
                return value.ToString();
            }
			else if (type == typeof(bool))
			{
				switch(value.ToString().ToLower())
				{
				case "true": return true;
				case "false": return false;
				case "0": return false;
				case "1": return true;
				default:
					return false;
				}
			}
            else if (type.IsPrimitive)
            {
                // convert
                return System.Convert.ChangeType(value.ToString(), type);
            }
			
            var classInfo = GetClassInfo(type);
            if ( classInfo != null )
            {
				// SR Scriptbale object instantiation failed in Unity unless the following was done..
				object result;
				if (type.IsSubclassOf(typeof(ScriptableObject)))
			    {
					result = ScriptableObject.CreateInstance(type.ToString());
				}
				else
				{
                	result = System.Activator.CreateInstance(type);
				}

                if (value is Hashtable)
                {
                    Hashtable values = (Hashtable)value;

                    foreach( KeyValuePair<string,System.Reflection.FieldInfo> entry in classInfo.fields )
                    {
                        var field = entry.Value;
                        var fieldName = entry.Key;

                        object fieldValue = InternalDeserialize(values[fieldName], field.FieldType);
                        if (fieldValue != null)
                        {
                            field.SetValue(result, fieldValue);
                        }
                    }
                }
                return result;
            }      
            else
            {
                EB.Debug.Log("missing serialization attribute " + type.FullName);
                return null;
            }
        }

        public static T CopyFields<T>(object dest, object src)
        {
            System.Type type = typeof(T);
            System.Reflection.FieldInfo[] fields = type.GetFields();

            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.GetCustomAttributes(typeof(NoSerializeAttribute), true).Length > 0 || field.IsPublic == false || field.IsStatic)
                {
                    continue;
                }
                
                object value = field.GetValue(src);
                
                field.SetValue(dest, value);

                //EB.Debug.Log("Copying field " + field.Name + " before=" + value.ToString() + " after=" + field.GetValue(dest).ToString() );
            }
            return (T)dest;
        }

		public static void Serialize(string name, object obj, IDictionary hashtable, bool skipDefaults = true)
        {
            hashtable[name] = InternalSerialize(obj, skipDefaults);
        }
		
		public static object Serialize(object obj, bool skipDefaults = true)
		{
			return InternalSerialize(obj, skipDefaults);
		}

        private static object InternalSerializeArray(object obj, bool skipDefaults)
        {
            if ( obj == null || !(obj is IList) )
            {
                return string.Empty;
            }

            IList list = obj as IList;
            ArrayList items = new ArrayList();
            for ( int i = 0; i < list.Count; ++i )
            {
                items.Add(InternalSerialize(list[i], skipDefaults));
            }

            return items;
        }

        private static System.Type[] kNumericTypes = new System.Type[]
        {
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(float),
            typeof(byte),
        };

        private static bool IsNumeric(object o)
        {
            return System.Array.IndexOf(kNumericTypes, o.GetType()) >= 0;
        }

        private static bool IsArray(System.Type type)
        {
            if (type.IsArray)
            {
                return true;
            }
            else 
            {
                foreach( System.Type i in type.GetInterfaces() )
                {
                    if ( i.Equals( typeof(IList) ) )
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static string GetFieldName(System.Reflection.FieldInfo field)
        {
            object[] attributes = field.GetCustomAttributes(typeof(SerializableAttribute), true);
            if (attributes.Length > 0)
            {
                SerializableAttribute attr = (SerializableAttribute)attributes[0];
                if (string.IsNullOrEmpty(attr.Tag) == false)
                {
                    return attr.Tag;
                }
            }

            return field.Name.Replace("_", "");
        }

        private static object InternalSerialize(object obj, bool skipDefaults)
        {
            if (obj == null)
            {
                return null;
            }

            System.Type type = obj.GetType();

            // check for custom serialization
            if (_callbacks.ContainsKey(type))
            {
                return _callbacks[type].serialize(obj);
            }

            if (IsNumeric(obj) || (obj is bool) )
            {
                return obj;
            }
            else if (IsArray(type))
            {
                return InternalSerializeArray(obj, skipDefaults);
            }
            else if (type.IsEnum)
            {
                return System.Convert.ChangeType(obj, typeof(int));
            }
            else if (type.IsPrimitive || type == typeof(System.String))
            {
                // default, return string
                return obj.ToString();
            }

            var classInfo = GetClassInfo(type);
            if ( classInfo != null )
            {
                Hashtable values = new Hashtable();

                object def = null;
                _defaults.TryGetValue(type, out def);

                foreach (KeyValuePair<string, System.Reflection.FieldInfo> entry in classInfo.fields)
                {
                    var field = entry.Value;
                    var fieldName = entry.Key;

                    object fieldValue = field.GetValue(obj);
                    if ( skipDefaults && def != null)
                    {
                        // see if this value is different from the default
                        object defaultField = field.GetValue(def);
                        if (defaultField != null && defaultField.Equals(fieldValue))
                        {
                            continue;
                        }
                    }
                    values[fieldName] = InternalSerialize(fieldValue, skipDefaults);
                }

                return values;
            }
            else
            {
                EB.Debug.Log("missing serialization attribute " + type.Name);
                return string.Empty;
            }
        }
    }
}
