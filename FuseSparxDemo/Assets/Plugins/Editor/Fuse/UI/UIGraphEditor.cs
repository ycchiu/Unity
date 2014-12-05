using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(UIGraph), true)]
public class UIGraphEditor : Editor
{
	private const double FrameTime = 1.0 / 30.0;
	private System.DateTime _nextFrame = System.DateTime.Now;
	private UIGraphEditorDisplay display;
	// This is a copy of data to prevent Unity UI getting mad when it changes mid frame.
	private string serializedDataCopy;
	

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
		UIGraph uiGraph = target as UIGraph;
		base.OnInspectorGUI();

		// Don't try to draw a graph for prefabs.
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
		{
			return;
		}
		
		if (GUILayout.Button("Auto-Link"))
		{
			uiGraph.Initialize();
			uiGraph.AutoGenerateLinks();
			Debug.Log(uiGraph.ToString());
		}

		DrawGraphEditor();
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

		UIGraph uiGraph = target as UIGraph;

		// Copy serialized data during the layout phase to prevent Unity UI complaining.
		if (Event.current.type == EventType.Layout)
		{
			serializedDataCopy = EB.JSON.Stringify(uiGraph.Serialize());
		}
		if (uiGraph.serializedData != serializedDataCopy)
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField("There are unsaved changes to the UIGraph!");
			GUI.color = Color.white;
			if (GUILayout.Button("Save"))
			{
				uiGraph.serializedData = serializedDataCopy;
			}
			if (GUILayout.Button("Load"))
			{
				Hashtable data = EB.JSON.Parse(serializedDataCopy) as Hashtable;
				uiGraph.Deserialize(data);
			}
		}
		else // No changes
		{
			if (uiGraph.graphItems.Count < 1)
			{
				Hashtable data = EB.JSON.Parse(serializedDataCopy) as Hashtable;
				uiGraph.Deserialize(data);
			}
		}
	}
}
