using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EB.Rendering;

namespace EB.Rendering
{
	public class TrailRendererManager : MonoBehaviour 
	{
		public class Config
		{
			public delegate EB.Rendering.TrailRenderer.eTRAIL_QUALITY GetQuality();
			public GetQuality GetQualityHandler = null;

			public delegate float GetMinimumDistanceThreshold();
			public GetMinimumDistanceThreshold GetMinimumDistanceThresholdHandler = null;

			public delegate float GetSegmentLength();
			public GetSegmentLength GetSegmentLengthHandler = null;
		}

		public enum eTRAIL_LENGTH
		{
			Short = 0,
			Medium =1,
			Long = 2
		};

		private static Config _config;

		public static void SetConfig(Config config)
		{
			_config = config;
		}

		static TrailRendererManager _this = null;
	
		public static TrailRendererManager Instance
		{ 
			get 
			{ 
				if(_this == null)
				{
					GameObject go = GameObject.Find("TrailRendererManager");
					if(go != null )
					{
						if(Application.isPlaying)
						{
							Destroy(go);
						}
						else
						{
							DestroyImmediate(go);
						}
					}
					go = new GameObject("TrailRendererManager");
					go.hideFlags = HideFlags.HideAndDontSave;
					
					_this = go.AddComponent<TrailRendererManager>();

					_this.Init();
				}
				return _this; 
			} 
		} 
		
		public TrailRenderer.eTRAIL_QUALITY GetQuality()
		{
			if(_config != null)
			{
				return _config.GetQualityHandler();
			}
			return TrailRenderer.eTRAIL_QUALITY.High;
		}
		
		public float GetMinimumDistanceThreshold()
		{
			if(_config != null)
			{
				return _config.GetMinimumDistanceThresholdHandler();
			}
			return 0.05f;
		}
		
		public float GetSegmentLength()
		{
			if(_config != null)
			{
				return _config.GetSegmentLengthHandler();
			}
			return 0.1f;
		}

		private int[] _VertSizeMap = {500, 750, 1000};
		private int[] _Capacity = { 1, 2, 1 };
		private Dictionary<eTRAIL_LENGTH, List<TrailRenderer>> _TrailPool;
		private Dictionary<TrailRenderer, eTRAIL_LENGTH> _ActiveTrails;
		GameObject parent;

		public void Init()
		{
			parent = new GameObject();
			parent.name = "TrailManagerPool";
			DontDestroyOnLoad(parent);
			_TrailPool = new Dictionary<eTRAIL_LENGTH, List<TrailRenderer>>();
			foreach(eTRAIL_LENGTH length in System.Enum.GetValues(typeof(eTRAIL_LENGTH)))
			{
				int capacity = _Capacity[(int)length];
				int vertSize = _VertSizeMap[(int)length];
				_TrailPool[length] = new List<TrailRenderer>(capacity);
				for (int i = 0; i < capacity; ++i)
				{
					_TrailPool[length].Add(new TrailRenderer(vertSize,parent));
				}
			}
			_ActiveTrails = new Dictionary<TrailRenderer, eTRAIL_LENGTH>();
		}



		public TrailRenderer GetTrailRenderer(eTRAIL_LENGTH length)
		{
			TrailRenderer trail = null;
			int vertSize = _VertSizeMap[(int)length];
			if(_TrailPool != null && _TrailPool[length].Count > 0) 
			{
				trail = _TrailPool[length][0];
				_TrailPool[length].RemoveAt(0);
				//Debug.Log("Trail "+ length.ToString() +" Requested - Give from pool Capacity: " + _TrailPool[length].Count);
			}
			else 
			{
				trail = new TrailRenderer(vertSize,parent);
				//Debug.Log("Trail "+ length.ToString() +" Requested - New One Created");
			}
			_ActiveTrails.Add(trail,length);
			//trail.SetupTrail();
			return trail;
		}
		
		public void ReturnTrailRenderer(TrailRenderer renderer)
		{
			if(renderer != null && _ActiveTrails != null && _ActiveTrails.ContainsKey(renderer)) 
			{
				eTRAIL_LENGTH length = _ActiveTrails[renderer];
				if(_TrailPool[length].Count < _Capacity[(int)length])
				{
					_TrailPool[length].Add(renderer);
					//_ActiveTrails.Remove(renderer);
					//Debug.Log("Trail "+ length.ToString() +" Returned to Pool");
				}
				else
				{
					renderer.DestroyTrail();

					//Debug.Log("Trail "+ length.ToString() +" Returned. Pool Maxed, Destroy");
				}
				_ActiveTrails.Remove(renderer);
			}
		}
	}
}