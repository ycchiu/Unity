using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(AudioCategory))]
public class AudioCategoryEditor : Editor
{
	private const float s_LabelWidth = 200.0f;
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public override void OnInspectorGUI() 
	{
		DrawDefaultInspector();
		
		AudioCategory TargetAudioCategory = target as AudioCategory;
			
		FloatSlider("Volume (dB)", ref TargetAudioCategory.m_VolumeDB, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax);
		
		if (GUI.changed)
		{
			TargetAudioCategory.ShutDown();
			EditorUtility.SetDirty(target);
			Repaint();
		}		
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
		
	private void FloatSlider(string Label, ref float Value, float Min, float Max)
	{		
		Value = EditorGUILayout.Slider(Label, Value, Min, Max);	
	}
}