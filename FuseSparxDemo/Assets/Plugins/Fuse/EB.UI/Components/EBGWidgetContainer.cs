// #define DEBUG_EBG_WIDGET_CONTAINER_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class EBGWidgetContainer : UIWidgetContainer
{
	///////////////////////////////////////////////////////////////////////////
	#region Recompile Refresh
	///////////////////////////////////////////////////////////////////////////
	#if UNITY_EDITOR
	private class RecompileChecker
	{
		public static bool CheckRecompile()
		{
			bool result = recompiled;
			if (result)
			{
				recompiled = false;
			}
			return result;
		}
		
		private static bool recompiled = false;
		static RecompileChecker()
		{
			recompiled = true;
		}
	}
	
	private void CheckForRecompile()
	{
		if (RecompileChecker.CheckRecompile())
		{
			// Only one instance will get this check, so pass it along to all the others.
			EBGWidgetContainer[] all = GameObject.FindObjectsOfType(typeof(EBGWidgetContainer)) as EBGWidgetContainer[];
			foreach (EBGWidgetContainer instance in all)
			{
				instance._isActivated = false;
				if (instance.enabled)
				{
					instance.RebuildWidgetCache();
					instance.ActivateComponent();
				}
			}
		}
	}
	#endif
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////
	#region Internal Data Structures
	///////////////////////////////////////////////////////////////////////////
	private class CachedContainer
	{
		public EBGWidgetContainer container;
		public OnDimensionsChanged onWidgetContainerChanged;
		public Bounds bounds;
		public float timestamp;
	}
	
	private class CachedWidget
	{
		public UIWidget widget;
		public UIWidget.OnDimensionsChanged onWidgetChanged;
		public Bounds bounds;
		public float timestamp;
	}
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR
	[System.NonSerialized]
	public string calculationDetails;
#endif

	public bool labelsUseWidgetCorners = false;

	public delegate void OnDimensionsChanged();
	public OnDimensionsChanged onChange;

	public int width
	{
		get
		{
			if (boundsDirty)
			{
				CalculateSize();
			}
			return _width;
		}
	}

	public int height
	{
		get
		{
			if (boundsDirty)
			{
				CalculateSize();
			}
			return _height;
		}
	}
	
	// Bounds are stored in world coordinates.
	public Bounds bounds
	{
		get
		{
			if (boundsDirty)
			{
				CalculateSize();
			}
			return _bounds;
		}
	}
	
	public bool hasAnyBounds
	{
		get
		{
			return _hasAnyBounds;
		}
		private set
		{
			_hasAnyBounds = value;
		}
	}
	
	public bool boundsDirty
	{
		get
		{
			if (_subContainers != null)
			{
				foreach (EBGWidgetContainer container in _subContainers)
				{
					if (container == null) continue;
					if (container.boundsDirty) return true;
				}
			}
			return _boundsDirty || _subContainerHadNoBounds;
		}
		set
		{
			_boundsDirty = value;
		}
	}
	
	public int widgetCount
	{
		get
		{
			return _trackedWidgets.Count;
		}
	}
	
	public int subContainerCount
	{
		get
		{
			return _subContainers.Count;
		}
	}
	
	void Awake()
	{
		RebuildWidgetCache();
	}

	public void RebuildWidgetCache()
	{
		bool toggleActivation = false;
		if (_isActivated)
		{
			toggleActivation = true;
			// Toggle activation to rebuild caches.
			DeactivateComponent();
		}

		_rebuildCacheRequired = false;
		_trackedWidgets = new List<UIWidget>();
		_subContainers = new List<EBGWidgetContainer>();
		SearchInChildren(gameObject);

		if (toggleActivation)
		{
			ActivateComponent();
		}
	}

	public bool IsEmpty()
	{
		return (_trackedWidgets != null && _trackedWidgets.Count == 0) &&
			(_subContainers != null && _subContainers.Count == 0);
	}

	public List<UIWidget> GetCachedWidgets()
	{
		List<UIWidget> widgets = new List<UIWidget>();
		
		foreach (var kvp in _cachedWidgets)
		{
			widgets.Add(kvp.Key);
		}
		
		return widgets;
	}
	
	public List<EBGWidgetContainer> GetCachedSubcontainers()
	{
		List<EBGWidgetContainer> containers = new List<EBGWidgetContainer>();
		
		foreach (var kvp in _cachedContainers)
		{
			containers.Add(kvp.Key);
		}
		
		return containers;
	}
	
	void Update()
	{
#if UNITY_EDITOR
		CheckForRecompile();
		if (!Application.isPlaying)
		{
			_rebuildCacheRequired = true;
		}
#endif
		if (_rebuildCacheRequired)
		{
			DeactivateComponent();
			_cachedWidgets = new Dictionary<UIWidget, CachedWidget>();
			_cachedContainers = new Dictionary<EBGWidgetContainer, CachedContainer>();
			RebuildWidgetCache();
			_rebuildCacheRequired = false;
		}
		if (!_isActivated)
		{
			ActivateComponent();
		}
		if (transform.hasChanged)
		{
			boundsDirty = true;
			ForceUpdateWidgetBounds();
			transform.hasChanged = false;
		}
		if (boundsDirty)
		{
			CalculateSize();
		}
	}

	// Recursive rebuild of all data. This is going to be an expensive call!
	// This is to be used in those situations where you require that all bounds data be updated immediately.
	public void ForceFullUpdate()
	{
		if (_subContainers != null)
		{
			foreach (EBGWidgetContainer container in _subContainers)
			{
				container.ForceFullUpdate();
			}
		}
		ForceUpdateWidgetBounds();
		CalculateSize();
	}

	public void CalculateSize()
	{
#if UNITY_EDITOR
		calculationDetails = string.Format("Last CalculateSize at frame {0}.\n", Time.frameCount);
#endif
		_width = 0;
		_height = 0;
		Bounds lastBounds = _bounds;
		hasAnyBounds = false;
		_subContainerHadNoBounds = false;
		if (_trackedWidgets == null)
		{
			RebuildWidgetCache();
		}
		if (_trackedWidgets.Count > 0 || _subContainers.Count > 0)
		{
			foreach (EBGWidgetContainer container in _subContainers)
			{
				// Ignore inactive references.
				if (container == null)
				{
					continue;
				}
				// Bypass disabled containers.
				if (!container.enabled || !container.gameObject.activeInHierarchy)
				{
#if UNITY_EDITOR
					string reason = "???";
					if (container == null) reason = "null";
					if (!container.enabled) reason = "not enabled";
					if (!container.gameObject.activeInHierarchy) reason = "inactive";
					calculationDetails += string.Format("<EBGWC>{0} -> disabled: {1}\n", container.gameObject.name, reason);
#endif
					continue;
				}

				if (!container.hasAnyBounds && !container.IsEmpty())
				{
					_subContainerHadNoBounds = true;
				}

				if (_cachedContainers.ContainsKey(container))
				{
					Bounds curBounds = _cachedContainers[container].bounds;
					EncapsulateBounds(curBounds);
#if UNITY_EDITOR
					// For debug info in the editor only.
					Vector3 containerLocalMin = transform.worldToLocalMatrix.MultiplyPoint3x4(curBounds.min);
					Vector3 containerLocalMax = transform.worldToLocalMatrix.MultiplyPoint3x4(curBounds.max);
					calculationDetails += string.Format("<EBGWC>{0} -> x:({1:0.0},{2:0.0}) y:({3:0.0},{4:0.0})\n", container.gameObject.name, containerLocalMin.x, containerLocalMax.x, containerLocalMin.y, containerLocalMax.y);
#endif
				}
			}
			
			// Calculate the local bounds
			for (int i = 0; i < _trackedWidgets.Count; ++i)
			{
				UIWidget curWidget = _trackedWidgets[i];
				if (IsActive(curWidget))
				{
					if (_cachedWidgets.ContainsKey(curWidget))
					{
						Bounds curBounds = _cachedWidgets[curWidget].bounds;
						EncapsulateBounds(curBounds);
#if UNITY_EDITOR
						// For debug info in the editor only.
						Vector3 widgetLocalMin = transform.worldToLocalMatrix.MultiplyPoint3x4(curBounds.min);
						Vector3 widgetLocalMax = transform.worldToLocalMatrix.MultiplyPoint3x4(curBounds.max);
						calculationDetails += string.Format("{0} -> x:({1:0.0},{2:0.0}) y:({3:0.0},{4:0.0})\n", curWidget.gameObject.name, widgetLocalMin.x, widgetLocalMax.x, widgetLocalMin.y, widgetLocalMax.y);
#endif
					}
				}
#if UNITY_EDITOR
				else
				{
					string reason = "???";
					if (curWidget == null) reason = "null";
					else if (!curWidget.enabled) reason = "not enabled";
					else if (!curWidget.isVisible) reason = "not visible";
					else if (!curWidget.gameObject.activeInHierarchy) reason = "gameObject inactive";
					if (curWidget != null)
					{
						calculationDetails += string.Format("{0} -> disabled: {1}\n", curWidget.gameObject.name, reason);
					}
				}
#endif
			}
			
			// Finally convert world coords to local coords:
			Vector3 localMin = transform.worldToLocalMatrix.MultiplyPoint3x4(_bounds.min);
			Vector3 localMax = transform.worldToLocalMatrix.MultiplyPoint3x4(_bounds.max);
			int newWidth = Mathf.RoundToInt(Mathf.Abs(localMax.x - localMin.x));
			if (_width != newWidth)
			{
				_width = newWidth;
			}
			int newHeight = Mathf.RoundToInt(Mathf.Abs(localMax.y - localMin.y));
			if (_height != newHeight)
			{
				_height = newHeight;
			}
			boundsDirty = false;
			if (!ScreenBoundsEqual(lastBounds, _bounds))
			{
#if DEBUG_EBG_WIDGET_CONTAINER_CLASS
				Debug.Log ("EBGWC: On change for '" + name + "'", gameObject);
#endif
				if (onChange != null)
				{
					onChange();
				}
			}
#if UNITY_EDITOR
			// For debug info in the editor only.
			calculationDetails += string.Format("{0} -> x:({1:0.0},{2:0.0}) y:({3:0.0},{4:0.0})\n", "BOUNDS", localMin.x, localMax.x, localMin.y, localMax.y);
			calculationDetails += string.Format("width: {0}\n", _width);
			calculationDetails += string.Format("height: {0}\n", _height);
#endif
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		// Draw the gizmo
		Gizmos.matrix = Matrix4x4.identity;
		if (boundsDirty)
		{
			CalculateSize();
		}

		if (Application.isPlaying)
		{
			foreach (var kvp in _cachedWidgets)
			{
				float timeSinceUpdate = Time.realtimeSinceStartup - kvp.Value.timestamp;
				float alpha = Mathf.Clamp(1f - timeSinceUpdate, 0f, 1f);
				if (alpha > 0f)
				{
					Gizmos.color = new Color(1.0f, 0f, 0f, alpha);
					Gizmos.DrawWireCube(kvp.Value.bounds.center, kvp.Value.bounds.size);
				}
			}
		}

		Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
		Gizmos.DrawWireCube(_bounds.center, _bounds.size);
	}
#endif

	private bool IsActive (UIWidget curWidget)
	{
		return curWidget != null && curWidget.enabled && curWidget.isVisible && curWidget.gameObject.activeInHierarchy;
	}

	// Custom search function which stops when it reaches a nested EBGWidgetContainer.
	private void SearchInChildren(GameObject obj)
	{
		if ( obj != null )
		{
			bool searchDeeper = true;
			SharedComponentInstance sharedComponentInstance = obj.GetComponent<SharedComponentInstance>();
			if (sharedComponentInstance != null && !sharedComponentInstance.isInstanceReady)
			{
				_rebuildCacheRequired = true;
			}

			EBGWidgetContainer container = obj.GetComponent<EBGWidgetContainer>();
			if (container != null && container != this)
			{
				_subContainers.Add(container);
				searchDeeper = false;
			}
			else
			{
				UIWidget[] components = obj.GetComponents<UIWidget>();
				if (components != null)
				{
					_trackedWidgets.AddRange(components);
				}
			}
			
			if (searchDeeper)
			{
				foreach (Transform child in obj.transform)
				{
					SearchInChildren(child.gameObject);
				}
			}
		}
	}

	private void OnEnable()
	{
		ActivateComponent();
	}
	
	private void OnDisable()
	{
		DeactivateComponent();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (GetComponent<UIWidget>() != null)
		{
			Debug.LogError("There should not be a UIWidget on the same gameObject as an EBGWidgetContainer. Nest widgets under the container instead.");
		}
	}
#endif

	private void ActivateComponent()
	{
		if (!_isActivated && _trackedWidgets != null && _subContainers != null)
		{
			ForceUpdateWidgetBounds();
			
			foreach (EBGWidgetContainer subContainer in _subContainers)
			{
				if (subContainer == null) continue;
				if (subContainer == this) continue;
				
				if (!_cachedContainers.ContainsKey(subContainer))
				{
					CachedContainer cc = new CachedContainer();
					cc.container = subContainer;
					cc.onWidgetContainerChanged = delegate() {
						OnChildChanged(cc.container);
					};
					cc.container.onChange += cc.onWidgetContainerChanged;
					cc.bounds = subContainer.bounds;
					cc.timestamp = Time.realtimeSinceStartup;
					_cachedContainers.Add(subContainer, cc);
				}
				else
				{
					CachedContainer cc = _cachedContainers[subContainer];
					cc.container.onChange += cc.onWidgetContainerChanged;
				}
			}
			_isActivated = true;
		}
	}
	
	private void DeactivateComponent()
	{
		foreach (var kvp in _cachedWidgets)
		{
			CachedWidget cw = kvp.Value;
			cw.widget.onChange -= cw.onWidgetChanged;
		}
		foreach (var kvp in _cachedContainers)
		{
			CachedContainer cc = kvp.Value;
			cc.container.onChange -= cc.onWidgetContainerChanged;
		}
		_isActivated = false;
	}
	
	private void ForceUpdateWidgetBounds()
	{
		if (_trackedWidgets != null)
		{
			foreach (UIWidget widget in _trackedWidgets)
			{
				if (widget == null) continue;
				if (!_cachedWidgets.ContainsKey(widget))
				{
					CachedWidget cw = new CachedWidget();
					cw.widget = widget;
					cw.timestamp = Time.realtimeSinceStartup;
					cw.onWidgetChanged = delegate() {
						OnChildChanged(cw.widget);
					};
					cw.widget.onChange += cw.onWidgetChanged;
					cw.bounds = GetWidgetBounds(widget);
					_cachedWidgets.Add(widget, cw);
				}
				else
				{
					CachedWidget cw = _cachedWidgets[widget];
					cw.bounds = GetWidgetBounds(widget);
					cw.timestamp = Time.realtimeSinceStartup;
					cw.widget.onChange += cw.onWidgetChanged;
				}
			}
		}
	}

	private void EncapsulateBounds(Bounds encapsulateBounds)
	{
		if (!hasAnyBounds)
		{
			hasAnyBounds = true;
			_bounds = encapsulateBounds;
		}
		else
		{
			_bounds.Encapsulate(encapsulateBounds);
		}
	}

	private Bounds CornersToBounds(Vector3[] corners)
	{
		// Start with the first corner...
		Bounds cornerBounds = new Bounds(corners[0], Vector3.zero);

		// Encapsulate the rest.
		for (int i = 1; i < corners.Length; ++i)
		{
			cornerBounds.Encapsulate(corners[i]);
		}

		return cornerBounds;
	}

	private Bounds GetWidgetBounds(UIWidget w)
	{
		if (w is UILabel && !labelsUseWidgetCorners)
		{
			UILabel l = w as UILabel;
			return CornersToBounds(EB.UIUtils.GetLabelWorldCorners(l));
		}
		else
		{
			return CornersToBounds(w.worldCorners);
		}
	}

	private void OnChildChanged(UIWidget w)
	{
#if DEBUG_EBG_WIDGET_CONTAINER_CLASS
		Debug.Log ("A child widget of '" + gameObject.name + "' called '" + w.name + "' changed ...");
#endif
		if (_cachedWidgets.ContainsKey(w))
		{
			_cachedWidgets[w].bounds = GetWidgetBounds(w);
			_cachedWidgets[w].timestamp = Time.realtimeSinceStartup;
			boundsDirty = true;
		}
		else
		{
			_rebuildCacheRequired = true;
		}
	}
	
	private void OnChildChanged(EBGWidgetContainer c)
	{
		if (c == this)
		{
			return;
		}
#if DEBUG_EBG_WIDGET_CONTAINER_CLASS
		Debug.Log ("A child container of '" + gameObject.name + "' called '" + c.name + "' changed ...");
#endif
		if (_cachedContainers.ContainsKey(c))
		{
			_cachedContainers[c].bounds = c.bounds;
			_cachedContainers[c].timestamp = Time.realtimeSinceStartup;
			boundsDirty = true;
		}
		else
		{
			_rebuildCacheRequired = true;
		}
	}

	// Should this use an epsilon based on a screen pixel in world space?
	private bool ScreenBoundsEqual(Bounds lastBounds, Bounds curBounds)
	{
		return lastBounds == curBounds;
	}
	
	private List<UIWidget> _trackedWidgets;
	private List<EBGWidgetContainer> _subContainers;
	private Dictionary<EBGWidgetContainer, CachedContainer> _cachedContainers = new Dictionary<EBGWidgetContainer, CachedContainer>();
	private Dictionary<UIWidget, CachedWidget> _cachedWidgets = new Dictionary<UIWidget, CachedWidget>();
	private int _width = -1;
	private int _height = -1;
	private bool _hasAnyBounds = false;
	private Bounds _bounds;
	private bool _rebuildCacheRequired = false;
	private bool _subContainerHadNoBounds = false;
	private bool _boundsDirty = true;
	private bool _isActivated = false;
}
