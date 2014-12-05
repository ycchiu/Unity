using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(UIControlGraph), true)]
public class UIControlGraphEditor : Editor
{
	private const double FrameTime = 1.0 / 30.0;
	private System.DateTime _nextFrame = System.DateTime.Now;
	private UIGraphEditorDisplay display;
	// This is a copy of data to prevent Unity UI getting mad when it changes mid frame.
	private string serializedDataCopy
	{
		set
		{
			_serializedDataCopy = value;
		}
		get
		{
			if (_serializedDataCopy == null)
			{
				UIGraph graph = target as UIGraph;
				_serializedDataCopy = graph.serializedData;
			}
			return _serializedDataCopy;
		}
	}

	private string _serializedDataCopy = null;
	
	
	private void OnEnable()
	{
		UIGraph graph = target as UIGraph;
		
		// Don't try to draw a graph for prefabs.
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
		{
			return;
		}

		if (graph.graphItems == null)
		{
			graph.Initialize();
		}
		display = new UIGraphEditorDisplay(this);
		display.Initialize(graph);

		serializedDataCopy = EB.JSON.Stringify(graph.Serialize());
		
		EditorApplication.update += OnEditorUpdate;
	}
	
	private void OnDisable()
	{
		EditorApplication.update -= OnEditorUpdate;
	}
	
	private void OnEditorUpdate()
	{
		if (_nextFrame < System.DateTime.Now)
		{
			if (display != null)
			{
				display.Update();
			}
			_nextFrame = System.DateTime.Now.AddSeconds(FrameTime);
		}
	}
	
	
	public override void OnInspectorGUI()
	{
		UIControlGraph uiGraph = target as UIControlGraph;
		bool prevAutoLink = uiGraph.autoLink;
		base.OnInspectorGUI();
		if (prevAutoLink != uiGraph.autoLink)
		{
			HandleAutoLinkChange();
		}

		// Don't try to draw a graph for prefabs.
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
		{
			return;
		}

		try
		{
			string[] options = new string[uiGraph.graphItems.Count + 1];
			options[uiGraph.graphItems.Count] = "None specified";
			for (int i = 0; i < uiGraph.graphItems.Count; ++i)
			{
				//GameObject go = uiGraph.graphItems[i].uiObject.gameObject;
				string goName = "<null>";
				if (goName != null)
				{
					goName = display.GetUniqueName(uiGraph.graphItems[i]);
				}
				options[i] = string.Format("[{0}] {1}", i, goName);
			}
			UIGraph.GraphItem curItem = uiGraph.graphItems.Find(item => item.uiObject.component == uiGraph.defaultInputHandler);
			int oldIndex = uiGraph.graphItems.IndexOf(curItem);
			if (oldIndex == -1)
			{
				oldIndex = uiGraph.graphItems.Count;
			}
			EditorGUILayout.BeginHorizontal();
			int newIndex = EditorGUILayout.Popup("Default Control", oldIndex, options);
			GUILayoutOption[] btnOpt = { GUILayout.Width(50f) };
			if (GUILayout.Button("ping", btnOpt))
			{
				EditorGUIUtility.PingObject(uiGraph.graphItems[newIndex].uiObject.gameObject);
			}
			EditorGUILayout.EndHorizontal();
			if (newIndex != oldIndex)
			{
				if (newIndex == uiGraph.graphItems.Count)
				{
					uiGraph.defaultInputHandler = null;
				}
				else
				{
					uiGraph.defaultInputHandler = uiGraph.graphItems[newIndex].uiObject.component as ControllerInputHandler;
				}
			}

			if (Application.isPlaying)
			{
				EditorGUILayout.BeginHorizontal();
				oldIndex = uiGraph.graphItems.FindIndex(i => i.uiObject.component == uiGraph.GetActiveControl());
				if (oldIndex == -1)
				{
					oldIndex = uiGraph.graphItems.Count;
				}
				newIndex = EditorGUILayout.Popup("Active Control", oldIndex, options);
				if (GUILayout.Button("ping", btnOpt))
				{
					EditorGUIUtility.PingObject(uiGraph.graphItems[newIndex].uiObject.gameObject);
				}
				EditorGUILayout.EndHorizontal();
				if (newIndex != oldIndex)
				{
					if (newIndex == uiGraph.graphItems.Count)
					{
						uiGraph.SetActiveControl(null);
					}
					else
					{
						uiGraph.SetActiveControl(uiGraph.graphItems[newIndex].uiObject.component as ControllerInputHandler);
					}
				}
			}
		}
		catch (MissingReferenceException)
		{
			// SharedComponentInstance was rebuilt.
			uiGraph.Initialize();
		}
		catch (System.NullReferenceException)
		{
			// SharedComponentInstance has not been built?
			uiGraph.Initialize();
		}

		try
		{
			DrawGraphEditor();
		}
		catch (System.ArgumentException)
		{
		}
	}
	
	protected virtual void DrawGraphEditor()
	{
		if (!string.IsNullOrEmpty(display.errors))
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField(display.errors);
			GUI.color = Color.white;
		}
		
		display.DrawGUI();
		
		UIControlGraph uiGraph = target as UIControlGraph;

		// Copy serialized data during the layout phase to prevent Unity UI complaining.
		if (Event.current.type == EventType.Layout)
		{
			// Check for recompile / save
			if (!Application.isPlaying)
			{
				if (uiGraph.graphItems == null)
				{
					uiGraph.Initialize();
				}
				serializedDataCopy = EB.JSON.Stringify(uiGraph.Serialize());
			}
		}

		DrawMissingControls(uiGraph);

		if (uiGraph.serializedData != serializedDataCopy)
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField("There are unsaved changes to the UIGraph!");
			GUI.color = Color.green;
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Save"))
			{
				uiGraph.serializedData = serializedDataCopy;
			}
			GUI.color = Color.white;
			if (GUILayout.Button("Load"))
			{
				uiGraph.Initialize();
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private void HandleAutoLinkChange()
	{
		UIControlGraph uiGraph = target as UIControlGraph;

		if (uiGraph.autoLink)
		{
			uiGraph.AutoGenerateLinks();
		}
	}

	void DrawMissingControls(UIControlGraph uiGraph)
	{
		List<UIControlGraph.GraphItem> expectedGraphItems = uiGraph.GetGraphItems(uiGraph.gameObject);

		List<UIControlGraph.GraphItem> missingGraphItems = new List<UIControlGraph.GraphItem>();
		foreach (UIControlGraph.GraphItem expected in expectedGraphItems)
		{
			if (uiGraph.graphItems.Find(item => item.uiObject.component == expected.uiObject.component) == null)
			{
				missingGraphItems.Add(expected);
			}
		}

		if (missingGraphItems.Count > 0)
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField("There are controls missing from the graph!");
			GUI.color = Color.white;
			foreach (UIControlGraph.GraphItem missing in missingGraphItems)
			{
				if (GUILayout.Button("Add missing: " + missing.uiObject.gameObject.name))
				{
					uiGraph.graphItems.Add(missing);
				}
			}
		}
	}
}
