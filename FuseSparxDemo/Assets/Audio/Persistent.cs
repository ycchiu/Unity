using UnityEngine;
using System.Collections;

public class Persistent : MonoBehaviour 
{
	public void Awake()
	{
		DontDestroyOnLoad(gameObject);		
	}	
}
