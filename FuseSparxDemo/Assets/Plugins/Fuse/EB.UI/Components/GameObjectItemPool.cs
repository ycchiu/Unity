// #define DEBUG_ITEM_POOL_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameObjectItemPool
{
	/////////////////////////////////////////////////////////////////////////
	#region Public Data Structures
	/////////////////////////////////////////////////////////////////////////
	public class Item
	{
		public GameObject gameObject;
		public object data;
		public int index;
		public bool inUse = false;
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Variables
	/////////////////////////////////////////////////////////////////////////
	public int useCount { get; private set; }
	public int resourceCount { get { return poolItems.Count; } }
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Variables
	/////////////////////////////////////////////////////////////////////////
	private List<Item> poolItems = new List<Item>();
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	public List<GameObject> GetResources()
	{
		List<GameObject> resources = new List<GameObject>(poolItems.Count);
		
		foreach (Item item in poolItems)
		{
			resources.Add(item.gameObject);
		}
		
		return resources;
	}
	
	public void UpdateActiveResources()
	{
		foreach (Item item in poolItems)
		{
			if (item.inUse != item.gameObject.activeSelf)
			{
				item.gameObject.SetActive(item.inUse);
			}
		}
	}
	
	// Request use of a pool item:
	public Item UseItem(int index)
	{
		Item i = poolItems.Find(item => !item.inUse);
		
		if (i != null)
		{
			i.index = index;
			i.inUse = true;
			i.gameObject.SetActive(true);
			i.gameObject.name = "poolitem_" + index;
			++useCount;
#if DEBUG_ITEM_POOL_CLASS
			Report(string.Format("Use Item > {0} ({1}/{2})", index, useCount, resourceCount));
#endif
		}
		else
		{
			Debug.LogError(string.Format("Use Item > {0} ({1}/{2})", index, useCount, resourceCount));
		}
		
		return i;
	}
	
	// Release use of a pool item:
	public void ReleaseItem(int index)
	{
		Item i = poolItems.Find(item => item.index == index);
		
		if (i != null)
		{
#if DEBUG_ITEM_POOL_CLASS
			Report(string.Format("Release Item > {0} ({1}/{2})", index, useCount, resourceCount));
#endif
			i.index = -1;
			i.inUse = false;
			i.gameObject.SetActive(false);
			--useCount;
		}
		else
		{
			Debug.LogError(string.Format("Release Item > {0} ({1}/{2})", index, useCount, resourceCount));
		}
	}
	
	public void ReleaseAll()
	{
		foreach (Item i in poolItems)
		{
			i.index = -1;
			i.inUse = false;
			i.gameObject.SetActive(false);
		}
		useCount = 0;
	}
	
	// Add resources to the pool.
	public void AddResource(GameObject go)
	{
		Item item = new Item();
		item.gameObject = go;
		item.inUse = false;
		item.index = -1;
		item.data = null;
		item.gameObject.SetActive(false);
		
		poolItems.Add(item);
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Report(string msg)
	{
#if DEBUG_ITEM_POOL_CLASS
		EB.Debug.Log(string.Format("[{0}] ItemPool > {1}", Time.frameCount, msg));
#endif
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
