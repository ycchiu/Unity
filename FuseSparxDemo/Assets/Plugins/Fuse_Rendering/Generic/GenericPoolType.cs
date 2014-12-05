using UnityEngine;
using System.Collections;

public abstract class GenericPoolType : MonoBehaviour
{
	[System.NonSerialized]
	public bool IsPlaying = false;

	public abstract void Play();
	public abstract void Stop();
}
