using UnityEngine;
using System.Collections;

namespace EB.Rendering
{
	public class DynamicPointLightInstance : GenericPoolType 
	{
		public AnimationCurve Intensity;
		public float IntensityMultiplier = 1.0f;
		public float IntensityFallOffDistance = 1.0f;
		public Gradient Gradient;
		public float CycleTime = 1.0f;
		public float Lifetime { get; private set; }
		public bool AutoPlay = true;
		public bool OneShot = true;
		public bool SimMode = false;

		float _PrevTick = 0f;

		public DynamicPointLightInstance()
		{
			Intensity = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
			Gradient = new Gradient();
		}

		// SR This is for the Matinee Cutscene, i need to be able to control the time independently
		// If you'd like to refactor this, lemme know..
		public void EnableSimMode(bool enable)
		{
			// Kick off the dynamic point light for Matinee!
			if (enable)
			{
				IsPlaying=false;
				SimMode=true;
				Play();
			}
			else
			{
				Stop();
			}
		}

		void OnEnable() 
		{
			if(AutoPlay) 
			{
				Play();
			}
		}

		public override void Play() 
		{
			if(IsPlaying) 
			{
				return;
			}
			Reset();
			DynamicPointLightManager.Instance.Register(this);
			IsPlaying = true;
		}

		public override void Stop()
		{
			if(!IsPlaying) 
			{
				return;
			}
			Reset();
			DynamicPointLightManager.Instance.DeRegister(this);
			IsPlaying = false;
		}

		public void Reset()
		{
			Lifetime = 0;
			_PrevTick = 0;
		}

		void OnDestroy()
		{
			if(IsPlaying)
			{
				Stop();
			}
		}

		void Update()
		{
			if(IsPlaying)
			{
				if (SimMode==false)
				{
					Sim (UnityEngine.Time.time);
				}
			}
		}

		// SR I want to be able to run 
		public void Sim(float tick)
		{
			if(!IsPlaying) 
			{ 
				return; 
			}

			float deltaTick = tick-_PrevTick;
			if(_PrevTick == 0) {
				deltaTick = 0;
			}
			//Debug.Log("Life Time " +Lifetime+ " CYCLE "+CycleTime+" DELTA " + tick + " prev " + _PrevTick + " delta " +deltaTick);
			_PrevTick = tick;
			Lifetime += deltaTick;

			if(OneShot && Lifetime > CycleTime) 
			{
				Stop();
			}
		}
	}
}