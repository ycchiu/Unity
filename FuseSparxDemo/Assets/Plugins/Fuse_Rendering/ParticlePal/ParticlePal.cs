using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EB.Rendering;
namespace EB.Rendering
{
	[ExecuteInEditMode]
	public class ParticlePal : MonoBehaviour
	{
		public enum QUALITY
		{
			Off = -1,
			Low = 0,
			Med = 1,
			High = 2,
		}
		
		public enum PARAMETER
		{
			None,
			EmissionRate,
			GravityMultiplier,
			StartingColor,
			StartingLifeSpan,
			StartingRotation,
			StartingSize,
			StartingSpeed
		}
		
		public enum TRIGGER
		{
			Constant,
			Height,
			Velocity
		}
		
		public enum TUNING
		{
			Constant,
			Linear,
			Curve,
		}
		
		public static int PARAMETER_COUNT 	= Enum.GetValues(typeof(ParticlePal.PARAMETER)).Length;
		public static int TRIGGER_COUNT 	= Enum.GetValues(typeof(ParticlePal.TRIGGER)).Length;
		public static int TUNING_COUNT 		= Enum.GetValues(typeof(ParticlePal.TUNING)).Length;	
		public static int QUALITY_COUNT 	= Enum.GetValues(typeof(ParticlePal.QUALITY)).Length - 1; //account for 'Off'

		public class Config
		{
			public delegate ParticlePal.QUALITY GetParticleQuality();
			public delegate int GetParticleQualityCount();
			public GetParticleQuality GetQualityHandler = null;
		}
		
		private static Config _config;
		
		public static void SetConfig(Config config)
		{
			_config = config;
		}
		
		public ParticlePal.QUALITY GetParticleQuality()
		{
			if(_config != null)
			{
				return _config.GetQualityHandler();
			}
			return ParticlePal.QUALITY.High;
		}

		[System.Serializable]
		public class Condition
		{
			public bool expanded; // for the editor
			public ParticlePal.PARAMETER parameter;
			public ParticlePal.TRIGGER trigger;
			
			[System.Serializable]
			public class Tuning
			{
				public ParticlePal.TUNING type;
				public float constant;
				public Color constantColor;
				public AnimationCurve curve;
				public float minX;
				public float minY;
				public float maxX;
				public float maxY;
				public Color minColor;
				public Color maxColor;
				
				public Tuning()
				{
					type = ParticlePal.TUNING.Linear;
					constant = 0.0f;
					constantColor = Color.white;
					curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
					minX = 0.0f;
					minY = 0.0f;
					maxX = 10.0f;
					maxY = 10.0f;
					minColor = Color.white;
					maxColor = Color.white;
				}
			}
			
			public Tuning[] tunings;
			
			public Condition()
			{
				expanded = true;
				parameter = ParticlePal.PARAMETER.None;
				trigger = ParticlePal.TRIGGER.Constant;
				tunings = new ParticlePal.Condition.Tuning[ParticlePal.QUALITY_COUNT];
				for (var i = 0; i < ParticlePal.QUALITY_COUNT; ++i)
				{
					tunings[i] = new ParticlePal.Condition.Tuning();
				}
			}
		}

		[SerializeField]
		private ParticleSystem _particleSystem;
		public List<Condition> conditions = new List<Condition>(); 
		public float VelocityDamping = 0.5f;
		public List<bool> isEnabled = new List<bool>();

		void Awake()
		{
			if (isEnabled == null || isEnabled.Count != ParticlePal.QUALITY_COUNT)
			{
				isEnabled = new List<bool>() { true, true, true };
			}
			
			if (Application.isPlaying)
			{
				quality = GetParticleQuality();
				DestroyIfPossible(gameObject);
			}
		}

		void Start () 
		{
			#if UNITY_EDITOR
			lastTime = EditorApplication.timeSinceStartup;
			#endif
			
			_particleSystem = particleSystem;

			if (conditions == null)
			{
				conditions = new List<Condition>();
			}
		}
		
		private Vector3 lastPosition = Vector3.zero;
		private float velocity = 0.0f;

	#if UNITY_EDITOR
		private double lastTime;
	#endif
		
		[System.NonSerialized]
		private ParticlePal.QUALITY quality = ParticlePal.QUALITY.High;

		private void UpdateVelocity()
		{
			Vector3 positionDiff = this.transform.position - lastPosition;
			#if UNITY_EDITOR
				float deltaTime = (float)(EditorApplication.timeSinceStartup - lastTime);
				lastTime = EditorApplication.timeSinceStartup;
			#else
				float deltaTime = Time.deltaTime;
			#endif
			velocity = (VelocityDamping * velocity) + ((1 - VelocityDamping) * positionDiff.magnitude / deltaTime);
			lastPosition = this.transform.position;
		}
		
		void Update () 
		{
			if (_particleSystem == null) return;

			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				quality = ParticlePalPreview.Quality;
				this.renderer.enabled = (quality != QUALITY.Off) && isEnabled[(int)quality];
			}
			else
			{
				quality = GetParticleQuality();
				this.renderer.enabled = (quality != QUALITY.Off) && isEnabled[(int)quality];
			}
			#endif

			
			UpdateVelocity();

			bool allConstant = true;
			
			foreach (Condition condition in conditions)
			{
				Condition.Tuning tuning = condition.tunings[(int)quality];
				
				if (condition.trigger == ParticlePal.TRIGGER.Constant || tuning.type == ParticlePal.TUNING.Constant)
				{
					//constant value for all qualities or this particular quality, early out
					if (condition.parameter == ParticlePal.PARAMETER.StartingColor)
					{
						SetParameter(condition.parameter, tuning.constantColor);
					}
					else
					{
						SetParameter(condition.parameter, tuning.constant);
					}
					continue;
				}

				allConstant = false;
				
				//find the trigger value to use in the tuning phase
				var triggerVal = 0.0f;
				
				switch (condition.trigger)
				{
				case(ParticlePal.TRIGGER.Height):
					triggerVal = this.transform.position.y;
					break;
				case(ParticlePal.TRIGGER.Velocity):
					triggerVal = velocity;
					break;
				default:
					#if UNITY_EDITOR
					Debug.LogError("ParticlePal trigger " + condition.trigger.ToString() + " is not recognized");
					#endif
					break;
				}
				
				//find the interpolated key based of tuning type
				float interpolateKey = 0.0f;
		
				switch(tuning.type)
				{
				case(ParticlePal.TUNING.Linear):
					interpolateKey = Mathf.Clamp01((triggerVal - tuning.minX) / (tuning.maxX - tuning.minX));
					break;
				case(ParticlePal.TUNING.Curve):
					AnimationCurve curve = tuning.curve;
					interpolateKey = curve.Evaluate((triggerVal - tuning.minX) / (tuning.maxX - tuning.minX));
					break;
				default:
					#if UNITY_EDITOR
					Debug.LogError("ParticlePal tuning " + condition.tunings[(int)quality].ToString() + " is not recognized");
					#endif
					break;
				}
				
				//interpolate the float or color
				
				switch(condition.parameter)
				{
				case(ParticlePal.PARAMETER.StartingColor):
					Color interpolatedColor = Color.Lerp(tuning.minColor, tuning.maxColor, interpolateKey);
					SetParameter(condition.parameter, interpolatedColor);
					break;
					
				default:
					float iterpolatedFloat = interpolateKey * (tuning.maxY - tuning.minY) + tuning.minY;
					SetParameter(condition.parameter, iterpolatedFloat);
					break;
				}
			}
			
			if (allConstant && Application.isPlaying)
			{
				//in game, disable the particle pal script, as we have completed an update and set all the constants
				this.enabled = false;
			}
		}
		
		private void SetParameter(ParticlePal.PARAMETER parameter, float val)
		{
			#if UNITY_EDITOR
			if (this.particleSystem == null)
			{
				Debug.LogError("Partical Pal is attached to something without a particle system");
				return;
			}
			#endif
		
			switch(parameter)
			{
			case(ParticlePal.PARAMETER.EmissionRate):
				this.particleSystem.emissionRate = val;
				break;
			case(ParticlePal.PARAMETER.StartingLifeSpan):
				this.particleSystem.startLifetime = val;
				break;
			case(ParticlePal.PARAMETER.StartingSize):
				this.particleSystem.startSize = val;
				break;
			case(ParticlePal.PARAMETER.StartingSpeed):
				this.particleSystem.startSpeed = val;
				break;
			case(ParticlePal.PARAMETER.StartingRotation):
				this.particleSystem.startRotation = val;
				break;
			case(ParticlePal.PARAMETER.GravityMultiplier):
				this.particleSystem.gravityModifier = val;
				break;
			case(ParticlePal.PARAMETER.None):
				break;
			default:
				#if UNITY_EDITOR
				Debug.LogError("ParticlePal float parameter " + parameter.ToString() + " is not recognized");
				#endif
				break;
			}
		}
					
		private void SetParameter(ParticlePal.PARAMETER parameter, Color val)
		{
			switch(parameter)
			{
			case(ParticlePal.PARAMETER.StartingColor):
				this.particleSystem.startColor = val;
				break;
			default:
				#if UNITY_EDITOR
				Debug.LogError("ParticlePal color parameter " + parameter.ToString() + " is not recognized");
				#endif
				break;
			}	
		}

		public static bool DisabledByParticlePal(GameObject gameObject)
		{
			ParticlePal particlePal = gameObject.GetComponent<ParticlePal>();
			
			if (particlePal == null)
			{
				return false;
			}
			
			if (particlePal.isEnabled == null || particlePal.isEnabled.Count != ParticlePal.QUALITY_COUNT)
			{
				particlePal.isEnabled = new List<bool>() { true, true, true };
			}

			var quality = particlePal.GetParticleQuality();
			return (quality == QUALITY.Off) || !particlePal.isEnabled[(int)quality];
		}

		public static bool WillDelete(GameObject gameObject)
		{
			if(!DisabledByParticlePal(gameObject))
			{
				return false;
			}

			bool canDelete = true;

			for(int i = 0; i < gameObject.transform.childCount; ++i)
			{
				canDelete &= WillDelete(gameObject.transform.GetChild(i).gameObject);
			}
			
			return canDelete;
		}
		
		private static void DestroyIfPossible(GameObject gameObject)
		{

			ParticlePal particlePal = gameObject.GetComponent<ParticlePal>();
			
			if (particlePal == null)
			{
				return;
			}

			if (WillDelete(gameObject))
			{
				Destroy(gameObject);
			}
			else
			{
				//couldn't delete all the children; can't delete me
				var quality = particlePal.GetParticleQuality();
				if ((quality == QUALITY.Off) || !particlePal.isEnabled[(int)quality])
				{
					//disable the particle system, then disable particlePal
					gameObject.particleSystem.Pause();
					gameObject.particleSystem.enableEmission = false;
					particlePal.enabled = false;
				}
			}
		}			
		
	#if UNITY_EDITOR
		public void AddCondition() 
		{
			conditions.Add(new Condition());
		}
		
		public void RemoveCondition(Condition condition) 
		{
			conditions.Remove(condition);
		}
	#endif
	}
}
