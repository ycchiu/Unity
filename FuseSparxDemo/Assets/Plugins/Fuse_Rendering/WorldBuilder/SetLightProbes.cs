using UnityEngine;
using System.Collections;

public class SetLightProbes : MonoBehaviour 
{
	public LightProbes Probes;
	
	// Use this for initialization
	void Start () 
	{
		LightmapSettings.lightProbes = Probes;	
	}

}
