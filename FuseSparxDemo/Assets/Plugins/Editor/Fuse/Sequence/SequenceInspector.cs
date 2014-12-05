using UnityEngine;
using UnityEditor;
using System.Collections;
using EB.Sequence;
using EB.Sequence.Runtime;

[CustomEditor(typeof(Sequence))]
public class SequenceInspector : UnityEditor.Editor 
{
	private static bool _showInternals = false;
	
	public override void OnInspectorGUI ()
	{
		Sequence sequence  = (Sequence)this.target;
		if ( sequence == null )
		{
			return;
		}
		
		if ( GUILayout.Button("Open Editor") )
		{
			EditorWindow.GetWindow (typeof (SequenceEditor)); 
		}
		
		GUILayout.Label("Debugging");
		_showInternals = EditorGUILayout.Toggle("Show Internals", _showInternals);
		
		
		if ( _showInternals)
		{
			GUILayout.Label("Internals");
			base.OnInspectorGUI ();
		}
	}
	
}
