using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EditorWorker), true)]
public class EditorWorkerInspector : Editor
{
	public override void OnInspectorGUI() 
	{
		EditorWorker instance = target as EditorWorker;

		bool oldWordWrap = EditorStyles.textField.wordWrap;
		EditorStyles.textField.wordWrap = true;
		GUILayout.Label("Worker Status:");
		GUILayout.Label(instance.GetProgressText());
		EditorStyles.textField.wordWrap = oldWordWrap;


		DrawDefaultInspector();
	}
}
