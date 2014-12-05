using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WindowLayer = WindowManager.WindowLayer;

/////////////////////////////////////////////////////////////////////////////
/// TODO: 
/// X) UpdateScrollbars in UIScrollBar - set a min size?
/////////////////////////////////////////////////////////////////////////////

public class DynamicScrollViewScreen : Window
{
	// This is the data structure that represents a single entry in the scroll view.
	public class ScrollItemData
	{
		public string name;
		public string imagePath;
		public Color bgColor;
		public bool isSelected = false;

		public ScrollItemData(string name, string path, Color bgColor)
		{
			this.name = name;
			this.imagePath = path;
			this.bgColor = bgColor;
		}
	}

	// Cached references to hierachy elements
	private GameObject scrollItemPrefab;
	private DynamicScrollView dynamicScrollView;
	private GameObject infoContainer;
	private Dictionary<GameObject, SampleScrollItem> itemCache;

	// Private member variables
	private List<ScrollItemData> allItemsData;
	
	protected override void SetupWindow()
	{
		base.SetupWindow();
		itemCache = new Dictionary<GameObject, SampleScrollItem>();
		
		string scrollItemPrefabPath = WindowManager.Instance.GetLayerPath(windowInfo.layer) + "DynamicScrollViewScreen/ScrollItemMicro";
		Debug.Log("scrollItemPrefabPath:" + scrollItemPrefabPath);
		scrollItemPrefab = Resources.Load(scrollItemPrefabPath, typeof(GameObject)) as GameObject;

		GameObject dynamicViewContainer = EB.Util.GetObjectExactMatch(gameObject, "DynamicViewContainer");
		dynamicScrollView = EB.Util.FindComponent<DynamicScrollView>(dynamicViewContainer);
		dynamicScrollView.scrollItemPrefab = scrollItemPrefab;
		infoContainer = EB.Util.GetObjectExactMatch(gameObject, "InfoContainer");
		// Test data set
		allItemsData = new List<ScrollItemData>();
		for (int i = 0; i < 55; ++i)
		{
			Color c = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
			string imagePath = ((allItemsData.Count % 32) + 1).ToString();
			allItemsData.Add(new ScrollItemData("Item " + i, imagePath, c));
		}
		
		// Assign scroll view dependencies, then initialize.
		dynamicScrollView.itemData = allItemsData;
		dynamicScrollView.assignItemData = EB.SafeAction.Wrap<GameObjectItemPool.Item>(this, HandleDataAssignment);
		dynamicScrollView.Initialize();
		
		SetupDebugButtons();
	}

	private void SetupDebugButtons()
	{
		// Buttons to add / remove items to / from the display.
		GameObject addItemsButton = EB.Util.GetObjectExactMatch(gameObject, "AddItemsButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(addItemsButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			for (int i = 0; i < 10; ++i)
			{
				Color c = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
				string imagePath = ((allItemsData.Count % 32) + 1).ToString();
				allItemsData.Add(new ScrollItemData("Item " + allItemsData.Count, imagePath, c));
			}
			dynamicScrollView.RecreateScrollView();
		};
		GameObject removeItemsButton = EB.Util.GetObjectExactMatch(gameObject, "RemoveItemsButton");
		interactive = EB.Util.FindComponent<BoxCollider>(removeItemsButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			if (allItemsData.Count < 10)
			{
				allItemsData.RemoveRange(1, allItemsData.Count - 1);
			}
			else
			{
				allItemsData.RemoveRange(allItemsData.Count - 10, 10);
			}
			dynamicScrollView.RecreateScrollView();
		};
		
		// Delete button
		GameObject deleteButton = EB.Util.GetObjectExactMatch(gameObject, "DeleteButton");
		interactive = EB.Util.FindComponent<BoxCollider>(deleteButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			List<ScrollItemData> selectedList = new List<ScrollItemData>();
			foreach (ScrollItemData sid in allItemsData)
			{
				if (sid.isSelected)
				{
					selectedList.Add(sid);
				}
			}
			
			foreach (ScrollItemData removeItem in selectedList)
			{
				allItemsData.Remove(removeItem);
			}
			dynamicScrollView.RecreateScrollView(true);
		};
		
		// Close button
		GameObject closeButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		interactive = EB.Util.FindComponent<BoxCollider>(closeButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};
	}
	
	private void HandleDataAssignment(GameObjectItemPool.Item item)
	{
		SampleScrollItem scrollItem;
		if (itemCache.ContainsKey(item.gameObject))
		{
			scrollItem = itemCache[item.gameObject];
		}
		else // Fill cache
		{
			scrollItem = EB.Util.FindComponent<SampleScrollItem>(item.gameObject);
			itemCache[item.gameObject] = scrollItem;
		}
		scrollItem.SetData(item.data as ScrollItemData);
	}

	private void Update()
	{
		if (state == State.Open)
		{
			EB.UIUtils.SetLabelContents(infoContainer, "ItemCountLabel", string.Format("{0} items in list", allItemsData.Count));
			EB.UIUtils.SetLabelContents(infoContainer, "ScrollViewInfoLabel", string.Format("{0}/{1} of item pool used", dynamicScrollView.itemPool.useCount, dynamicScrollView.itemPool.resourceCount));
		}
	}
}
