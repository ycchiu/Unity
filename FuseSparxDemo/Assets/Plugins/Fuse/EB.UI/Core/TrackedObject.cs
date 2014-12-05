using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EB.UI
{
	// Tracked Objects can be either GameObjects or Components.
	public class TrackedObject
	{
		public UIObject uiObject {get; private set;}
		public List<TrackedProperty> trackedProperties = new List<TrackedProperty>();
		
		public TrackedObject(UIObject target)
		{
			uiObject = target;
		}
		
		public TrackedObject(GameObject containingObject, Hashtable serializationData)
		{
			Deserialize(containingObject, serializationData);
		}
		
		public string GetRelativePath(GameObject containingObject)
		{
			string ownerPath = EB.UIUtils.GetFullName(containingObject);
			string trackedObjPath = "";
			
			trackedObjPath = EB.UIUtils.GetFullName(uiObject.gameObject);
			
			if (!trackedObjPath.StartsWith(ownerPath))
			{
				EB.Debug.LogError(string.Format("Tracked Object Path did not start with the owner path!\nownerPath:{0}\ntrackedObjPath:{1}\n", ownerPath, trackedObjPath));
				return "";
			}
			
			return trackedObjPath.Substring(ownerPath.Length);
		}
		
		/////////////////////////////////////////////////////////////////////////
		#region Serialization / Deserialization
		/////////////////////////////////////////////////////////////////////////
		public Hashtable Serialize(GameObject containingObject)
		{
			Hashtable data = new Hashtable();
			
			data["uiObject"] = uiObject.Serialize(containingObject);

			Hashtable propertiesData = new Hashtable();
			int propIdx = 0;
			foreach (TrackedProperty tp in trackedProperties)
			{
				propertiesData["prop" + propIdx] = tp.Serialize();
				++propIdx;
			}
			
			data["props"] = propertiesData;
			data["propCount"] = propIdx;
			
			return data;
		}
		
		public void Deserialize(GameObject containingObject, Hashtable data)
		{
			if (containingObject == null)
			{
				EB.Debug.LogError("TrackedObject was passed a null containingObject.");
				return;
			}

			Hashtable uiObjectData = EB.Dot.Object("uiObject", data, new Hashtable());
			uiObject = new UIObject(containingObject, uiObjectData);

			int propCount = EB.Dot.Integer("propCount", data, 0);
			Hashtable allPropertiesObj = EB.Dot.Object("props", data, null);
			for (int propIdx = 0; propIdx < propCount; ++propIdx)
			{
				string propDataName = "prop" + propIdx;
				Hashtable propertyData = EB.Dot.Object(propDataName, allPropertiesObj, null);
				if (propertyData != null)
				{
					TrackedProperty tp = new TrackedProperty(this, propertyData);
					if (tp.owner != null)
					{
						trackedProperties.Add(tp);
					}
				}
				else
				{
					EB.Debug.LogError("Data for property called '{0}' was not found.", propDataName);
				}
			}
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		public void AddTrackedProperty(PropertyInfo propInfo)
		{
			TrackedProperty existing = trackedProperties.Find(tp => tp.property == propInfo);
			
			if (existing == null)
			{
				trackedProperties.Add(new TrackedProperty(uiObject.obj, propInfo));
			}
		}
		
		public void RemoveTrackedProperty(PropertyInfo propInfo)
		{
			TrackedProperty existing = trackedProperties.Find(tp => tp.property == propInfo);
			
			trackedProperties.Remove(existing);
		}
		
		public TrackedProperty GetTrackedProperty(PropertyInfo propInfo)
		{
			TrackedProperty found = trackedProperties.Find(tp => tp.property == propInfo);
			return found;
		}
		
		public override string ToString()
		{
			string result = string.Format("[TrackedObject: obj='{0}', type='{1}']\n", uiObject.obj.name, uiObject.type.ToString());
			
			foreach (TrackedProperty tp in trackedProperties)
			{
				result += tp.ToString() + "\n";
			}
			
			return result;
		}
	}
}