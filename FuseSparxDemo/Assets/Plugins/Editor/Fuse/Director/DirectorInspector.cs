using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Director))]
public class DirectorInspector : UnityEditor.Editor 
{
	private static bool _showInternals = true;
	
	public override void OnInspectorGUI ()
	{
		if ( GUILayout.Button("Open Editor") )
		{
			EditorWindow.GetWindow (typeof(DirectorEditor)); 
		}
		
		if ( _showInternals)
		{
			GUILayout.Label("Internals");
			base.OnInspectorGUI ();
		}
	}
		
}
