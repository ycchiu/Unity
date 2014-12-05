using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MatchWidgetPosition))]
public class MatchWidgetPositionEditor : Editor
{
	public override void OnInspectorGUI() 
	{
		MatchWidgetPosition matchPos = target as MatchWidgetPosition;

		base.OnInspectorGUI();
		GUILayout.Space(5f);
		if (GUILayout.Button("Reposition"))
		{
			matchPos.Reposition();
		}
	}
}
