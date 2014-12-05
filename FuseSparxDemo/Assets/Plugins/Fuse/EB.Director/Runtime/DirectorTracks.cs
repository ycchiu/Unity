using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Director.Runtime
{
	public abstract class BlendableTrackInstance : TrackInstance
	{
		public static float Blend( EB.Director.BlendMode mode, float t ) 
		{
			switch(mode)
				{
				case BlendMode.Cut:
					{
						t = 0.0f; // do the cut
					}
					break;
				case BlendMode.EaseIn:
					{
						t = EZAnimation.sinusOut( t, 0, 1, 1 ); 
					}
					break;
				case BlendMode.EaseOut:
					{
						t = EZAnimation.sinusIn( t, 0, 1, 1 ); 
					}
					break;
				case BlendMode.CubicIn:
					{
						t = EZAnimation.cubicIn( t, 0, 1, 1 );
					}
					break;
				case BlendMode.CubicOut:
					{
						t = EZAnimation.cubicOut( t, 0, 1, 1 );
					}
					break;	
				case BlendMode.EaseOutIn:
					{
						t = EZAnimation.sinusInOut( t, 0, 1, 1 );
					}
					break;	
				case BlendMode.EaseInOut:
					{
						t = EZAnimation.sinusOutIn( t, 0, 1, 1 );
					}
					break;		
				case BlendMode.CubicInOut:
					{
						t = EZAnimation.cubicInOut( t, 0, 1, 1 );
					}
					break;		
				case BlendMode.CubicOutIn:
					{
						t = EZAnimation.cubicOutIn( t, 0, 1, 1 );
					}
					break;	
				default:
					// linear
					break;
				}
			return t;
		}
				
		public override void Update( float frameTime )
		{
			int lowerFrame = (int)frameTime;
			var first = LowerEqual(lowerFrame);
			var second = Greater(lowerFrame);
			
			if ( first != null && second != null )
			{
				// blend
				float t = Mathf.Clamp01( (frameTime-first.frame)/ (float)( second.frame - first.frame ) );
				t = Blend( first.mode, t );
				
				Apply(t, first, second);
			}
			else if ( first != null )
			{
				// the case where we have a single keyframe
				Apply(0.0f, first,first);
			}			
		}
		
		public EB.Director.Serialization.KeyFrame LowerEqual( int frameNumber )
		{
			EB.Director.Serialization.KeyFrame kf = null;
			foreach( var frame in _track.frames ) 
			{
				if ( frame.frame <= frameNumber )
				{
					kf = frame;
				}
				else
				{
					break;
				}
			}
			return kf;
		}
		
		public EB.Director.Serialization.KeyFrame Greater( int frameNumber )
		{
			foreach( var frame in _track.frames ) 
			{
				if ( frame.frame > frameNumber )
				{
					return frame;
				}
			}
			return null;
		}
		
		public abstract void Apply( float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to );
	}
	
	public class CameraShakeInstance : BlendableTrackInstance
	{
		private QuatPos _state;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
//			Uncomment for Camera Shake to work
//			if ( _target != null )
//			{
//				var v1 = from.QuatPosValue;
//				var v2 = to.QuatPosValue;
//				var v = QuatPos.Lerp(v1,v2,blend);
//				
//				if (_track.space == SpaceType.Local || _track.space == SpaceType.LocalToWorld)
//				{
//					_target.transform.localPosition += new Vector3(Random.Range(-v.pos.x,v.pos.x), Random.Range(-v.pos.y,v.pos.y), Random.Range(-v.pos.z,v.pos.z));
//					_target.transform.localEulerAngles += new Vector3(Random.Range(-v.quat.x,v.quat.x), Random.Range(-v.quat.y,v.quat.y), Random.Range(-v.quat.z,v.quat.z));
//				}
//				else if (_track.space == SpaceType.World)
//				{
//					_target.transform.position += new Vector3(Random.Range(-v.pos.x,v.pos.x), Random.Range(-v.pos.y,v.pos.y), Random.Range(-v.pos.z,v.pos.z));
//					_target.transform.eulerAngles += new Vector3(Random.Range(-v.quat.x,v.quat.x), Random.Range(-v.quat.y,v.quat.y), Random.Range(-v.quat.z,v.quat.z));
//				}
//			}
		}
		
		public override void SaveState ()
		{
			if ( _target != null )
			{
				_state = QuatPos.FromTransform(_target.transform, _track.space);
			}
		}
		
		public override void RestoreState()
		{
			if ( _target != null )
			{
				_state.Apply(_target.transform, _track.space);
			}
		}
	}
	
	public class TransformTrackInstance : BlendableTrackInstance
	{
		private QuatPos _state;

		public GameObject _playerCar;
		public int frame = 0;
		
		private float timer = 0.0f;
		private float timeToLerp = 0.75f;
		
		private QuatPos _currentQuatPos;
		
		public void Reset()	
		{
			//EB.Debug.Log("RESETTING TRANSFORM TRACK IN DIRECTION : " + _parent.name);
			
			if (DirectorInformation.LocalToWorldDirectors.Contains(_parent))
			{
				_track.space = SpaceType.LocalToWorld;
			}
			
			DirectorInformation.LastTimeScale = Time.timeScale;
			_parent.PlayTime = 0.0f;
			frame = 0;
			timer = 0.0f;
		}
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			//EB.Debug.Log("CHECKING: " + _parent.name + " FOR PAUSED");
			
			if (_parent.IsPaused || DirectorInformation.Paused) return;
			
			//Debug.Log("INSIDE THE DIRECTOR");
			
			if (_playerCar == null)
			{
				// ignore cameras
				Transform playerLocator = GameObject.Find("LOCATOR_PLAYER").transform;
				int childCount = playerLocator.childCount;
				for(int i = 0; i < childCount; ++i)
				{
					Transform child = playerLocator.GetChild(i);
					if(child.camera == null)
					{
						_playerCar = child.gameObject;
						break;
					}
				}
			}
			
			//EB.Debug.Log("PLAYER CAR EXISTS");
			
			// Set parent to local to world in correct location --- THIS ONLY HAPPENS ONCE
			if (_target.name == "Director Camera" && frame == 0)
			{	
				// Only happen if it's the first frame and we're sure this director is supposed to be local to world
				if (_track.space == SpaceType.LocalToWorld)
				{				
					if (!DirectorInformation.LocalToWorldDirectors.Contains(_parent))
					{
						DirectorInformation.LocalToWorldDirectors.Add(_parent);
					}
					
					// Find the player car and locator
					GameObject localToWorldLocator = GameObject.Find("localToWorldLocator");
					
					// If the locator doesn't exist, create it
					if (localToWorldLocator == null)
					{
						localToWorldLocator = new GameObject("localToWorldLocator");
					}
					
					// Set the position & rotation of the locator to that of the player car
					localToWorldLocator.transform.position = _playerCar.transform.position;
					localToWorldLocator.transform.eulerAngles = _playerCar.transform.eulerAngles;
					
					// If the camera isn't already a child of the locator, make it so
					if (_target.transform.parent != localToWorldLocator.transform)
					{	
						localToWorldLocator.transform.position = _target.transform.parent.position;
						localToWorldLocator.transform.eulerAngles = _target.transform.parent.eulerAngles;
					
						_target.transform.parent = localToWorldLocator.transform;
					}
					
					// Zero out the camera under it's new parent, the locator
					_target.transform.localPosition = Vector3.zero;
					_target.transform.localEulerAngles = Vector3.zero;
					
					// Act in local space relative to the locator parent
					_track.space = SpaceType.Local;
					
					//EB.Debug.Log("SET L2W CAMERA: " + _parent.name);
				}
				
				// If this director is *not* supposed to be local to world, make sure the camera is a child of the car!
				else if (_target.transform.parent != null)
				{
					if (_target.transform.parent.name == "localToWorldLocator")
					{
						//EB.Debug.Log("RESET PLAYERCAR CAMERA: " + _parent.name);
						
						_target.transform.parent = _playerCar.transform;
						
						DirectorInformation.LastPosition = _target.transform.localPosition;
						DirectorInformation.LastRotation = _target.transform.localRotation;
					}
				}
			}
			
			frame++;
			
			//Debug.Log("MOVING FORWARD");
			
			if (DirectorInformation.LerpToNextCamera && Application.isPlaying)
			{
				_parent.PlayTime = 0f;
				timeToLerp = DirectorInformation.LerpTime;
				
				if (timer < timeToLerp)
				{					
					//EB.Debug.Log("LERPING CAMERA: " + _parent.name + " --- TIMER: " + timer + " --- Parent: " + _target.transform.parent.name);
					
					timer += Time.deltaTime * ((Time.timeScale != 0) ? 1.0f/Time.timeScale : 0.0f);
					DirectorInformation.LerpProgress = timer;
					
					float easeInOutValue = EZAnimation.sinusInOut(timer, 0.0f, 1.0f, timeToLerp);
					
					var v = new QuatPos();
					v.pos = Vector3.Lerp(DirectorInformation.LastPosition, from.QuatPosValue.pos, easeInOutValue);
					v.quat = Quaternion.Lerp(DirectorInformation.LastRotation, from.QuatPosValue.quat, easeInOutValue);
					
					v.Apply(_target.transform, _track.space);
						
					DirectorInformation.LastPosition = v.pos; 
					DirectorInformation.LastRotation = v.quat;
					
					_currentQuatPos.pos = _playerCar.transform.TransformPoint(v.pos);
					_currentQuatPos.quat = v.quat;
					
					if (timer > timeToLerp)
					{
						//EB.Debug.Log("LERPED CAMERA: " + _parent.name);
						
						_target.transform.localPosition = from.QuatPosValue.pos;
						_target.transform.localRotation = from.QuatPosValue.quat;
						DirectorInformation.LerpTime = 1.0f;
						DirectorInformation.LerpToNextCamera = false;
						timer = 0f;
					}
				}
				
				return;
			}
			
			//Debug.Log("NOT LERPING");
			
			// Apply transform tween to target
			if ( _target != null && !DirectorInformation.LerpToNextCamera)
			{
				//EB.Debug.Log("BLENDING CAMERA: " + _parent.name + " --- BLEND: " + blend + " --- Parent: " + _target.transform.parent.name);
			
				var v1 = from.QuatPosValue;
				var v2 = to.QuatPosValue;
				var v = QuatPos.Lerp(v1,v2,blend);
				
				v.Apply(_target.transform, _track.space);
				
				if (_playerCar != null)
				{
					_currentQuatPos.pos = _playerCar.transform.TransformPoint(v.pos);
					_currentQuatPos.quat = v.quat;
					
					DirectorInformation.LastPosition = v.pos; 
					DirectorInformation.LastRotation = v.quat;
				}
			}
		}
		
		public override void SaveState ()
		{	
			if ( _target != null )
			{
				_state = QuatPos.FromTransform(_target.transform, _track.space);
			}
		}
		
		public override void RestoreState()
		{
			_state.Apply(_target.transform, _track.space);
		}
	}
	
	public class FOVTrackInstance : BlendableTrackInstance
	{
		private float _state;
		
		Camera targetCamera;
		private float timer = 0.0f;
		private float timeToLerp = 0.75f;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			if (_parent.IsPaused || DirectorInformation.Paused) return;
			
			if (_target.camera == null) _target = _target.GetComponentInChildren<Camera>().gameObject;
			
			if (DirectorInformation.LerpToNextCamera && Application.isPlaying)
			{
				_parent.PlayTime = 0f;
				timeToLerp = DirectorInformation.LerpTime;
				
				if (timer < timeToLerp)
				{
					timer += Time.deltaTime;
			
					float easeInOutValue = EZAnimation.sinusInOut(timer, 0.0f, 1.0f, timeToLerp);
					
					var v = Mathf.Lerp(DirectorInformation.LastFOV, from.FloatValue, easeInOutValue);
					_target.camera.fieldOfView = v;
						
					DirectorInformation.LastFOV = v;
					
					if (timer > timeToLerp)
					{
						_target.camera.fieldOfView = from.FloatValue;
					}
				}
				
				return;
			}

			if ( _target != null && _target.camera != null && !DirectorInformation.LerpToNextCamera)
			{
				var v1 = from.FloatValue;
				var v2 = to.FloatValue;
				var v = Mathf.Lerp(v1,v2,blend);
				
				if (v == 0) v = 45f;
				
				_target.camera.fieldOfView = v;
				DirectorInformation.LastFOV = v;
			}
		}
		
		public override void SaveState ()
		{
			if ( _target != null)
			{
				if (_target.camera == null) _target = _target.GetComponentInChildren<Camera>().gameObject;
					
				_state = _target.camera.fieldOfView;
			}
		}
		
		public override void RestoreState()
		{
			if ( _target != null )
			{
				if (_target.camera == null) _target = _target.GetComponentInChildren<Camera>().gameObject;
				
				if (_state == 0) _state = 45f;
				_target.camera.fieldOfView = _state;
			}
		}
	}
	
	public class OrthographicSizeTrackInstance : BlendableTrackInstance
	{
		private float _state;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			if ( _target != null && _target.camera != null )
			{
				var v1 = from.FloatValue;
				var v2 = to.FloatValue;
				var v = Mathf.Lerp(v1,v2,blend);
				_target.camera.orthographicSize = v;
			}
		}
		
		public override void SaveState ()
		{
			if ( _target != null && _target.camera != null )
			{
				_state = _target.camera.orthographicSize;
			}
		}
		
		public override void RestoreState()
		{
			if ( _target != null )
			{
				_target.camera.orthographicSize = _state;
			}
		}
	}
	
	public class TimeScaleTrackInstance : BlendableTrackInstance
	{
		private float _state;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			if (_parent.IsPaused || DirectorInformation.Paused) return;
			
			if (DirectorInformation.LerpToNextCamera)
			{
				Time.timeScale = Mathf.Lerp(DirectorInformation.LastTimeScale, from.FloatValue, DirectorInformation.LerpProgress);
				
				//EB.Debug.Log("Timescale: " + Time.timeScale + " - Director: " + _parent.name + " - TargetTS: " + from.FloatValue);
				
				DirectorInformation.LastTimeScale = Time.timeScale;
				
				return;
			}
			
			Time.timeScale = Mathf.Lerp( from.FloatValue, to.FloatValue, blend); 				
			DirectorInformation.LastTimeScale = Time.timeScale;
		}
		
		public override void SaveState ()
		{
			_state = Time.timeScale;
		}
		
		public override void RestoreState()
		{
			Time.timeScale = _state;
		}
	}
	
	public class DirectorTrackInstance : BlendableTrackInstance
	{
		private Camera _camera;
		private Camera _main;
		private Dictionary<int,Camera> _cameraMap = new Dictionary<int, Camera>();
		
		private static int _mainCullingMask = 0;
		
		private CameraData GetCamera( int id )
		{
			Camera camera;
			if ( _cameraMap.TryGetValue(id, out camera) )
			{
				return new CameraData(camera);
			}
			//EB.Debug.LogError("Failed to get camera: " + id );
			
			return new CameraData(_camera);
		}
		
		public override void Apply (float blend, Serialization.KeyFrame from, Serialization.KeyFrame to)
		{
			var v1 = GetCamera( (int)from.ByteValue ); 
			var v2 = GetCamera( (int)to.ByteValue );
			var v = CameraData.Lerp(v1, v2, blend);
			v.Apply(_camera);
		}
		
		public override void SaveState ()
		{
			// create the director camera
			_camera = new GameObject("director_camera for " + _parent.name, typeof(Camera) ).GetComponent<Camera>();
			GameObject.DontDestroyOnLoad(_camera.gameObject);
			
			//_camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
			_main = Camera.main;

			float aspect = (float)Screen.width / (float)Screen.height;
			if ( Application.isPlaying == false )
			{
				aspect = 4.0f / 3.0f;
			}
			//EB.Debug.Log(aspect);
			
			if ( _main != null )
			{
				// don't draw this camera
				
				if ( _main.cullingMask != 0 )
				{
					_mainCullingMask = _main.cullingMask;
				}
				
				_camera.cullingMask = _mainCullingMask;
				_camera.depth = 0;
					
				_camera.clearFlags = _main.clearFlags;
				_camera.backgroundColor = _main.backgroundColor;
				_camera.layerCullDistances = _main.layerCullDistances;
				_camera.depthTextureMode = _main.depthTextureMode;
				_camera.renderingPath = _main.renderingPath;
				
				_main.enabled = false;
				_main.tag = "";
				
				_cameraMap[0] = _main;
				EB.Debug.Log("MainCamera:" + _main.name );
			}
			else
			{
				EB.Debug.LogWarning("NO MAIN CAMERA!");
			}
			
			_camera.aspect = aspect;
			_camera.tag = "MainCamera";
			
			// fetch all the camera
			foreach( var group in _parent.Instances ) 
			{
				if ( group._group.type == GroupType.Camera )
				{
					var go = group._target;
					if ( go != null )
					{
						var camera = go.camera;
						if ( camera != null )
						{
							_cameraMap[group._group.id] = camera;
							camera.ResetProjectionMatrix();
							camera.ResetWorldToCameraMatrix();
							camera.ResetAspect();
							camera.aspect = aspect;
						}
					}
				}
			}
			
			// setup the director camera
			_parent.DirectorCamera = _camera.gameObject;
		}
		
		public override void RestoreState ()
		{
			if ( _camera != null )
			{
				GameObject.DestroyImmediate(_camera.gameObject);
			}
			
			if ( _main != null )
			{
				//Debug.LogError("Restoring camera state");
				_main.enabled = true;
				_main.tag = "MainCamera";
				_main.ResetAspect();
				_main.ResetProjectionMatrix();
				_main.ResetWorldToCameraMatrix();
			}
						
			_parent.DirectorCamera = null;
		}
	};
	
	public class EventTrackInstance : TrackInstance
	{
		private int _lastFrame = 0;
		private int _lastIndex = 0;
		
		public override void Update (float frameTime)
		{
			int frame = (int)frameTime;
			for ( ; _lastFrame <= frame; ++_lastFrame )
			{
				// find events for this frame
				for ( int i = _lastIndex; i < _track.frames.Count; ++i )
				{
					var kf = _track.frames[i];
					if ( kf.frame == _lastFrame )
					{
						_parent.FireEvent(kf.StringValue);
						_lastIndex = i + 1;
					}
					else if ( kf.frame > _lastFrame )
					{
						break;
					}
				}
			}
		}		
	}
	
	public abstract class ComponentTrackInstance : BlendableTrackInstance
	{
		protected Behaviour _component = null;
		bool _added = false;
				
		public override void SaveState ()
		{
			// create the component
			if ( _target != null )
			{
				string componentName = _group.targetName;
				_component = (Behaviour)_target.GetComponent(componentName);
				if ( _component == null )
				{
					_component = (Behaviour)_target.AddComponent(componentName);
					_component.enabled = false;
					_added = true;
				}
			}
			else
			{
				EB.Debug.LogError("Failed to get target for component enable");	
			}
					
			base.SaveState ();
		}
				
		public override void RestoreState ()
		{
			base.RestoreState ();
			
			if ( _component != null && _added )
			{
				Object.Destroy(_component);	
			}
		}
	}
		
	
	public class EnableComponentTrackInstance : ComponentTrackInstance
	{
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			if ( _component != null )
			{
				bool enabled = from.BoolValue;
				if ( _component.enabled != enabled )
				{
					_component.enabled = enabled;
				}
			}
		}
		
	}
	
	public class ComponentVariableTrackInstance : ComponentTrackInstance
	{
		System.Reflection.FieldInfo _field = null;
		VariableType _type = VariableType.None;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			Utils.ApplyVariable( _type, _field, _component, blend, from, to ); 
		}
		
		public override void SaveState ()
		{
			base.SaveState ();
			
			if ( _component != null )
			{
				_field = _component.GetType().GetField( _track.target );
				_type = Utils.GetVariableType(_field);
			}	
		}
	}
	
	public class StaticVariableTrackInstance : BlendableTrackInstance
	{
		System.Reflection.FieldInfo _field = null;
		VariableType _type = VariableType.None;
		object _default = null;
		
		public override void Apply (float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			Utils.ApplyVariable( _type, _field, null, blend, from, to ); 
		}
		
		public override void SaveState ()
		{
			base.SaveState ();
			
			var type = Utils.GetType(_group.targetName);
			if ( type != null )
			{
				_field = type.GetField( _track.target );
				_type = Utils.GetVariableType(_field);
				
				if ( _field != null )
				{
					_default = _field.GetValue(null);
				}
			}	
		}
		
		public override void RestoreState ()
		{
			if ( _default != null )
			{
				_field.SetValue(null,_default);
			}
			
			base.RestoreState ();
		}
	}
	
}
