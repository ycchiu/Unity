using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[System.Serializable]
public class UITransition : MonoBehaviour 
{
	public FilterType CurrentFilterType = FilterType.SHOW_ALL;
	public enum FilterType
	{
		SHOW_ALL,
		SHOW_SELECTED,
	}
	
	public Transition In
	{
		get { return Transitions[0]; }
		set { Transitions[0] = value; }
	}
	
	public Transition Out
	{
		get { return Transitions[1]; }
		set { Transitions[1] = value; }
	}
	
	public static bool LimitFEAnimation = false;
	
	[SerializeField]
	public Transition[] Transitions;
	
	private bool _isPlaying = false;
	public bool IsPlaying
	{
		get { return _isPlaying; }
	}
	
	public int CurrentTransitionID = 2;
	public int CurrentTrackID = 0;
	
	//--------------------------------------------------------------------------------------------------------
	public Transition GetTransitionFromID(int id)
	{
		for (int i = 0; i < Transitions.Length; i++)
		{
			if (Transitions[i].id == id)	
			{
				return Transitions[i];
			}
		}
			
		return null;
	}
	
	//--------------------------------------------------------------------------------------------------------
	[System.Serializable]
	public class Transition
	{
		public UITransition UITransition;
		public int id = -1;
		
		public string Name;
		
		private bool _isPlaying = false;
		public bool IsPlaying
		{
			get { return _isPlaying; }
			set { _isPlaying = value; }
		}
		
		public bool IsPreviewing = false;
		
		public bool IsLooping = false;
		public bool FastDeviceOnly = false;
		public float PreviewStartTime = 0.0f;
		
		public float SimTime
		{
			get { return _simTime; }
			set 
			{
				if (value != _simTime)
				{
					UpdateTracks(_simTime, true);
					_simTime = value;
				}
			}
		}
		private float _simTime = 0.0f;
		
		public int CopyTransitionIndex = 0;
		
		public float Duration = 0.0f;
		public bool IsOpen = true;
		public Track.TYPE TrackType = Track.TYPE.POSITION;
		
		public Track[] Tracks = new Track[0];
	
		public bool PositionTracksOpen = false;
		public bool RotationTracksOpen = false;
		public bool ScaleTracksOpen = false;
		public bool AlphaTracksOpen = false;
		public bool ParticleTracksOpen = false;
		public bool ColorTracksOpen = false;
		public bool WidgetSizeTracksOpen = false;
		public bool EventTracksOpen = false;
		
		private EB.Action _callback;
		
		//--------------------------------------------
		public void InitializeTracks()
		{
			Tracks = new Track[0];
		}
		
		//--------------------------------------------
		public bool TracksOpen(Track.TYPE type)
		{
			switch (type)
			{
				case Track.TYPE.POSITION:
				{
					return PositionTracksOpen;
				}
				
				case Track.TYPE.ROTATION:
				{
					return RotationTracksOpen;
				}
				
				case Track.TYPE.SCALE:
				{
					return ScaleTracksOpen;
				}
				
				case Track.TYPE.ALPHA:
				{
					return AlphaTracksOpen;
				}
				
				case Track.TYPE.PARTICLE:
				{
					return ParticleTracksOpen;
				}
				
				case Track.TYPE.COLOR:
				{
					return ColorTracksOpen;
				}
				
				case Track.TYPE.WIDGET_SIZE:
				{
					return WidgetSizeTracksOpen;
				}
				
				case Track.TYPE.EVENT:
				{
					return EventTracksOpen;
				}
			}
			
			return false;
		}
		
		//--------------------------------------------
		public void SetTracksOpen(Track.TYPE type, bool isOpen)
		{
			switch (type)
			{
				case Track.TYPE.POSITION:
				{
					PositionTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.ROTATION:
				{
					RotationTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.SCALE:
				{
					ScaleTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.ALPHA:
				{
					AlphaTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.PARTICLE:
				{
					ParticleTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.COLOR:
				{
				
					ColorTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.WIDGET_SIZE:
				{
					WidgetSizeTracksOpen = isOpen;
				}
				break;
				
				case Track.TYPE.EVENT:
				{
					EventTracksOpen = isOpen;
				}
				break;
			}
		}
		
		//--------------------------------------------
		public void Play(EB.Action callback)
		{
			EB.Debug.Log("PLAY - Transition: " + Name);
			
			_callback = callback;
			Reset();
			UITransition._isPlaying = true;
			IsPlaying = true;
		}
		
		//--------------------------------------------
		public void Preview()
		{
			SimTime = 0.0f;
			Reset();
			PreviewStartTime = Time.realtimeSinceStartup;
			IsPreviewing = true;
			IsPlaying = false;
		}
		
		//--------------------------------------------------------------------------------------------------------
		public bool UpdateTracks(float time, bool simulateToTime = false)
		{
			if (Application.isPlaying && FastDeviceOnly && LimitFEAnimation)
			{
				for (int i = 0; i < Tracks.Length; i++)
				{
					Tracks[i].JumpToEnd();
				}
			}
			else
			{
				IsPlaying = false;
				
				for (int i = 0; i < Tracks.Length; i++)
				{
					Tracks[i].Update(time, simulateToTime);
					
					if (!simulateToTime)
					{
						if (!Tracks[i].Complete) 	
						{
							IsPlaying = true;
						}
					}
				}
				
				if (!IsPlaying && Application.isPlaying)
				{
					if (_callback != null)
					{
						_callback();
					}
				}
			}
			
			return IsPlaying;
		}
		
		//--------------------------------------------
		public void JumpToStart()
		{
			IsPreviewing = false;
			
			foreach(Track track in Tracks)
			{
				track.JumpToStart();
			}
		}
		
		//--------------------------------------------
		public void JumpToEnd()
		{
			IsPreviewing = false;
			
			foreach(Track track in Tracks)
			{
				track.JumpToEnd();
			}
		}
		
		//--------------------------------------------
		public void GetDuration()
		{
			float duration = 0.0f;
			
			foreach(Track t in Tracks)
			{
				switch (t.Type)
				{
					case Track.TYPE.PARTICLE:	
					{
						float trackLength = t.ActualStartTime;
						duration = Mathf.Max(duration, trackLength);
					}
					break;
					
					default:
					{
						float trackLength = t.ActualStartTime + t.Duration;
						duration = Mathf.Max(duration, trackLength);
					}
					break;
				}	
			}
			
			Duration = duration;
		}
		
		//--------------------------------------------
		public void Reset()
		{
			foreach(Track track in Tracks)
			{
				track.Reset();
			}
		}
		
		//--------------------------------------------
		public Track GetTrackWithID(int id)
		{
			for (int i = 0; i < Tracks.Length; i++)
			{
				if (Tracks[i].id == id)	
				{
					return Tracks[i];
				}
			}
			
			return null;
		}
		
		//--------------------------------------------
		public Track GetTrackWithName(string name)
		{
			for (int i = 0; i < Tracks.Length; i++)
			{
				if (Tracks[i].Name == name)	
				{
					return Tracks[i];
				}
			}
			
			return null;
		}
	}

	//--------------------------------------------------------------------------------------------------------
	[System.Serializable]
	public class Track
	{
		public enum TYPE
		{
			POSITION,
			ROTATION,
			SCALE,
			ALPHA,
			PARTICLE,
			WIDGET_SIZE,
			COLOR,
			EVENT,
			COUNT,
		}
		
		public int id = -1;
		
		public TYPE Type;
		public GameObject Target = null;
		
		private bool _complete = false;
		public bool Complete
		{
			get { return _complete; }
			set { _complete = value; }
		}
		
		public bool IsSet = false;
		public bool IsOpen = true;
		public bool ExtraOptionsOpen = false;
		
		public UITransition TriggerUITransition = null;
		public int TriggerTrackID = -1;
		public int TriggerTransitionID = -1;
		public float TriggerTrackTime = 0.0f;
		public TriggerType TriggerMode = TriggerType.END;
		public string Name = string.Empty;
		
		public Track TriggerTrack
		{
			get 
			{
				if (TriggerUITransition == null) return null;
				else if (TriggerTransitionID == -1 || TriggerTrackID == -1) return null;
				
				return GetTriggerTrack(TriggerUITransition, TriggerTransitionID, TriggerTrackID); 
			}
		}
		
		public Track GetTriggerTrack(UITransition uiTransition, int transitionID, int trackID)
		{
			if (uiTransition != null)
			{
				Transition trans = uiTransition.GetTransitionFromID(transitionID);
				
				if (trans != null)
				{
					return trans.GetTrackWithID(trackID);
				}
			}
			
			return null;
		}
		
		public void SetTriggerTrack(UITransition uiTransition, int transitionID, int trackID)
		{
			TriggerUITransition = uiTransition;
			TriggerTransitionID = transitionID;
			TriggerTrackID = trackID;
			
			if (TriggerTrack != null)
			{
				bool infiniteLoop = TriggerTrack.IsInfiniteLoop(this);
				
				if (infiniteLoop)
				{
					TriggerUITransition = null;
					TriggerTransitionID = -1;
					TriggerTrackID = -1;
				}
			}
		}
		
		public bool IsInfiniteLoop(Track trackToLookFor)
		{
			Track triggerTrack = TriggerTrack;
			if (triggerTrack != null)
			{
				if (triggerTrack.Parent.UITransition == trackToLookFor.Parent.UITransition &&
					triggerTrack.Parent.id == trackToLookFor.Parent.id &&
					triggerTrack.id == trackToLookFor.id)
				{
					EB.Debug.LogWarning("Setting this trigger track creates an infinite loop!");
					return true;
				}
				else
				{
					return triggerTrack.IsInfiniteLoop(trackToLookFor);
				}
			}
			else return false;
		}
		
		public string DisplayedName
		{
			get
			{
				if (Name != string.Empty)
				{
					return Name;
				}
				else if (Target != null)
				{
					return Target.name;
				}
				else
				{
					return "New Track";
				}
			}
		}
		
		public string Function = "";
		
		public enum TriggerType
		{
			START,
			END,
		}
		
		public float StartTime = 0.0f;
		public float Duration = 0.0f;
		
		[SerializeField] 
		private Vector3 _startVector3;
		public Vector3 StartVector3
		{
			get { return _startVector3; }
			set
			{
				_startVector3 = value;
			}
		}
		
		[SerializeField] 
		private Vector3 _endVector3;
		public Vector3 EndVector3
		{
			get { return _endVector3; }
			set
			{
				_endVector3 = value;
			}
		}
		
		public Vector3 VariableStartVector3;
		public Vector3 VariableEndVector3;
		
		public float StartFloat = 0.0f;
		public float EndFloat = 0.0f;
		
		public Color StartColor;
		public Color EndColor;
		
		public bool BoolValue = false;
		
		public float VariableStartFloat;
		public float VariableEndFloat;
		
		public EZAnimation.ANIM_MODE AnimMode = EZAnimation.ANIM_MODE.FromTo;
		public EZAnimation.EASING_TYPE EasingType = EZAnimation.EASING_TYPE.Linear;
		
		private float _internalTimer = 0.0f;
		public float InternalTimer
		{
			get { return  _internalTimer; }
			set { _internalTimer = value; }
		}
		
		private EZAnimation.Interpolator _interpolator;
		public EZAnimation.Interpolator Interpolator
		{
			get 
			{
				if (_interpolator == null)
				{
					_interpolator = EZAnimation.GetInterpolator(EasingType);
				}
				
				return _interpolator;
			}
			
			set { _interpolator = value; }
		}
		
		public UITransition ParentUITransition;
		public int ParentID;
		public Transition Parent
		{
			get 
			{ 
				if (ParentUITransition == null) return null;
				return ParentUITransition.GetTransitionFromID(ParentID);
			}
			
			set
			{
				ParentID = value.id;
				ParentUITransition = value.UITransition;
				Debug.Log("Setting " + DisplayedName + "'s parent UITransition to: " + ParentUITransition);
			}
		}
		
		//---START ALPHA---//
		public List<UIWidget> Widgets;
		public UIPanel Panel;
		public bool ShowContents = false;
		
		public enum AlphaTrackType
		{
			PANEL,
			WIDGET,
			CHILDREN,
		}
		
		[SerializeField] 
		private AlphaTrackType _alphaType;
		public AlphaTrackType AlphaType
		{
			get { return _alphaType; }
			set
			{
				_alphaType = value;
				
				switch (value)
				{
					case AlphaTrackType.PANEL:
					{
						Widgets = new List<UIWidget>();
						if (Target != null)
						{
							Panel = Target.GetComponent<UIPanel>();
						}
					}
					break;
					
					case AlphaTrackType.WIDGET:
					{	
						Widgets = new List<UIWidget>();
						if (Target != null)
						{
							Widgets.Add(Target.GetComponent<UIWidget>());
						}
					}
					break;
					
					case AlphaTrackType.CHILDREN:
					{
						Widgets = new List<UIWidget>();
					
						if (Target != null)
						{
							foreach(UIWidget w in Target.GetComponentsInChildren<UIWidget>())
							{
								Widgets.Add(w);
							}
						}
					}
					break;
				}
			}
		}
		//---END ALPHA---//
		
		//---START PARTICLE---//
		private bool _playing = false;
		
		public List<ParticleSystem> ParticleSystems = new List<ParticleSystem>();
		
		public enum ParticleType
		{
			TARGET,
			TARGET_AND_CHILDREN,
		}
		
		[SerializeField] 
		private ParticleType _particleTrackType = ParticleType.TARGET;
		public ParticleType ParticleTrackType
		{
			get { return _particleTrackType; }
			set
			{
				_particleTrackType = value;
				
				switch (value)
				{
					case ParticleType.TARGET:
					{
						ParticleSystems = new List<ParticleSystem>();
					
						if (Target != null)
						{
							ParticleSystem p = Target.GetComponent<ParticleSystem>();
						
							if (p != null) ParticleSystems.Add(p);
						}
					}
					break;
					
					case ParticleType.TARGET_AND_CHILDREN:
					{
						ParticleSystems = new List<ParticleSystem>();
					
						if (Target != null)
						{
							foreach (ParticleSystem p in Target.GetComponentsInChildren<ParticleSystem>())
							{
								if (p != null) ParticleSystems.Add(p);
							}
						}
					}
					break;
				}	
			}
		}
		//---END PARTICLE---//
		
		//--------------------------------------------
		public float ActualStartTime
		{
			get
			{
				float realStartTime = StartTime;
				
				if (TriggerTrack != null)
				{
					switch (TriggerMode)
					{
						case TriggerType.START:
						{
							realStartTime = StartTime + TriggerTrack.ActualStartTime;
						}
						break;
						
						case TriggerType.END:
						{
							realStartTime = StartTime + TriggerTrack.ActualStartTime + TriggerTrack.Duration;
						}
						break;
					}
				}
				
				return realStartTime;
			}
		}
		
		//--------------------------------------------
		public void Update( float time, bool simulateToTime )
		{
			switch (Type)
			{
				case TYPE.POSITION:
				{
					UpdatePosition(time, simulateToTime);
				}
				break;
				
				case TYPE.ROTATION:
				{
					UpdateRotation(time, simulateToTime);
				}
				break;
				
				case TYPE.SCALE:
				{
					UpdateScale(time, simulateToTime);
				}
				break;
				
				case TYPE.ALPHA:
				{
					UpdateAlpha(time, simulateToTime);
				}
				break;
				
				case TYPE.PARTICLE:
				{
					UpdateParticle(time, simulateToTime);
				}
				break;
				
				case TYPE.COLOR:
				{
					UpdateColor(time, simulateToTime);
				}
				break;
				
				case TYPE.WIDGET_SIZE:
				{
					UpdateWidgetSize(time, simulateToTime);
				}
				break;
				
				case TYPE.EVENT:
				{
					UpdateEvent(time, simulateToTime);
				}
				break;
			}	
		}
		
		//--------------------------------------------
		public void UpdatePosition( float time, bool simulateToTime)
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			// Return if target does not exist
			if (Target == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null) Interpolator = EZAnimation.GetInterpolator(EasingType);
			}
			else
			{
				InternalTimer += time;
			}
			
			Vector3 properStart = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
			Vector3 properEnd = (AnimMode == EZAnimation.ANIM_MODE.By) ? VariableEndVector3 : EndVector3;
			
			float realStartTime = ActualStartTime;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;
				Target.transform.localPosition = properEnd;
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				Target.transform.localPosition = Vector3.Lerp(properStart, properEnd, progress);
			}
			else if (simulateToTime)
			{
				Target.transform.localPosition = properStart;
			}
		}
		
		//--------------------------------------------
		public void UpdateRotation( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Target == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null)
				{
					Interpolator = EZAnimation.GetInterpolator(EasingType);
				}
			}
			else
			{
				InternalTimer += time;
			}
			
			Vector3 properStart = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
			Vector3 properEnd = (AnimMode == EZAnimation.ANIM_MODE.By) ? VariableEndVector3 : EndVector3;
			
			float realStartTime = ActualStartTime;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;
				Target.transform.localEulerAngles = properEnd;
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				Target.transform.localEulerAngles = Vector3.Lerp(properStart, properEnd, progress);
			}
			else if (simulateToTime)
			{
				Target.transform.localEulerAngles = properStart;
			}
		}
		
		//--------------------------------------------
		public void UpdateScale( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Target == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null)
				{
					Interpolator = EZAnimation.GetInterpolator(EasingType);
				}
			}
			else
			{
				InternalTimer += time;
			}
			
			float realStartTime = ActualStartTime;
			
			Vector3 properStart = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
			Vector3 properEnd = (AnimMode == EZAnimation.ANIM_MODE.By) ? VariableEndVector3 : EndVector3;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;
				Target.transform.localScale = properEnd;
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				Target.transform.localScale = Vector3.Lerp(properStart, properEnd, progress);
			}
			else if (simulateToTime)
			{
				Target.transform.localScale = properStart;
			}
		}
		
		//--------------------------------------------
		public void UpdateAlpha( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Widgets == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null)
				{
					Interpolator = EZAnimation.GetInterpolator(EasingType);
				}
			}
			else
			{
				InternalTimer += time;
			}
			
			float realStartTime = ActualStartTime;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;
				UpdateWidgetAlpha(EndFloat);
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				float alpha = Mathf.Lerp(StartFloat, EndFloat, progress);
				
				UpdateWidgetAlpha(alpha);
			}
		}
		
		//--------------------------------------------
		private void UpdateWidgetAlpha(float alpha)
		{
			if (_alphaType == AlphaTrackType.PANEL)
			{
				if (Panel != null)
				{
					Panel.alpha = alpha;
				}
			}
			else
			{
				foreach(UIWidget w in Widgets)
				{
					if (w != null)
					{
						w.alpha = alpha;
					}
				}
			}
		}
		
		//--------------------------------------------
		public void UpdateParticle( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Target == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				UpdateParticleSystems(time - ActualStartTime);
			}
			else
			{
				InternalTimer += time;
				
				float realStartTime = ActualStartTime;
				float properDuration = realStartTime + Duration;
				
				if (Duration > realStartTime) // Has a stop time
				{
					if (InternalTimer > realStartTime && !_playing)
					{
						foreach(ParticleSystem p in ParticleSystems)
						{
							if (p != null)
							{
								p.Play();
							}
						}
					}
					else if (InternalTimer > properDuration && _playing)
					{
						foreach(ParticleSystem p in ParticleSystems)
						{
							if (p != null)
							{
								p.Stop();
							}
						}
						
						Complete = true;
					}
				}
				else // No stop time
				{
					if (InternalTimer > realStartTime)
					{
						foreach(ParticleSystem p in ParticleSystems)
						{
							if (p != null)
							{
								p.Play();
							}
						}
						
						Complete = true;
					}
				}		
			}
		}
		
		//--------------------------------------------
		private void UpdateParticleSystems(float simTime)
		{
			if (Duration > StartTime && simTime > Duration)
			{
				simTime = 0.0f;
			}
			
			foreach(ParticleSystem p in ParticleSystems)
			{
				if (p != null)
				{
					p.Simulate(simTime);
				}
			}
		}
		
		//--------------------------------------------
		public void UpdateColor( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Widgets == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null)
				{
					Interpolator = EZAnimation.GetInterpolator(EasingType);
				}
			}
			else
			{
				InternalTimer += time;
			}
			
			float realStartTime = ActualStartTime;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;

				UpdateWidgetColor(EndColor);
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				Color c = Color.Lerp(StartColor, EndColor, progress);
				
				UpdateWidgetColor(c);
			}
		}
		
		//--------------------------------------------
		private void UpdateWidgetColor(Color color)
		{
			foreach(UIWidget w in Widgets)
			{
				if (w != null)
				{
					w.color = color;
				}
			}
		}
		
		//--------------------------------------------
		public void UpdateWidgetSize( float time, bool simulateToTime = false )
		{
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Widgets == null) return;
			else if (Widgets.Count == 0) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				if (Interpolator == null)
				{
					Interpolator = EZAnimation.GetInterpolator(EasingType);
				}
			}
			else
			{
				InternalTimer += time;
			}
			
			float realStartTime = ActualStartTime;
			
			Vector3 properStart = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
			Vector3 properEnd = (AnimMode == EZAnimation.ANIM_MODE.By) ? VariableEndVector3 : EndVector3;
			
			float timeToUse = (simulateToTime) ? time : InternalTimer;
			
			if (timeToUse > realStartTime + Duration)
			{
				Complete = true;
				Widgets[0].SetDimensions((int)properEnd.x, (int)properEnd.y);
			}
			else if (timeToUse > realStartTime)
			{
				float progress = Interpolator(timeToUse-realStartTime, 0.0f, 1.0f, Duration);
				Vector3 size = Vector3.Lerp(properStart, properEnd, progress);
				
				Widgets[0].SetDimensions((int)size.x, (int)size.y);
			}
			else if (simulateToTime)
			{
				Widgets[0].SetDimensions((int)properStart.x, (int)properStart.y);
			}
		}
		
		//--------------------------------------------
		public void UpdateEvent( float time, bool simulateToTime = false )
		{
			if (!Application.isPlaying) return;
			
			// Return if track is playing live and complete
			if (!simulateToTime && Complete) return;
			
			if (Target == null) return;
			
			// If Scrubbing / Previewing
			if (simulateToTime)
			{
				UpdateParticleSystems(time - ActualStartTime);
			}
			else
			{
				InternalTimer += time;
				
				float realStartTime = ActualStartTime;
				
				if (InternalTimer > realStartTime)
				{
					// FIRE EVENT
					if (!BoolValue)
					{
						Target.SendMessage(Function, SendMessageOptions.DontRequireReceiver);
					}
					else
					{
						//Sequence.Activate(null, Function, typeof(SequenceEvent_GameEvent));
					}
					Complete = true;
				}		
			}
		}
		
		//--------------------------------------------
		public virtual void JumpToStart()
		{
			if (Target == null) return;
			
			switch (Type)
			{
				case TYPE.POSITION:
				{
					Target.transform.localPosition = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
				}
				break;
				
				case TYPE.ROTATION:
				{
					Target.transform.localEulerAngles = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
				}
				break;	
			
				case TYPE.SCALE:
				{
					Target.transform.localScale = (AnimMode == EZAnimation.ANIM_MODE.FromTo) ? StartVector3 : VariableStartVector3;
				}
				break;
				
				case TYPE.ALPHA:
				{
					foreach(UIWidget w in Widgets)
					{
						if (w != null)
						{
							w.alpha = StartFloat;
						}
					}
				}
				break;
				
				case TYPE.PARTICLE:
				{
					UpdateParticleSystems(0.0f);
				}
				break;
				
				case TYPE.COLOR:
				{
					UpdateWidgetColor(StartColor);
				}
				break;
				
				case TYPE.WIDGET_SIZE:
				{
					UpdateWidgetSize(0.0f, true);
				}
				break;
			}	
		}
		
		//--------------------------------------------
		public virtual void JumpToEnd()
		{
			switch (Type)
			{
				case TYPE.POSITION:
				{
					ResetPosition();
				
					switch (AnimMode)
					{
						case EZAnimation.ANIM_MODE.To:
						case EZAnimation.ANIM_MODE.FromTo:
						{
							Target.transform.localPosition = EndVector3;
						}
						break;
					
						case EZAnimation.ANIM_MODE.By:
						{
							Target.transform.localPosition = StartVector3 + EndVector3;
						}
						break;
					}
				}
				break;
				
				case TYPE.ROTATION:
				{
					ResetRotation();
				
					switch (AnimMode)
					{
						case EZAnimation.ANIM_MODE.To:
						case EZAnimation.ANIM_MODE.FromTo:
						{
							Target.transform.localEulerAngles = EndVector3;
						}
						break;
					
						case EZAnimation.ANIM_MODE.By:
						{
							Target.transform.localEulerAngles = StartVector3 + EndVector3;
						}
						break;
					}
				}
				break;	
			
				case TYPE.SCALE:
				{
					ResetScale();
					
					switch (AnimMode)
					{
						case EZAnimation.ANIM_MODE.To:
						case EZAnimation.ANIM_MODE.FromTo:
						{
							Target.transform.localScale = EndVector3;
						}
						break;
					
						case EZAnimation.ANIM_MODE.By:
						{
							Target.transform.localScale = StartVector3 + EndVector3;
						}
						break;
					}
				}
				break;
				
				case TYPE.ALPHA:
				{
					foreach(UIWidget w in Widgets)
					{
						if (w != null)
						{
							w.alpha = EndFloat;
						}
					}
				}
				break;
				
				case TYPE.PARTICLE:
				{
					UpdateParticleSystems(Duration);
				}
				break;
				
				case TYPE.COLOR:
				{
					UpdateWidgetColor(EndColor);
				}
				break;
				
				case TYPE.WIDGET_SIZE:
				{
					ResetWidgetSize();
					UpdateWidgetSize(Duration, true);
				}
				break;
			}	
		}
		
		//--------------------------------------------
		public virtual void Reset()
		{
			if (Target == null) return;
			
			switch (Type)
			{
				case TYPE.POSITION:
				{
					ResetPosition();
				}
				break;
				
				case TYPE.ROTATION:
				{
					ResetRotation();
				}
				break;	
			
				case TYPE.SCALE:
				{
					ResetScale();
				}
				break;
				
				case TYPE.ALPHA:
				{
					ResetAlpha();
				}
				break;
				
				case TYPE.PARTICLE:
				{	
					ResetParticle();
				}
				break;
				
				case TYPE.COLOR:
				{	
					ResetColor();
				}
				break;
				
				case TYPE.WIDGET_SIZE:
				{	
					ResetWidgetSize();
				}
				break;
			}	
			
			Interpolator = EZAnimation.GetInterpolator(EasingType);
			InternalTimer = 0.0f;
			Complete = false;
			JumpToStart();
		}
		
		//--------------------------------------------
		public void ResetPosition()
		{
			if (Application.isPlaying)
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = Target.transform.localPosition;
						VariableEndVector3 = EndVector3 + VariableStartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = Target.transform.localPosition;
					}
					break;
				}
			}
			else
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = StartVector3;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = StartVector3;
					}
					break;
				}
			}
		}
	
		//--------------------------------------------
		public void ResetRotation()
		{
			if (Application.isPlaying)
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = Target.transform.localEulerAngles;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = Target.transform.localEulerAngles;
					}
					break;
				}
			}
			else
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = StartVector3;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = StartVector3;
					}
					break;
				}
			}
		}
		
		//--------------------------------------------
		public void ResetScale()
		{
			if (Application.isPlaying)
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = Target.transform.localScale;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = Target.transform.localScale;
					}
					break;
				}
			}
			else
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = StartVector3;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = StartVector3;
					}
					break;
				}
			}
		}
		
		//--------------------------------------------
		public void ResetAlpha()
		{
			if (Widgets != null)
			{
				AlphaType = AlphaType; // reset the widget list
				
				foreach(UIWidget w in Widgets)
				{
					if (w != null)
					{
						w.alpha = StartFloat;
					}
				}
			}
		}
		
		//--------------------------------------------
		public void ResetParticle()
		{
			UpdateParticleSystems(0.0f);
		}
		
		//--------------------------------------------
		public void ResetWidgetSize()
		{
			if (Application.isPlaying)
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = Widgets[0].localSize;
						VariableEndVector3 = EndVector3 + VariableStartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = Widgets[0].localSize;
					}
					break;
				}
			}
			else
			{
				switch (AnimMode)
				{
					case EZAnimation.ANIM_MODE.By:
					{
						VariableStartVector3 = StartVector3;
						VariableEndVector3 = EndVector3 + StartVector3;
					}
					break;
					
					case EZAnimation.ANIM_MODE.To:
					{
						VariableStartVector3 = StartVector3;
					}
					break;
				}
			}
		}
		
		//--------------------------------------------
		public void ResetColor()
		{
			if (Widgets != null)
			{
				foreach(UIWidget w in Widgets)
				{
					if (w != null)
					{
						w.color = StartColor;
					}
				}
			}
		}
		
	}
	
	//--------------------------------------------------------------------------------------------------------
	public void TransitionIn(EB.Action callback)
	{
		In.Play(callback);
	}
	
	//--------------------------------------------------------------------------------------------------------
	public void TransitionOut(EB.Action callback)	
	{
		Out.Play(callback);
	}
	
	//--------------------------------------------------------------------------------------------------------
	public Transition GetTransition(string name)
	{
		foreach (Transition t in Transitions)
		{
			if (t.Name.Equals(name))
			{
				return t;
			}
		}
		
		return null;
	}
		
	//--------------------------------------------------------------------------------------------------------
	public void PlayTransitionByName(string transitionName, EB.Action callback)
	{
		Transition t = GetTransition(transitionName);

		if (t != null)
		{
			if (t.Tracks.Length > 0)
			{
				t.Play(callback);
			}
			else
			{
				if (callback != null)
				{
					callback();
				}
			}
			return;
		}
		
		EB.Debug.LogWarning("Could not play transition with name: '" + transitionName + "' on UITransition " + name); 
	}
	
	//--------------------------------------------------------------------------------------------------------
	void Update () 
	{
		if (_isPlaying)
		{
			_isPlaying = false;
			
			foreach(Transition t in Transitions)
			{
				// _isPlaying is set to true if a track is still playing
				if (t.IsPlaying)
				{
					_isPlaying |= t.UpdateTracks(Time.deltaTime);
				}
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------
	public void OfflineUpdate(Transition transition)
	{
		if (transition.IsPreviewing)
		{
			float simTime = Time.realtimeSinceStartup-transition.PreviewStartTime;
			
			_isPlaying = false;
			
			_isPlaying = transition.UpdateTracks(simTime, true);
			
			if (simTime > transition.Duration) 
			{
				if (transition.IsLooping)
				{
					transition.Reset();
					transition.PreviewStartTime = Time.realtimeSinceStartup + simTime-transition.Duration;
				}
				else
				{
					transition.IsPreviewing = false;
				}
			}
		}
	}
}