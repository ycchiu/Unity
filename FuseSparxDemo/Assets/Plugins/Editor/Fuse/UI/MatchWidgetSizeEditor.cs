using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MatchWidgetSize))]
public class MatchWidgetSizeEditor : Editor
{
	private void DrawWhatToMatch()
	{
		MatchWidgetSize match = target as MatchWidgetSize;
		if (match.containerToMatch != null)
		{
			EditorGUILayout.BeginHorizontal();
			match.containerToMatch = EditorGUILayout.ObjectField("EBG Widget Container", match.containerToMatch, typeof(EBGWidgetContainer), true) as EBGWidgetContainer;
			GUI.color = Color.red;
			if (GUILayout.Button("-", GUILayout.Width(24f)))
			{
				match.containerToMatch = null;
			}
			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();
		}
		else if (match.widgetToMatch != null)
		{
			EditorGUILayout.BeginHorizontal();
			match.widgetToMatch = EditorGUILayout.ObjectField("Match Widget", match.widgetToMatch, typeof(UIWidget), true) as UIWidget;
			GUI.color = Color.red;
			if (GUILayout.Button("-", GUILayout.Width(24f)))
			{
				match.widgetToMatch = null;
			}
			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();
		}
		else // Nothing yet specified.
		{
			match.widgetToMatch = EditorGUILayout.ObjectField("UI Widget", null, typeof(UIWidget), true) as UIWidget;
			match.containerToMatch = EditorGUILayout.ObjectField("EBG Widget Container", null, typeof(EBGWidgetContainer), true) as EBGWidgetContainer;
		}
	}

	private void DrawParameters()
	{
		MatchWidgetSize match = target as MatchWidgetSize;
		match.whatToResize = (MatchWidgetSize.WhatToResize) EditorGUILayout.EnumPopup("Resize", match.whatToResize);
		match.matchDirection = (MatchWidgetSize.MatchDirection) EditorGUILayout.EnumPopup("Match Direction", match.matchDirection);
		match.resizeInWorldSpace = EditorGUILayout.Toggle("Resize In World Space", match.resizeInWorldSpace);
		match.offset = EditorGUILayout.IntField("offset", match.offset);
		match.minSize = EditorGUILayout.IntField("minSize", match.minSize);
		match.maxSize = EditorGUILayout.IntField("maxSize", match.maxSize);
		match.tileSize = EditorGUILayout.IntField("tileSize", match.tileSize);
		if (match.widgetToMatch is UILabel)
		{
			match.labelUsesWidgetCorners = EditorGUILayout.Toggle("Label does not use printed text dimensions", match.labelUsesWidgetCorners);
		}

		match.Resize();
	}

	private bool showParameters = false;

	public override void OnInspectorGUI() 
	{
		MatchWidgetSize match = target as MatchWidgetSize;
		DrawWhatToMatch();

		showParameters = EditorGUILayout.Foldout(showParameters, "Advanced Parameters");
		if (showParameters)
		{
			DrawParameters();
		}

		GUILayout.BeginHorizontal();
		if (match.widgetToMatch != null)
		{
			GUILayout.Label("Match Target Dimensions:");
			GUILayout.Label(match.widgetToMatch.width + "x" + match.widgetToMatch.height);
		}
		GUILayout.EndHorizontal();
		
		if (match.GetComponent<UIWidget>() == null && match.whatToResize == MatchWidgetSize.WhatToResize.UIWidget)
		{
			GUI.color = Color.red;
			GUILayout.Label("There is no UI Widget to resize on this object!");
		}
	}
}
