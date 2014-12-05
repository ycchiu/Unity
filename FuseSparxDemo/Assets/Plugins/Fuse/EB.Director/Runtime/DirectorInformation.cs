using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EB.Sequence;

public class DirectorInformation : MonoBehaviour 
{
	public static bool Paused = false;
	
	public static string CameraToLerpFrom;
	
	public static Vector3 LastPosition = Vector3.zero;
	public static Quaternion LastRotation = Quaternion.identity;
	public static float LastFOV = 45f;
	public static float LastTimeScale = 1.0f;
	public static bool ACameraIsLerping = false;

	public static List<EB.Director.Component> LocalToWorldDirectors = new List<EB.Director.Component>();
	
	public static bool LerpToNextCamera = false;
	
	public static float LerpTime = 1.0f;
	public static float LerpProgress = 0.0f;
	
	public static RaceState CurrentRaceState = RaceState.NECK_WINNING;
	
	public enum RaceState
	{
		REALLY_WINNING,
		WINNING,
		NECK_WINNING,
		NECK_LOSING,
		LOSING,
		REALLY_LOSING,
	}	
}
