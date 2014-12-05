using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// This class abstracts a gameObject or component for serialization.
public class UIObject
{
	public enum Type
	{
		GameObject,
		Component
	}
	
	public GameObject gameObject { get { return goRef; } }
	public Component component { get { return compRef; } }
	public UnityEngine.Object obj { get; private set; }
	public Type type { get; private set; }

	private GameObject goRef = null;
	private Component compRef = null;
	
	public UIObject(UnityEngine.Object obj)
	{
		AssignUIObject(obj);
	}
	
	public UIObject(GameObject containingObject, Hashtable serializationData)
	{
		Deserialize(containingObject, serializationData);
	}
	
	public string GetRelativePath(GameObject containingObject)
	{
		string ownerPath = EB.UIUtils.GetFullName(containingObject);
		string objPath = "";

		if (gameObject == null)
		{
			return "";
		}
		
		objPath = EB.UIUtils.GetFullName(gameObject);
		
		if (!objPath.StartsWith(ownerPath))
		{
			EB.Debug.LogError(string.Format("UI Object Path did not start with the owner path!\nownerPath:{0}\ntrackedObjPath:{1}\n", ownerPath, objPath));
			return "";
		}
		
		return objPath.Substring(ownerPath.Length);
	}
	
	/////////////////////////////////////////////////////////////////////////
	#region Serialization / Deserialization
	/////////////////////////////////////////////////////////////////////////
	public Hashtable Serialize(GameObject containingObject)
	{
		Hashtable data = new Hashtable();
		
		data["path"] = GetRelativePath(containingObject);
		data["type"] = type.ToString();
		if (type == Type.Component)
		{
			if (component != null)
			{
				data["componentType"] = component.GetType().ToString();
			}
		}

		return data;
	}
	
	public void Deserialize(GameObject containingObject, Hashtable data)
	{
		if (containingObject == null)
		{
			EB.Debug.LogError("UIObject was passed a null containingObject.");
			return;
		}
		
		string pathToObject = EB.Dot.String("path", data, "");
		while (pathToObject.StartsWith("/"))
		{
			pathToObject = pathToObject.Substring(1);
		}

		GameObject go;
		if (string.IsNullOrEmpty(pathToObject))
		{
			go = containingObject;
		}
		else
		{
			go = EB.Util.GetObjectExactMatch(containingObject, pathToObject);
		}

		type = EB.Util.GetEnumValueFromString<Type>(EB.Dot.String("type", data, ""));
		if (type == Type.Component)
		{
			string componentType = EB.Dot.String("componentType", data, "");
			if (componentType.Contains("."))
			{
				componentType = componentType.Substring(componentType.LastIndexOf(".") + 1);
			}
			if (go != null && !string.IsNullOrEmpty(componentType))
			{
				Component c = go.GetComponent(componentType);
				AssignUIObject(c);
			}
		}
		else
		{
			AssignUIObject(go);
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	public override string ToString()
	{
		string result = string.Format("[UIObject: obj='{0}', type='{1}']\n", (obj != null ? obj.name : "<null>"), type.ToString());
		
		return result;
	}

	private void AssignUIObject(UnityEngine.Object obj)
	{
		this.obj = obj;
		
		if (obj is GameObject)
		{
			type = Type.GameObject;
			goRef = obj as GameObject;
		}
		else if (obj is Component)
		{
			type = Type.Component;
			compRef = obj as Component;
			goRef = compRef.gameObject;
		}
	}
}