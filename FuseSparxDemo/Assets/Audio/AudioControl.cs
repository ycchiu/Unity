using UnityEngine;
using System.Collections;

public class AudioControl : MonoBehaviour 
{
	public static AudioControl	Instance{get; private set;}
	
	// GA: These sounds are accessed primarily (solely?) by Profile.cs
	public AudioEvent	_softCurrencyUp;
	public AudioEvent	_hardCurrencyUp;
	public AudioEvent	_energyUp;
	public AudioEvent	_energyFull;
	public AudioEvent	_outOfFuel;		// aka, out of energy
	
	private AudioEmitter	_utilityOneshot;
	
	private AudioMix		_pauseMix;
	private AudioMix		_muteMix;
	
	private void Awake()
	{
		Instance = this;
		
		_audioMix = GetComponent<AudioMix>();
		Instance._MusicLevel = EB.Options.Music;
		Instance._SFXLevel = EB.Options.SFX;
		
		_utilityOneshot = GetComponent<AudioEmitter>();
		
		_pauseMix = EB.Util.GetObjectExactMatch(gameObject,"PauseMix").GetComponent<AudioMix>();
		_muteMix = EB.Util.GetObjectExactMatch(gameObject,"MuteMix").GetComponent<AudioMix>();
		
		EB.Debug.Log (string.Format("AudioControl on startup : output sample rate {0}Hz", AudioSettings.outputSampleRate));
	}
	
	public static float MusicLevel
	{
		set
		{
			Instance._MusicLevel = value;
			EB.Options.Music = value;
		}
		get
		{
			return Instance._MusicLevel;
		}
	}
	
	private float _MusicLevel
	{
		set
		{
			// Assumptions about category names here
			foreach (AudioCategory acat in _audioMix.AudioCategories)
			{
				if (acat.name.EndsWith("MX"))
				{
					acat.Modulate = value;
				}
			}
			
			_musicLevel = value;
		}
		get
		{
			return _musicLevel;
		}
	}
	
	public static float SFXLevel
	{
		set
		{
			Instance._SFXLevel = value;
			EB.Options.SFX = value;
		}
		get
		{
			return Instance._SFXLevel;
		}
	}
	
	private float _SFXLevel
	{
		set
		{
			// Assumptions about category names here
			foreach (AudioCategory acat in _audioMix.AudioCategories)
			{
				if (!acat.name.EndsWith("MX"))
				{
					acat.Modulate = value;
				}
			}
			_sfxLevel = value;
		}
		get
		{
			return _sfxLevel;
		}	
	}
	
	public static void Pause(bool pause)
	{
		if (pause != Paused)
		{
			bool force = true;
			
			if (pause)
			{
				Messenger<AudioMix>.BroadcastToAllListeners("ActivateAudioMix", ref Instance._pauseMix);
			}
			else
			{
				Messenger<AudioMix, bool>.BroadcastToAllListeners("DeactivateAudioMix", ref Instance._pauseMix, ref force);
			}
			Paused = pause;
		}
	}
	
	public static void Mute(bool mute)
	{
		if (mute != Muted)
		{
			bool force = true;
			
			if (mute)
			{
				Messenger<AudioMix>.BroadcastToAllListeners("ActivateAudioMix", ref Instance._muteMix);
			}
			else
			{
				Messenger<AudioMix, bool>.BroadcastToAllListeners("DeactivateAudioMix", ref Instance._muteMix, ref force);
			}
			Muted = mute;
		}
	}
	
	private static bool Paused{get;set;}
	private static bool Muted{get;set;}
	
	private void _Pause(bool pause)
	{
		foreach (AudioCategory acat in _audioMix.AudioCategories)
		{
			if (!acat.name.EndsWith("MX"))
			{
				acat.Muted = pause;
			}
		}
	}
	
	public static void EnableJukebox(bool enable)
	{
		if (enable)
		{
			Messenger.BroadcastToAllListeners("Restart");
		}
//		else
//		{
//			bool mute1 = true, mute2 = true;
//			Messenger<bool, bool>.BroadcastToAllListeners("MuteAudioJukebox", ref mute1, ref mute2);
//		}
	}
	
	public static void PlaySoftCurrencyUp()
	{
		if(Instance != null)
		{
			Instance._utilityOneshot.Play(Instance._softCurrencyUp);
		}
	}
	
	public static void PlayHardCurrencyUp()
	{
		if(Instance != null)
		{
			Instance._utilityOneshot.Play(Instance._hardCurrencyUp);
		}
	}
	
	public static void PlayEnergyUp()
	{
		if(Instance != null)
		{
			Instance._utilityOneshot.Play(Instance._energyUp);
		}
	}
	
	public static void PlayOutOfFuel()
	{
		if(Instance != null)
		{
			Instance._utilityOneshot.Play(Instance._outOfFuel);	
		}
	}
	
	private AudioMix 	_audioMix;
	private float		_musicLevel;
	private float		_sfxLevel;
	
#if GRAEME
	private void OnGUI()
	{
		const float dy = 20;
		const float x = 10;
		float y = 200;
		
		foreach (AudioCategory acat in _audioMix.AudioCategories)
		{
			GUI.Label(new Rect(x, y+=dy, 300, dy), string.Format ("Cat {0} vol {1}", acat.name, acat.GetVolumeScalar().ToString()));
		}
		
		GUI.Label(new Rect(x, y+=dy, 300, dy), string.Format ("SFX {0}", _sfxLevel));
		GUI.Label(new Rect(x, y+=dy, 300, dy), string.Format ("MUS {0}", _musicLevel));
	}
#endif
}
