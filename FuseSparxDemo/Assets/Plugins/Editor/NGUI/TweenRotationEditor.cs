//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenRotation))]
public class TweenRotationEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		NGUIEditorTools.SetLabelWidth(120f);
		
		TweenRotation tw = target as TweenRotation;
		GUI.changed = false;
		
		Vector3 from = EditorGUILayout.Vector3Field("From", tw.from);
		Vector3 to = EditorGUILayout.Vector3Field("To", tw.to);
		// EBG START
		bool lerpAsAngles = EditorGUILayout.Toggle("Lerp As Angles", tw.lerpAsAngles);
		// EBG END
		
		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			// EBG START
			tw.lerpAsAngles = lerpAsAngles;
			// EBG END
			NGUITools.SetDirty(tw);
		}
		
		DrawCommonProperties();
	}
}
