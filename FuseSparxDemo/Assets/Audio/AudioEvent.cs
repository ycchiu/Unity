using UnityEngine;
using System.Collections;

public class AudioEvent : MonoBehaviour
{
	public enum SelectMode
	{
		Sequential,
		Random,
		RandomNoRepeat
	}
	public AudioClip[] m_AudioClips;
	public SelectMode m_SelectMode = SelectMode.RandomNoRepeat;
	public AudioCategory m_AudioCategory = null;
	[HideInInspector] public int m_ChanceToNotPlay = 0;
	[HideInInspector] public float m_VolumeDB = AudioConstants.s_VolumeDecibelsDefault;
	[HideInInspector] public float m_VolumeDBRandomDelta = 0.0f;
	[HideInInspector] public float m_FadeInTime = 0.0f;
	[HideInInspector] public float m_FadeOutTime = 0.0f;
	[HideInInspector] public float m_PitchST = AudioConstants.s_PitchSemitonesDefault;
	[HideInInspector] public float m_PitchSTRandomDelta = 0.0f;
	public bool m_Loop = false;
	[HideInInspector] public int m_Priority = AudioConstants.s_PriorityDefault;
	[HideInInspector] public float m_DopplerLevel = AudioConstants.s_DopplerLevelDefault;
	[HideInInspector] public float m_MinDistance = AudioConstants.s_MinDistanceDefault;
	[HideInInspector] public float m_MaxDistance = AudioConstants.s_MaxDistanceDefault;
	public AudioRolloffMode m_RolloffMode = AudioConstants.s_RolloffModeDefault;	
	
	private int m_LastPlayedAudioClipIndex = -1;	

	///////////////////////////////////////////////////////////////////////////////////////////////////////////	

	public bool GetHasAudioClips()
	{
		return (m_AudioClips != null) && (0 != m_AudioClips.Length);
	}
		
	public bool PrimeAudioSource(AudioSource AudioSource)
	{	
		if (false == GetHasAudioClips())
		{
			EB.Debug.LogError(name + ": No AudioClips to play!");
			return (false);
		}

		if (null == m_AudioCategory)
		{
			EB.Debug.LogError(name + ": No AudioCategory assigned!");
			return (false);
		}
		
		AudioSource.clip = null;
		if ((0 == m_ChanceToNotPlay) || (Random.Range(1, 100) > m_ChanceToNotPlay))
		{
			int SelectedAudioClipIndex = -1;
			switch (GetSelectMode())
			{
				case SelectMode.Sequential:
				{
					SelectedAudioClipIndex = (m_LastPlayedAudioClipIndex + 1) % m_AudioClips.Length;
					AudioSource.clip = m_AudioClips[SelectedAudioClipIndex];

					break;
				}

				case SelectMode.Random:
				{
					SelectedAudioClipIndex = Random.Range(0, m_AudioClips.Length);
					AudioSource.clip = m_AudioClips[SelectedAudioClipIndex];

					break;
				}

				case SelectMode.RandomNoRepeat:
				{
					SelectedAudioClipIndex = m_LastPlayedAudioClipIndex;
					int TryCount = 0;
					do
					{
						SelectedAudioClipIndex = Random.Range(0, m_AudioClips.Length);
						TryCount++;
					} while ((SelectedAudioClipIndex == m_LastPlayedAudioClipIndex) && (TryCount < Constants.s_SafetyLoopCount));
					AudioSource.clip = m_AudioClips[SelectedAudioClipIndex];

					break;
				}

				default:
				{
					EB.Debug.LogError(name + ": Invalid SelectMode " + m_SelectMode + " !");
					return (false);
				}
			}

			m_LastPlayedAudioClipIndex = SelectedAudioClipIndex;

			AudioSource.volume = SelectVolumeScalar();
			AudioSource.pitch = SelectPitchScalar();
			AudioSource.loop = m_Loop;
			AudioSource.priority = m_Priority;
			AudioSource.dopplerLevel = m_DopplerLevel;
			AudioSource.rolloffMode = m_RolloffMode;
			AudioSource.minDistance = m_MinDistance;
			AudioSource.maxDistance = Mathf.Max(m_MaxDistance, (m_MinDistance * 1.01f)); // Inspector spams errors if max is at least not 1% larger than min
			
			AudioSource.enabled = (AudioControl.SFXLevel > 0.0f);
		}

		return (true);
	}

	public float GetAudioCategoryVolumeScalar()
	{
		if (null == m_AudioCategory)
		{
			return (AudioConstants.s_VolumeScalarDefault);
		}
		else
		{
			return (m_AudioCategory.GetVolumeScalar());
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private SelectMode GetSelectMode()
	{
		SelectMode SelectMode = SelectMode.Sequential;
		if (m_AudioClips.Length > 1)
		{
			SelectMode = m_SelectMode;
		}

		return (SelectMode);
	}

	private float SelectVolumeScalar()
	{
		float VolumeRandomDelta = Random.Range(-(Mathf.Abs(m_VolumeDBRandomDelta)), (Mathf.Abs(m_VolumeDBRandomDelta)));
		float VolumeScalar = Utilities.VolumeDecibelsToScalar(Mathf.Clamp(m_VolumeDB + VolumeRandomDelta, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax));
		return (VolumeScalar);
	}

	private float SelectPitchScalar()
	{
		float PitchRandomDelta = Random.Range(-(Mathf.Abs(m_PitchSTRandomDelta)), (Mathf.Abs(m_PitchSTRandomDelta)));
		float PitchScalar = Utilities.PitchSemitonesToScalar(Mathf.Clamp(m_PitchST + PitchRandomDelta, AudioConstants.s_PitchSemitonesMin, AudioConstants.s_PitchSemitonesMax));
		return (PitchScalar);
	}
}

	