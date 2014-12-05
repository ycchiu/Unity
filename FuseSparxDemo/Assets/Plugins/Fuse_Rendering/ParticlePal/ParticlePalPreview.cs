#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using EB.Rendering;

public class ParticlePalPreview 
{
#if UNITY_EDITOR
	public static ParticlePal.QUALITY Quality = ParticlePal.QUALITY.High;

	[MenuItem("EBG/ParticlePal/Low")]
	public static void Low()
	{
		Quality = ParticlePal.QUALITY.Low;
	}

	[MenuItem("EBG/ParticlePal/Medium")]
	public static void Medium()
	{
		Quality = ParticlePal.QUALITY.Med;
	}

	[MenuItem("EBG/ParticlePal/High")]
	public static void High()
	{
		Quality = ParticlePal.QUALITY.High;
	}
#endif
}
