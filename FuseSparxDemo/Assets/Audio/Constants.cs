using UnityEngine;
using System.Collections;

public static class Constants
{
	public const int s_SafetyLoopCount = 32;

	public const float s_SmallDelta = 0.0001f;

	public const float s_Log2 = 0.301029996f;

	public const float s_HalfPi = Mathf.PI / 2.0f;
	public const float s_TwoPi = Mathf.PI * 2.0f;

	public const float s_DegreesInCircle = 360.0f;
	public const float s_HalfDegreesInCircle = s_DegreesInCircle / 2.0f;
	public const float s_QuarterDegreesInCircle = s_DegreesInCircle / 4.0f;
	
	public const int s_CullDistance = 100000;	
}