using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(AudioEvent))]
public class AudioEventEditor : Editor
{
	private const float s_LabelWidth = 200.0f;
	private static GameObject s_AudioListenerGameObject = null;	
	private static GameObject s_AudioEmitterGameObject = null;	
		
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public AudioEventEditor()
	{	
		EditorApplication.update += Update;
	}
	
	public override void OnInspectorGUI() 
	{
		AudioEvent TargetAudioEvent = target as AudioEvent;

		GUILayout.BeginHorizontal();
		GUILayout.Space(10.0f);
		if (true == GUILayout.Button("Play"))
		{
			PlayAudioEmitter(TargetAudioEvent);
		}
		if (true == GUILayout.Button("Stop"))
		{
			StopAudioEmitter();
		}		
		GUILayout.EndHorizontal();

		DrawDefaultInspector();	
				
		IntSlider("Chance To Not Play (%)", ref TargetAudioEvent.m_ChanceToNotPlay, 0, 100);
		
		FloatSlider("Volume (dB)", ref TargetAudioEvent.m_VolumeDB, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax);
		FloatSlider("Volume Random Delta (dB)", ref TargetAudioEvent.m_VolumeDBRandomDelta, 0.0f, Mathf.Abs(AudioConstants.s_VolumeDecibelsMax - AudioConstants.s_VolumeDecibelsMin));

		FloatSlider("Pitch (semitones)", ref TargetAudioEvent.m_PitchST, AudioConstants.s_PitchSemitonesMin, AudioConstants.s_PitchSemitonesMax);
		FloatSlider("Pitch Random Delta (semitones)", ref TargetAudioEvent.m_PitchSTRandomDelta, 0.0f, Mathf.Abs(AudioConstants.s_PitchSemitonesMax - AudioConstants.s_PitchSemitonesMin));
		
		FloatSlider("Fade In Time (sec)", ref TargetAudioEvent.m_FadeInTime, AudioConstants.s_FadeTimeMin, AudioConstants.s_FadeTimeMax);
		FloatSlider("Fade Out Time (sec)", ref TargetAudioEvent.m_FadeOutTime, AudioConstants.s_FadeTimeMin, AudioConstants.s_FadeTimeMax);

		IntSlider("Priority (0 = highest)", ref TargetAudioEvent.m_Priority, AudioConstants.s_PriorityMin, AudioConstants.s_PriorityMax);

		FloatSlider("Doppler Level", ref TargetAudioEvent.m_DopplerLevel, AudioConstants.s_DopplerLevelMin, AudioConstants.s_DopplerLevelMax);

		TargetAudioEvent.m_MinDistance = Mathf.Clamp(EditorGUILayout.FloatField("Min Distance", TargetAudioEvent.m_MinDistance), AudioConstants.s_MinDistanceMin, AudioConstants.s_MinDistanceMax);
		TargetAudioEvent.m_MaxDistance = Mathf.Clamp(EditorGUILayout.FloatField("Max Distance", TargetAudioEvent.m_MaxDistance), AudioConstants.s_MaxDistanceMin, AudioConstants.s_MaxDistanceMax);
		TargetAudioEvent.m_MaxDistance = Mathf.Max(TargetAudioEvent.m_MaxDistance, TargetAudioEvent.m_MinDistance);
		
		if (GUI.changed)
		{			
			EditorUtility.SetDirty(target);
			Repaint();
		}		
	}

	public void OnDestroy()
	{
		DestroyAuditioner();
		
		EditorApplication.update -= Update;
	}

	private void Update()
	{
		// Not the ideal approach here, but sadly I can't find a callback to use for entering/exiting play mode
		if (true == EditorApplication.isPlayingOrWillChangePlaymode)
		{
			DestroyAuditioner();
		}
		else
		{
			CreateAuditioner();
			UpdateAuditioner();
		}		
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private void CreateAuditioner()
	{
		if (null == s_AudioEmitterGameObject)
		{
			//Debug.Log("Creating...");

			AudioListener TheAudioListener = Object.FindObjectOfType(typeof(AudioListener)) as AudioListener;
			if (null == TheAudioListener)
			{
				s_AudioListenerGameObject = new GameObject("AudioEvent Auditioner Listener");
				s_AudioListenerGameObject.hideFlags = HideFlags.HideInHierarchy;
				TheAudioListener = s_AudioListenerGameObject.AddComponent<AudioListener>();
			}

			s_AudioEmitterGameObject = new GameObject("AudioEvent Auditioner");
			s_AudioEmitterGameObject.hideFlags = HideFlags.HideInHierarchy;
			AudioEmitter TheAudioEmitter = s_AudioEmitterGameObject.AddComponent<AudioEmitter>();
			s_AudioEmitterGameObject.transform.parent = TheAudioListener.gameObject.transform;
			s_AudioEmitterGameObject.transform.localPosition = Vector3.zero;
			TheAudioEmitter.Awake();
			TheAudioEmitter.Start();			
		}
	}

	private void DestroyAuditioner()
	{
		if (null != s_AudioEmitterGameObject)
		{
			//Debug.Log("Destroying AudioEmitterGameObject...");
			DestroyImmediate(s_AudioEmitterGameObject);
			s_AudioEmitterGameObject = null;		
		}

		if (null != s_AudioListenerGameObject)
		{
			//Debug.Log("Destroying AudioListenerGameObject...");
			DestroyImmediate(s_AudioListenerGameObject);
			s_AudioListenerGameObject = null;
		}
	}

	private void UpdateAuditioner()
	{
		if (null != s_AudioEmitterGameObject)
		{
			AudioEmitter TheAudioEmitter = s_AudioEmitterGameObject.GetComponent<AudioEmitter>();
			if (null != TheAudioEmitter)
			{
				TheAudioEmitter.Update(Time.fixedDeltaTime);
			}
		}
	}

	private void PlayAudioEmitter(AudioEvent AudioEventToPlay)
	{
		if (null != AudioEventToPlay)
		{
			CreateAuditioner();

			if (null != s_AudioEmitterGameObject)
			{
				AudioEmitter TheAudioEmitter = s_AudioEmitterGameObject.GetComponent<AudioEmitter>();
				if (null != TheAudioEmitter)
				{
					TheAudioEmitter.m_AudioEvent = AudioEventToPlay;
					TheAudioEmitter.Play();
				}
			}
		}
	}

	private void StopAudioEmitter()
	{
		if (null != s_AudioEmitterGameObject)
		{
			AudioEmitter TheAudioEmitter = s_AudioEmitterGameObject.GetComponent<AudioEmitter>();
			if (null != TheAudioEmitter)
			{
				TheAudioEmitter.Stop();
			}
		}
	}

	private void IntSlider(string Label, ref int Value, int Min, int Max)
	{
		Value = EditorGUILayout.IntSlider(Label, Value, Min, Max);		
	}

	private void FloatSlider(string Label, ref float Value, float Min, float Max)
	{		
		Value = EditorGUILayout.Slider(Label, Value, Min, Max);	
	}
}