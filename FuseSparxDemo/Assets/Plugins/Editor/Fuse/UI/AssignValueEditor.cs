using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AssignValue))]
public class AssignValueEditor : Editor
{
	private AssignValue assignValue;
	private bool showAddProperties = false;
	private bool showDebugProperties = false;

	private void DrawProperty(System.Reflection.PropertyInfo propInfo, out bool removeProp)
	{
		removeProp = false;
		GUILayout.BeginHorizontal();

		if (EB.Serialization.CanSerializeType(propInfo.PropertyType))
		{
			GUILayout.Label(propInfo.Name);
		}
		else
		{
			GUI.color = Color.yellow;
			GUILayout.Label(propInfo.Name);
			if (GUILayout.Button("!", GUILayout.Width(32f)))
			{
				Debug.Log ("No Serialization callback setup for type: '" + propInfo.PropertyType + "'");
			}
		}

		GUI.color = Color.red;
		if (GUILayout.Button("X", GUILayout.Width(32f)))
		{
			removeProp = true;
		}
		GUI.color = Color.white;
		GUILayout.EndHorizontal();
	}

	private Component ShowComponents(GameObject go)
	{
		Component[] allComponents = go.GetComponents(typeof(Component));
		Component chosen = null;

		GUILayout.Label("Pick Component to track:");
		foreach (Component component in allComponents)
		{
			if (GUILayout.Button(component.GetType().Name))
			{
				chosen = component;
			}
		}

		return chosen;
	}

	private void ShowPropertyEditor()
	{
		bool saveRequired = false;
		EB.UI.TrackedObject trackedObj = assignValue.target;

		GUILayout.Label("Currently Tracked Properties:");
		List<System.Reflection.PropertyInfo> removeProps = new List<System.Reflection.PropertyInfo>();
		foreach (EB.UI.TrackedProperty trackedProp in trackedObj.trackedProperties)
		{
			bool removeProp;
			DrawProperty(trackedProp.property, out removeProp);
			if (removeProp)
			{
				removeProps.Add(trackedProp.property);
			}
		}
		GUILayout.Space(5f);

		foreach (System.Reflection.PropertyInfo removeProp in removeProps)
		{
			trackedObj.RemoveTrackedProperty(removeProp);
		}
		if (removeProps.Count > 0)
		{
			assignValue.Save();
		}

		showAddProperties = EditorGUILayout.Foldout(showAddProperties, "Add Tracked Properties");
		if (showAddProperties)
		{
			System.Type t = trackedObj.uiObject.obj.GetType();
			System.Reflection.PropertyInfo[] infos = t.GetProperties();
			List<System.Reflection.PropertyInfo> sortedInfos = EB.ArrayUtils.ToList<System.Reflection.PropertyInfo>(infos);
			sortedInfos.Sort(delegate(System.Reflection.PropertyInfo x, System.Reflection.PropertyInfo y) {
				return x.Name.CompareTo(y.Name);
			});
			foreach (var info in sortedInfos)
			{
				if (info.CanRead && info.CanWrite)
				{
					if (GUILayout.Button(info.Name))
					{
						trackedObj.AddTrackedProperty(info);
						saveRequired = true;
						Debug.Log ("Begin tracking:" + info.Name);
					}
				}
			}
		}

		if (saveRequired)
		{
			assignValue.Save();
			assignValue.valueApplied = false;
		}
	}

	public override void OnInspectorGUI() 
	{
		showDebugProperties = EditorGUILayout.Foldout(showDebugProperties, "Show Serialized Data");
		if (showDebugProperties)
		{
			base.OnInspectorGUI();
		}
		assignValue = target as AssignValue;

		if (assignValue.target != null &&
		    assignValue.target.uiObject != null &&
		    assignValue.target.uiObject.obj != null)
		{
			GUI.color = Color.white;
			UnityEngine.Object targetObj = EditorGUILayout.ObjectField("Target (" + assignValue.target.uiObject.obj.GetType() + ")", assignValue.target.uiObject.obj, typeof(UnityEngine.Object), true);

			if (targetObj == null)
			{
				assignValue.target = null;
			}
			else if (assignValue.target.uiObject.type == UIObject.Type.GameObject)
			{
				Component chosen = ShowComponents(assignValue.target.uiObject.gameObject);
				if (chosen != null)
				{
					assignValue.target = new EB.UI.TrackedObject(new UIObject(chosen));
				}
			}
			else
			{
				ShowPropertyEditor();
			}
		}
		else if (!assignValue.gameObject.activeInHierarchy)
		{
			GUI.color = Color.yellow;
			GUILayout.Label("(GameObject must be enabled to show target)");
		}
		else
		{
			GUI.color = Color.yellow;
			GUILayout.Label("Assign an object to target:");
			UnityEngine.Object targetObj = EditorGUILayout.ObjectField("Target", null, typeof(UnityEngine.Object), true);
			if (targetObj != null)
			{
				UIObject uiObject = new UIObject(targetObj);
				assignValue.target = new EB.UI.TrackedObject(uiObject);
				assignValue.Save();
			}
		}

		if (assignValue.valueLocked)
		{
			GUI.color = Color.yellow;
			GUILayout.Label("This component is locked! I am not saving editor changes!");
		}
	}
}
