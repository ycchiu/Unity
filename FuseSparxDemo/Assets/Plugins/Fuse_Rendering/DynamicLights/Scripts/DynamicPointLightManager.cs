using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Rendering
{
	public class DynamicPointLightManager : MonoBehaviour 
	{
		static DynamicPointLightManager _this = null;
		static bool markDontReMake = false;

		public class Config
		{
			public delegate bool IsEnabled();
			public IsEnabled GetEnableHandler = null;
		}

		private static Config _config;
		
		public static void SetConfig(Config config)
		{
			_config = config;
		}

		public bool IsEnabled()
		{
			if(_config != null)
			{
				return _config.GetEnableHandler();
			}
			return true;
		}

		public static DynamicPointLightManager Instance
		{ 
			get 
			{ 
				if (!markDontReMake && _this == null)
				{
					InitializeInstance();
				}

				return _this; 
			}
		} 

		public static void InitializeInstance()
		{
			_this = FindObjectOfType<DynamicPointLightManager>();

			if (_this == null)
			{
				GameObject go = new GameObject("DynamicPointLightManager");
				_this = go.AddComponent<DynamicPointLightManager>();
				DontDestroyOnLoad(go);
			}

			_this.Init();
		}

		public static bool IsInitialized()
		{
			return _this != null;
		}


		List<DynamicPointLightInstance> lights = new List<DynamicPointLightInstance>(4);
		
		public void Init() 
		{
			lights = new List<DynamicPointLightInstance>(4);
		}

		public void Sim() 
		{
			if(!IsEnabled())
			{
				return;
			}

			Matrix4x4 lighting = new Matrix4x4();
			Matrix4x4 position = new Matrix4x4();
			Vector4 multiplier = new Vector4();
			Vector4 intensity = new Vector4();

			for(int i = 0; i < lights.Count; ++i)
			{
				DynamicPointLightInstance light = lights[i];

				float t = (light.Lifetime % light.CycleTime) / light.CycleTime;


				Color col = light.Gradient.Evaluate(t);
				lighting.SetColumn(i, col);

				Vector3 pos = light.gameObject.transform.position;
				position.SetRow(i, new Vector4( pos.x, pos.y, pos.z, 0.0f));

				intensity[i] = light.Intensity.Evaluate(t) * light.IntensityMultiplier;

				float fallOff = Mathf.Max(0.01f, light.IntensityFallOffDistance);
				multiplier[i] = 25.0f / (fallOff * fallOff);
			}

			for (int i = lights.Count; i < 4; ++i)
			{	
				position.SetRow(i, Vector4.zero);
				lighting.SetColumn(i, Vector4.zero);
				multiplier[i] = 0;
				intensity[i] = 1;
			}

			Shader.SetGlobalMatrix( "_EBGPointLightColor", lighting );
			Shader.SetGlobalMatrix( "_EBGPointLightPosition", position );
			Shader.SetGlobalVector( "_EBGPointLightMultiplier", multiplier );
			Shader.SetGlobalVector( "_EBGPointLightIntensity", intensity );
		}

		void Update() 
		{
			Sim();
		}

		public void Register(DynamicPointLightInstance light)
		{
			if (lights.Contains(light))
			{
				EB.Debug.LogWarning("DynamicPointLight trying to register the same light!");
				return;
			}
			if (lights.Count < lights.Capacity)
			{
				lights.Add(light);
			}
			else
			{
				EB.Debug.LogWarning("Too many DynamicPointLights!");
			}
		}

		public void DeRegister(DynamicPointLightInstance light)
		{
			if (lights.Contains(light))
			{
				//EB.Debug.Log("light deregistered!");
			}
			lights.Remove(light);
		}

		public void DeRegisterAll()
		{
			lights.Clear();
		}

		private void OnApplicationQuit()
		{
			Destroy(gameObject);
			markDontReMake = true;
		}
		
	#if UNITY_EDITOR
		public void Clear()
		{
			if(_this != null && _this.gameObject != null)
			{
				GameObject.DestroyImmediate(_this.gameObject);
			}
		}
	#endif
	}
}