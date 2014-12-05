using UnityEngine;
using System.Collections;

public class AudioJukeboxSequenced : MonoBehaviour 
{
	[System.Serializable]
	public class AudioJukeboxSequenceNode
	{
		public AudioEvent m_AudioEvent;
		public float m_Duration = 0.0f;		
	}
	public AudioJukeboxSequenceNode[] m_AudioJukeboxSequenceNodes;
	public bool m_IsForMusic = true;
		
	private AudioEmitter m_AudioEmitter;
	private int m_CurrentAudioJukeboxSequenceNodeIndex = -1;
	private Timer m_Timer;	
		
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Awake()
	{
		GlobalSettings.SetUpGlobalGameSettingsGameObject();
				
		m_AudioEmitter = gameObject.AddComponent<AudioEmitter>();
		m_AudioEmitter.m_PlayAtStart = false;
		m_AudioEmitter.m_PlayMode = AudioEmitter.PlayMode.Stack;
		m_Timer = new Timer(0.0f, true);
	}

	public void Start()
	{
		//Utilities.Log(gameObject, "Start!");

		if (true == m_IsForMusic)
		{
			// ARNEL
			bool IsMusicMuted = EB.Options.SFX == 0;
			Messenger<bool>.Broadcast(GlobalSettings.GetGlobalSettingsGameObject(), "GetIsMusicMuted", ref IsMusicMuted);
			Mute(IsMusicMuted);
		}

		Restart();
	}
	
	public void OnEnable()
	{
		EB.Debug.Log("Adding audiojukebox callbacks.");
		Messenger.AddListener(gameObject, "Restart", OnRestart);
		Messenger.AddListener(gameObject, "AudioJukeboxAdvance", OnAudioJukeboxAdvance);
		Messenger<bool, bool>.AddListener(gameObject, "MuteAudioJukebox", OnMuteAudioJukebox);		
	}

	public void OnDisable()
	{
		EB.Debug.Log("Removing audiojukebox callbacks.");
		Messenger.RemoveListener(gameObject, "Restart", OnRestart);
		Messenger.RemoveListener(gameObject, "AudioJukeboxAdvance", OnAudioJukeboxAdvance);
		Messenger<bool, bool>.RemoveListener(gameObject, "MuteAudioJukebox", OnMuteAudioJukebox);		
	}
			
	public void Update()
	{
		if (false == m_Timer.GetIsElapsed())
		{
			m_Timer.Update();

			if (true == m_Timer.GetIsElapsed())
			{
				Advance();
			}
		}
		else
		{
			if (m_CurrentAudioJukeboxSequenceNodeIndex < m_AudioJukeboxSequenceNodes.Length)
			{
				if (false == m_AudioEmitter.GetIsPlaying(m_AudioJukeboxSequenceNodes[m_CurrentAudioJukeboxSequenceNodeIndex].m_AudioEvent))
				{
					Advance();
				}
			}
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////	

	private void OnRestart()
	{
		Restart();
	}

	private void OnAudioJukeboxAdvance()
	{
		Advance();
	}

	private void OnMuteAudioJukebox(ref bool DoMute, ref bool OnlyIfIsForMusic)
	{
		if ((false == OnlyIfIsForMusic) || (true == m_IsForMusic))
		{
			Mute(DoMute);
		}
	}

	private void Restart()
	{
		//Utilities.Log(gameObject, "Restart!");

		int NextNodeIndex = GetNextNodeIndex(-1);
		if (NextNodeIndex != m_CurrentAudioJukeboxSequenceNodeIndex)
		{
			m_CurrentAudioJukeboxSequenceNodeIndex = -1;
			Advance();
		}
	}
		
	private void Mute(bool DoMute)
	{
		//Utilities.Log(gameObject, "Muting:  " + DoMute);

		m_AudioEmitter.Mute(DoMute);		
	}

	private int GetNextNodeIndex(int CurrentNodeIndex)
	{
		do
		{
			CurrentNodeIndex++;
		} while ((CurrentNodeIndex < m_AudioJukeboxSequenceNodes.Length) && (null == m_AudioJukeboxSequenceNodes[CurrentNodeIndex].m_AudioEvent));

		return (CurrentNodeIndex);
	}
		
	private void Advance()
	{
		//Utilities.Log(gameObject, "Advance!");

		if (m_CurrentAudioJukeboxSequenceNodeIndex < m_AudioJukeboxSequenceNodes.Length)
		{
			m_CurrentAudioJukeboxSequenceNodeIndex = GetNextNodeIndex(m_CurrentAudioJukeboxSequenceNodeIndex);

			if (m_CurrentAudioJukeboxSequenceNodeIndex < m_AudioJukeboxSequenceNodes.Length)
			{
				m_AudioEmitter.m_AudioEvent = m_AudioJukeboxSequenceNodes[m_CurrentAudioJukeboxSequenceNodeIndex].m_AudioEvent;

				m_Timer.Elapse();				
				if (m_AudioJukeboxSequenceNodes[m_CurrentAudioJukeboxSequenceNodeIndex].m_Duration > 0.0f)
				{					
					m_Timer.SetLength(m_AudioJukeboxSequenceNodes[m_CurrentAudioJukeboxSequenceNodeIndex].m_Duration);
					m_Timer.Reset();
				}

				m_AudioEmitter.Stop(false, true);
				m_AudioEmitter.Play();
			}
		}
	}
}
