using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PrefabPointer : MonoBehaviour 
{
	[SerializeField] private string m_GUIDSerialized = System.Guid.Empty.ToString();
	private System.Guid m_GUID;
	private GameObject m_Prefab = null;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Awake()
	{
		BuildPrefabPointers(false);
		BuildGUIDIfNecessary();
	}

	public static void BuildPrefabPointers(bool ForceRebuild)
	{
		//Debug.Log("Building PrefabPointers...");
		var AllPrefabPointers = Resources.FindObjectsOfTypeAll(typeof(PrefabPointer));
		var ActiveLoadedPrefabPointers = Object.FindObjectsOfType(typeof(PrefabPointer));
		
		foreach( var CurrentObject in AllPrefabPointers )
		{
			if (System.Array.IndexOf(ActiveLoadedPrefabPointers,CurrentObject)>=0)
			{
				continue;
			}
			else 
			{
				PrefabPointer CurrentPrefabPointer = CurrentObject as PrefabPointer;
				CurrentPrefabPointer.BuildGUIDIfNecessary();
			}
		}
	}

	public static void BuildPrefabPointers()
	{
		BuildPrefabPointers(false);
	}
	
	public GameObject GetPrefab()
	{
		if (null == m_Prefab)
		{
			BuildPrefabPointers(false);
		}

		return (m_Prefab);
	}

	public bool Is(PrefabPointer TestPrefabPointer)
	{
		if (null == TestPrefabPointer)
		{
			return (false);
		}

		BuildGUIDIfNecessary();
		TestPrefabPointer.BuildGUIDIfNecessary();
		
		//Utilities.Log(gameObject, "My GUID is " + GetGUIDAsString() + " and I am testing against " + TestPrefabPointer.gameObject.name + "(" + TestPrefabPointer.GetGUIDAsString() + ")");

		return (m_GUID == TestPrefabPointer.m_GUID);
	}
	
	// Use with caution!  Any saved data referencing the old GUID will need to be updated with the new one.
	public void GenerateGUID()
	{
		//Utilities.Log(gameObject, "Generating GUID...");

		m_GUID = System.Guid.NewGuid();
		m_GUIDSerialized = m_GUID.ToString();
	}

	public string GetGUIDAsString()
	{
		BuildGUIDIfNecessary();

		return (m_GUID.ToString());
	}

	public bool IsGUIDEmpty()
	{
		BuildGUIDIfNecessary();

		return (m_GUID == System.Guid.Empty);
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private void BuildGUIDIfNecessary()
	{
		if (System.Guid.Empty == m_GUID)
		{
			m_GUID = new System.Guid(m_GUIDSerialized);
		}
	}
}
