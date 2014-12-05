using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Director.Runtime
{
	public abstract class TrackInstance
	{
		public EB.Director.Serialization.Track _track;
		public EB.Director.Serialization.Group _group;
		public EB.Director.Component _parent;
		public GameObject _target;
		
		public abstract void Update( float frameTime );
		
		public virtual void SaveState() {}
		public virtual void RestoreState() {}
	}
	
	public class GroupInstance
	{
		public readonly EB.Director.Serialization.Group _group;		
		public readonly GameObject _target;
		
		private List<TrackInstance> _tracks = new List<TrackInstance>();
		public List<TrackInstance> Tracks
		{
			get { return _tracks; }
		}
		
		public GroupInstance( EB.Director.Component parent, EB.Director.Serialization.Group group, EB.Sequence.Runtime.Variable variable )
		{		
			_group = group;
			
			switch( _group.type )
			{
			case GroupType.ImageEffect:
				{
					_target = parent.DirectorCamera;
				
					// disabled image effects for mac until they fix their web player.
					if ( Application.platform == RuntimePlatform.OSXWebPlayer )
					{
						return;
					}
				}
				break;
			default:
				{
					var obj = variable.Value;
					if ( obj != null && obj is GameObject )
					{
						_target = (GameObject)obj;			
					}
					else
					{
						_target = (GameObject)GameObject.Find(group.targetName);
						
						if (_target == null )
						{
							EB.Debug.Log("GroupInstance Cant find "+group.targetName);
						}
					}
				}
				break;
			}
		
			foreach( var track in _group.tracks )
			{
				TrackInstance ti = null;
				switch( track.type )
				{
				case TrackType.Transform:
					{
						ti = new TransformTrackInstance();
					}
					break;
				case TrackType.FOV:
					{
						ti = new FOVTrackInstance();
					}
					break;
				case TrackType.CameraShake:
					{
						ti = new CameraShakeInstance();
					}
					break;
				case TrackType.OrthographicSize:
					{
						ti = new OrthographicSizeTrackInstance();
					}
					break;	
				case TrackType.Director:
					{
						ti = new DirectorTrackInstance();
					}
					break;
				case TrackType.Event:
					{
						ti = new EventTrackInstance();
					}
					break;
				case TrackType.TimeScale:
					{
						ti = new TimeScaleTrackInstance();
					}
					break;	
				case TrackType.EnableComponent:
					{
						ti = new EnableComponentTrackInstance();
					}
					break;		
				case TrackType.Variable:
					{
						ti = new ComponentVariableTrackInstance();
					}
					break;		
				}
				
				if ( ti != null )
				{
					ti._track = track;
					ti._group = _group;
					ti._target = _target;
					ti._parent = parent;
					ti.SaveState();
					_tracks.Add(ti);
				}
			}
		}
		
		// not in real time, in frame time
		public void Update( float frameTime )
		{
			foreach( var track in _tracks )
			{
				track.Update(frameTime);
			}
		}
		
		public void Restore()
		{
			foreach( var track in _tracks )
			{
				if ( track._track.restoreState || !Application.isPlaying)
				{
					track.RestoreState();	
				}
			}
		}
	}
}