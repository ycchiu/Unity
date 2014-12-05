using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(AudioMix))]
public class AudioMixEditor : Editor
{
	private const float s_LabelWidth = 200.0f;
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public override void OnInspectorGUI() 
	{
		DrawDefaultInspector();

		AudioMix TargetAudioMix = target as AudioMix;
			
		FloatSlider("Volume (dB)", ref TargetAudioMix.m_VolumeDB, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax);

		IntSlider("Priority (0 = highest)", ref TargetAudioMix.m_Priority, AudioConstants.s_PriorityMin, AudioConstants.s_PriorityMax);
		
		if (GUI.changed)
		{
			if (true == EditorApplication.isPlaying)
			{
				AudioMixer AudioMixerComponent = AudioMixer.GetAudioMixerGameObject().GetComponent<AudioMixer>();
				AudioMixerComponent.ReapplyAudioMix(TargetAudioMix);
			}

			EditorUtility.SetDirty(target);
			Repaint();
		}		
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
		
	private void FloatSlider(string Label, ref float Value, float Min, float Max)
	{		
		Value = EditorGUILayout.Slider(Label, Value, Min, Max);	
	}

	private void IntSlider(string Label, ref int Value, int Min, int Max)
	{
		Value = EditorGUILayout.IntSlider(Label, Value, Min, Max);
	}
}