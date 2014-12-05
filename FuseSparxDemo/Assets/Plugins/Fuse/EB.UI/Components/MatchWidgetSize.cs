using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MatchWidgetSize : MonoBehaviour
{
	/////////////////////////////////////////////////////////////////////////
	#region Public Enumerations
	/////////////////////////////////////////////////////////////////////////
	public enum MatchDirection
	{
		Horizontal,
		Vertical,
		All
	}
	
	public enum WhatToResize
	{
		UIWidget,
		Transform,
		BoxCollider
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Variables
	/////////////////////////////////////////////////////////////////////////
	public UIWidget widgetToMatch;
	public EBGWidgetContainer containerToMatch;
	public bool resizeInWorldSpace = false;
	
	public MatchDirection matchDirection = MatchDirection.All;
	public WhatToResize whatToResize = WhatToResize.UIWidget;
	public int offset = 0;
	public int minSize = 0;
	public int maxSize = 0;
	public int tileSize = 0;
	public bool labelUsesWidgetCorners = false;
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Variables
	/////////////////////////////////////////////////////////////////////////
	private UILabel uiLabelToMatch;
	private UIWidget resizeWidget;
	private BoxCollider resizeCollider;
	private Vector3 lastMatchedSize = Vector3.zero;
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	public void Resize()
	{
		if (resizeInWorldSpace)
		{
			ResizeWorldSpace();
		}
		else
		{
			ResizeLocalSpace();
		}
	}

	public void ResizeWorldSpace()
	{
		#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			if (whatToResize == WhatToResize.UIWidget)
			{
				resizeWidget = GetComponent<UIWidget>();
			}
			else if (whatToResize == WhatToResize.BoxCollider)
			{
				resizeCollider = GetComponent<BoxCollider>();
			}
			if (widgetToMatch != null)
			{
				uiLabelToMatch = widgetToMatch as UILabel;
			}
		}
		#endif

		int width = 0;
		int height = 0;
		if (widgetToMatch != null)
		{
			if (uiLabelToMatch != null && !labelUsesWidgetCorners)
			{
				width = Mathf.RoundToInt(uiLabelToMatch.printedSize.x);
				height = Mathf.RoundToInt(uiLabelToMatch.printedSize.y);
			}
			else
			{
				width = widgetToMatch.width;
				height = widgetToMatch.height;
			}
		}
		if (containerToMatch != null)
		{
			width = containerToMatch.width;
			height = containerToMatch.height;
		}

		Transform observedTransform = null;
		if (uiLabelToMatch != null)
		{
			observedTransform = uiLabelToMatch.cachedTransform;
		}
		else if (widgetToMatch != null)
		{
			observedTransform = widgetToMatch.cachedTransform;
		}
		else if (containerToMatch != null)
		{
			observedTransform = containerToMatch.transform;
		}
		else
		{
			Debug.LogError("Missing impl");
		}
		
		Transform resizeTransform = null;
		if (whatToResize == WhatToResize.UIWidget)
		{
			resizeTransform = resizeWidget.cachedTransform;
		}
		else if (whatToResize == WhatToResize.BoxCollider)
		{
			resizeTransform = resizeCollider.transform;
		}
		else
		{
			Debug.LogError("Missing impl");
		}

		Vector3 zero = observedTransform.TransformPoint(Vector3.zero);
		Vector3 max = observedTransform.TransformPoint(new Vector3(width, height, 0f));

		Vector3 zeroInLocal = resizeTransform.InverseTransformPoint(zero);
		Vector3 maxInLocal = resizeTransform.InverseTransformPoint(max);

		int widthInLocal = Mathf.RoundToInt(maxInLocal.x - zeroInLocal.x);
		int heightInLocal = Mathf.RoundToInt(maxInLocal.y - zeroInLocal.y);

		Resize(widthInLocal, heightInLocal);
	}

	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	public void ResizeLocalSpace()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			if (whatToResize == WhatToResize.UIWidget)
			{
				resizeWidget = GetComponent<UIWidget>();
			}
			else if (whatToResize == WhatToResize.BoxCollider)
			{
				resizeCollider = GetComponent<BoxCollider>();
			}
			if (widgetToMatch != null)
			{
				uiLabelToMatch = widgetToMatch as UILabel;
			}
		}
#endif
		int width = 0;
		int height = 0;
		if (widgetToMatch != null)
		{
			if (uiLabelToMatch != null && !labelUsesWidgetCorners)
			{
				width = Mathf.RoundToInt(uiLabelToMatch.printedSize.x);
				height = Mathf.RoundToInt(uiLabelToMatch.printedSize.y);
			}
			else
			{
				width = widgetToMatch.width;
				height = widgetToMatch.height;
			}
		}
		if (containerToMatch != null)
		{
			width = containerToMatch.width;
			height = containerToMatch.height;
		}

		Resize(width, height);
	}

	public void Resize(int width, int height)
	{
		
		if (matchDirection == MatchDirection.Vertical || matchDirection == MatchDirection.All)
		{
			int targetValue = height;
			if (minSize > 0)
			{
				targetValue = Mathf.Max(minSize, targetValue);
			}
			if (maxSize > 0)
			{
				targetValue = Mathf.Min(maxSize, targetValue);
			}
			if (tileSize > 1)
			{
				targetValue = targetValue + (tileSize - (targetValue % tileSize));
			}
			targetValue += offset;
			int size = 0;
			if (whatToResize == WhatToResize.UIWidget)
			{
				size = resizeWidget.height;
			}
			else if (whatToResize == WhatToResize.BoxCollider)
			{
				size = Mathf.RoundToInt(resizeCollider.bounds.size.y);
			}
			else
			{
				size = Mathf.RoundToInt(transform.localScale.y);
			}
			if (size != targetValue)
			{
				if (whatToResize == WhatToResize.UIWidget)
				{
					resizeWidget.height = targetValue;
				}
				else if (whatToResize == WhatToResize.BoxCollider)
				{
					Vector3 scale = resizeCollider.size;
					scale.y = targetValue;
					resizeCollider.size = scale;
				}
				else
				{
					Vector3 scale = transform.localScale;
					scale.y = targetValue;
					transform.localScale = scale;
				}
			}
		}
		
		if (matchDirection == MatchDirection.Horizontal || matchDirection == MatchDirection.All)
		{
			int targetValue = width;
			if (minSize > 0)
			{
				targetValue = Mathf.Max(minSize, targetValue);
			}
			if (maxSize > 0)
			{
				targetValue = Mathf.Min(maxSize, targetValue);
			}
			if (tileSize > 1)
			{
				targetValue = targetValue + (tileSize - (targetValue % tileSize));
			}
			targetValue += offset;
			int size = 0;
			if (whatToResize == WhatToResize.UIWidget)
			{
				size = resizeWidget.width;
			}
			else if (whatToResize == WhatToResize.BoxCollider)
			{
				size = Mathf.RoundToInt(resizeCollider.bounds.size.x);
			}
			else
			{
				size = Mathf.RoundToInt(transform.localScale.x);
			}
			if (size != targetValue)
			{
				if (whatToResize == WhatToResize.UIWidget)
				{
					resizeWidget.width = targetValue;
				}
				else if (whatToResize == WhatToResize.BoxCollider)
				{
					Vector3 scale = resizeCollider.size;
					scale.x = targetValue;
					resizeCollider.size = scale;
				}
				else
				{
					Vector3 scale = transform.localScale;
					scale.x = targetValue;
					transform.localScale = scale;
				}
			}
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Monobehaviour Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Awake()
	{
		if (whatToResize == WhatToResize.UIWidget)
		{
			resizeWidget = GetComponent<UIWidget>();
			uiLabelToMatch = widgetToMatch as UILabel;
		}
		else if (whatToResize == WhatToResize.BoxCollider)
		{
			resizeCollider = GetComponent<BoxCollider>();
		}

		Resize();
	}

	private void OnEnable()
	{
		if (widgetToMatch != null)
		{
			widgetToMatch.onChange += OnMatchedWidgetChange;
		}
		if (containerToMatch != null)
		{
			containerToMatch.onChange += OnMatchedContainerChange;
		}
	}

	private void OnDisable()
	{
		if (widgetToMatch != null)
		{
			widgetToMatch.onChange -= OnMatchedWidgetChange;
		}
		if (containerToMatch != null)
		{
			containerToMatch.onChange -= OnMatchedContainerChange;
		}
	}

	private void OnMatchedWidgetChange()
	{
		Resize();
	}

	private void OnMatchedContainerChange()
	{
		Vector3 size = containerToMatch.bounds.size;
		if (size != lastMatchedSize)
		{
			lastMatchedSize = size;
			Resize();
		}
	}

#if UNITY_EDITOR
	// Update after changes immediately when Editing.
	private void Update()
	{
		if (!Application.isPlaying)
		{
			Resize();
		}
	}
#endif
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
