// #define DEBUG_ALIGN_UI_ELEMENTS_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class AlignUIElements : MonoBehaviour, UIDependency
{
	public class AlignUIElmentsDefault
	{
		public bool TrackAllWidgetChanged = false;
	}
	static public AlignUIElmentsDefault Defaults = new AlignUIElmentsDefault();
	
	public enum Direction
	{
		Vertical,
		Horizontal
	}
	
	public enum Alignment
	{
		Decreasing,
		Centered,
		Increasing
	}
	
	[System.Serializable]
	public class AlignedObject
	{
		public GameObject runtimeContainer
		{
			get
			{
				if (container != null)
				{
					return container;
				}
				else if (widget != null)
				{
					return widget.gameObject;
				}
				else if (widgetHolder != null)
				{
					return widgetHolder.gameObject;
				}
				return null;
			}
			set
			{
				container = value;
			}
		}

		public MonoBehaviour target
		{
			get
			{
				if (widget != null)
				{
					return widget;
				}
				else if (widgetHolder != null)
				{
					return widgetHolder;
				}
				return null;
			}
		}

		public GameObject container;
		public UIWidget widget;
		public EBGWidgetContainer widgetHolder;
		public bool overrideOffset = false;
		public float customOffset = 0f;
		public bool labelUsesWidgetCorners = false;

		public bool IsActive()
		{
			if (runtimeContainer == null)
			{
				return false;
			}
			if (target == null)
			{
				return false;
			}
			if (!target.enabled)
			{
				return false;
			}
			if (!runtimeContainer.activeSelf)
			{
				return false;
			}
			if (!target.gameObject.activeSelf)
			{
				return false;
			}
			return true;
		}
	}
	
	public Direction alignmentDirection;
	public Alignment alignment = Alignment.Centered;
	public List<AlignedObject> alignedObjects = new List<AlignedObject>();
	public float defaultOffset = 0f;
	public bool lastElementNeverHasOffset = false;
	public bool trackWidgetsChanged = false;

	private bool widgetsChanged = false;

#if UNITY_EDITOR
	[System.NonSerialized]
	public bool alwaysShowGizmos = false;
	// Custom editor helper function to display pivot status.
	public void CheckPivots(out List<string> errors, out List <GameObject> errorTargets)
	{
		errors = new List<string>();
		errorTargets = new List<GameObject>();
		if (alignedObjects != null)
		{
			foreach (AlignedObject obj in alignedObjects)
			{
				if (obj.target == null) continue;
				if (alignmentDirection == Direction.Horizontal && obj.widget != null && obj.widget.pivot != UIWidget.Pivot.Left && obj.widget.pivot != UIWidget.Pivot.TopLeft && obj.widget.pivot != UIWidget.Pivot.BottomLeft)
				{
					errors.Add(string.Format("Alignment for '{0}' ({1}) should be left.", obj.widget.gameObject.name, obj.widget.GetType().ToString()));
					errorTargets.Add(obj.widget.gameObject);
				}
				else if (alignmentDirection == Direction.Vertical && obj.widget != null && obj.widget.pivot != UIWidget.Pivot.Top && obj.widget.pivot != UIWidget.Pivot.TopLeft && obj.widget.pivot != UIWidget.Pivot.TopRight)
				{
					errors.Add(string.Format("Alignment for '{0}' ({1}) should be top.", obj.widget.gameObject.name, obj.widget.GetType().ToString()));
					errorTargets.Add(obj.widget.gameObject);
				}
			}
		}
	}
	
	// Custom editor helper function to check widgets are inside their containers.
	public void CheckParents(out List<string> errors, out List <GameObject> errorTargets)
	{
		errors = new List<string>();
		errorTargets = new List<GameObject>();
		if (alignedObjects != null)
		{
			foreach (AlignedObject obj in alignedObjects)
			{
				// Missing widget.
				if (obj.target == null || obj.runtimeContainer == null)
				{
					continue;
				}
				// Can be its own container.
				if ((obj.widget != null && obj.runtimeContainer.transform == obj.widget.transform) ||
				    (obj.widgetHolder != null && obj.runtimeContainer.transform == obj.widgetHolder.transform))
				{
					continue;
				}
				// Otherwise, try to find the ancestor.
				if (obj.widget != null)
				{
					Transform ancestor = EB.Util.Ascend(obj.runtimeContainer.name, obj.widget.transform);
					if (ancestor == null)
					{
						errors.Add(string.Format("Widget '{0}' ({1}) should be under its container '{2}' in the hierachy.", obj.widget.gameObject.name, obj.widget.GetType().ToString(), obj.container.gameObject.name));
						errorTargets.Add(obj.widget.gameObject);
					}
				}
				else if (obj.widgetHolder != null)
				{
					Transform ancestor = EB.Util.Ascend(obj.runtimeContainer.name, obj.widgetHolder.transform);
					if (ancestor == null)
					{
						errors.Add(string.Format("EBGWidgetContainer '{0}' ({1}) should be under its container '{2}' in the hierachy.", obj.widgetHolder.gameObject.name, obj.widgetHolder.GetType().ToString(), obj.container.gameObject.name));
						errorTargets.Add(obj.widgetHolder.gameObject);
					}
				}
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (alwaysShowGizmos)
		{
			OnDrawGizmosSelected();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (alignedObjects != null)
		{
			foreach (AlignedObject obj in alignedObjects)
			{
				if (obj != null && obj.IsActive())
				{
					DrawWidgetGizmo(obj);
				}
			}
		}
	}
	
	private void DrawWidgetGizmo(AlignedObject obj)
	{
		// Draw the object itself:
		Vector3 localMin;
		Vector3 localMax;
		GetObjectBounds(obj, out localMin, out localMax);
		Vector3 pos = (localMax + localMin) / 2f;
		Vector3 size = localMax - localMin;
		Bounds b = new Bounds(pos, size);
		Gizmos.matrix = gameObject.transform.localToWorldMatrix;
		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawWireCube(b.center, b.size);

		// Draw the offset:
		float offset = obj.overrideOffset ? obj.customOffset : defaultOffset;
		if (lastElementNeverHasOffset && obj == GetLastActiveItem())
		{
			offset = 0f;
		}
		float pixelSize = 1f;
		if (offset > 0)
		{
			Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
			if (alignmentDirection == Direction.Horizontal)
			{
				pos.x +=  (size.x + offset) / 2f;
				size.x = offset - pixelSize;
			}
			else 
			{
				pos.y -= (size.y + offset) / 2f;
				size.y = offset - pixelSize;
			}
			pos.z = -1f;
			Gizmos.DrawWireCube(pos, size);
		}
	}
#endif

	/////////////////////////////////////////////////////////////////////////
	#region UIDependency Implementation
	public EB.Action onReadyCallback
	{
		get
		{
			return _onReadyCallback;
		}
		set
		{
			_onReadyCallback = value;
		}
	}
	private EB.Action _onReadyCallback;
	
	public EB.Action onDeactivateCallback
	{
		get
		{
			return _onDeactivateCallback;
		}
		set
		{
			_onDeactivateCallback = value;
		}
	}
	private EB.Action _onDeactivateCallback;
	
	public bool IsReady()
	{
		return isAligned;
	}
	private bool isAligned = false;
	
	private void OnDisable()
	{
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}
	private void OnDestroy()
	{
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	protected void Start()
	{
		if (Application.isPlaying)
		{
			List<UIDependency> dependencies = EB.UIUtils.GetUIDependencies(this);
			EB.UIUtils.WaitForUIDependencies(EB.SafeAction.Wrap(this, delegate()
			{
				EB.Coroutines.NextFrame(EB.SafeAction.Wrap(this, delegate()
				{
					Reposition();
				}));
			}), dependencies);
		}
	}

	void OnEnable()
	{
		RegisterOnChangeHandlers();
	}

	void Disable()
	{
		DeregisterOnChangeHandlers();
	}

	// In the editor this updates in real time. In game it should be repositioned manually.
	public void Update()
	{
		if (!Application.isPlaying)
		{
			Reposition();
		}
		else
		{
			if( widgetsChanged && ( trackWidgetsChanged || Defaults.TrackAllWidgetChanged ) )
			{
				widgetsChanged = false;
				Reposition();
			}
		}
	}
	
	private void GetObjectBounds(AlignedObject obj, out Vector3 localMin, out Vector3 localMax)
	{
		Bounds b = new Bounds();

		if (obj.widget != null)
		{
			Vector3[] worldCorners = null;
			UILabel label = obj.widget as UILabel;
			if (label != null && !obj.labelUsesWidgetCorners)
			{
				worldCorners = EB.UIUtils.GetLabelWorldCorners(label);
			}
			else
			{
				worldCorners = obj.widget.worldCorners;
			}
			// Start with the first corner...
			b = new Bounds(worldCorners[0], Vector3.zero);
			// Encapsulate the rest.
			for (int i = 1; i < worldCorners.Length; ++i)
			{
				Vector3 curCorner = worldCorners[i];
				b.Encapsulate(curCorner);
			}
		}
		else if (obj.widgetHolder != null)
		{
			b = obj.widgetHolder.bounds;
		}

		// Convert world coords to local coords:
		localMin = transform.worldToLocalMatrix.MultiplyPoint3x4(b.min);
		localMax = transform.worldToLocalMatrix.MultiplyPoint3x4(b.max);

		localMin.x = CustomRound(localMin.x);
		localMax.x = CustomRound(localMax.x);
		localMin.y = CustomRound(localMin.y);
		localMax.y = CustomRound(localMax.y);
	}

	// An attempt to deal with floating point errors that occur when dealing with
	// values around 0.5 for width and height, which can end up rounding to either
	// higher or lower values.
	private float CustomRound(float f)
	{
		// For values below zero:
		// floor: -5.5 => -6
		// ceil: -5.5 => -5

		// The epsilon part of the check is to prevent floating point rounding errors from
		// making our sizes off by one.
		const float epsilon = 0.1f;
		if (f < 0)
		{
			if (GetAbsoluteFractionalPart(f) > epsilon)
			{
				return Mathf.Floor(f);
			}
			else
			{
				return Mathf.Ceil(f);
			}
		}
		else
		{
			if (GetAbsoluteFractionalPart(f) > epsilon)
			{
				return Mathf.Ceil(f);
			}
			else
			{
				return Mathf.Floor(f);
			}
		}
	}

	private float GetAbsoluteFractionalPart(float f)
	{
		if (f < 0)
		{
			f = -f;
		}

		return f - Mathf.Floor(f);
	}

	private float GetPixelSize(AlignedObject obj, Direction dir)
	{
		Vector3 localMin;
		Vector3 localMax;
		GetObjectBounds(obj, out localMin, out localMax);
		return (dir == Direction.Horizontal) ? localMax.x - localMin.x : localMax.y - localMin.y;
	}
	
	public void Reposition()
	{
		Report ("Reposition()");
		if (alignedObjects != null)
		{
			AlignedObject lastActiveItem = GetLastActiveItem();
			
			float totalSize = 0f;
			// Add up sizes:
			float[] sizes = new float[alignedObjects.Count];
			int itemIndex = 0;
			foreach (AlignedObject obj in alignedObjects)
			{
				if (obj != null && obj.IsActive())
				{
					float size = Mathf.Ceil(GetPixelSize(obj, alignmentDirection));
					sizes[itemIndex] = size;
					totalSize += size;
					float offset = obj.overrideOffset ? obj.customOffset : defaultOffset;
					if (lastElementNeverHasOffset && lastActiveItem == obj)
					{
						offset = 0f;
					}
					totalSize += offset;
				}
				else
				{
					sizes[itemIndex] = 0f;
				}
				++itemIndex;
			}

			// Use a simple multiplier on the total size to figure out where to start.
			// This gives us the alignment effect.
			float multiplier = 1f;
			switch (alignment)
			{
			case Alignment.Increasing:
				multiplier = 0f;
				break;
			case Alignment.Centered:
				multiplier = 0.5f;
				break;
			case Alignment.Decreasing:
				multiplier = 1f;
				break;
			default:
				Debug.LogError("Unhandled Alignment!");
				break;
			}
			
			// x increases to the right, while y increases upwards.
			float curPos = Mathf.Round(((alignmentDirection == Direction.Horizontal) ? -totalSize : totalSize) * multiplier);
			itemIndex = 0;
			foreach (AlignedObject obj in alignedObjects)
			{
				if (obj != null && obj.IsActive())
				{
					float size = sizes[itemIndex];
					Vector3 pos = obj.runtimeContainer.transform.localPosition;
					if (alignmentDirection == Direction.Horizontal)
					{
						pos.x = curPos;
						if (obj.widgetHolder != null)
						{
							pos.x += size / 2f;
						}
					}
					else
					{
						pos.y = curPos;
						if (obj.widgetHolder != null)
						{
							pos.y -= size / 2f;
						}
					}
					// Don't adjust values unless they have changed noticably.
					const float epsilon = 0.1f;
					if ((alignmentDirection == Direction.Horizontal && !EB.Util.FloatEquals(obj.runtimeContainer.transform.localPosition.x, pos.x, epsilon)) ||
					    (alignmentDirection == Direction.Vertical && !EB.Util.FloatEquals(obj.runtimeContainer.transform.localPosition.y, pos.y, epsilon)))
					{
						obj.runtimeContainer.transform.localPosition = pos;
					}
					
					curPos += (alignmentDirection == Direction.Horizontal) ? size : -size;
					float offset = obj.overrideOffset ? obj.customOffset : defaultOffset;
					if (lastElementNeverHasOffset && lastActiveItem == obj)
					{
						offset = 0f;
					}
					curPos += (alignmentDirection == Direction.Horizontal) ? offset : -offset;
				}
				++itemIndex;
			}
		}

		isAligned = true;
		if (onReadyCallback != null)
		{
			onReadyCallback();
		}
	}

	private AlignedObject GetLastActiveItem()
	{
		AlignedObject lastActiveItem = null;
		int itemIndex = 0;
		
		foreach (AlignedObject obj in alignedObjects)
		{
			if (obj != null && obj.IsActive())
			{
				lastActiveItem = obj;
			}
			++itemIndex;
		}
		
		return lastActiveItem;
	}

	private void RegisterOnChangeHandlers()
	{
		for (int i = 0; i < alignedObjects.Count; ++i)
		{
			AlignedObject obj = alignedObjects[i];
			if (obj.widget != null)
			{
				obj.widget.onChange += OnWidgetsChanged;
			}
			else if (obj.widgetHolder != null)
			{
				obj.widgetHolder.onChange += OnWidgetsChanged;
			}
			
		}
	}

	private void DeregisterOnChangeHandlers()
	{
		for (int i = 0; i < alignedObjects.Count; ++i)
		{
			AlignedObject obj = alignedObjects[i];
			if (obj.widget != null)
			{
				obj.widget.onChange -= OnWidgetsChanged;
			}
			else if (obj.widgetHolder != null)
			{
				obj.widgetHolder.onChange -= OnWidgetsChanged;
			}
			
		}
	}

	private void OnWidgetsChanged()
	{
		widgetsChanged = true;
	}

	private void Report(string msg)
	{
#if DEBUG_ALIGN_UI_ELEMENTS_CLASS
		EB.Debug.Log(string.Format("[{0}] AlignUIElements > {1} > {2}",
			Time.frameCount,
			EB.UIUtils.GetFullName(gameObject),
			msg));
#endif
	}
}
