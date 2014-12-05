using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EB.UI
{
	public class TrackedProperty
	{
		public UnityEngine.Object owner { get; private set; }
		public PropertyInfo property { get; private set; }
		
		public TrackedProperty(TrackedObject parent, Hashtable serializedData)
		{
			Deserialize(parent, serializedData);
		}
		
		public TrackedProperty(UnityEngine.Object owner, PropertyInfo property)
		{
			this.owner = owner;
			this.property = property;
		}
		
		public object Serialize()
		{
			Hashtable data = new Hashtable();
			data["name"] = property.Name;
			return data;
		}
		
		public void Deserialize(TrackedObject parent, Hashtable data)
		{
			if (parent == null)
			{
				EB.Debug.LogError("Tracked Property was passed a null parent.");
				return;
			}
			
			owner = parent.uiObject.obj;
			if (owner != null)
			{
				PropertyInfo[] properties = owner.GetType().GetProperties();
				string propName = EB.Dot.String("name", data, "");
				foreach (PropertyInfo prop in properties)
				{
					if (prop.Name == propName)
					{
						property = prop;
						break;
					}
				}
				
				if (property == null)
				{
					EB.Debug.LogWarning("Tracked Property was unable to find Property '{0}' on type '{1}'.", propName, owner.GetType().Name);
				}
			}
			else
			{
				EB.Debug.LogWarning("uiObject reference is missing. Did you rename a gameObject in the hierachy?");
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[TrackedProperty: owner={0}, property={1}]", owner.name, property.Name);
		}
	}
}
