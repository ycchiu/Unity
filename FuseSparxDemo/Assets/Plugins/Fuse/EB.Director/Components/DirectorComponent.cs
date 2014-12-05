using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Director
{
	public class Component : MonoBehaviour 
	{
		public const int kFPS = 15;
		public float Length = 10.0f;
		
		public int _nextId = 1;
			
		public List<Serialization.Group> Groups = new List<Serialization.Group>();
		
		private bool _playing = false;
		private bool _paused = false;
        private bool _inUpdate = false;
        private bool _doStop = false;

		private float _playSpeed = 1.0f;
		private float _playTime = 0.0f;
		private float _playRealTime = 0.0f;
		private SequenceAction_Director _action = null;
		
		private List<Runtime.GroupInstance> _instances = new List<Runtime.GroupInstance>();
		private Dictionary<string,EB.Sequence.Runtime.Trigger> _events = new Dictionary<string, EB.Sequence.Runtime.Trigger>();
		
		public List<Runtime.GroupInstance> Instances { get { return _instances;} }
	
		public bool IsPlaying { get { return _playing;} }  
		public float PlayTime 
		{ 
			get { return _playTime;} 
			set { _playTime = value; }
		}
		
		public virtual bool IsPaused { get { return _paused; } }
		
		public GameObject DirectorCamera {get;set;}
		
		public string[] GetEvents()
		{
			List<string> events = new List<string>();
			
			var group = GetGroupByType(GroupType.Event);
			if ( group != null )
			{
				foreach( var track in group.tracks ) 
				{
					if ( track.type == TrackType.Event )
					{
						foreach( var kf in track.frames ) 
						{
							string value = kf.StringValue;
							if ( events.Contains(value) == false )
							{
								events.Add(value);
							}
						}						
					}
				}
			}
			return events.ToArray();
		}
		
		public Serialization.Group GetGroupByType( GroupType type )
		{
			foreach( var group in Groups )
			{
				if ( group.type == type )
				{
					return group;
				}
			}
			return null;
		}
		
		public Runtime.GroupInstance GetGroupInstanceById( int id )
		{
			foreach( var group in _instances )
			{
				if ( group._group.id == id )
				{
					return group;
				}
			}
			return null;
		}
		
		public Serialization.Group GetGroupById( int id )
		{
			foreach( var group in Groups )
			{
				if ( group.id == id )
				{
					return group;
				}
			}
			return null;
		}
		
		public bool AddGroup( Serialization.Group group ) 
		{
			// only allow single instances of groups that dont have any input
			if ( Utils.HasGroupInput(group.type) == false &&  GetGroupByType(group.type) != null )
			{
				return false;
			}
			
			group.id = _nextId++;
			Groups.Add(group);
			
			Groups.Sort( delegate(Serialization.Group group1,Serialization.Group group2){
				if ( group1.type==group2.type)
				{
					return group1.id - group2.id;
				}
				return group1.type - group2.type;	
			});
			
			return true;
		}
		
		public void Pause()
		{
//			EB.Debug.Log("PAUSING DIRECTOR: " + name);
			_paused = true;
		}
		
		public void Resume()
		{
			_paused = false;
		}
		
		public void FireEvent( string eventName ) 
		{
			//EB.Debug.Log("Director: Fired Event " + eventName );
			if ( _action != null )
			{
				EB.Sequence.Runtime.Trigger trigger;
				if ( _events.TryGetValue(eventName, out trigger) )
				{
					trigger.Invoke();
				}
			}
		}
		
		public void Bind( SequenceAction_Director action ) 
		{
			_action = action;
			_events.Clear();

			// get the events
			var events = GetEvents();
			
			action.Events = new EB.Sequence.Runtime.Trigger[events.Length];
			
			// bind the events
			for ( int i = 0; i < events.Length; ++i )
			{
				action.Events[i] = new EB.Sequence.Runtime.Trigger();
				_events[events[i]] = action.Events[i];
			}
		}
		
		// todo hookup sequence editor
		public void Play()
		{
			_paused = false;
			 
			if (_playing == false )
			{
				_instances.Clear();
				
				int variableIndex = 0;
				foreach( var group in Groups )
				{
					// link in variable inputs
					var variable = EB.Sequence.Runtime.Variable.Null;
					if ( Utils.HasGroupInput(group.type) && _action != null )
					{
						if ( variableIndex < _action.Groups.Length )
						{
							variable = _action.Groups[variableIndex];
						}
						++variableIndex;
					}
					
					var instance = new Runtime.GroupInstance(this, group, variable );
					_instances.Add(instance);
				}
				
				if (_action != null )
				{
					_action.Started.Invoke();
				}
				
				_playTime = 0.0f;
				_playRealTime = Time.realtimeSinceStartup;
				_playing = true;
                _doStop = false;
                _inUpdate = false;

                OnPlay();
				
				UpdateTracks(0);
			}
		}
		
		public void Stop()
		{
			Stop(true);
		}
		
		public void Stop( bool notify )
		{
            if ( _inUpdate )
            {
                _doStop = true;
                return;
            }

			if ( _playing )
			{
				foreach( var instance in _instances ) 
				{
					instance.Restore();
				}
				_instances.Clear();
				_playing = false;
                _doStop = false;
				
				OnStop();
				
				if (_action != null && notify )
				{
					_action.Stopped.Invoke();
				}
			}
		}

        public virtual void OnPlay() { }
		public virtual void OnStop() {}
		
		public void LateUpdate()
		{
			float dT = Time.realtimeSinceStartup - _playRealTime;
			_playRealTime = Time.realtimeSinceStartup;
			
			dT =  Mathf.Min(dT,1.0f);
			//if ( Application.isPlaying && Time.timeScale == 1 )
			//{
			//	dT = Time.deltaTime;
			//}
			
			UpdateTracks(dT);
		}
		
		public void UpdateTracks( float dT )
		{
			if ( _playing )
			{
				_playTime += IsPaused ? 0.0f : (dT*_playSpeed);
				
				bool done = false;
				if ( _playTime > Length )
				{
					_playTime = Length;
					done = true;
				}
				
				float frameTime = _playTime * kFPS;

                _inUpdate = true;
				foreach( var instance in _instances ) 
				{
					instance.Update( frameTime );
				}
                _inUpdate = false;

				if ( done || _doStop )
				{
					Stop();
				}
			}
		}
		
	}
}