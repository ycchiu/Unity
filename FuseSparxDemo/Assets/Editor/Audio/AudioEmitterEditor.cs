using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(AudioEmitter))]
public class AudioEmitterEditor : Editor
{
	private const float s_LabelWidth = 200.0f;	
		
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public override void OnInspectorGUI() 
	{
		AudioEmitter TargetAudioEmitter = target as AudioEmitter;
				
		DrawDefaultInspector();	
				
		FloatSlider("Initial Volume (dB)", ref TargetAudioEmitter.m_InitialVolumeDB, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax);
		
		FloatSlider("Initial Pitch (semitones)", ref TargetAudioEmitter.m_InitialPitchST, AudioConstants.s_PitchSemitonesMin, AudioConstants.s_PitchSemitonesMax);
				
		if (GUI.changed)
		{			
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