using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIControlGraph : UIGraph, ControllerInputHandler
{
	public bool allowOffscreenControls = false;
	public bool allowInactiveControls = false;
	public ControllerInputHandler defaultInputHandler = null;

	private bool isEnabled = true;

	public ControllerInputHandler activeChild
	{
		get
		{
			return _activeChild;
		}
		set
		{
			// Deselect old.
			if (_activeChild != null)
			{
				_activeChild.SetFocus(false);
			}
			
			_activeChild = value;
			
			// Select new.
			if (_activeChild != null)
			{
				_activeChild.SetFocus(true);
				if (centerOnChild != null)
				{
					centerOnChild.CenterOn(_activeChild.gameObject.transform);
				}
			}
		}
	}
	private ControllerInputHandler _activeChild = null;

	private UICenterOnChild centerOnChild = null;
	
	/////////////////////////////////////////////////////////////////////////
	#region Serialization / Deserialization Methods

	public override Hashtable Serialize()
	{
		Hashtable data = base.Serialize();

		data["allowOffscreenControls"] = allowOffscreenControls;
		data["allowInactiveControls"] = allowInactiveControls;

		int defaultControlID = graphItems.FindIndex(item => item.uiObject.component == defaultInputHandler);
		if (defaultControlID >= 0)
		{
			data["defaultInputHandler"] = defaultControlID;
		}

		return data;
	}

	public override void Deserialize(Hashtable data)
	{
		base.Deserialize(data);
		allowOffscreenControls = EB.Dot.Bool("allowOffscreenControls", data, false);
		allowInactiveControls = EB.Dot.Bool("allowInactiveControls", data, false);
		int defaultControlID = EB.Dot.Integer("defaultInputHandler", data, -1);

		if (defaultControlID >= 0 && defaultControlID < graphItems.Count)
		{
			defaultInputHandler = graphItems[defaultControlID].uiObject.component as ControllerInputHandler;
		}
		else
		{
			defaultInputHandler = null;
		}
	}

	#endregion
	/////////////////////////////////////////////////////////////////////////

	public void SetFocus(bool isFocused)
	{
		if (isFocused)
		{
			AssignDefaultItem();
		}
		else
		{
			activeChild = null;
		}
	}

	public void SetActiveControl(ControllerInputHandler control)
	{
		if(graphItems == null)
			return;
		
		if (graphItems.Find(i => i.uiObject.component == control) != null)
		{
			activeChild = control;
		}
	}
	
	public ControllerInputHandler GetActiveControl()
	{
			return activeChild;
	}
	
	// Return true to indicate that input was handled.
	public bool HandleInput(FocusManager.UIInput input)
	{
		if (!isEnabled)
		{
			return false;
		}

		if (graphItems == null || graphItems.Count < 1)
		{
			Initialize();
		}

		bool handled = false;
		
		if (activeChild != null && activeChild.IsEnabled())
		{
			handled = activeChild.HandleInput(input);
		}
		else // This isn't supposed to happen, but is possible due to scripts executing in unexpected orders.
		{
			AssignDefaultItem();
			return true;
		}

		if (!handled)
		{
			bool validInput = true;
			UIGraph.Link inputDirection = UIGraph.Link.Up;
			switch (input)
			{
			case FocusManager.UIInput.Up:
				inputDirection = UIGraph.Link.Up;
				break;
			case FocusManager.UIInput.Down:
				inputDirection = UIGraph.Link.Down;
				break;
			case FocusManager.UIInput.Left:
				inputDirection = UIGraph.Link.Left;
				break;
			case FocusManager.UIInput.Right:
				inputDirection = UIGraph.Link.Right;
				break;
			default:
				validInput = false;
				break;
			}
			if (validInput)
			{
				UIGraph.GraphItem currentGraphItem = graphItems.Find(item => item.uiObject.component == activeChild);
				if (currentGraphItem == null)
				{
					// This should never happen.
					EB.Debug.LogError("currentGraphItem is null? That's not supposed to happen?");
					if (activeChild == null)
					{
						EB.Debug.Log("activeChild is null."); 
					}
					else EB.Debug.Log("activeChild:" + EB.UIUtils.GetFullName(activeChild.gameObject));

					handled = false;
				}
				else
				{
					UIGraph.GraphItem nextGraphItem = GetLink(currentGraphItem, inputDirection);
					if (nextGraphItem != null)
					{
						activeChild = nextGraphItem.uiObject.component as ControllerInputHandler;
						handled = true;
					}
				}
			}
		}
		
		return handled;
	}
	
	// Return true to indicate that this handler is enabled and able to accept input.
	public bool IsEnabled()
	{
		return isEnabled;
	}

	public void SetEnabled(bool isEnabled)
	{
		this.isEnabled = isEnabled;
	}

	/// GetGraphItems ///////////////////////////////////////////////////////
	/// Gets the elements underneath the specified object in the unity 
	/// hierachy which meet the criteria to be a node on our graph.
	/// 
	/// Override this method to make a graph of different monobehaviours.
	/////////////////////////////////////////////////////////////////////////
	public override List<GraphItem> GetGraphItems(GameObject parentObj)
	{
		List<GraphItem> results = new List<GraphItem>();

		Component[] controls = EB.Util.FindAllComponents(parentObj, typeof(ControllerInputHandler));
		
		foreach (Component c in controls)
		{
			GameObject go = c.gameObject;

			if (go == gameObject ||
			    (!allowInactiveControls && !go.activeSelf) ||
			    (!allowOffscreenControls && !IsOnScreen(go)))
			{
				continue;
			}
			// Subgraphs take care of their own children.
			UIControlGraph parentGraph = EB.Util.FindComponentUpwards<UIControlGraph>(go);
			if (parentGraph.gameObject == go)
			{
				Transform graphParent = go.transform.parent;
				if (graphParent != null)
				{
					GameObject parent = graphParent.gameObject;
					parentGraph = EB.Util.FindComponentUpwards<UIControlGraph>(parent);
				}
			}
			if (parentGraph == this)
			{
				GraphItem graphItem = new GraphItem();
				graphItem.uiObject = new UIObject(c);
				graphItem.uiPosition = GetUiPosition(go);
				results.Add(graphItem);
			}
		}
		
		return results;
	}

	private void AssignDefaultItem()
	{
		if (graphItems != null && graphItems.Count > 0)
		{
			if (defaultInputHandler != null)
			{
				activeChild = defaultInputHandler;
			}
			else
			{
				activeChild = graphItems[0].uiObject.component as ControllerInputHandler;
			}
		}
#if UNITY_EDITOR
		else
		{
			EB.Debug.Log("UIControlGraph > Cannot assign default item!");
		}
#endif
	}
	
	private void Awake()
	{
		centerOnChild = GetComponent<UICenterOnChild>();
	}
}
