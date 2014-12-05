using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioMixer : MonoBehaviour 
{
	private static GameObject s_AudioMixerGameObject = null;
	private const string s_GameObjectName = "AudioMixer";
	private List<AudioMix> m_AudioMixes = new List<AudioMix>();
	private Dictionary<AudioCategory, AudioMix> m_AudioCategoryTrackers = new Dictionary<AudioCategory, AudioMix>();

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public static void SetUpAudioMixerGameObject()
	{
		if (null == s_AudioMixerGameObject)
		{
			s_AudioMixerGameObject = new GameObject(s_GameObjectName);

			s_AudioMixerGameObject.AddComponent<Persistent>();
			AudioMixer AudioMixerComponent = s_AudioMixerGameObject.AddComponent<AudioMixer>();
			AudioMixerComponent.RegisterMessageHandlers();
		}
	}

	public static GameObject GetAudioMixerGameObject()
	{
		if (null == s_AudioMixerGameObject)
		{
			SetUpAudioMixerGameObject();
		}

		return (s_AudioMixerGameObject);
	}

	public static bool Exists()
	{
		return (null != s_AudioMixerGameObject);
	}

	public void OnDestroy()
	{
		//Utilities.Log(gameObject, "Destroy!");

		List<AudioMix> AudioMixesToDeactivate = new List<AudioMix>(m_AudioMixes.Count);
		foreach( var t in m_AudioMixes)
		{
			AudioMixesToDeactivate.Add(t);
		}

		foreach (AudioMix AudioMixToDeactivate in AudioMixesToDeactivate)
		{
			DeactivateAudioMix(AudioMixToDeactivate, true, false);
		}		

		UnregisterMessageHandlers();
	}

	public void Update()
	{
		UpdateAudioCategories();		
	}

	public void ReapplyAudioMix(AudioMix AudioMixToReapply)
	{
		if (null != AudioMixToReapply)
		{
			DeactivateAudioMix(AudioMixToReapply, true, false);
			ActivateAudioMix(AudioMixToReapply, false);
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private void RegisterMessageHandlers()
	{
		Messenger<AudioMix>.AddListener(gameObject, "ActivateAudioMix", OnActivateAudioMix);
		Messenger<AudioMix, bool>.AddListener(gameObject, "DeactivateAudioMix", OnDeactivateAudioMix);		
	}

	private void UnregisterMessageHandlers()
	{
		Messenger<AudioMix>.RemoveListener(gameObject, "ActivateAudioMix", OnActivateAudioMix);
		Messenger<AudioMix, bool>.RemoveListener(gameObject, "DeactivateAudioMix", OnDeactivateAudioMix);		
	}
			
	private void OnActivateAudioMix(ref AudioMix AudioMixToActivate)
	{
		ActivateAudioMix(AudioMixToActivate, true);
	}

	private void OnDeactivateAudioMix(ref AudioMix AudioMixToDeactivate, ref bool Force)
	{
		DeactivateAudioMix(AudioMixToDeactivate, Force, true);
	}

	private void RegisterAudioCategory(AudioCategory AudioCategoryToRegister)
	{
		if (false == m_AudioCategoryTrackers.ContainsKey(AudioCategoryToRegister))
		{
			//Utilities.Log(gameObject, "Registering AudioCategory " + AudioCategoryToRegister.name);

			m_AudioCategoryTrackers.Add(AudioCategoryToRegister, null);
		}
	}

	private void RegisterAudioMix(AudioMix AudioMixToRegister)
	{
		if (null != AudioMixToRegister)
		{
			if (false == m_AudioMixes.Contains(AudioMixToRegister))
			{
				//Utilities.Log(gameObject, "Registering AudioMix " + AudioMixToRegister.name);

				m_AudioMixes.Add(AudioMixToRegister);

				foreach (AudioCategory CurrentAudioCategory in AudioMixToRegister.m_AudioCategories)
				{
					RegisterAudioCategory(CurrentAudioCategory);
				}
			}
		}
	}

	private void UnregisterAudioMix(AudioMix AudioMixToUnregister)
	{
		if (null != AudioMixToUnregister)
		{
			//Utilities.Log(gameObject, "Unregistering AudioMix " + AudioMixToUnregister.name);

			m_AudioMixes.Remove(AudioMixToUnregister);

			// Other AudioMixes may be using this AudioMix's AudioCategories, so leave that registry alone
		}
	}

	private void ActivateAudioMix(AudioMix AudioMixToActivate, bool UseTransitionTime)
	{
		RegisterAudioMix(AudioMixToActivate);		

		if (null != AudioMixToActivate)
		{
			bool AudioMixWasActive = AudioMixToActivate.GetIsActive();
			AudioMixToActivate.Activate();

			if ((false == AudioMixWasActive) && (true == AudioMixToActivate.GetIsActive()))
			{
				float TransitionTime = ((true == UseTransitionTime) ? AudioMixToActivate.m_FadeInTime : 0.0f);
				foreach (AudioCategory CurrentAudioCategory in AudioMixToActivate.m_AudioCategories)
				{
					AudioMix CurrentAudioMix = m_AudioCategoryTrackers[CurrentAudioCategory];
					if ((null == CurrentAudioMix) || (AudioMixToActivate.m_Priority < CurrentAudioMix.m_Priority))
					{
						CurrentAudioCategory.SetVolumeScalar(Utilities.VolumeDecibelsToScalar(AudioMixToActivate.m_VolumeDB), TransitionTime);
						m_AudioCategoryTrackers[CurrentAudioCategory] = AudioMixToActivate;

						//Utilities.Log(gameObject, "AudioMix for AudioCategory " + CurrentAudioCategory.name + " is now " + AudioMixToActivate.name);
					}					
				}
			}
		}
	}

	private void DeactivateAudioMix(AudioMix AudioMixToDeactivate, bool Force, bool UseTransitionTime)
	{
		if (null != AudioMixToDeactivate)
		{
			bool AudioMixWasActive = AudioMixToDeactivate.GetIsActive();
			AudioMixToDeactivate.Deactivate(Force);

			if ((true == AudioMixWasActive) && (false == AudioMixToDeactivate.GetIsActive()))
			{
				float TransitionTime = ((true == UseTransitionTime) ? AudioMixToDeactivate.m_FadeOutTime : 0.0f);

				List<AudioCategory> AudioCategoriesUsingAudioMixToDeactivate = new List<AudioCategory>();
				foreach (KeyValuePair<AudioCategory, AudioMix> AudioCategoryTracker in m_AudioCategoryTrackers)
				{
					if (AudioCategoryTracker.Value == AudioMixToDeactivate)
					{
						AudioCategoriesUsingAudioMixToDeactivate.Add(AudioCategoryTracker.Key);
					}
				}
				foreach (AudioCategory AudioCategoryUsingAudioMixToDeactivate in AudioCategoriesUsingAudioMixToDeactivate)
				{
					RemoveAudioMixForAudioCategory(AudioCategoryUsingAudioMixToDeactivate, TransitionTime);
				}

				UnregisterAudioMix(AudioMixToDeactivate);
			}
		}
	}

	private void RemoveAudioMixForAudioCategory(AudioCategory TargetAudioCategory, float TransitionTime)
	{
		AudioMix FallbackAudioMix = GetHighestPriorityActiveAudioMixForAudioCategory(TargetAudioCategory);
		if (null != FallbackAudioMix)
		{
			TargetAudioCategory.SetVolumeScalar(Utilities.VolumeDecibelsToScalar(FallbackAudioMix.m_VolumeDB), TransitionTime);

			//Utilities.Log(gameObject, "AudioMix for AudioCategory " + TargetAudioCategory.name + " is now " + FallbackAudioMix.name);
		}
		else
		{
			TargetAudioCategory.SetVolumeScalar(Utilities.VolumeDecibelsToScalar(TargetAudioCategory.m_VolumeDB), TransitionTime);

			//Utilities.Log(gameObject, "AudioMix for AudioCategory " + TargetAudioCategory.name + " reverting to default levels");
		}

		m_AudioCategoryTrackers[TargetAudioCategory] = FallbackAudioMix;
	}

	private AudioMix GetHighestPriorityActiveAudioMixForAudioCategory(AudioCategory TargetAudioCategory)
	{
		AudioMix HighestPriorityActiveAudioMix = null;
		foreach (AudioMix CurrentAudioMix in m_AudioMixes)
		{
			if (true == CurrentAudioMix.GetIsActive())
			{
				foreach (AudioCategory CurrentAudioCategory in CurrentAudioMix.m_AudioCategories)
				{
					if (CurrentAudioCategory == TargetAudioCategory)
					{
						if ((null == HighestPriorityActiveAudioMix) || (CurrentAudioMix.m_Priority < HighestPriorityActiveAudioMix.m_Priority))
						{
							HighestPriorityActiveAudioMix = CurrentAudioMix;							
						}

						break;
					}
				}
			}
		}

		return (HighestPriorityActiveAudioMix);
	}

	private void UpdateAudioCategories()
	{
		foreach (KeyValuePair<AudioCategory, AudioMix> AudioCategoryTracker in m_AudioCategoryTrackers)
		{			
			AudioCategoryTracker.Key.UpdateLerpers(Time.deltaTime);
		}
	}	
}
