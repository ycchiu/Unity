#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

// A class to allow dynamic value assignment for SharedComponentInstances.
[ExecuteInEditMode]
public class AssignValue : MonoBehaviour
{
	///////////////////////////////////////////////////////////////////////////
	#region Recompile Refresh
	///////////////////////////////////////////////////////////////////////////
	#if UNITY_EDITOR
	private class RecompileChecker
	{
		public static bool CheckRecompile()
		{
			bool result = recompiled;
			if (result)
			{
				recompiled = false;
			}
			return result;
		}
		
		private static bool recompiled = false;
		static RecompileChecker()
		{
			recompiled = true;
		}
	}
	
	private void CheckRecompile()
	{
		if (RecompileChecker.CheckRecompile())
		{
			// Only one instance will get this check, so pass it along to all the others.
			AssignValue[] all = GameObject.FindObjectsOfType(typeof(AssignValue)) as AssignValue[];
			foreach (AssignValue instance in all)
			{
				instance.ApplyValue();
			}
		}
	}
	#endif
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////

	public bool applyInEditor = true;
	[System.NonSerialized]
	public bool valueApplied = false;
	public bool valueLocked = false;
	public string serializedObject;
	public string serializedValues;
	[System.NonSerialized]
	public EB.UI.TrackedObject target;

#if UNITY_EDITOR
	private System.DateTime nextEditorUpdate = System.DateTime.Now;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			EditorApplication.update += OnEditorUpdate;
		}
	}

	private void OnDisable()
	{
		if (!Application.isPlaying)
		{
			EditorApplication.update -= OnEditorUpdate;
		}
	}

	private void OnEditorUpdate()
	{
		if (nextEditorUpdate <= System.DateTime.Now)
		{
			nextEditorUpdate = System.DateTime.Now.AddSeconds(0.25);
			Save();
		}
	}

	private void Update()
	{
		if (!Application.isPlaying)
		{
			CheckRecompile();
			if (!valueApplied)
			{
				ApplyValue();
			}
			else
			{
				Save();
			}
		}
	}

	public void Save()
	{
		try
		{
			if (target != null && !valueLocked)
			{
				Hashtable serializedObjectData = target.Serialize(gameObject);
				string testSerializedObject = EB.JSON.Stringify(serializedObjectData);
				if (testSerializedObject != serializedObject)
				{
					serializedObject = testSerializedObject;
				}
				
				ArrayList propValueList = new ArrayList();
				foreach (EB.UI.TrackedProperty trackedProp in target.trackedProperties)
				{
					object val = trackedProp.property.GetValue(trackedProp.owner, null);
					Hashtable propData = new Hashtable();
					propData["name"] = trackedProp.property.Name;
					propData["val"] = EB.Serialization.Serialize(val);
					propValueList.Add(propData);
				}
				
				string testSerializedValues = EB.JSON.Stringify(propValueList);
				if (testSerializedValues != serializedValues)
				{
					serializedValues = testSerializedValues;
				}
			}
		}
		catch (MissingReferenceException)
		{
		}
	}
#endif

	private void Start()
	{
		ApplyValue();
	}

	public void ApplyValue()
	{
		Load();

#if UNITY_EDITOR
		if (!Application.isPlaying && !applyInEditor)
		{
			return;
		}
#endif

		if (target != null && target.uiObject.obj != null)
		{
			ArrayList serializedValuesList = (ArrayList) EB.JSON.Parse(serializedValues);
			if (serializedValuesList != null)
			{
				foreach (Hashtable serializedValue in serializedValuesList)
				{
					string propName = EB.Dot.String("name", serializedValue, "");
					if (target.uiObject.obj.GetType() != null)
					{
						System.Reflection.PropertyInfo propInfo = target.uiObject.obj.GetType().GetProperty(propName);
						object propVal;
						if (propInfo.PropertyType == typeof(string))
						{
							propVal = "";
						}
						else
						{
							propVal = System.Activator.CreateInstance(propInfo.PropertyType);
						}
						EB.Serialization.DeserializeUntyped("val", serializedValue, ref propVal);
						propInfo.SetValue(target.uiObject.obj, propVal, null);
					}
				}
				valueApplied = true;
			}
		}
	}

	public void Load()
	{
		if (!string.IsNullOrEmpty(serializedObject))
		{
			Hashtable data = EB.JSON.Parse(serializedObject) as Hashtable;
			target = new EB.UI.TrackedObject(gameObject, data);
			if (target.uiObject == null || target.uiObject.obj == null)
			{
				Hashtable uiObjectData = EB.Dot.Object("uiObject", data, new Hashtable());
				string pathToObject = EB.Dot.String("path", uiObjectData, "");
				EB.Debug.Log("Failed to deserialize uiObject.obj from path '{0}'", pathToObject);
			}
		}
	}
}
