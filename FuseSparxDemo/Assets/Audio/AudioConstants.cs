using UnityEngine;
using System.Collections;

public static class AudioConstants
{
	public const float s_VolumeScalarMin = 0.0001f;
	public const float s_VolumeScalarMax = 1.0f;
	public const float s_VolumeScalarDefault = s_VolumeScalarMax;

	public const float s_VolumeDecibelsMin = -80.0f;
	public const float s_VolumeDecibelsMax = 0.0f;
	public const float s_VolumeDecibelsDefault = s_VolumeDecibelsMax;

	public const float s_PitchScalarMin = 0.0625f;
	public const float s_PitchScalarMax = 16.0f;
	public const float s_PitchScalarDefault = 1.0f;

	public const float s_PitchSemitonesMin = -48.0f;
	public const float s_PitchSemitonesMax = 48.0f;
	public const float s_PitchSemitonesDefault = 0.0f;

	public const int s_PriorityMin = 0;
	public const int s_PriorityMax = 255;
	public const int s_PriorityDefault = 128;

	public const float s_DopplerLevelMin = 0.0f;
	public const float s_DopplerLevelMax = 2.0f;
	public const float s_DopplerLevelDefault = s_DopplerLevelMin;

	public const float s_MinDistanceMin = 0.0f;
	public const float s_MinDistanceMax = 1000.0f;
	public const float s_MinDistanceDefault = 1.0f;

	public const float s_MaxDistanceMin = 0.0f;
	public const float s_MaxDistanceMax = 1000.0f;
	public const float s_MaxDistanceDefault = 10.0f;
	
	public const AudioRolloffMode s_RolloffModeDefault = AudioRolloffMode.Logarithmic;

	public const float s_FadeTimeMin = 0.0f;
	public const float s_FadeTimeMax = 30.0f;
}

