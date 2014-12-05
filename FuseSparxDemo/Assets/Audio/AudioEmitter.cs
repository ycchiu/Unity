using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioEmitter : MonoBehaviour 
{
	public enum PlayMode
	{
		Interrupt,	// Stick to this object in 3D, interrupting any previous playback.
		Stack,		// Stick to this object in 3D, stacking with any previous playback.
		Breadcrumb,	// Play at this object's position in 3D and leave behind. 
		Orphan		// Play at this object's position in 3D and play independently
	}
	public enum FadeState
	{
		None,
		In,
		Out	
	}
	
	public bool m_PlayAtStart = false;
	public AudioEvent m_AudioEvent = null;
	public PlayMode m_PlayMode = PlayMode.Interrupt;
	[HideInInspector] public float m_InitialVolumeDB = AudioConstants.s_VolumeDecibelsDefault;
	[HideInInspector] public float m_InitialPitchST = AudioConstants.s_PitchSemitonesDefault;

	private class AudioSourceTracker
	{
		public AudioEvent m_AudioEvent = null;
		public AudioSource m_AudioSource = null;
		public float m_NativeVolumeScalar = AudioConstants.s_VolumeScalarDefault;
		public float m_NativePitchScalar = AudioConstants.s_PitchScalarDefault;
		public Timer m_FadeTimer = new Timer(0.0f, true);
		public FadeState m_FadeState = FadeState.None;		
	};

	private const string s_ClonedAudioSourceGameObjectNameSuffix = "_AudioEmitter";
	private float m_VolumeScalar = AudioConstants.s_VolumeScalarDefault;
	private float m_PitchScalar = AudioConstants.s_PitchScalarDefault;	
	private List<AudioSourceTracker> m_AudioSourceTrackers = new List<AudioSourceTracker>();
	private Timer m_FadeTimer = null;
	private FadeState m_FadeState = FadeState.None;
	private bool m_IsMuted = false;
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Awake()
	{
		AudioSource NativeAudioSource = gameObject.AddComponent<AudioSource>();		
		NativeAudioSource.playOnAwake = false;
		NativeAudioSource.volume = AudioConstants.s_VolumeScalarDefault;
		AddAudioSourceTracker(NativeAudioSource);
		m_FadeTimer = new Timer(0.0f, true);
		m_VolumeScalar = Utilities.VolumeDecibelsToScalar(m_InitialVolumeDB);
		m_PitchScalar = Utilities.PitchSemitonesToScalar(m_InitialPitchST);
	}
	
	public void Start()
	{
		if (true == m_PlayAtStart)
		{
			Play();
		}
	}

	public void OnDestroy()
	{		
		for (int AudioSourceTrackerIndex = 1; AudioSourceTrackerIndex < m_AudioSourceTrackers.Count; ++AudioSourceTrackerIndex)
		{
			AudioSourceTracker CurrentAudioSourceTracker = m_AudioSourceTrackers[AudioSourceTrackerIndex];
			if (null != CurrentAudioSourceTracker.m_AudioSource)
			{
				GameObject.Destroy(CurrentAudioSourceTracker.m_AudioSource.gameObject);
			}
		}
		m_AudioSourceTrackers.Clear();
	}

	public void Update()
	{	
		UpdateAudioSources();
	}

	public void Update(float TimeDelta)
	{
		UpdateAudioSources(TimeDelta);
	}
	
	public void SetVolumeScalar(float VolumeScalar)
	{
		m_VolumeScalar = AudioControl.SFXLevel * Mathf.Clamp(VolumeScalar, AudioConstants.s_VolumeScalarMin, AudioConstants.s_VolumeScalarMax);
	}
	
	public void SetVolumeDB(float VolumeDB)
	{
		SetVolumeScalar(Utilities.VolumeDecibelsToScalar(VolumeDB));
	}

	public void SetPitchScalar(float PitchScalar)
	{
		m_PitchScalar = Mathf.Clamp(PitchScalar, AudioConstants.s_PitchScalarMin, AudioConstants.s_PitchScalarMax);
	}
	
	public float PitchScalar{get{return m_PitchScalar;}}

	public void SetPitchST(float PitchST)
	{
		SetPitchScalar(Utilities.PitchSemitonesToScalar(PitchST));
	}
		
	public bool Play()
	{
		return SelectAndPlayAudioClip();
	}

	public bool Play(AudioEvent NewAudioEvent)
	{
		m_AudioEvent = NewAudioEvent;
		return (Play());
	}

	public bool Stop(bool OnlyLoops, bool AllowNativeFadeOut)
	{
		foreach (AudioSourceTracker CurrentAudioSourceTracker in m_AudioSourceTrackers)
		{
			if (null != CurrentAudioSourceTracker.m_AudioSource)
			{
				if ((false == OnlyLoops) || (true == CurrentAudioSourceTracker.m_AudioSource.loop))
				{
					if ((null != CurrentAudioSourceTracker.m_AudioEvent) && (true == AllowNativeFadeOut) && (CurrentAudioSourceTracker.m_AudioEvent.m_FadeOutTime > 0.0f))
					{
						SetFade(CurrentAudioSourceTracker.m_FadeTimer, ref CurrentAudioSourceTracker.m_FadeState, FadeState.Out, CurrentAudioSourceTracker.m_AudioEvent.m_FadeOutTime);						
					}
					else
					{
						CurrentAudioSourceTracker.m_AudioSource.Stop();
					}
				}
			}
		}
		
		return (true);
	}

	public bool Stop(bool OnlyLoops)
	{
		return (Stop(OnlyLoops, true));
	}

	public bool Stop()
	{
		return (Stop(false, true));
	}

	public bool FadeIn(float FadeTime)
	{
		SetFade(m_FadeTimer, ref m_FadeState, FadeState.In, FadeTime);
		
		UpdateAudioSources();

		return (true);
	}

	public bool FadeOut(float FadeTime)
	{
		SetFade(m_FadeTimer, ref m_FadeState, FadeState.Out, FadeTime);
		
		UpdateAudioSources();

		return (true);
	}

	public bool GetIsPlaying(AudioEvent TestAudioEvent)
	{
		foreach (AudioSourceTracker CurrentAudioSourceTracker in m_AudioSourceTrackers)
		{
			if (null != CurrentAudioSourceTracker.m_AudioSource)
			{
				if ((true == AudioListener.pause) || (true == CurrentAudioSourceTracker.m_AudioSource.isPlaying))
				{	
					if ((null == TestAudioEvent) || ((null != CurrentAudioSourceTracker.m_AudioEvent) && (TestAudioEvent.GetInstanceID() == CurrentAudioSourceTracker.m_AudioEvent.GetInstanceID())))
					{
						return (true);
					}
				}
			}
		}
	
		return (false);
	}

	public bool GetIsPlaying()
	{
		return (GetIsPlaying(null));
	}

	public void Mute(bool DoMute)
	{
		foreach (AudioSourceTracker CurrentAudioSourceTracker in m_AudioSourceTrackers)
		{
			CurrentAudioSourceTracker.m_AudioSource.mute = DoMute;
		}
		
		m_IsMuted = DoMute;
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private AudioSourceTracker AddAudioSourceTracker(AudioSource AudioSourceToTrack)
	{
		AudioSourceTracker NewAudioSourceTracker = new AudioSourceTracker();
		NewAudioSourceTracker.m_AudioSource = AudioSourceToTrack;
		NewAudioSourceTracker.m_NativeVolumeScalar = AudioSourceToTrack.volume;
		NewAudioSourceTracker.m_NativePitchScalar = AudioSourceToTrack.pitch;
		m_AudioSourceTrackers.Add(NewAudioSourceTracker);

		return (NewAudioSourceTracker);
	}
	
	private void RemoveAudioSourceTracker(int AudioSourceTrackerIndex)
	{
		m_AudioSourceTrackers.RemoveAt(AudioSourceTrackerIndex);
	}

	private void SetFade(Timer FadeTimer, ref FadeState CurrentFadeState, FadeState NewFadeState, float FadeTime)
	{
		if (FadeState.None == NewFadeState)
		{
			FadeTimer.Elapse();
			CurrentFadeState = FadeState.None;
		}
		else
		{
			FadeTime = Mathf.Max(FadeTime, Constants.s_SmallDelta);

			float OldLength = FadeTimer.GetLength();
			float OldElapsed = FadeTimer.GetElapsed();
			float OldRemaining = FadeTimer.GetRemaining();
			FadeTimer.SetLength(FadeTime);

			if (FadeState.In == NewFadeState)
			{
				if (FadeState.Out == CurrentFadeState)
				{
					FadeTimer.SetRemaining((OldElapsed / OldLength) * FadeTime);
				}
				else if (FadeState.In == CurrentFadeState)
				{
					FadeTimer.SetRemaining((OldRemaining / OldLength) * FadeTime);
				}
				else
				{
					FadeTimer.Reset();
				}

				//Utilities.Log(gameObject, "Fading in (" + CurrentFadeState + "):  " + FadeTimer.GetElapsed() + " / " + FadeTimer.GetLength());

				CurrentFadeState = FadeState.In;
			}
			else if (FadeState.Out == NewFadeState)
			{
				if (FadeState.In == CurrentFadeState)
				{
					FadeTimer.SetRemaining((OldElapsed / OldLength) * FadeTime);
				}
				else if (FadeState.Out == CurrentFadeState)
				{
					FadeTimer.SetRemaining((OldRemaining / OldLength) * FadeTime);
				}
				else
				{
					FadeTimer.Reset();
				}

				//Utilities.Log(gameObject, "Fading out (" + CurrentFadeState + "):  " + FadeTimer.GetElapsed() + " / " + FadeTimer.GetLength());

				CurrentFadeState = FadeState.Out;
			}
		}
	}

	private bool SelectAndPlayAudioClip()
	{		
		bool Result = false;

		if ((null != m_AudioEvent) && (true == m_AudioEvent.GetHasAudioClips()))
		{			
			AudioSource AudioSourceToPlay = null;
			AudioSourceTracker AudioSourceToPlayTracker = null;
			switch (m_PlayMode)
			{
			case PlayMode.Interrupt:
					
				AudioSourceToPlayTracker = m_AudioSourceTrackers[0];
				AudioSourceToPlayTracker.m_AudioSource.Stop();
				AudioSourceToPlayTracker.m_FadeState = FadeState.None;
				AudioSourceToPlay = AudioSourceToPlayTracker.m_AudioSource;
				if (false == m_AudioEvent.PrimeAudioSource(AudioSourceToPlay))
				{
					EB.Debug.LogError(name + ":  Failed to prime AudioSource using AudioEvent " + m_AudioEvent.name);
				}
				else
				{
					Result = true;
				}
				
				AudioSourceToPlayTracker.m_NativeVolumeScalar = AudioSourceToPlay.volume;
				AudioSourceToPlayTracker.m_NativePitchScalar = AudioSourceToPlay.pitch;
				
				break;
				
			case PlayMode.Stack:
			case PlayMode.Breadcrumb:
			case PlayMode.Orphan: 
					
				GameObject ClonedAudioSourceGameObject = new GameObject(gameObject.name + s_ClonedAudioSourceGameObjectNameSuffix);
				AudioSourceToPlay = ClonedAudioSourceGameObject.AddComponent<AudioSource>();
				
			//	AudioSourceToPlay.volume = m_AudioEvent.m_AudioCategory.GetVolumeScalar();//AudioCo nstants.s_VolumeScalarDefault;
				if (false == m_AudioEvent.PrimeAudioSource(AudioSourceToPlay))
				{
					EB.Debug.LogError(name + ":  Failed to prime AudioSource using AudioEvent " + m_AudioEvent.name);
					GameObject.Destroy(ClonedAudioSourceGameObject);
				}
				else
				{
					if (null == AudioSourceToPlay.clip)
					{
						// This may happen if the AudioEvent failed to play due to "ChanceToNotPlay"							
						GameObject.Destroy(ClonedAudioSourceGameObject);							
					}
					else
					{ 
						AudioSourceToPlay.volume = m_AudioEvent.m_AudioCategory.GetVolumeScalar();
						if (PlayMode.Orphan != m_PlayMode)
						{
							AudioSourceToPlayTracker = AddAudioSourceTracker(AudioSourceToPlay);
						}
						
						if (PlayMode.Stack == m_PlayMode)
						{
							ClonedAudioSourceGameObject.transform.parent = gameObject.transform;
							ClonedAudioSourceGameObject.transform.localPosition = Vector3.zero;								
						}
						else
						{
							ClonedAudioSourceGameObject.transform.Translate(transform.position, Space.World);								
						}						
													
						Result = true;
					}						
				}					

				break;
			
			default:
				EB.Debug.LogError("Invalid SourceMode " + m_PlayMode + " !");
				break;
			}
			
			if (true == Result)
			{
				if (null != AudioSourceToPlay.clip)
				{
					if (PlayMode.Orphan == m_PlayMode)
					{
						AudioSourceToPlay.volume *= AudioSourceToPlay.volume = m_VolumeScalar;
					
						AudioSourceToPlay.Play();
						Object.Destroy(AudioSourceToPlay.gameObject, AudioSourceToPlay.clip.length);
					}
					else
					{
						AudioSourceToPlayTracker.m_AudioEvent = m_AudioEvent;
	
						if (m_AudioEvent.m_FadeInTime > 0.0f)
						{
							SetFade(AudioSourceToPlayTracker.m_FadeTimer, ref AudioSourceToPlayTracker.m_FadeState, FadeState.In, m_AudioEvent.m_FadeInTime);						
						}
						else
						{
							SetFade(AudioSourceToPlayTracker.m_FadeTimer, ref AudioSourceToPlayTracker.m_FadeState, FadeState.None, m_AudioEvent.m_FadeInTime);						
						}
	
						float EmitterFadeVolumeScalar = GetFadeVolumeScalar(m_FadeState, m_FadeTimer);
						float AudioSourceFadeVolumeScalar = GetFadeVolumeScalar(AudioSourceToPlayTracker.m_FadeState, AudioSourceToPlayTracker.m_FadeTimer);
						AudioSourceToPlay.volume = m_VolumeScalar * AudioSourceToPlayTracker.m_NativeVolumeScalar * AudioSourceToPlayTracker.m_AudioEvent.GetAudioCategoryVolumeScalar() * EmitterFadeVolumeScalar * AudioSourceFadeVolumeScalar;
						AudioSourceToPlay.pitch = m_PitchScalar * AudioSourceToPlayTracker.m_NativePitchScalar;
						AudioSourceToPlay.mute = m_IsMuted;
						
						AudioSourceToPlay.enabled = (AudioControl.SFXLevel > 0.0f);
						
						//	EB.Debug.LogWarning(string.Format("Audio source muted {0} vol {1} pitch {2}", AudioSourceToPlay.mute, AudioSourceToPlay.volume, AudioSourceToPlay.pitch));
						
						// Harms:  for some reason, adding this slight pre-delay prevents sounds from inexplicably stopping on the first frame.  
						// Bug has been reported to Unity, but for now this is our best workaround.
						AudioSourceToPlay.Play();	
#if GRAEME
						EB.Debug.Log(string.Format(">>>>> PLAYING {0} ({1}) at V = {2}", m_AudioEvent.name, AudioSourceToPlay.clip, AudioSourceToPlay.volume));
#endif
					}
				}
			}
		}

		return (Result);
	}

	private static float GetFadeVolumeScalar(FadeState FadeState, Timer FadeTimer)
	{
		float FadeVolumeScalar = AudioConstants.s_VolumeScalarDefault;		
		switch (FadeState)
		{
			case FadeState.None:
			{
				FadeVolumeScalar = AudioConstants.s_VolumeScalarMax;
				break;
			}

			case FadeState.In:
			{
				FadeVolumeScalar = FadeTimer.GetElapsedRatio();
				break;
			}

			case FadeState.Out:
			{
				FadeVolumeScalar = FadeTimer.GetRemainingRatio();			
				break;
			}
		}

		return (FadeVolumeScalar);
	}

	private void HousecleanAudioSources()
	{
		// Note that we don't ever clean out the AudioSourceTracker at index 0.  
		// This tracks the native AudioSource, which remains alive -- even when not playing anything
		for (int AudioSourceTrackerIndex = (m_AudioSourceTrackers.Count - 1); AudioSourceTrackerIndex > 0; --AudioSourceTrackerIndex)
		{
			AudioSourceTracker CurrentAudioSourceTracker = m_AudioSourceTrackers[AudioSourceTrackerIndex];
			if (null != CurrentAudioSourceTracker.m_AudioSource)
			{
				if ((false == AudioListener.pause) && (false == CurrentAudioSourceTracker.m_AudioSource.isPlaying))
				{
					Destroy(CurrentAudioSourceTracker.m_AudioSource.gameObject);
					CurrentAudioSourceTracker.m_AudioSource = null;
				}				
			}

			if (null == CurrentAudioSourceTracker.m_AudioSource)
			{
				RemoveAudioSourceTracker(AudioSourceTrackerIndex);
			}
		}
	}

	private void UpdateAudioSources(float TimeDelta)
	{		
		if (m_FadeTimer==null)
		{
			EB.Debug.Log ("FADE TIMER NULL)");
			return;
		}
		HousecleanAudioSources();

		m_FadeTimer.Update(TimeDelta);		
		float EmitterFadeVolumeScalar = GetFadeVolumeScalar(m_FadeState, m_FadeTimer);

		foreach (AudioSourceTracker CurrentAudioSourceTracker in m_AudioSourceTrackers)		
		{
			CurrentAudioSourceTracker.m_FadeTimer.Update(TimeDelta);			
			float AudioSourceFadeVolumeScalar = GetFadeVolumeScalar(CurrentAudioSourceTracker.m_FadeState, CurrentAudioSourceTracker.m_FadeTimer);
			
			CurrentAudioSourceTracker.m_AudioSource.volume = m_VolumeScalar * CurrentAudioSourceTracker.m_NativeVolumeScalar * EmitterFadeVolumeScalar * AudioSourceFadeVolumeScalar;
 			if (null != CurrentAudioSourceTracker.m_AudioEvent)
 			{
 				CurrentAudioSourceTracker.m_AudioSource.volume *= CurrentAudioSourceTracker.m_AudioEvent.GetAudioCategoryVolumeScalar();
 			}
			
			CurrentAudioSourceTracker.m_AudioSource.mute = m_IsMuted;
 					
			/*
  			if (CurrentAudioSourceTracker.m_FadeState != FadeState.None)
 			{				
 				Utilities.Log(gameObject, "AudioSource " + CurrentAudioSourceTracker.m_AudioSource.gameObject.name + " has volume " + 
					CurrentAudioSourceTracker.m_AudioSource.volume.ToString("F2") + 
					" = Emitter (" + m_VolumeScalar.ToString("F2") + 
					") * Native (" + CurrentAudioSourceTracker.m_NativeVolumeScalar.ToString("F2") + 
		            ") * EmitterFade (" + EmitterFadeVolumeScalar.ToString("F2") + 
					") * TrackerFade (" + AudioSourceFadeVolumeScalar.ToString("F2") + 
					") * Category (" + CurrentAudioSourceTracker.m_AudioEvent.GetAudioCategoryVolumeScalar().ToString("F2") +  		
			        ") [" + CurrentAudioSourceTracker.m_FadeTimer.GetRemaining().ToString("F2") + " / " + CurrentAudioSourceTracker.m_FadeTimer.GetLength().ToString("F2") + "] - " + CurrentAudioSourceTracker.m_FadeState);
  			}*/

			if ((FadeState.Out == CurrentAudioSourceTracker.m_FadeState) && (true == CurrentAudioSourceTracker.m_FadeTimer.GetIsElapsed()))
			{
				CurrentAudioSourceTracker.m_AudioSource.Stop();
				CurrentAudioSourceTracker.m_FadeState = FadeState.None;
			}
			
			// GA: Is this our only way of dynamic pitch shifting?
			CurrentAudioSourceTracker.m_AudioSource.pitch = m_PitchScalar * CurrentAudioSourceTracker.m_NativePitchScalar;
//			if (m_AudioEvent != null && m_AudioEvent.name.EndsWith("XPBarFill"))
//			{
//				EB.Debug.Log (string.Format("AudioEmitter update pitch {0} on {1}", CurrentAudioSourceTracker.m_AudioSource.pitch, m_AudioEvent == null ? "<missing>" : m_AudioEvent.name));
//			}
		}

		if ((FadeState.Out == m_FadeState) && (true == m_FadeTimer.GetIsElapsed()))
		{
			Stop(false, false);
			m_FadeState = FadeState.None;
		}					
	}

	private void UpdateAudioSources()
	{
		UpdateAudioSources(Time.deltaTime);
	}
	
	// UI
	void OnClick()
	{
#if GRAEME 
		EB.Debug.Log("AudioEmitter.ONCLICK " + name + "->" + transform.parent.name);
#endif
		if (!IgnoreOnClickMessages)
		{
			this.Play();
		}
	}
	
	public bool IgnoreOnClickMessages{get;set;}
	
#if GRAEME
	public override string ToString ()
	{
		if (m_AudioEvent != null)
		{
			return string.Format("name {0} vol {1} fade {2} > {3}", m_AudioEvent.name, this.m_VolumeScalar, this.m_FadeState, GetIsPlaying().ToString());
		}
		else
		{
			return "<Missing>";
		}
	}
#endif
}
