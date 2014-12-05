using UnityEngine;
using System.Collections;

public class AudioMix : MonoBehaviour
{
	public bool m_ActivateAtStart = true;
	public AudioCategory[] m_AudioCategories;
	[HideInInspector] public float m_VolumeDB = AudioConstants.s_VolumeDecibelsDefault;
	[HideInInspector] public int m_Priority = AudioConstants.s_PriorityDefault;				// Lower values implies higher priority (follows AudioEvent priority scheme)
	public float m_FadeInTime = 0.0f;
	public float m_FadeOutTime = 0.0f;	

	private int m_ReferenceCount = 0;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
		
	public bool ActivateAtStart 
	{ 
		get { return (m_ActivateAtStart); }
		set { m_ActivateAtStart = value; }
	}

	public AudioCategory[] AudioCategories
	{
		get { return (m_AudioCategories); }
		set { m_AudioCategories = value; }
	}

	public float VolumeDB
	{
		get { return (m_VolumeDB); }
		set { m_VolumeDB = value; }
	}

	public int Priority
	{
		get { return (m_Priority); }
		set { m_Priority = value; }
	}

	public float FadeInTime
	{
		get { return (m_FadeInTime); }
		set { m_FadeInTime = value; }
	}

	public float FadeOutTime
	{
		get { return (m_FadeOutTime); }
		set { m_FadeOutTime = value; }
	}
				
	///////////////////////////////////////////////////////////////////////////////////////////////////////////	

	public void Awake()
	{
		AudioMixer.SetUpAudioMixerGameObject();

		m_Priority = Mathf.Clamp(m_Priority, AudioConstants.s_PriorityMin, AudioConstants.s_PriorityMax);
		m_FadeInTime = Mathf.Max(m_FadeInTime, 0.0f, m_FadeInTime);
		m_FadeOutTime = Mathf.Max(m_FadeOutTime, 0.0f, m_FadeOutTime);		
	}	
		
	public void Start()
	{
		if (true == m_ActivateAtStart)
		{
			AudioMix Self = this;
			Messenger<AudioMix>.Broadcast(AudioMixer.GetAudioMixerGameObject(), "ActivateAudioMix", ref Self);					
		}
	}
		
	public void OnDestroy()
	{
		// If the AudioMixer has already been destroyed, then we've already been deactivated.
		if (true == AudioMixer.Exists())
		{
			AudioMix Self = this;
			bool Force = true;
			Messenger<AudioMix, bool>.Broadcast(AudioMixer.GetAudioMixerGameObject(), "DeactivateAudioMix", ref Self, ref Force);
		}
	}	
	
	public void Activate()	
	{
		m_ReferenceCount++;
	}

	public void Deactivate(bool Force)
	{
		if (true == Force)
		{
			m_ReferenceCount = 1;
		}
				
		if (m_ReferenceCount > 0)
		{
			m_ReferenceCount--;
		}	
	}

	public bool GetIsActive()
	{
		return (m_ReferenceCount > 0);
	}
}
