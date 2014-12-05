using UnityEngine;
using System.Collections;

public class AudioJukebox : MonoBehaviour 
{
	public AudioEvent m_AudioEvent;
	public float m_FadeInTime = 1.0f;
	public float m_FadeOutTime = 1.0f;	
	public float m_CycleTime = 60.0f;
	public bool m_IsForMusic = true;
	
	AudioEmitter[] m_AudioEmitters = new AudioEmitter[2];
	private int m_CurrentAudioEmitterID = 0;
	private Timer m_CycleTimer;	
		
	private bool 	_paused = true;
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Awake()
	{
		GlobalSettings.SetUpGlobalGameSettingsGameObject();

		m_AudioEmitters[0] = gameObject.AddComponent<AudioEmitter>();
		m_AudioEmitters[0].m_AudioEvent = m_AudioEvent;
		m_AudioEmitters[1] = gameObject.AddComponent<AudioEmitter>();
		m_AudioEmitters[1].m_AudioEvent = m_AudioEvent;
		
		
		m_AudioEmitters[0].Stop ();
		m_AudioEmitters[1].Stop ();
		
		m_CurrentAudioEmitterID = 0;
		m_CycleTimer = new Timer(m_CycleTime, true);
	}

	public void Start()
	{
		if (true == m_IsForMusic)
		{
			bool IsMusicMuted = false;
			Messenger<bool>.Broadcast(GlobalSettings.GetGlobalSettingsGameObject(), "GetIsMusicMuted", ref IsMusicMuted);
			Mute(IsMusicMuted);
		}
	}
	
	public void OnEnable()
	{
		Messenger.AddListener(gameObject, "Restart", OnRestart);
		Messenger<bool, bool>.AddListener(gameObject, "MuteAudioJukebox", OnMuteAudioJukebox);
		Messenger<AudioEvent>.AddListener(gameObject, "SetAudioJukeboxAudioEvent", OnSetAudioJukeboxAudioEvent);
		Messenger.AddListener(gameObject, "SetAudioJukeboxAudioEventNull", OnSetAudioJukeboxAudioEventNull);
	}

	public void OnDisable()
	{
		Messenger.RemoveListener(gameObject, "Restart", OnRestart);
		Messenger<bool, bool>.RemoveListener(gameObject, "MuteAudioJukebox", OnMuteAudioJukebox);
		Messenger<AudioEvent>.RemoveListener(gameObject, "SetAudioJukeboxAudioEvent", OnSetAudioJukeboxAudioEvent);	
		Messenger.RemoveListener(gameObject, "SetAudioJukeboxAudioEventNull", OnSetAudioJukeboxAudioEventNull);	
	}
			
	public void Update()
	{
		if (!_paused)
		{
			m_CycleTimer.Update();
			if (true == m_CycleTimer.GetIsElapsed())
			{
				PlayNewAudioClip();
				m_CycleTimer.Reset();
			}
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////	

	private void OnRestart()
	{		
		_paused = false;
		SetAudioEvent(m_AudioEvent);		
	}

	private void OnMuteAudioJukebox(ref bool DoMute, ref bool OnlyIfIsForMusic)
	{
		if ((false == OnlyIfIsForMusic) || (true == m_IsForMusic))
		{
			Mute(DoMute);
		}
	}
	
	private void OnSetAudioJukeboxAudioEventNull()
	{
		SetAudioEvent(null);
	}
	
	private void OnSetAudioJukeboxAudioEvent(ref AudioEvent AudioEvent)
	{
		SetAudioEvent(AudioEvent);
	}

	private void Mute(bool DoMute)
	{
		m_AudioEmitters[0].Mute(DoMute);
		m_AudioEmitters[1].Mute(DoMute);
	}

	private void SetAudioEvent(AudioEvent AudioEvent)
	{
		m_AudioEmitters[0].m_AudioEvent = AudioEvent;
		m_AudioEmitters[1].m_AudioEvent = AudioEvent;
		PlayNewAudioClip();
		m_CycleTimer.Reset();
	}
	
	private void PlayNewAudioClip()
	{
		FadeOutCurrentAudioEmitter(m_FadeOutTime);
		m_CurrentAudioEmitterID = ((0 == m_CurrentAudioEmitterID) ? 1 : 0);
		m_AudioEmitters[m_CurrentAudioEmitterID].Play();
		m_AudioEmitters[m_CurrentAudioEmitterID].FadeIn(m_FadeInTime);	
	}

	private void FadeOutCurrentAudioEmitter(float FadeTime)
	{
		if (true == m_AudioEmitters[m_CurrentAudioEmitterID].GetIsPlaying())
		{
			m_AudioEmitters[m_CurrentAudioEmitterID].FadeOut(FadeTime);
		}
	}
	
#if GRAEME
	private void OnGUI()
	{
		const float dy = 20.0f;
		const float x = 300.0f;
		float y = 200.0f;
		
		GUI.Label(new Rect(x, y+=20, 500, dy), "emtr 0: " + m_AudioEmitters[0].ToString ());
		GUI.Label(new Rect(x, y+=20, 500, dy), "emtr 1: " + m_AudioEmitters[1].ToString ());
	}
#endif
}
