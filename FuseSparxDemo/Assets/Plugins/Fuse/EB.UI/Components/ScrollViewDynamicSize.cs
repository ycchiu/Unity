using UnityEngine;
using System.Collections;

public class ScrollViewDynamicSize : MonoBehaviour 
{
	public enum ListEnd
	{
		HEAD,
		TAIL
	}

	public EB.Action<GameObject> CloneItemCallback;
	public EB.Action<ListEnd> ListEndReachedCallback;
	public int DragToLoadSize = 50;

	GameObject _gridItemPrefab;
	UIGrid _grid;
	UIPanel _panel;
	float _initXPos;
	float _initYPos;
	UIGrid.Arrangement direction;
	ListEnd _endReached;
	bool _springing = false;
	int _dragToLoadSqrd;

	public GameObject GridItemPrefab{ get{ return _gridItemPrefab; } set{ _gridItemPrefab = value; } }
	
	void Awake()
	{
		_initXPos = gameObject.transform.localPosition.x;
		_initYPos = gameObject.transform.localPosition.y;
		_grid = gameObject.GetComponent<UIGrid>();
		_panel = gameObject.GetComponent<UIPanel>();
		gameObject.GetComponent<UIScrollView>().onSpringbackFinished = OnSpringFinished;
		_dragToLoadSqrd = DragToLoadSize * DragToLoadSize;

		if(_grid == null)
			EB.Debug.LogError("Could not find UIGrid component in ScrollViewDynamicSize");
		if(_panel == null)
			EB.Debug.LogError("Could not find UIPanel component in ScrollViewDynamicSize");

		direction = _grid.arrangement;
	}
	
	void Update()
	{
		if(IsEndOfScrollBar())
		{
			if(ListEndReachedCallback != null)
				ListEndReachedCallback(_endReached);
			else
			{
				GameObject oldGridItem = FindFarthestGridItem();
				CloneGridItem(oldGridItem);
			}
		}
	}

	void OnSpringFinished()
	{
		_springing = false;
	}
	
	private void CloneGridItem(GameObject go, bool randColor = true)
	{
		GameObject newItem = NGUITools.AddChild(gameObject, _gridItemPrefab);

		if(_grid != null)
			_grid.Reposition();

		if(CloneItemCallback != null)
			CloneItemCallback(newItem);
	}
	
	private bool IsEndOfScrollBar()
	{
		UIScrollView scrollView = gameObject.GetComponent<UIScrollView>();
		Vector3 constraint = _panel.CalculateConstrainOffset(scrollView.bounds.min, scrollView.bounds.max);

		if(constraint.sqrMagnitude > _dragToLoadSqrd && !_springing)
		{
			_springing = true;
			if(direction == UIGrid.Arrangement.Horizontal)
			{
				//hit the far right
				if(gameObject.transform.localPosition.x < _initXPos)
					_endReached = ListEnd.TAIL;
				//hit the far left
				else if(gameObject.transform.localPosition.x > _initXPos)
					_endReached = ListEnd.HEAD;
				return true;
			}
			else
			{
				//hit the top
				if(gameObject.transform.localPosition.y < _initYPos)
					_endReached = ListEnd.HEAD;
				//hit the bottom
				else if(gameObject.transform.localPosition.y > _initYPos)
					_endReached = ListEnd.TAIL;
				return true;
			}
		}
		return false;
	}
	
	private GameObject FindFarthestGridItem()
	{
		UIDragScrollView[] gridItems = GetComponentsInChildren<UIDragScrollView>(false);
		if(gridItems.Length <= 0)
			return null;
		
		GameObject farthestItem = gridItems[0].gameObject;
		for(int i = 0; i < gridItems.Length; i++)
		{
			if(farthestItem.transform.localPosition.x < gridItems[i].gameObject.transform.localPosition.x)
				farthestItem = gridItems[i].gameObject;
		}
		return farthestItem;
	}
}
