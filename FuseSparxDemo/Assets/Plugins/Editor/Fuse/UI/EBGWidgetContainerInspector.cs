using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor(typeof(EBGWidgetContainer))]
public class EBGWidgetContainerInspector : UIWidgetContainerEditor {

	private bool showDetails = false;

	public override void OnInspectorGUI ()
	{
		EBGWidgetContainer wc = target as EBGWidgetContainer;
		if (wc != null)
		{
			EditorGUILayout.LabelField("Width: " + wc.width);
			EditorGUILayout.LabelField("Height: " + wc.height);

			List<UIWidget> trackedWidgets = wc.GetCachedWidgets();
			EditorGUILayout.LabelField("Tracked Widgets: " + trackedWidgets.Count);
			bool hasLabels = false;
			foreach (UIWidget w in trackedWidgets)
			{
				string itemName = "<null>";
				if (w != null)
				{
					itemName = w.name;
				}
				if (GUILayout.Button(itemName))
				{
					EditorGUIUtility.PingObject(w);
				}
				if (w is UILabel)
				{
					hasLabels = true;
				}
			}

			if (hasLabels)
			{
				wc.labelsUseWidgetCorners = EditorGUILayout.Toggle("Labels do not use printed text dimensions", wc.labelsUseWidgetCorners);
			}

			List<EBGWidgetContainer> trackedSubcontainers = wc.GetCachedSubcontainers();
			EditorGUILayout.LabelField("Tracked Subcontainers: " + trackedSubcontainers.Count);
			foreach (EBGWidgetContainer c in trackedSubcontainers)
			{
				string itemName = "<null>";
				if (c != null)
				{
					itemName = c.name;
				}
				if (GUILayout.Button(itemName))
				{
					EditorGUIUtility.PingObject(c);
				}
			}
		}

		if (Application.isPlaying)
		{
			GUILayout.Space(8f);
			if (GUILayout.Button("CalculateSize()"))
			{
				wc.CalculateSize();
			}
			if (GUILayout.Button("RebuildWidgetCache()"))
			{
				wc.RebuildWidgetCache();
			}
			if (GUILayout.Button("ForceFullUpdate()"))
			{
				wc.ForceFullUpdate();
			}
		}

		showDetails = EditorGUILayout.Foldout(showDetails, "Show Details");
		if (showDetails)
		{
			EditorGUILayout.TextArea(wc.calculationDetails);
		}
	}
}
