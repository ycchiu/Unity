// #define DEBUG_SHARED_COMPONENT_INSTANCE_CLASS
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

// TODO: Remove currentlyDeserializingFields

[ExecuteInEditMode]
public class SharedComponentInstance : MonoBehaviour, UIDependency
{
	public const string InstanceNameSuffix = "[Instance]";

#if UNITY_EDITOR
	/// Asset Modification Callback /////////////////////////////////////////
	public class SharedComponentAssetModificationProcessor : UnityEditor.AssetModificationProcessor
	{
		private static bool _isProcessingSharedComponentInstances = false;
		private static SharedComponentInstanceKillerWorker _worker = null;

		public static string[] OnWillSaveAssets(string[] paths)
		{
			if (Application.isPlaying)
			{
				return paths;
			}

			if (!_isProcessingSharedComponentInstances)
			{
				List<string> pathList = EB.ArrayUtils.ToList<string>(paths);
				List<string> prefabsToProcess = new List<string>();
				for (int i = 0; i < pathList.Count; ++i)
				{
					string path = paths[i];
					if (path.EndsWith(".prefab"))
					{
						GameObject prefab = Resources.LoadAssetAtPath<GameObject>(path);
						if (prefab != null)
						{
							SharedComponentInstance[] sharedComponentInstances = EB.Util.FindAllComponents<SharedComponentInstance>(prefab);
							if (sharedComponentInstances.Length > 0)
							{
								prefabsToProcess.Add(path);
								pathList.RemoveAt(i);
							}
						}
					}
				}

				if (prefabsToProcess.Count > 0)
				{
					_isProcessingSharedComponentInstances = true;

					GameObject workerContainer = new GameObject("SharedComponentInstanceKillerWorker");
					_worker = workerContainer.AddComponent<SharedComponentInstanceKillerWorker>();
					_worker.SearchPath = null;
					_worker.PrefabPaths = prefabsToProcess;
					_worker.Initialize();

					EditorApplication.update += OnEditorUpdate;
				}
			}
			else
			{
				_isProcessingSharedComponentInstances = false;
			}

			return paths;
		}

		// This waits for the _worker instance to finish running then resaves.
		private static void OnEditorUpdate()
		{
			if (Application.isPlaying)
			{
				EditorApplication.update -= OnEditorUpdate;
				return;
			}

			if (_worker == null)
			{
				EditorApplication.update -= OnEditorUpdate;
			}
			else if (!_worker.HasWorkToDo)
			{
				EditorApplication.update -= OnEditorUpdate;
				Debug.Log ("OnEditorUpdate is saving the scene now that shared component instances have been processed.");
				EditorApplication.SaveAssets();

				GameObject.DestroyImmediate(_worker.gameObject);
				_worker = null;
			}
		}
	}
#endif

	/// Public Variables ////////////////////////////////////////////////////
	public GameObject prefab;
	[HideInInspector] public bool autoSaveProperties = true;
	[HideInInspector] public string serializedProperties;
	// 'serializedProperties' as a field for AssignValue component.
	public string serializedProps { get { return serializedProperties; } set { serializedProperties = value; } }
	// We need to wait until the UIPanel is awake, and this component is 
	// awake before creating our instance. Best way to deal with this is to
	// have a callback once instantiation is complete?
	public event EB.Action instanceReady;

	public bool isInstanceReady {get; private set;}
	public GameObject instance {get; private set;}

	/// Private Member Variables ////////////////////////////////////////////
	private Dictionary<string, FieldInfo> currentlyDeserializingFields;
	private MonoBehaviour currentlyDeserializingMonobehaviour;
	private List<MonoBehaviour> serializedComponents = new List<MonoBehaviour>();

	/// Public Interface ////////////////////////////////////////////////////
	public void Load()
	{
		CleanupOldInstances();
		RebuildInstance();
	}

	public void Save()
	{
		if (instance != null)
		{
			StoreSerializedProperties();
		}
	}
	
	/// UIDependency Implementation /////////////////////////////////////////
	public EB.Action onReadyCallback
	{
		get
		{
			return _onReadyCallback;
		}
		set
		{
			_onReadyCallback = value;
		}
	}
	private EB.Action _onReadyCallback;
	
	public EB.Action onDeactivateCallback
	{
		get
		{
			return _onDeactivateCallback;
		}
		set
		{
			_onDeactivateCallback = value;
		}
	}
	private EB.Action _onDeactivateCallback;
	
	public bool IsReady()
	{
		return isInstanceReady;
	}
	
	/// Monobehaviour Implementation ////////////////////////////////////////
	private void Awake()
	{
		isInstanceReady = false;
		RegisterSerializationCallback();
		Load();
	}
	
	private void OnDisable()
	{
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}

#if UNITY_EDITOR
	private void Update()
	{
		if (!Application.isPlaying)
		{
			if (instance == null)
			{
				Load();
			}
			else
			{
				AutoSave();
			}
		}
	}
#endif
	
	private void OnDestroy()
	{
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}
	/// Private Implementation //////////////////////////////////////////////
	
	/// CreateInstance //////////////////////////////////////////////////////
	/// Creates our instance of the shared component.
	/////////////////////////////////////////////////////////////////////////
	private void CreateInstance()
	{
		if (prefab != null)
		{
			instance = NGUITools.AddChild(gameObject, prefab.gameObject);
			instance.name = prefab.gameObject.name + InstanceNameSuffix;
			// Assign window's current layer to the shared component instance.
			Window owner = EB.Util.FindComponentUpwards<Window>(instance);
			if (owner != null)
			{
				EB.Util.SetLayerRecursive(instance, owner.gameObject.layer);
			}
		}
		else if (Application.isPlaying)
		{
			EB.Debug.LogError("Missing prefab for SharedComponentInstance at:\n{0}", EB.UIUtils.GetFullName(gameObject));
		}
	}

	/// RegisterSerializationCallback ///////////////////////////////////////
	/// Assigns our customised callbacks.
	/////////////////////////////////////////////////////////////////////////
	private void RegisterSerializationCallback()
	{
		EB.Serialization.RegisterCallbacks(typeof(Hashtable), 
			SerializeHashtable,
			DeserializeHashtable);
	}
	
	/// SerializeHashtable //////////////////////////////////////////////////
	/// Serialize all sub-objects.
	/////////////////////////////////////////////////////////////////////////
	private object SerializeHashtable(object obj)
	{
		Hashtable serializeMe = (Hashtable)obj;
		Hashtable serialized = new Hashtable();
		foreach (object key in serializeMe.Keys)
		{
			serialized[key] = EB.Serialization.Serialize(serializeMe[key]);
		}
		return serialized;
	}
	
	/// DeserializeHashtable ////////////////////////////////////////////////
	/// Deserialize each sub-object in turn.
	/// TODO: Gareth - currentlyDeserializingMonobehaviour is a hack. Store
	/// the type as a string with the object instead, and move this to
	/// CoreSerialization.
	/////////////////////////////////////////////////////////////////////////
	private object DeserializeHashtable(object obj)
	{
		Hashtable deserializeMe = obj as Hashtable;
		Hashtable deserialized = new Hashtable();
		foreach (object key in deserializeMe.Keys)
		{
			string k = key as string;
			// Here we are using the stored type information to deserialize.
			if (currentlyDeserializingFields.ContainsKey(k))
			{
				FieldInfo f = currentlyDeserializingFields[k];
				object o = f.GetValue(currentlyDeserializingMonobehaviour);
				EB.Serialization.DeserializeUntyped(k, deserializeMe, ref o);
				deserialized[key] = o;
			}
		}
		return deserialized;
	}
	
	/// StoreSerializedProperties ///////////////////////////////////////////
	/// Goes through every monobehaviour on the shared component instance. 
	/// Serializes each public field on the monobehaviour and stores it to a
	/// Hashtable. Writes these hashtables to a JSON string and stores it in
	/// serializedProperties.
	/////////////////////////////////////////////////////////////////////////
	private void StoreSerializedProperties()
	{
		if (instance == null)
		{
			EB.Debug.LogError("Cannot save until an instance exists. Load first.");
			return;
		}
		
		RegisterSerializationCallback();
		Hashtable serialized = new Hashtable();
		
		MonoBehaviour[] mbs = instance.GetComponents<MonoBehaviour>();
		Dictionary<string, int> componentCount = new Dictionary<string, int>();
		foreach (MonoBehaviour mb in mbs)
		{
			// Ignore Missing monobehaviours.
			if (mb == null)
			{
				continue;
			}
			System.Type type = mb.GetType();
			if (serializedComponents.Contains(mb))
			{
				Hashtable currentMbSerialization = new Hashtable();
				string typeName = type.Name;
				if (!string.IsNullOrEmpty(type.Namespace))
				{
					typeName = type.Namespace + "." + typeName;
				}
				int instanceNumber = UpdateComponentCount(componentCount, typeName);
				typeName = typeName + "." + instanceNumber.ToString();
				
				FieldInfo[] fields = type.GetFields();
				foreach(var f in fields)
				{
					if (f.IsNotSerialized) continue;
					EB.Serialization.Serialize(f.Name, f.GetValue(mb), currentMbSerialization);
				}
				
				EB.Serialization.Serialize(typeName, currentMbSerialization, serialized);
			}
		}
		
		serializedProps = EB.JSON.Stringify(serialized);
	}

	/// ApplySerializedProperties ///////////////////////////////////////////
	/// Does the inverse of StoreSerializedProperties, retrieving stored data
	/// for all of the monobehaviours from serializedProperties.
	/////////////////////////////////////////////////////////////////////////
	private void ApplySerializedProperties()
	{
		if (string.IsNullOrEmpty(serializedProperties) || instance == null)
		{
			return;
		}
		
		RegisterSerializationCallback();
		Hashtable serialized = (Hashtable)EB.JSON.Parse(serializedProperties);
		
		MonoBehaviour[] mbs = instance.GetComponents<MonoBehaviour>();
		Dictionary<string, int> componentCount = new Dictionary<string, int>();
		foreach (MonoBehaviour mb in mbs)
		{
			// Ignore Missing monobehaviours.
			if (mb == null)
			{
				continue;
			}
			System.Type type = mb.GetType();
			if (serializedComponents.Contains(mb))
			{
				string typeName = type.Name;
				if (!string.IsNullOrEmpty(type.Namespace))
				{
					typeName = type.Namespace + "." + typeName;
				}
				int instanceNumber = UpdateComponentCount(componentCount, typeName);
				typeName = typeName + "." + instanceNumber.ToString();
				
				FieldInfo[] fields = type.GetFields();
				// Store the type information so that it can be used to deserialize fields.
				currentlyDeserializingMonobehaviour = mb;
				currentlyDeserializingFields = new Dictionary<string, FieldInfo>();
				foreach(var f in fields)
				{
					if (f.IsNotSerialized) continue;
					currentlyDeserializingFields[f.Name] = f;
				}
				
				object o = new Hashtable();
				EB.Serialization.DeserializeUntyped(typeName, serialized, ref o);
				Hashtable currentMbSerialization = o as Hashtable;
				
				foreach(var f in fields)
				{
					if (currentMbSerialization.ContainsKey(f.Name))
					{
						f.SetValue(mb, currentMbSerialization[f.Name]);
					}
				}
			}
		}
	}
	
	private int UpdateComponentCount(Dictionary<string, int> componentCount, string componentName)
	{
		if (componentCount.ContainsKey(componentName))
		{
			componentCount[componentName] ++;
		}
		else
		{
			componentCount[componentName] = 0;
		}
		return componentCount[componentName];
	}

#if UNITY_EDITOR
	public void PrefabCleanupOldInstances()
	{
		List<GameObject> leftOvers = new List<GameObject>();
		foreach (Transform child in transform)
		{
			if (child.name.EndsWith(InstanceNameSuffix) || child.GetComponent<SharedComponent>() != null)
			{
				leftOvers.Add(child.gameObject);
			}
		}
		foreach (GameObject go in leftOvers)
		{
			GameObject.DestroyImmediate(go, true);
		}
	}
#endif

	public void CleanupOldInstances()
	{
		Report("CleanupOldInstances");
		if (instance != null)
		{
			instance.transform.parent = null;
			NGUITools.Destroy(instance);
			instance = null;
		}
		
		// Any left over from previous runs / editor?
		try
		{
			List<GameObject> leftOvers = new List<GameObject>();
			foreach (Transform child in transform)
			{
				if (child.name.EndsWith(InstanceNameSuffix))
				{
					leftOvers.Add(child.gameObject);
				}
			}
			foreach (GameObject go in leftOvers)
			{
				NGUITools.Destroy(go);
			}
		}
		catch (MissingReferenceException)
		{
		}
		isInstanceReady = false;
	}

	private void RebuildInstance()
	{
		Report("RebuildInstance");
		CreateInstance();
		SharedComponent sharedComponentDefinition = EB.Util.FindComponent<SharedComponent>(instance);
		if (sharedComponentDefinition != null)
		{
			serializedComponents = sharedComponentDefinition.serializedComponents;
		}
		else
		{
			serializedComponents = new List<MonoBehaviour>();
		}
		ApplySerializedProperties();
		
		WaitForUIDependencies();
	}
	
	private void WaitForUIDependencies()
	{
		Report("WaitForUIDependencies");
		List<UIDependency> dependencies = EB.UIUtils.GetUIDependencies(this);
		EB.UIUtils.WaitForUIDependencies(EB.SafeAction.Wrap(this, delegate() {
			OnUIDependenciesReady();
		}), dependencies);
	}
	
	private void OnUIDependenciesReady()
	{
		Report("OnUIDependenciesReady");
		isInstanceReady = true;
		if (instanceReady != null)
		{
			instanceReady();
		}
		if (onReadyCallback != null)
		{
			onReadyCallback();
		}
	}
	
	private void AutoSave()
	{
		if (autoSaveProperties)
		{
			// Find nested Shared Component Instances and AutoSave them first in case we are dependent on them.
			List<SharedComponentInstance> childSCIs = EB.Util.FindNestedComponents<SharedComponentInstance>(gameObject);
			
			foreach (var sci in childSCIs)
			{
				sci.AutoSave();
			}
			Save();
		}
	}
	
	private void Report(string msg)
	{
#if DEBUG_SHARED_COMPONENT_INSTANCE_CLASS
		EB.Debug.Log(string.Format("[{0}] SharedComponentInstance > {1} > {2}",
			Time.frameCount,
			EB.UIUtils.GetFullName(gameObject),
			msg));
#endif
	}
}
