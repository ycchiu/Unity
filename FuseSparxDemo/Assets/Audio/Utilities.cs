using UnityEngine;
using System.Collections;
using System;

public static class Utilities
{	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
		
	public static float VolumeScalarToDecibels(float VolumeScalar)
	{
		// Decibels = 20 * log10(Scalar)
		return (20.0f * Mathf.Log10(Mathf.Clamp(VolumeScalar, AudioConstants.s_VolumeScalarMin, AudioConstants.s_VolumeScalarMax)));
	}
	
	public static float VolumeDecibelsToScalar(float VolumeDecibels)
	{
		// Scalar = 10 ^ (Decibels / 20)
		return (Mathf.Pow(10.0f, Mathf.Clamp(VolumeDecibels, AudioConstants.s_VolumeDecibelsMin, AudioConstants.s_VolumeDecibelsMax) / 20.0f));
	}

	public static float PitchScalarToSemitones(float PitchScalar)
	{
		// Semitones = 12 * log2(Scalar) = 12 * (log10(Scalar) / log10(2))
		return (12.0f * (Mathf.Log10(Mathf.Clamp(PitchScalar, AudioConstants.s_PitchScalarMin, AudioConstants.s_PitchScalarMax)) / Constants.s_Log2));
	}

	public static float PitchSemitonesToScalar(float PitchSemitones)
	{
		// Scalar = 2 ^ (Semitones / 12)
		return (Mathf.Pow(2.0f, Mathf.Clamp(PitchSemitones, AudioConstants.s_PitchSemitonesMin, AudioConstants.s_PitchSemitonesMax) / 12.0f));
	}

	public static bool AreFloatsEqual(float A, float B)
	{
		return (!(Mathf.Abs(A - B) > Mathf.Epsilon));
	}
	
}
