using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(UITransition))]
public class UITransitionInspector : Editor 
{
	//--------------------------------------------------------------------------------------------------------
	public override void OnInspectorGUI ()
	{
		if (GUILayout.Button("Open UI Transition Editor"))
		{
			UITransitionWindow.Init();
		}
	}
}