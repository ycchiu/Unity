using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AlignedObject = AlignUIElements.AlignedObject;

[CustomEditor(typeof(AlignUIElements))]
public class AlignUIElementsEditor : Editor
{
	private AlignUIElements alignUiElementsInstance;
	private Dictionary <AlignedObject, bool> foldStates = new Dictionary<AlignedObject, bool>();

	private void DrawAlignmentSettingsGUI()
	{
		NGUIEditorTools.DrawProperty("Alignment Direction", serializedObject, "alignmentDirection", GUILayout.MinWidth(140f));
		NGUIEditorTools.DrawProperty("Alignment Type", serializedObject, "alignment", GUILayout.MinWidth(140f));
		NGUIEditorTools.DrawProperty("Default Offset", serializedObject, "defaultOffset", GUILayout.MinWidth(140f));
		NGUIEditorTools.DrawProperty("Last Element Never Has Offset", serializedObject, "lastElementNeverHasOffset", GUILayout.MinWidth(140f));
		if( AlignUIElements.Defaults.TrackAllWidgetChanged == false )
		{
			NGUIEditorTools.DrawProperty("Track Widget Changed Events", serializedObject, "trackWidgetsChanged", GUILayout.MinWidth(140f));
		}

		// This property is not serialized.
		alignUiElementsInstance.alwaysShowGizmos = EditorGUILayout.Toggle("Always Show Gizmos", alignUiElementsInstance.alwaysShowGizmos);

		if (Application.isPlaying && GUILayout.Button("Call Reposition()"))
		{
			alignUiElementsInstance.Reposition();
		}
	}

	private void DrawAlignedObjectsGUI()
	{
		var alignedObjs = alignUiElementsInstance.alignedObjects;

		var removeItems = new List<AlignedObject>();
		var addItems = new List<int>();
		int i;
		for (i = 0; i < alignedObjs.Count; ++i)
		{
			var alignedObj = alignedObjs[i];
			bool widgetExists = (alignedObj.widget != null);
			bool widgetActive = (alignedObj.widget != null && alignedObj.widget.gameObject.activeInHierarchy);
			EditorGUILayout.BeginHorizontal();

			string displayName = "Element " + i;
			Color elementDefaultColor = Color.white;
			Color elementDisabledColor = Color.gray;
			if (widgetExists && !widgetActive)
			{
				displayName += " (inactive)";
				elementDefaultColor = Color.gray;
				elementDisabledColor = new Color(0.37f, 0.37f, 0.37f);
			}
			if (!foldStates.ContainsKey(alignedObj))
			{
				foldStates[alignedObj] = true;
			}
			GUI.color = elementDefaultColor;
			foldStates[alignedObj] = EditorGUILayout.Foldout(foldStates[alignedObj], displayName);
			// Insert button:
			GUI.color = Color.green;
			if (GUILayout.Button("+", GUILayout.Width(24f)))
			{
				addItems.Add(i);
			}
			GUI.color = Color.white;
			// Delete button:
			GUI.color = Color.red;
			if (GUILayout.Button("-", GUILayout.Width(24f)))
			{
				removeItems.Add(alignedObj);
			}
			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(4f);

			if (foldStates[alignedObj])
			{
				// Aligned Object data:
				EditorGUI.indentLevel++;
				{
					bool containerInferred = (alignedObj.container == null && alignedObj.widget != null);
					GUI.color = containerInferred ? elementDisabledColor : elementDefaultColor;
					alignedObj.container = EditorGUILayout.ObjectField("Container", alignedObj.container, typeof(GameObject), true) as GameObject;
					GUI.color = elementDefaultColor;
					if (alignedObj.widget == null && alignedObj.widgetHolder == null)
					{
						GUILayout.Label("Supply one of:");
						EditorGUI.indentLevel++;
						alignedObj.widget = EditorGUILayout.ObjectField("Widget", alignedObj.widget, typeof(UIWidget), true) as UIWidget;
						alignedObj.widgetHolder = EditorGUILayout.ObjectField("WidgetHolder", alignedObj.widgetHolder, typeof(EBGWidgetContainer), true) as EBGWidgetContainer;
						EditorGUI.indentLevel--;
					}
					else if (alignedObj.widget != null)
					{
						alignedObj.widget = EditorGUILayout.ObjectField("Widget", alignedObj.widget, typeof(UIWidget), true) as UIWidget;
					}
					else if (alignedObj.widgetHolder != null)
					{
						alignedObj.widgetHolder = EditorGUILayout.ObjectField("WidgetHolder", alignedObj.widgetHolder, typeof(EBGWidgetContainer), true) as EBGWidgetContainer;
					}
					alignedObj.overrideOffset = EditorGUILayout.Toggle("Override Offset", alignedObj.overrideOffset);

					if (alignedObj.overrideOffset)
					{
						alignedObj.customOffset = EditorGUILayout.FloatField("Custom Offset", alignedObj.customOffset);
					}

					if (alignedObj.widget is UILabel)
					{
						alignedObj.labelUsesWidgetCorners = EditorGUILayout.Toggle ("Do not use printed text world corners", alignedObj.labelUsesWidgetCorners);
					}
				}
				EditorGUI.indentLevel--;
				GUILayout.Space(10f);
			}
		}

		// Add button:
		EditorGUILayout.BeginHorizontal();
		GUI.color = Color.white;
		GUILayout.Label("Drag to add element");

		GameObject newElementGameObject = EditorGUILayout.ObjectField(null, typeof(GameObject), true) as GameObject;

		if (newElementGameObject != null)
		{
			EBGWidgetContainer holder = newElementGameObject.GetComponent<EBGWidgetContainer>();
			UIWidget widget = newElementGameObject.GetComponent<UIWidget>();
			if (holder != null)
			{
				var newObj = new AlignedObject();
				newObj.widgetHolder = holder;
				alignUiElementsInstance.alignedObjects.Add(newObj);
				foldStates[newObj] = true;
			}
			else if (widget != null)
			{
				var newObj = new AlignedObject();
				newObj.widget = widget;
				alignUiElementsInstance.alignedObjects.Add(newObj);
				foldStates[newObj] = true;
			}
		}

		GUI.color = Color.green;
		if (GUILayout.Button("+", GUILayout.Width(24f)))
		{
			var newObj = new AlignedObject();
			alignUiElementsInstance.alignedObjects.Add(newObj);
			foldStates[newObj] = true;
		}
		GUI.color = Color.white;
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(10f);

		foreach (int addIndex in addItems)
		{
			alignUiElementsInstance.alignedObjects.Insert(addIndex, new AlignedObject());
		}

		foreach (AlignedObject removeMe in removeItems)
		{
			alignUiElementsInstance.alignedObjects.Remove(removeMe);
			foldStates.Remove(removeMe);
		}
	}

	private void DrawPivotWarningsGUI()
	{
		List<string> pivotWarnings;
		List<GameObject> pivotWarningTargets;
		alignUiElementsInstance.CheckPivots(out pivotWarnings, out pivotWarningTargets);
		for (int i = 0; i < pivotWarnings.Count && i < pivotWarningTargets.Count; ++i)
		{
			GUI.color = Color.yellow;
			if (GUILayout.Button(pivotWarnings[i]))
			{
				// change selection to this warning.
				Selection.activeObject = pivotWarningTargets[i];
			}
		}
	}
	
	private void DrawParentWarningsGUI()
	{
		List<string> parentWarnings;
		List<GameObject> parentWarningTargets;
		alignUiElementsInstance.CheckParents(out parentWarnings, out parentWarningTargets);
		
		for (int i = 0; i < parentWarnings.Count && i < parentWarningTargets.Count; ++i)
		{
			GUI.color = Color.yellow;
			if (GUILayout.Button(parentWarnings[i]))
			{
				// change selection to this warning.
				Selection.activeObject = parentWarningTargets[i];
			}
		}
	}

	private void OnEnable()
	{
		alignUiElementsInstance = target as AlignUIElements;
		var alignedObjs = alignUiElementsInstance.alignedObjects;
		if (alignedObjs != null)
		{
			foreach (var alignedObj in alignedObjs)
			{
				foldStates[alignedObj] = true;
			}
		}
	}

	public override void OnInspectorGUI() 
	{
		DrawAlignmentSettingsGUI();
		DrawAlignedObjectsGUI();
		DrawPivotWarningsGUI();
		DrawParentWarningsGUI();

		serializedObject.ApplyModifiedProperties();
		serializedObject.UpdateIfDirtyOrScript();
		if (!Application.isPlaying)
		{
			alignUiElementsInstance.Reposition();
		}
	}
}
