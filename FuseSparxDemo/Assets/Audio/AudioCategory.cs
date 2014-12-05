using UnityEngine;
using System;
using System.Collections;

public class AudioCategory : MonoBehaviour
{
	[HideInInspector] public float m_VolumeDB = AudioConstants.s_VolumeDecibelsDefault;

	[NonSerialized] private bool m_IsSetUp = false;
	[NonSerialized] private AudioLerper m_VolumeScalarLerper = new AudioLerper();

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void SetUp()
	{
		if (false == m_IsSetUp)
		{
			m_VolumeScalarLerper.Lerp(Utilities.VolumeDecibelsToScalar(m_VolumeDB), 0.0f);
			m_IsSetUp = true;
		}
	}

	public void ShutDown()
	{
		m_IsSetUp = false;
	}

	public void UpdateLerpers(float DeltaTime)
 	{
		SetUp();
		
		m_VolumeScalarLerper.Update(DeltaTime);

		//Utilities.Log(gameObject, "Volume is now " + Utilities.VolumeScalarToDecibels(m_VolumeScalarLerper.GetValue()).ToString("F2"));
 	}

	public float GetVolumeScalar()
	{
		SetUp();
		
	//	EB.Debug.Log(string.Format("Getting vol scalar {0} on cat {1}", (m_VolumeScalarLerper.GetValue()), name));
		return (m_VolumeScalarLerper.GetValue() * _modulate);
	}

	public void SetVolumeScalar(float VolumeScalar, float TransitionTime)
	{
		SetUp();
		
	//	EB.Debug.Log(string.Format("Setting vol scalar {0} on cat {1}", VolumeScalar, name));
		m_VolumeScalarLerper.Lerp(AudioControl.SFXLevel * Mathf.Clamp(VolumeScalar, AudioConstants.s_VolumeScalarMin, AudioConstants.s_VolumeScalarMax), TransitionTime);	// moko: scaled audio volume with AudioControl setting (FFSIX-2687)
	}

	public void SetVolumeScalar(float VolumeScalar)
	{
		SetVolumeScalar(VolumeScalar, 0.0f);
	}
	
	public float Modulate
	{
		set
		{
			_modulate = value;
			SetVolumeScalar(m_VolumeScalarLerper.GetValue(), 0.0f);
		}
	}
	
	private float _modulate = 1.0f;
	
	private bool _muted;
	public bool Muted{get{return _muted;} set{_muted = value; Debug.Log("****** : " + _muted);}}
}

	