using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Light Probe Helper/Light Probe Generator")]
public class LightProbeGenerator : MonoBehaviour
{
	[System.Serializable]
	public class LightProbeArea
	{
		public Bounds ProbeVolume;
		public Vector3 Subdivisions = Vector3.one * 5;
		public int RandomCount = 0;
	}

	public enum LightProbePlacementType
	{
		Grid,
		Random
	}
	
#if UNITY_EDITOR	
	public LightProbeArea[] LightProbeVolumes;
	public LightProbePlacementType PlacementAlgorithm;
	
	public void ClearProbes()
	{
		LightProbeGroup lprobe = GetComponent<LightProbeGroup>();
		if( lprobe == null )
		{
			Debug.LogError( "LightProbeGenerator: Must have LightProbeGroup attached!" );
			return;
		}
		
		lprobe.probePositions = null;
	}
	
	public void GenProbes()
	{
		Debug.Log("Generating probes in scene: " + UnityEditor.EditorApplication.currentScene);
		
		ClearProbes();
		//todo: generate the probes
		LightProbeGroup lprobe = GetComponent<LightProbeGroup>();
		if( lprobe == null )
		{
			Debug.LogError( "LightProbeGenerator: Must have LightProbeGroup attached!" );
			return;
		}

		List<Vector3> probePositions = new List<Vector3>();

		foreach( LightProbeArea area in LightProbeVolumes )
		{
			if( PlacementAlgorithm == LightProbePlacementType.Grid )
			{
				probePositions.AddRange( GetProbesForVolume_Grid( area.ProbeVolume, area.Subdivisions ) );
			}
			else
			{
				probePositions.AddRange( GetProbesForVolume_Random( area.ProbeVolume, area.RandomCount ) );
			}
		}

		lprobe.probePositions = probePositions.ToArray();
	}
	
	public void SetVolumeAuto(float factor = 0.15f)
	{
		// Determine scene bounds
		Bounds b = new Bounds(Vector3.zero, Vector3.zero);
		foreach (Renderer r in GameObject.FindObjectsOfType(typeof(Renderer)))
		{
    		b.Encapsulate(r.bounds);
		}
		
		foreach (MeshRenderer r in GameObject.FindObjectsOfType(typeof(MeshRenderer)))
		{
    		b.Encapsulate(r.bounds);
		}
		
		if (LightProbeVolumes.Length > 0)
		{
			LightProbeVolumes[0].ProbeVolume.extents = b.extents * 2;
			LightProbeVolumes[0].ProbeVolume.center = b.center;
			
			LightProbeVolumes[0].Subdivisions = new Vector3((int)(LightProbeVolumes[0].ProbeVolume.extents.x*factor), 1, (int)(LightProbeVolumes[0].ProbeVolume.extents.z*factor));
		}
	}
	
	List<Vector3> GetProbesForVolume_Grid( Bounds ProbeVolume, Vector3 Subdivisions )
	{
		List<Vector3> probePositions = new List<Vector3>();

		Vector3 step = new Vector3( ProbeVolume.extents.x / Subdivisions.x, ProbeVolume.extents.y / Subdivisions.y, ProbeVolume.extents.z / Subdivisions.z );

		for( int x = 0; x <= Subdivisions.x; x++ )
		{
			for( int y = 0; y <= Subdivisions.y; y++ )
			{
				for( int z = 0; z <= Subdivisions.z; z++ )
				{
					Vector3 probePos = ( ProbeVolume.center - ( ProbeVolume.extents / 2 ) ) + new Vector3( step.x * x, step.y * y, step.z * z );
					
					// Only add probe if it is above the ground, and start high enough to hit various levels of track
					RaycastHit hitInfo;
					
					if (Physics.Raycast(probePos, -Vector3.up, out hitInfo, 1000))
					{
						probePositions.Add(hitInfo.point + new Vector3(0,0.1f,0));
						probePositions.Add(hitInfo.point + new Vector3(0,3,0));
					}
				}
			}
		}

		return probePositions;
	}

	List<Vector3> GetProbesForVolume_Random( Bounds ProbeVolume, int Count)
	{
		List<Vector3> probePositions = new List<Vector3>();

		for( int c = 0; c <= Count; c++ )
		{
			Vector3 probePos = ProbeVolume.center + new Vector3( Random.Range( -0.5f, 0.5f ) * ProbeVolume.extents.x, Random.Range( -0.5f, 0.5f ) * ProbeVolume.extents.y, Random.Range( -0.5f, 0.5f ) * ProbeVolume.extents.z );
			probePositions.Add( probePos - transform.position );
		}

		return probePositions;
	}

	void OnDrawGizmosSelected()
	{
		if( LightProbeVolumes != null )
		{
			Gizmos.color = Color.red;
			foreach( LightProbeArea volume in LightProbeVolumes )
			{
				Gizmos.DrawWireCube( volume.ProbeVolume.center, volume.ProbeVolume.extents );
			}
		}
	}
#endif
}