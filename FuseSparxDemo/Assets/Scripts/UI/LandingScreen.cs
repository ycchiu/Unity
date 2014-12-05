using UnityEngine;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;
using WindowInitInfo = WindowManager.WindowInitInfo;


public class LandingScreen : Window
{
	[System.Serializable]
	public class ButtonItem
	{
		public string name;
		public string targetWindow;
		public WindowManager.WindowLayer layer;
	}

	public List<ButtonItem> buttonItems;
	public GameObject buttonPrefab;

	private UIGrid buttonGrid;
	private UIScrollView buttonScrollView;

	protected override void SetupWindow()
	{
		base.SetupWindow();

		// A common first step for UI code is to cache references to important gameObjects and components.
		// Finding game objects and components is an expensive operation in Unity, so it is preferred to 
		// do it once during initialization and then refer to those references.
		// 
		// Why do we do a code lookup, and not assign the objects to public GameObjects instead?
		// 1) It is generally quicker to write code, especially once you are dealing with a large number
		//    of elements.
		// 2) If items are dynamically created, such as those in a list, or shared component instances, you
		//    cannot assign them via public properties. Consistency is better.
		// 3) If someone who does not look at code, like an artist, replaces a gameObject and does not re-hook
		//    up the public property link, the game will break. It is intuitive to name a replacement 
		//    gameObject the same thing as the item it is replacing, but it is not obvious nor desirable to
		//    search through all scripts to see if they are referring to a specific object.
		//
		// The EB.Util class contains several useful functions for traversing the Unity hierachy. You should
		// use these rather than the built in functions, such as GameObject.Find() where possible, because
		// that built in one cannot find gameObjects that are inactive.
		GameObject buttonGridContainer = EB.Util.GetObjectExactMatch(gameObject, "ButtonGridContainer");
		buttonGrid = EB.Util.FindComponent<UIGrid>(buttonGridContainer);
		buttonScrollView = EB.Util.FindComponent<UIScrollView>(buttonGridContainer);
		
		EB.UIUtils.SetLabelContents( gameObject, "LabelUID", string.Format( "User ID: {0}", SparxHub.Instance.LoginManager.LocalUserId ) );
		EB.UIUtils.SetLabelContents( gameObject, "LabelGold", string.Format( "Gold: {0}", SparxHub.Instance.ResourcesManager.GetAmount("hc") ) );	
		EB.UIUtils.SetLabelContents( gameObject, "LabelLevel", string.Format( "Level: {0}", SparxHub.Instance.LevelRewardsManager.GetLevel("xp") ) );
		EB.UIUtils.SetLabelContents( gameObject, "LabelXP", string.Format( "XP: {0}", SparxHub.Instance.ResourcesManager.GetAmount("xp") ) );

		SetupButtonGrid();
	}

	private void SetupButtonGrid()
	{
		// Setting up the contents of a simple grid is usually a matter of:
		// 1) Deleting any items already in the grid. Although on this screen this is not necessary,
		//    it is show here as an example:
		DestroyGridItems();
		// 2) Creating new items and populating the grid:
		CreateGridItems();
		// 3) Telling the NGUI components to initialize themselves:
		buttonGrid.Reposition();
		buttonScrollView.ResetPosition();
	}

	protected override void OnIntroTransitionBegin()
	{
		base.OnIntroTransitionBegin();
		buttonGrid.Reposition();
		buttonScrollView.ResetPosition();

		//Ensure an update of these levels after the screen is re-shown
		EB.UIUtils.SetLabelContents( gameObject, "LabelLevel", string.Format( "Level: {0}", SparxHub.Instance.LevelRewardsManager.GetLevel("xp") ) );
		EB.UIUtils.SetLabelContents( gameObject, "LabelXP", string.Format( "XP: {0}", SparxHub.Instance.ResourcesManager.GetAmount("xp") ) );
		
		CheckLevelUps();
	}
	
	private void CheckLevelUps()
	{
		EB.Sparx.LevelUpNode node = SparxHub.Instance.LevelRewardsManager.GetNextLevelUp("xp");
		if (node != null)
		{
			ShowLevelUp(node);
			return;
		}
		
		node = SparxHub.Instance.LevelRewardsManager.GetNextLevelUp("energy");
		if (node != null)
		{
			ShowLevelUp(node);
			return;
		}
	}
	
	private void ShowLevelUp(EB.Sparx.LevelUpNode node)
	{
		if (node != null)
		{
			WindowInitInfo initInfo = new WindowInitInfo();
			initInfo.sourceName = windowInfo.name;
			// The target window supplies us with this data structure which we fill out.
			GenericPopup.InitInfo popupInitData = new GenericPopup.InitInfo();
			popupInitData.titleText = node.Category + " now Level " + node.NewLevel;
			popupInitData.bodyText += "Rewards: ";
			for (int i=0; i<node.Rewards.Count; i++)
			{
				popupInitData.bodyText += node.Rewards[i].ToString() + "    ";
			}
			popupInitData.buttons = new List<GenericPopup.ButtonInfo>();
			popupInitData.hasCloseButton = true;
			popupInitData.buttons.Add(new GenericPopup.ButtonInfo("Ok"));
			initInfo.data = popupInitData;
			// Finally, pass the initInfo and a callback into the Open method.
			WindowManager.Instance.Open(WindowLayer.Popup, "GenericPopup", initInfo, EB.SafeAction.Wrap<WindowInfo>(this, OnPopupClosed));
		}
	}
	
	private void OnPopupClosed(WindowInfo closingInfo)
	{
	}
	
	private void DestroyGridItems()
	{
		GameObject[] gridItems = EB.Util.GetObjects(buttonGrid.gameObject);
		foreach (GameObject item in gridItems)
		{
			// Don't destroy the container itself.
			if (item == buttonGrid.gameObject)
			{
				continue;
			}
			// When we destroy items from the grid, we first remove them from the grid's hierachy.
			// This is done because gameObject destruction in Unity is deferred. If we don't do this,
			// the items will remain on the grid for at least a frame, and will mess up our next call
			// to position items on the grid, as it will have no way of knowing the items have been 
			// marked for destruction.
			item.transform.parent = null;
			// Now use the GameObject.Destroy() call to flag this gameObject for cleanup by Unity.
			Destroy(item);
		}
	}

	private void CreateGridItems()
	{
		foreach (ButtonItem item in buttonItems)
		{
			GameObject createdItem = NGUITools.AddChild(buttonGrid.gameObject, buttonPrefab);
			GameObject createdInteractive = EB.Util.FindComponent<BoxCollider>(createdItem).gameObject;
			EB.UIUtils.SetLabelContents(createdItem, "Label", item.name);
			
			ButtonItem closureItem = item;
			// Create a callback delegate for each button:
			// Here we are using an anonymous delegate, and in doing so we are create a closure,
			// which means we can refer to local variables, and their values will remain the same
			// when the callback is fired. This saves us the effort of creating a data structure
			// or writing comparison code to store information required by the callback.
			//
			// Notes:
			// Only local variables declared within this block scope will remain the same despite
			// loop iterations. If we try to use item instead of closureItem, which was effectively
			// declared outside of the loop, it would refer to the same ButtonItem in every closure
			// (specifically, the last one).
			//
			// Caveats:
			// Closures can behave unexpectedly in coroutines.
			// 
			// Input Handling Troubleshooting:
			// Q) Help! My UI element is not receiving input!
			// 1) You must have a widget on the gameObject as well for NGUI to send it input. That is
			//    why button backgrounds are usually used for interaction in our code.
			// 2) Check it is on the "GUI" layer in the editor.
			// 3) Check it has a box collider on it, whose size roughly matches the widget size.
			// 4) Check at runtime that the UIEventListener has been attached to the right gameObject.
			// 5) Check that another gameObject with a box collider is not above your interactive element.
			UIEventListener.Get(createdInteractive).onClick += delegate(GameObject go) {
				EB.Debug.Log("Clicked item: " + closureItem.name);
				WindowManager.Instance.Open(closureItem.layer, closureItem.targetWindow);
			};
		}
	}
}
