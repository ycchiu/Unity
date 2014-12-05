using UnityEngine;
using System.Collections;
using EB.Rendering;

namespace EB.Rendering
{
	public class GenericTrailRendererInstance : GenericPoolType 
	{ 
		public TrailRenderer trailRenderer;

		public float _DistanceThreshold = 0.05f;
		public float _TrailTime = 5.0f;
		public float _SegmentLength = 0.2f;
		public float _CurveTension = 0.75f;
		public float _FadeStartTime = 1f;
		public float _FadeDuration = 0.5f;
		public float _FadeTimeMultipler = 0;
		public float _TextureRepeat = 1;
		public float _TextureMetersSecond = 0;
		public int _TextureYSplit = 0;
		public GameObject _Point1;
		public GameObject _Point2;
		public AnimationCurve _WidthCurve = new AnimationCurve();
		public bool _IgnoreZ = false;
		public bool _SpanOverTrail = false;
		public Gradient _ColorGradient;
		public bool _AddColor = false;
		public Gradient _LifeGradient;
		public Material _Material;
		public TrailRenderer.eTRAIL_TYPE _TrailType;
		public TrailRendererManager.eTRAIL_LENGTH _TrailLength;
		public bool _AutoPlay = false;
		
		public TrailRenderer.eTIME_UNITS _TimeUnits;
		public int _TrailTimeInFrames = 3;
		public int _FadeStartTimeInFrames = 1;
		public int _FadeDurationInFrames = 1;

		private Vector3 _Offset1;
		private Vector3 _Offset2;
		private bool _UseLocalOffsets;

		private Vector3 _OriginalPos1;
		private Vector3 _OriginalPos2;

		// TJ: use an AttachTransform to lock the trail position along specified axes
		//private AttachTransform	_AttachTransform;

		public bool isSim=false;

		public bool isPaused = false;
		public float pauseOffset = 0;
		public override void Play() { Play(UnityEngine.Time.time); }
		public void Play(float time)
		{
			if(IsPlaying)
			{
				return;
			}

			// TJ: reset points before playing
			_Point1.transform.localPosition = _OriginalPos1;
			_Point2.transform.localPosition = _OriginalPos2;

			trailRenderer = TrailRendererManager.Instance.GetTrailRenderer(_TrailLength);
			Animation anim = GetComponent<Animation>();
			if(anim && !anim.isPlaying) {
				anim.Play();
			}
			trailRenderer._TrailTime = _TrailTime;
			UpdateValues();
			trailRenderer.SetupTrail(time);
			IsPlaying = true;

			UpdateTrailRenderer();
		}

		public override void Stop()
		{

			if (trailRenderer != null)
			{
				pauseOffset = 0;
				trailRenderer.Reset();
				if(!Application.isPlaying) 
				{
					#if UNITY_EDITOR
					trailRenderer.DestroyTrail();
					#endif
				}
				else 
				{	
					TrailRendererManager.Instance.ReturnTrailRenderer(trailRenderer);
				}

				trailRenderer = null;
			}
			IsPlaying = false;
		}

		void Awake() 
		{
			_OriginalPos1 = _Point1.transform.localPosition;
			_OriginalPos2 = _Point2.transform.localPosition;

			if(_AutoPlay)
			{
				IsPlaying = false;
				Play(UnityEngine.Time.time);
			}
		}

		public void Pause(bool pause)
		{
			isPaused = pause;
		}


		void LateUpdate () 
		{
			if(isPaused)
			{
				pauseOffset += Time.deltaTime;
				return;
			}
			// SR This is for matinee playback
			if (isSim)
				return;

			if(trailRenderer != null) 
			{
				UpdateTrailRenderer();
			}
		}

		public virtual void UpdateTrailRenderer()
		{
			#if UNITY_EDITOR
			UpdateValues();
			if(_TimeUnits ==  TrailRenderer.eTIME_UNITS.Frames) 
			{
				ConvertFramesToSeconds();
			}
			trailRenderer._TrailTime = _TrailTime;
			#endif
			
			// TJ: Update our attach transform before updating trail
			//if (_AttachTransform != null)
			//{
			//	_AttachTransform.UpdateAttachment();
			//}
			
			if (!trailRenderer.Update(pauseOffset))
			{
				Stop();
			}
		}

		void OnDestroy()
		{
			if (trailRenderer != null)
			{
				Stop ();
			}
		}

		public void UpdateValues() 
		{
			trailRenderer._TrailType = _TrailType;
			trailRenderer._DistanceThreshold = _DistanceThreshold;
			trailRenderer._CurveTension = _CurveTension;
			trailRenderer._TextureRepeat = _TextureRepeat;
			trailRenderer._FadeStartTime = _FadeStartTime;
			trailRenderer._FadeDuration = _FadeDuration;
			trailRenderer._TextureMetersSecond = _TextureMetersSecond;
			trailRenderer._TextureYSplit = _TextureYSplit;
			trailRenderer._WidthCurve = _WidthCurve;
			trailRenderer._SpanOverTrail = _SpanOverTrail;
			trailRenderer._AddColor = _AddColor;
			trailRenderer._LifeGradient = _LifeGradient;
			trailRenderer._ColorGradient = _ColorGradient;
			trailRenderer._Material = _Material;
			trailRenderer._Point1 = _Point1;
			trailRenderer._Point2 = _Point2;
			trailRenderer._Offset1 = _Offset1;
			trailRenderer._Offset2 = _Offset2;
			trailRenderer._UseLocalOffsets = _UseLocalOffsets;
			trailRenderer._IgnoreZ = _IgnoreZ;
		}
		 
		public void ConvertFramesToSeconds(float fps = 30.0f)
		{
			_TrailTime = _TrailTimeInFrames / fps;
			_FadeStartTime = _FadeStartTimeInFrames / fps;
			_FadeDuration = _FadeDurationInFrames / fps;
		}

		public void ConvertSecondsToFrames(float fps = 30.0f)
		{
			_TrailTimeInFrames = Mathf.FloorToInt(_TrailTime * fps);
			_FadeStartTimeInFrames = Mathf.FloorToInt(_FadeStartTime * fps);
			_FadeDurationInFrames = Mathf.FloorToInt(_FadeDuration * fps);
		}

		public void UpdatePointsAndEnable(GameObject p1, GameObject p2)
		{	
			trailRenderer._Point1 = p1;
			trailRenderer._Point2 = p2;
		}

		public void OffsetPoints(Vector3 p1, Vector3 p2, bool useLocalOffsets)
		{	
			_Offset1 = p1;
			_Offset2 = p2;
			_UseLocalOffsets = useLocalOffsets;
		}

		//public void SetAttachTransform(AttachTransform attachTransform)
		//{
		//	_AttachTransform = attachTransform;
	//		_AttachTransform.UpdateManually = true;
	//	}

	//	#if UNITY_EDITOR
		public void ClearSimulate()
		{
			GameObject simulateObject = GameObject.Find("EditorTrailRender");
			
			if(simulateObject != null)
			{	
				DestroyImmediate(simulateObject);
			}
			trailRenderer = null;
			IsPlaying = false;
			isSim=false;
		}
		
		public void Simulate(float time)
		{

			isSim=true;
			GameObject simulateObject = GameObject.Find("EditorTrailRender");
					
			if(simulateObject == null) 
			{
				simulateObject = new GameObject();
				simulateObject.name = "EditorTrailRender";
			}

			SimulateWithEntity(time,simulateObject);
		}

		public void SimulateWithEntity(float time, GameObject simObject)
		{

			isSim=true;

			SetupSimTrail(time,simObject);
			trailRenderer.Sim(time);
		}

		public void SetupSimTrail(float time, GameObject simObject)
		{
			if(trailRenderer == null)
			{
				trailRenderer = new TrailRenderer(1000,simObject);
				UpdateValues();
				trailRenderer._TrailTime = _TrailTime;
				trailRenderer.SetupTrail(time);
			}
		}
		
		public void ResetTrail()
		{
			if(trailRenderer != null)
			{
				trailRenderer.Reset();
				IsPlaying = false;
			}
		}
	//	#endif
	}
}
