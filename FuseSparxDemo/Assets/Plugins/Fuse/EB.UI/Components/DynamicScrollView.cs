using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DynamicScrollView : MonoBehaviour, ControllerInputHandler
{
	/////////////////////////////////////////////////////////////////////////
	#region Public Data Structures
	/////////////////////////////////////////////////////////////////////////
	[System.Serializable]
	public class AlignmentInfo
	{
		public int itemWidth;
		public int itemHeight;
		public int maxItemsPerLine;
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Variables
	/////////////////////////////////////////////////////////////////////////
	public AlignmentInfo scrollViewAlignmentInfo;
	public GameObject scrollItemPrefab;
	public GameObjectItemPool itemPool = new GameObjectItemPool();
	public IList itemData;
	public EB.Action<GameObjectItemPool.Item> assignItemData;
	public bool isInitialized {get; private set;}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Variables
	/////////////////////////////////////////////////////////////////////////
	private const int NumBufferLines = 1;

	// Cached component references
	private UIScrollView uiScrollView;
	private UIPanel scrollViewPanel;
	private UICenterOnInputHandler centerOnInputHandler;
	
	// Data
	private Rect scrollViewArea;
	private int itemsPerScreen;
	private int prevStartIndex = 0;
	private int prevEndIndex = 0;
	private int prevScrollVal = 0;
	private Vector2 lastScrollPoint = Vector2.zero;
	private bool isEnabled = true;
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region ControllerInputHandler
	/////////////////////////////////////////////////////////////////////////
	public void SetFocus(bool isFocused)
	{
		if (!isInitialized)
		{
			return;
		}
	
		ControllerInputHandler inputHandler = null;
		if (centerOnInputHandler.centeredObject != null)
		{
			Debug.Log("centerOnFocusableControl.centeredObject: " + centerOnInputHandler.centeredObject.name);
			inputHandler = EB.Util.FindComponent(centerOnInputHandler.centeredObject, typeof(ControllerInputHandler)) as ControllerInputHandler;
		}

		if (inputHandler != null)
		{
			inputHandler.SetFocus(isFocused);
		}
	}

	public bool HandleInput(FocusManager.UIInput input)
	{
		if (!isEnabled || !isInitialized)
		{
			return false;
		}

		ControllerInputHandler inputHandler = null;
		if (centerOnInputHandler.centeredObject != null)
		{
			inputHandler = EB.Util.FindComponent(centerOnInputHandler.centeredObject, typeof(ControllerInputHandler)) as ControllerInputHandler;
		}
		else
		{
			centerOnInputHandler.CenterOn(itemPool.GetResources()[0].transform);
		}

		if (inputHandler != null)
		{
			switch (input)
			{
			case FocusManager.UIInput.Left:
			case FocusManager.UIInput.Right:
			case FocusManager.UIInput.Up:
			case FocusManager.UIInput.Down:
				ControllerInputHandler next = GetNextInDirection(inputHandler, input);
				if (next != null)
				{
					inputHandler.SetFocus(false);
					next.SetFocus(true);
					centerOnInputHandler.CenterOn(next.gameObject.transform);
					return true;
				}
				break;
			case FocusManager.UIInput.Action:
				return inputHandler.HandleInput(input);
			}
		}

		return false;
	}

	public bool IsEnabled()
	{
		return isEnabled;
	}
	
	public void SetEnabled(bool isEnabled)
	{
		this.isEnabled = isEnabled;
	}
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	/// Assign dependencies before calling this method.
	/////////////////////////////////////////////////////////////////////////
	public void Initialize(EB.Action scrollViewReadyCallback = null)
	{
		isInitialized = false;
		Validate();
		
		uiScrollView = EB.Util.FindComponent<UIScrollView>(gameObject);
		if (uiScrollView == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing UIScrollView component.");
		}
		scrollViewPanel = EB.Util.FindComponent<UIPanel>(uiScrollView.gameObject);
		if (uiScrollView == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing UIPanel component for UIScrollView.");
		}
		centerOnInputHandler = EB.Util.FindComponent<UICenterOnInputHandler>(uiScrollView.gameObject);
		if (centerOnInputHandler == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing UICenterOnInputHandler component for UIScrollView.");
		}

		// Allow a frame to ensure the scroll view is initialized.
		CreateItemPool(EB.SafeAction.Wrap(this, delegate() {
			RecreateScrollView();
			isInitialized = true;
			if (scrollViewReadyCallback != null)
			{
				scrollViewReadyCallback.Invoke();
			}
		}));
	}

	/////////////////////////////////////////////////////////////////////////
	/// Call this method if you want to recreate items in the scroll view, 
	/// for example when adding or removing items from the data set.
	/////////////////////////////////////////////////////////////////////////
	public void RecreateScrollView(bool keepPosition = false)
	{
		// Release old items:
		itemPool.ReleaseAll();
		// Set up item pool:
		// First and last always exist:

		object data = (itemData != null && itemData.Count > 0) ? itemData[0] : null;
		if (data != null)
		{
			var item = itemPool.UseItem(0);
			item.data = data;
			item.gameObject.transform.localPosition = GetPositionForIndex(0);
			assignItemData(item);
		}
		
		int lastIndex = itemData.Count - 1;
		data = (itemData != null && lastIndex > 0) ? itemData[lastIndex] : null;
		if (data != null)
		{
			var item = itemPool.UseItem(lastIndex);
			item.data = data;
			item.gameObject.transform.localPosition = GetPositionForIndex(lastIndex);
			assignItemData(item);
		}
		
		Vector3 lastPosition = uiScrollView.transform.localPosition;
		Vector2 clipOffset = scrollViewPanel.clipOffset;
		
		uiScrollView.ResetPosition();
		CacheScrollViewDimensions();
		
		if (keepPosition)
		{
			uiScrollView.transform.localPosition = lastPosition;
			scrollViewPanel.clipOffset = clipOffset;
		}
		
		CheckScrollViewNeedsUpdate(true);
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Validate()
	{
		if (itemData == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing item data!");
		}
		if (assignItemData == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing item data assignment action!");
		}
		if (scrollViewAlignmentInfo == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing scrollViewAlignmentInfo!");
		}
		if (scrollViewAlignmentInfo.itemWidth < 1)
		{
			EB.Debug.LogError("DynamicScrollView: Missing scrollViewAlignmentInfo width.");
		}
		if (scrollViewAlignmentInfo.itemHeight < 1)
		{
			EB.Debug.LogError("DynamicScrollView: Missing scrollViewAlignmentInfo height.");
		}
		if (scrollViewAlignmentInfo.maxItemsPerLine < 1)
		{
			EB.Debug.LogError("DynamicScrollView: Missing scrollViewAlignmentInfo max items per line.");
		}
		if (scrollItemPrefab == null)
		{
			EB.Debug.LogError("DynamicScrollView: Missing scrollItemPrefab.");
		}
	}
	
	private void CreateItem()
	{
		GameObject createdItem = NGUITools.AddChild(gameObject, scrollItemPrefab);
		createdItem.name = scrollItemPrefab.name;
		itemPool.AddResource(createdItem);
	}
	
	/////////////////////////////////////////////////////////////////////////
	/// This method takes a callback to report when item creation has been 
	/// completed.
	/////////////////////////////////////////////////////////////////////////
	private void CreateItemPool(EB.Action creationCompleteCb)
	{
		// First and last always exist:
		CreateItem();
		object data = (itemData != null && itemData.Count > 0) ? itemData[0] : null;
		if (data != null)
		{
			GameObjectItemPool.Item item = itemPool.UseItem(0);
			item.data = data;
			item.gameObject.transform.localPosition = GetPositionForIndex(0);
			assignItemData(item);
		}
		
		int lastIndex = itemData.Count - 1;
		CreateItem();
		data = (itemData != null && lastIndex > 0) ? itemData[lastIndex] : null;
		if (data != null)
		{
			GameObjectItemPool.Item item = itemPool.UseItem(lastIndex);
			item.data = data;
			item.gameObject.transform.localPosition = GetPositionForIndex(lastIndex);
			assignItemData(item);
		}
		
		if (CacheScrollViewDimensions())
		{
			CreateRemainingPoolItems();
			creationCompleteCb();
		}
		else
		{
			// Well this is fun. NGUI may need a frame for the items to be created.
			// What this means is we have to delay caching the view bounds until we
			// can garuantee that everything is ready.
			EB.Coroutines.Run(WaitForScrollViewReady(EB.SafeAction.Wrap(this, delegate() {
				CreateRemainingPoolItems();
				creationCompleteCb();
			})));
		}
	}

	/////////////////////////////////////////////////////////////////////////
	/// Assumes first and last items exist. Creates pool items for the rest.
	/////////////////////////////////////////////////////////////////////////
	private void CreateRemainingPoolItems()
	{
		int poolSize = GetMaxPoolSize();
		for (int i = 2; i < poolSize; ++i)
		{
			CreateItem();
		}
	}
	
	/////////////////////////////////////////////////////////////////////////
	/// This coroutine Fires the callback given once the uiScrollView is 
	/// properly initialized.
	/////////////////////////////////////////////////////////////////////////
	private IEnumerator WaitForScrollViewReady(EB.Action readyCallback)
	{
		// Call CacheScrollViewDimensions every frame until it returns true.
		while (true)
		{
			if (CacheScrollViewDimensions())
			{
				break;
			}
			yield return null;
			// force reposition of the scroll view, which will also then force recalculation of bounds
			uiScrollView.ResetPosition();
		}
		readyCallback();
	}
	
	/////////////////////////////////////////////////////////////////////////
	/// In this method we are looking at what items are currently visible on
	/// screen. Any items which exist and are not onscreen should be 
	/// destroyed, and any which are onscreen but do not exist must be 
	/// created.
	/// 
	/// We will keep a small buffer to either side of the actual view to 
	/// 
	/// Exception: In order for the UIScrollView to maintain its boundaries,
	/// for internal calculations, the first and last elements of the list 
	/// always exist.
	/////////////////////////////////////////////////////////////////////////
	private void UpdateDisplayedScrollViewItems(int startIndex, int endIndex, bool reset = false)
	{
		if (reset)
		{
			prevStartIndex = 0;
			prevEndIndex = 0;
		}
		// Make sure that we are rounding to the nearest whole line:
		startIndex -= startIndex % scrollViewAlignmentInfo.maxItemsPerLine;
		// Subtract 1 because endIndex is inclusive.
		endIndex += scrollViewAlignmentInfo.maxItemsPerLine - (endIndex % scrollViewAlignmentInfo.maxItemsPerLine) - 1;

		// Add an extra lines of buffer space on either side. This will reduce
		// the chance of the user noticing items being reassigned.
		int lastIndex = itemData.Count - 1;
		for (int i = 0; i < NumBufferLines; ++i)
		{
			startIndex = Mathf.Max(startIndex - scrollViewAlignmentInfo.maxItemsPerLine, 0);
			endIndex = Mathf.Min(endIndex + scrollViewAlignmentInfo.maxItemsPerLine, lastIndex);
		}

		// Unassign any indices which are now out of range.
		// Remove the ones before the new sliding window range.
		int removeFrom = prevStartIndex;
		int removeTo = Mathf.Min(startIndex - 1, prevEndIndex);
		for (int i = removeFrom; i <= removeTo; ++i)
		{
			// Never unassign the first and last indices.
			if (i == 0 || i == lastIndex)
			{
				continue;
			}
			
			itemPool.ReleaseItem(i);
		}
		// Remove the ones after the new sliding window range.
		removeFrom = Mathf.Max(endIndex + 1, prevStartIndex);
		removeTo = prevEndIndex;
		for (int i = removeFrom; i <= removeTo; ++i)
		{
			// Never unassign the first and last indices.
			if (i == 0 || i == lastIndex)
			{
				continue;
			}
			
			itemPool.ReleaseItem(i);
		}
		
		// Assign new indices.
		// Add before:
		int addFrom = startIndex;
		int addTo = Mathf.Min(prevStartIndex - 1, endIndex);
		for (int i = addFrom; i <= addTo; ++i)
		{
			// Never reassign the first and last indices.
			if (i == 0 || i == lastIndex)
			{
				continue;
			}
			
			var item = itemPool.UseItem(i);
			if (item != null)
			{
				item.data = itemData[i];
				item.gameObject.transform.localPosition = GetPositionForIndex(i);
				assignItemData(item);
			}
			else
			{
				Debug.LogError("insufficient pool items!");
			}
		}
		
		// Add after:
		addFrom = Mathf.Max(prevEndIndex + 1, startIndex);
		addTo = endIndex;
		for (int i = addFrom; i <= addTo; ++i)
		{
			// Never reassign the first and last indices.
			if (i == 0 || i == lastIndex)
			{
				continue;
			}
			
			var item = itemPool.UseItem(i);
			if (item != null)
			{
				item.data = itemData[i];
				item.gameObject.transform.localPosition = GetPositionForIndex(i);
				assignItemData(item);
			}
			else
			{
				Debug.LogError("insufficient pool items!");
			}
		}
		
		// Since turning gameObjects on and off is expensive, wait until all 
		// are updated, and then refresh status as required.
		itemPool.UpdateActiveResources();
		
		prevStartIndex = startIndex;
		prevEndIndex = endIndex;
	}

	/////////////////////////////////////////////////////////////////////////
	/// Calculates the local position for a given item index.
	/////////////////////////////////////////////////////////////////////////
	private Vector3 GetPositionForIndex(int index)
	{
		Vector3 result = Vector3.zero;
		
		if (uiScrollView.movement == UIScrollView.Movement.Horizontal)
		{
			result.x = (index / scrollViewAlignmentInfo.maxItemsPerLine) * scrollViewAlignmentInfo.itemWidth;
			result.y = -((index % scrollViewAlignmentInfo.maxItemsPerLine) * scrollViewAlignmentInfo.itemHeight);
			result.y += (scrollViewAlignmentInfo.maxItemsPerLine - 1) * 0.5f * scrollViewAlignmentInfo.itemHeight;
		}
		else if (uiScrollView.movement == UIScrollView.Movement.Vertical)
		{
			result.x = (index % scrollViewAlignmentInfo.maxItemsPerLine) * scrollViewAlignmentInfo.itemWidth;
			result.x -= (scrollViewAlignmentInfo.maxItemsPerLine - 1) * 0.5f * scrollViewAlignmentInfo.itemWidth;
			result.y = -((index / scrollViewAlignmentInfo.maxItemsPerLine) * scrollViewAlignmentInfo.itemHeight);
		}
		else
		{
			EB.Debug.LogError("Movement type is not handled!");
		}
		
		return result;
	}

	/////////////////////////////////////////////////////////////////////////
	/// A lot of code in this method is taken directly from the SetDragAmount
	/// method in the UIScrollView class. This allows us to convert from 
	/// localPosition x and y values to ones that are a ratio of 0 to 1.
	/////////////////////////////////////////////////////////////////////////
	private bool CacheScrollViewDimensions()
	{
		Bounds b = uiScrollView.bounds;
		if (b.min.x == b.max.x || b.min.y == b.max.y)
		{
			itemsPerScreen = 0;
			if (itemPool.useCount == 0)
			{
				return true;
			}
			return false;
		}

		Vector4 clip = scrollViewPanel.finalClipRegion;
		clip.x = Mathf.Round(clip.x);
		clip.y = Mathf.Round(clip.y);
		clip.z = Mathf.Round(clip.z);
		clip.w = Mathf.Round(clip.w);
		
		float hx = clip.z * 0.5f;
		float hy = clip.w * 0.5f;
		float left = b.min.x + hx;
		float right = b.max.x - hx;
		float bottom = b.min.y + hy;
		float top = b.max.y - hy;
		
		if (scrollViewPanel.clipping == UIDrawCall.Clipping.SoftClip)
		{
			Vector2 softness = scrollViewPanel.clipSoftness;
			left -= softness.x;
			right += softness.x;
			bottom -= softness.y;
			top += softness.y;
		}
		
		scrollViewArea = new Rect();
		scrollViewArea.xMin = left;
		scrollViewArea.xMax = right;
		scrollViewArea.yMin = top;
		scrollViewArea.yMax = bottom;
		
		if (uiScrollView.movement == UIScrollView.Movement.Horizontal)
		{
			itemsPerScreen = Mathf.FloorToInt(clip.z / scrollViewAlignmentInfo.itemWidth);
		}
		else if (uiScrollView.movement == UIScrollView.Movement.Vertical)
		{
			itemsPerScreen = Mathf.FloorToInt(clip.w / scrollViewAlignmentInfo.itemHeight);
		}
		itemsPerScreen *= scrollViewAlignmentInfo.maxItemsPerLine;
		return true;
	}
	
	private void CheckScrollViewNeedsUpdate(bool forceUpdate = false)
	{
		Vector3 localPos = uiScrollView.transform.localPosition;
		
		Vector2 currentScrollPoint;
		currentScrollPoint.x = Mathf.InverseLerp(scrollViewArea.xMin, scrollViewArea.xMax, - localPos.x);
		currentScrollPoint.y = Mathf.InverseLerp(scrollViewArea.yMin, scrollViewArea.yMax, - localPos.y);

		int numPositions = Mathf.Max(itemData.Count - itemsPerScreen, 1);
		int unfilledCountAtEnd = numPositions % scrollViewAlignmentInfo.maxItemsPerLine;
		if (unfilledCountAtEnd > 0)
		{
			numPositions += scrollViewAlignmentInfo.maxItemsPerLine - unfilledCountAtEnd;
		}

		int currScrollVal;
		if (uiScrollView.movement == UIScrollView.Movement.Horizontal)
		{
			currScrollVal = Mathf.FloorToInt(currentScrollPoint.x * numPositions);
		}
		else
		{
			currScrollVal = Mathf.FloorToInt(currentScrollPoint.y * numPositions);
		}

		if (forceUpdate || currScrollVal != prevScrollVal)
		{
			prevScrollVal = currScrollVal;
			int startIndex = currScrollVal;
			int endIndex = startIndex + itemsPerScreen;
			
			UpdateDisplayedScrollViewItems(startIndex, endIndex, forceUpdate);
		}
	}
	
	private int GetMaxPoolSize()
	{
		// We always want first and last items.
		int poolSize = 2;
		// Add a buffer on either side, and one extra line to account for
		// itemsPerScreen being a rounded down integer.
		poolSize += scrollViewAlignmentInfo.maxItemsPerLine * ((NumBufferLines * 2) + 1);
		
		// And everything in between:
		poolSize += itemsPerScreen;

		return poolSize;
	}

	private List<GameObject> GetActivePoolItems()
	{
		List<GameObject> activeResources = new List<GameObject>();
		
		foreach (GameObject item in itemPool.GetResources())
		{
			if (item.activeSelf)
			{
				activeResources.Add(item);
			}
		}
		
		return activeResources;
	}
	
	private bool IsInDirection(Vector3 start, Vector3 end, FocusManager.UIInput input)
	{
		switch(input)
		{
		case FocusManager.UIInput.Left:
		case FocusManager.UIInput.Right:
			if (EB.Util.FloatEquals(start.y, end.y, 0.01f))
			{
				return (input == FocusManager.UIInput.Left) ? start.x > end.x : start.x < end.x;
			}
			break;
		case FocusManager.UIInput.Up:
		case FocusManager.UIInput.Down:
			if (EB.Util.FloatEquals(start.x, end.x, 0.01f))
			{
				return (input == FocusManager.UIInput.Up) ? start.y < end.y : start.y > end.y;
			}
			break;
		}
		
		return false;
	}
	
	private List<GameObject> GetActivePoolItemsInDirection(GameObject startFrom, FocusManager.UIInput input)
	{
		Vector3 startPos = startFrom.transform.localPosition;
		List<GameObject> activeResources = GetActivePoolItems();
		List<GameObject> results = new List<GameObject>();
		
		foreach (GameObject item in activeResources)
		{
			if (IsInDirection(startPos, item.transform.localPosition, input))
			{
				results.Add(item);
			}
		}
		
		return results;
	}
	
	private ControllerInputHandler GetNextInDirection(ControllerInputHandler ih, FocusManager.UIInput input)
	{
		// TODO: Gareth - I am pretty sure we could just use the indices of the pool items instead of
		// doing all this positional checking work. Should be faster and less error prone.
		Vector3 startPos = ih.gameObject.transform.localPosition;
		
		List<GameObject> possibleMatches = GetActivePoolItemsInDirection(ih.gameObject, input);
		
		float min = float.MaxValue;
		GameObject closest = null;
		
		foreach (GameObject item in possibleMatches)
		{
			float dist = float.MaxValue;
			switch(input)
			{
			case FocusManager.UIInput.Left:
			case FocusManager.UIInput.Right:
				dist = Mathf.Abs(item.transform.localPosition.x - startPos.x);
				break;
			case FocusManager.UIInput.Up:
			case FocusManager.UIInput.Down:
				dist = Mathf.Abs(item.transform.localPosition.y - startPos.y);
				break;
			}
			if (dist < min)
			{
				closest = item;
				min = dist;
			}
		}
		
		return EB.Util.FindComponent(closest, typeof(ControllerInputHandler)) as ControllerInputHandler;
	}
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Monobehaviour Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Update()
	{
		// Needs initialization before trying to update.
		if (uiScrollView == null)
		{
			return;
		}
	
		CheckScrollViewNeedsUpdate();
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
