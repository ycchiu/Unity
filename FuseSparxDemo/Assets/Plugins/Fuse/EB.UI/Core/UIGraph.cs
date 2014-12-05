using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIGraph : MonoBehaviour
{
	/////////////////////////////////////////////////////////////////////////
	#region Internal Data Structures
	/////////////////////////////////////////////////////////////////////////
	public enum Link
	{
		Up,
		Down,
		Left,
		Right
	}
	
	/// GraphItem ///////////////////////////////////////////////////////////
	/// This class is used to represent a node on the graph, and stores its
	/// links to other nodes.
	/////////////////////////////////////////////////////////////////////////
	public class GraphItem
	{
		public UIObject uiObject;
		public Dictionary<Link, GraphItem> links = new Dictionary<Link, GraphItem>();
		public Vector2 uiPosition;

		public Hashtable Serialize(UIGraph container)
		{
			Hashtable data = new Hashtable();

			if (uiObject != null)
			{
				data["obj"] = uiObject.Serialize(container.gameObject);
			}

			Hashtable linkData = new Hashtable();

			foreach (KeyValuePair<Link, GraphItem> kvp in links)
			{
				int id = container.GetSerializationID(kvp.Value);
				if (id >= 0)
				{
					linkData[kvp.Key.ToString()] = id;
				}
			}

			data["links"] = linkData;
			
			return data;
		}

		public void Deserialize(UIGraph container, Hashtable data)
		{
			Hashtable objData = EB.Dot.Object("obj", data, new Hashtable());
			uiObject = new UIObject(container.gameObject, objData);
			uiPosition = container.GetUiPosition(uiObject.gameObject);

			Hashtable linkData = EB.Dot.Object("links", data, new Hashtable());
			foreach (Link direction in EB.Util.GetEnumValues<Link>())
			{
				int linkToItemID = EB.Dot.Integer(direction.ToString(), linkData, -1);
				if (linkToItemID != -1)
				{
					links[direction] = container.GetItemBySerializationID(linkToItemID);
				}
			}
		}

		public override string ToString()
		{
			string msg = "GraphItem\n";
			
			msg += "uiObject: " + EB.UIUtils.GetFullName(uiObject.gameObject) + "\n";
			msg += string.Format("pos: {0:0.00},{1:0.00}\n", uiPosition.x, uiPosition.y);
			msg += "links: " + links.Count + "\n";
			foreach (KeyValuePair<Link, GraphItem> kvp in links)
			{
				string itemName = "null";
				if (kvp.Value != null && kvp.Value.uiObject.gameObject != null)
				{
					GameObject container = kvp.Value.uiObject.gameObject;
					itemName = EB.UIUtils.GetFullName(container);
				}
				msg += kvp.Key.ToString() + ": " + itemName + "\n";
			}
			
			return msg.Trim();
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region uiCamera
	protected Camera uiCamera
	{
		get
		{
			if (_uiCamera == null)
			{
				UICamera uiCam = EB.Util.FindComponentUpwards<UICamera>(gameObject);
				_uiCamera = uiCam.camera;
			}
			return _uiCamera;
		}
	}
	private Camera _uiCamera = null;
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	public List<GraphItem> graphItems;
	public string serializedData = "";
	public bool autoLink = true;

	/////////////////////////////////////////////////////////////////////////
	#region Public Interface

	/////////////////////////////////////////////////////////////////////////
	#region Serialization / Deserialization Methods
	public int GetSerializationID(GraphItem item)
	{
		return graphItems.IndexOf(item);
	}

	public GraphItem GetItemBySerializationID(int linkToItemID)
	{
		if (linkToItemID >= 0 && linkToItemID < graphItems.Count)
		{
			return graphItems[linkToItemID];
		}
		return null;
	}

	public virtual Hashtable Serialize()
	{
		if (graphItems == null)
		{
			return null;
		}

		Hashtable data = new Hashtable();
		data["autoLink"] = autoLink;

		if (!autoLink)
		{
			ArrayList graphItemStorage = new ArrayList(graphItems.Count);
			for (int i = 0; i < graphItems.Count; ++i)
			{
				graphItemStorage.Add(graphItems[i].Serialize(this));
			}
			data["graphItems"] = graphItemStorage;
		}

		return data;
	}
	
	public virtual void Deserialize(Hashtable data)
	{
		autoLink = EB.Dot.Bool("autoLink", data, true);
		if (autoLink)
		{
			AutoGenerateLinks();
		}
		else
		{
			ArrayList graphItemStorage = EB.Dot.Array("graphItems", data, new ArrayList());
			
			// Populate list with references first so that we can relink by ID.
			graphItems = new List<GraphItem>();
			for (int i = 0; i < graphItemStorage.Count; ++i)
			{
				graphItems.Add(new GraphItem());
			}

			for (int i = 0; i < graphItemStorage.Count; ++i)
			{
				graphItems[i].Deserialize(this, graphItemStorage[i] as Hashtable);
			}
			
			// Remove items which could not be deserialized.
			for (int i = 0; i < graphItems.Count; ++i)
			{
				if (graphItems[i].uiObject == null || graphItems[i].uiObject.gameObject == null)
				{
					graphItems.RemoveAt(i);
					--i;
				}
			}
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	public virtual void Initialize()
	{
		if (serializedData.Length > 0)
		{
			Deserialize(EB.JSON.Parse(serializedData) as Hashtable);
		}
		else
		{
			AutoGenerateLinks();
		}
	}

	public void AutoGenerateLinks()
	{
		autoLink = true;
		graphItems = GetGraphItems(gameObject);
		// Generate links.
		foreach (Link ld in EB.Util.GetEnumValues<Link>())
		{
			foreach (GraphItem item in graphItems)
			{
				List<GraphItem> itemsInDirection = GetItemsInDirection(item, ld);
				GraphItem closestItem = GetClosestItem(item.uiPosition, itemsInDirection);
				if (closestItem != null)
				{
					item.links[ld] = closestItem;
				}
			}
		}
	}

	public GraphItem GetLink(GraphItem item, Link direction)
	{
		if (item != null &&
		    item.links != null &&
		    item.links.ContainsKey(direction))
		{
			return item.links[direction];
		}
		return null;
	}
	
	public void AssignLink(GraphItem start, Link direction, GraphItem target)
	{
		if (start.links == null)
		{
			start.links = new Dictionary<Link, GraphItem>();
		}
		start.links[direction] = target;
		autoLink = false;
	}

	/// GetUiPosition //////////////////////////////////////////////////////
	/// Gets the position of the object in UI screen space. This is 
	/// represented as a 2D vector with results between 0 and 1 for both x 
	/// and y dimensions.
	/////////////////////////////////////////////////////////////////////////
	public Vector2 GetUiPosition(GameObject go)
	{
		if (uiCamera == null)
		{
			EB.Debug.LogError("No UI Camera could be found.");
			return Vector2.zero;
		}
		if (go == null)
		{
			return Vector2.one / 2f;
		}
		
		Vector3 controlPosInWorldSpace = go.transform.TransformPoint(Vector3.zero);
		Vector3 controlPosInScreenSpace = uiCamera.WorldToScreenPoint(controlPosInWorldSpace);
		
		float fWidth = uiCamera.pixelWidth;
		float fHeight = uiCamera.pixelHeight;
		
		return new Vector2(controlPosInScreenSpace.x / fWidth, controlPosInScreenSpace.y / fHeight);
	}

	public override string ToString()
	{
		string result = string.Format("UIGraph ({0} items):\n", graphItems.Count);

		foreach (var item in graphItems)
		{
			result += item.ToString() + "\n";
		}

		return result.Trim();
	}

	/// GetGraphItems ///////////////////////////////////////////////////////
	/// Gets the elements underneath the specified object in the unity 
	/// hierachy which meet the criteria to be a node on our graph.
	/// 
	/// Override this method to make a graph of different monobehaviours.
	/////////////////////////////////////////////////////////////////////////
	public virtual List<GraphItem> GetGraphItems(GameObject parentObj)
	{
		List<GraphItem> results = new List<GraphItem>();
		
		Component[] all = EB.Util.FindAllComponents(parentObj, typeof(ControllerInputHandler));
		
		foreach (Component comp in all)
		{
			GameObject go = comp.gameObject;
			if (go != gameObject && go.activeSelf && IsOnScreen(go))
			{
				GraphItem graphItem = new GraphItem();
				graphItem.uiObject = new UIObject(go);
				graphItem.uiPosition = GetUiPosition(go);
				results.Add(graphItem);
			}
		}
		
		return results;
	}

	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region Protected Implementation

	/// IsOnScreen //////////////////////////////////////////////////////////
	/// Defines if a given gameObject is considered onscreen.
	/////////////////////////////////////////////////////////////////////////
	protected virtual bool IsOnScreen(GameObject go)
	{
		Vector2 uiPos = GetUiPosition(go);

		return uiPos.x >= 0f && uiPos.x <= 1f && uiPos.y >= 0f && uiPos.y <= 1f;
	}

	/// IsInDirection ///////////////////////////////////////////////////////
	/// Split directions into quadrants. The angle is tunable. Note that the
	/// angle is somewhat warped by the fact that screen space is mapped to a
	/// (0->1,0->1) square, while UI space is really a rectangle with a 4/3, 
	/// 16/9 or 16/10 ratio. This means that in practice, this algorithm 
	/// will favour up/down links over left/right links for items positioned 
	/// diagionally from each other.
	/////////////////////////////////////////////////////////////////////////
	protected bool IsInDirection(GraphItem item, Link link, GraphItem compareItem)
	{
		// Must be within this many degrees of the direction specified to be validated.
		const float allowedAngle = 45f;
		
		Vector2 delta = compareItem.uiPosition - item.uiPosition;
		float angle = Mathf.Atan(delta.y / delta.x) * Mathf.Rad2Deg;
		float absAngle = Mathf.Abs(angle);
		
		switch (link)
		{
		case Link.Left:
			return (delta.x < 0f && absAngle < allowedAngle);
		case Link.Right:
			return (delta.x > 0f && absAngle < allowedAngle);
		case Link.Up:
			return (delta.y > 0f && (90f - absAngle) < allowedAngle);
		case Link.Down:
			return (delta.y < 0f && (90f - absAngle) < allowedAngle);
		default:
			EB.Debug.LogError("IsInDirection > Unhandled Link: '{0}'", link.ToString());
			break;
		}
		
		return false;
	}

	/// GetClosestItem //////////////////////////////////////////////////////
	/// Gets the list item which is closest to the UI position specified, as
	/// defined by the GetUiPosition() method.
	/////////////////////////////////////////////////////////////////////////
	protected GraphItem GetClosestItem(Vector2 curPos, List<GraphItem> items)
	{
		GraphItem closestItem = null;
		float closestDistance = float.MaxValue;
		
		foreach (GraphItem item in items)
		{
			float curDistance = (item.uiPosition - curPos).sqrMagnitude;
			if (curDistance < closestDistance)
			{
				closestDistance = curDistance;
				closestItem = item;
			}
		}
		
		return closestItem;
	}

	protected List<GraphItem> GetItemsInDirection(GraphItem item, Link direction)
	{
		List<GraphItem> itemsInDirection = new List<GraphItem>();
		
		// Find items that are in the requested direction:
		foreach (GraphItem compareItem in graphItems)
		{
			if (item != compareItem)
			{
				if (IsInDirection(item, direction, compareItem))
				{
					itemsInDirection.Add(compareItem);
				}
			}
		}
		
		return itemsInDirection;
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////
}