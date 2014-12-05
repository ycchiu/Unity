#define DEBUG_UI_GRAPH_EDITOR_DISPLAY_CLASS
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GraphItem = UIGraph.GraphItem;

/////////////////////////////////////////////////////////////////////////////
public class UIGraphEditorDisplay
{
	public string errors = "";
	
	// Percentage of screen width used by element radius
	private const float ElementRadius = 0.015f;

	/////////////////////////////////////////////////////////////////////////
	#region Internal Data Structures
	/////////////////////////////////////////////////////////////////////////
	private class ControlTooltip
	{
		public GraphItem tooltipTarget = null;
		public UIGraph.Link tooltipLinkDirection;
		public bool usingTooltipLinkDirection = false;
	}
	
	private class DragInfo
	{
		public enum DragType
		{
			Link
		}
		
		public GraphItem source = null;
		public DragType dragType;
		public UIGraph.Link direction;
		public bool active = false;
	}
	
	// Mapping of GraphItems to UI Positions.
	private class GraphElement
	{
		public string displayName;
		public GraphItem item;
		// This starts out as the uiPosition (values are 0 to 1 representing screen space),
		// but will be moved after the fact to prevent overlapping of elements in the 
		// Display UI.
		public Vector2 pos;
		
		public GraphElement(GraphItem i, Vector2 p)
		{
			item = i;
			pos = p;
		}
	}
	
	private class GraphArea
	{
		public const float aspectRatio = 16f / 9f;
		
		// The area of the GUI screen that we are drawing this graph to:
		public Rect fullDrawingRect;
		// The area within fullDrawingRect that represents the NGUI screen display as visible to the user.
		public Rect screenDrawingRect;
		
		public void SetDrawArea(Rect rect)
		{
			this.fullDrawingRect = rect;
		}
		
		public void DrawBgRect(Color fill, Color outline, Color offscreen)
		{
			Rect outlineRect = Expand(fullDrawingRect, 1f);
			DrawingUtils.Quad(outlineRect, outline);
			DrawingUtils.Quad(fullDrawingRect, offscreen);
			DrawingUtils.Quad(screenDrawingRect, fill);
		}
		
		// Converts GUI coordinates to 0 -> 1 scale screen space.
		public Vector2 ToScreenSpace(Vector2 guiPosition)
		{
			Vector2 screenPoint = Vector2.zero;
			
			screenPoint.x = (guiPosition.x - screenDrawingRect.x) / screenDrawingRect.width;
			screenPoint.y = (guiPosition.y - screenDrawingRect.y) / screenDrawingRect.height;
			
			return screenPoint;
		}
		
		public void DefineConstraints(List<GraphElement> elements)
		{
			Vector2 min = Vector2.zero;
			Vector2 max = Vector2.one;
			// Make sure there's plenty of room to fully draw all the elements by forcing an area around them:
			float radiusOffset = 4f * UIGraphEditorDisplay.ElementRadius;
			if (elements != null)
			{
				foreach (var el in elements)
				{
					min.x = Mathf.Min(el.pos.x - radiusOffset, min.x);
					min.y = Mathf.Min(el.pos.y - radiusOffset, min.y);
					max.x = Mathf.Max(el.pos.x + radiusOffset, max.x);
					max.y = Mathf.Max(el.pos.y + radiusOffset, max.y);
				}
			}
			
			Vector2 size = max - min;
			Rect screenRatio = new Rect();
			screenRatio.xMin = (0f - min.x) / size.x;
			screenRatio.yMin = (0f - min.y) / size.y;
			screenRatio.xMax = (1f - min.x) / size.x;
			screenRatio.yMax = (1f - min.y) / size.y;
			
			//Vector2 rectSize = new Vector2(fullDrawingRect.width, fullDrawingRect.height);
			screenDrawingRect.xMin = (screenRatio.xMin * fullDrawingRect.width) + fullDrawingRect.xMin;
			screenDrawingRect.xMax = (screenRatio.xMax * fullDrawingRect.width) + fullDrawingRect.xMin;
			// Invert y-axis.
			screenDrawingRect.yMin = ((1f - screenRatio.yMax) * fullDrawingRect.height) + fullDrawingRect.yMin;
			screenDrawingRect.yMax = ((1f - screenRatio.yMin) * fullDrawingRect.height) + fullDrawingRect.yMin;
			
			// Normalize to screen ratio:			
			float oversizeWidth = (screenDrawingRect.width / aspectRatio) - screenDrawingRect.height;
			if (oversizeWidth > 0f)
			{
				screenDrawingRect.xMin += oversizeWidth / 2f;
				screenDrawingRect.xMax -= oversizeWidth / 2f;
			}
			else
			{
				screenDrawingRect.yMin -= oversizeWidth / 2f;
				screenDrawingRect.yMax += oversizeWidth / 2f;
			}
		}
	}
	
	private class ArrowInfo
	{
		public Vector3 start;
		public Vector3 end;
		
		public ArrowInfo(Vector3 start, Vector3 end)
		{
			this.start = start;
			this.end = end;
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	// Seconds between repaints.
	private const double RepaintTime = 0.1;
	// Seconds between repaints while dragging.
	private const double DragRepaintTime = 0.03;

	public bool moveCloseItemsApart = false;

	// Color scheme
	private readonly Color BgColorDisabled             = new Color(0.15f, 0.15f, 0.15f);
	private readonly Color BgColorEnabled              = new Color(0.30f, 0.30f, 0.30f);
	private readonly Color BgOutlineDisabled           = new Color(0.25f, 0.00f, 0.00f);
	private readonly Color BgOutlineEnabled            = new Color(0.50f, 0.00f, 0.00f);
	private readonly Color BgOffscreenEnabled          = new Color(0.15f, 0.15f, 0.15f);
	private readonly Color BgOffscreenDisabled         = new Color(0.07f, 0.07f, 0.07f);
	private readonly Color ArrowHighlighted            = new Color(1.00f, 0.00f, 0.00f);
	private readonly Color ArrowNeutralColorEnabled    = new Color(1.00f, 1.00f, 1.00f);
	private readonly Color ArrowNeutralColorDisabled   = new Color(0.50f, 0.50f, 0.50f);
	private readonly Color ArrowUnhighlightedEnabled   = new Color(0.50f, 0.50f, 0.50f);
	private readonly Color ArrowUnhighlightedDisabled  = new Color(0.25f, 0.25f, 0.25f);
	private readonly Color NodeFillEnabled             = new Color(0.50f, 0.50f, 1.00f);
	private readonly Color NodeFillDisabled            = new Color(0.25f, 0.25f, 0.50f);
	private readonly Color NodeOutlineEnabled          = new Color(0.00f, 0.00f, 0.00f);
	private readonly Color NodeOutlineDisabled         = new Color(0.10f, 0.10f, 0.10f);
	private readonly Color NodeOutlineHighlighted      = new Color(1.00f, 1.00f, 1.00f);
	private readonly Color NodeOutlineActive           = new Color(1.00f, 0.00f, 1.00f);
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Variables
	/////////////////////////////////////////////////////////////////////////
	private Editor _editor;
	private UIGraph _graph;
	private System.DateTime _nextRepaint = System.DateTime.Now;
	private ControlTooltip _tooltip = new ControlTooltip();
	private DragInfo _dragInfo = new DragInfo();
	private GraphArea _screenGraph = new GraphArea();
	private float _elementRadius = 15f;
	private Vector3 _mousePos = Vector3.zero;
	private bool _isReadOnly = false;
	private List<GraphElement> _graphElements;
	private double _lastRenderTime = 0;
	private double _lastUpdateTime = 0;
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	public static Rect Expand(Rect r, float size)
	{
		r.xMin -= size;
		r.xMax += size;
		r.yMin -= size;
		r.yMax += size;
		
		return r;
	}
	
	public UIGraphEditorDisplay(Editor editor)
	{
		this._editor = editor;
	}
	
	public void Initialize(UIGraph graph)
	{
		_graph = graph;
	}
	
	public void DrawGUI()
	{
		if (_graph == null || _graph.graphItems == null)
		{
			Color last = GUI.color;
			GUI.color = Color.red;
			GUILayout.Label("UIGraph is not initialized");
			GUI.color = last;
			return;
		}

		foreach (GraphItem item in _graph.graphItems)
		{
			if (item.uiObject.gameObject == null)
			{
				Color last = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("UIGraph elements have been destroyed");
				GUI.color = last;
				_graph.Deserialize(EB.JSON.Parse(_graph.serializedData) as Hashtable);
				return;
			}
		}
		GUILayout.Label("ItemCount:" + _graph.graphItems.Count);
		
		var renderStartTime = System.DateTime.Now;
		
		_mousePos = new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y, 0f);
		
		_isReadOnly = _graph.autoLink;
		
		// SCREEN RECT DEFINED HERE!!!
		const float scrollbarWidth = 36f;
		float drawingWidth = Screen.width - scrollbarWidth;
		_screenGraph.SetDrawArea(GUILayoutUtility.GetRect(drawingWidth, drawingWidth / GraphArea.aspectRatio));

		bool allowOffscreenItems = true;
		if (allowOffscreenItems)
		{
			_screenGraph.DefineConstraints(_graphElements);
		}
		else
		{
			_screenGraph.DefineConstraints(null);
		}
		_elementRadius = _screenGraph.screenDrawingRect.width * ElementRadius;
		
		DrawScreenRepresentation(_screenGraph);
		DrawTooltip();
		
		var renderEndTime = System.DateTime.Now;
		
		_lastRenderTime = (renderEndTime - renderStartTime).TotalSeconds;

		/*
		GUILayout.Label(string.Format("_lastUpdateTime: {0:0.0}ms", _lastUpdateTime * 1000));
		GUILayout.Label(string.Format("_lastRenderTime: {0:0.0}ms", _lastRenderTime * 1000));
		*/
	}
	
	public void Update()
	{
		if (_nextRepaint < System.DateTime.Now)
		{
			var updateStartTime = System.DateTime.Now;
			RebuildControlList();
			var updateEndTime = System.DateTime.Now;
			_lastUpdateTime = (updateEndTime - updateStartTime).TotalSeconds;
			
			double extraWaitTime = _lastUpdateTime + _lastRenderTime;
			_nextRepaint = System.DateTime.Now.AddSeconds((_dragInfo.active ? DragRepaintTime : RepaintTime) + extraWaitTime);
			// Schedule a UI redraw.
			_editor.Repaint();
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	private bool debugging = false;
	private void MoveGraphItemsApart()
	{
		Rect screen = _screenGraph.screenDrawingRect;
		if (screen.width <= 0 || screen.height <= 0)
		{
			return;
		}
		float minDist = 10f * ElementRadius;
		float moveApartDist = 0.01f;
		int maxIterations = 50;
		
		while (maxIterations > 0)
		{
			bool areApart = true;
			for (int i = 0; i < _graphElements.Count; ++i)
			{
				GraphElement element = _graphElements[i];
				for (int j = 0; j < _graphElements.Count; ++j)
				{
					GraphElement compare = _graphElements[j];
					if (element.item != compare.item)
					{
						Vector2 delta = element.pos - compare.pos;
						delta.x *= GraphArea.aspectRatio;
						
						if (delta.magnitude < minDist)
						{
							Vector2 moveDelta = delta.normalized * moveApartDist;
							// If the items are in the same spot, the move delta will be a zero vector.
							// Take 95% of the value to avoid floating point accuracy errors.
							if (moveDelta.magnitude < moveApartDist * 0.95f)
							{
								moveDelta = Vector2.right * moveApartDist;
							}
							moveDelta.x /= GraphArea.aspectRatio;
							if (debugging)
							{
								Debug.Log(string.Format("normalized movement distance: {0:0.000},{1:0.000}", moveDelta.x, moveDelta.y));
							}
							
							areApart = false;
							// Move them apart:
							// Debug.Log(string.Format("Move apart:\n{0} -> {1}\n{2} -> {3}", element.pos, EBUI.UIUtils.GetFullName(element.item.uiObject.gameObject), compare.pos, EBUI.UIUtils.GetFullName(compare.item.uiObject.gameObject)));
							element.pos += moveDelta;
							compare.pos -= moveDelta;
						}
					}
				}
			}
			
			if (areApart)
			{
				debugging = false;
				break;
			}
			
			--maxIterations;
		}
		if (debugging)
		{
			Debug.Log("apart after: " + (50 - maxIterations) + " iterations.");
		}
	}
	
	public string GetUniqueName(GraphItem item)
	{
		string result = "<null>";

		if (_graphElements != null)
		{
			GraphElement element = _graphElements.Find(el => el.item == item);
			if (element != null)
			{
				result = element.displayName;
			}
		}

		return result;
	}
	
	private void DrawTooltip()
	{
		if (_tooltip.tooltipTarget == null)
		{
			return;
		}
		
		Vector3 pos = GetDrawingPosition(_tooltip.tooltipTarget, _screenGraph.screenDrawingRect);
		string msg = GetUniqueName(_tooltip.tooltipTarget);
		Rect r = DrawingUtils.Text(msg, pos, TextAnchor.UpperLeft, Color.black, false);
		DrawingUtils.Fill(r, Color.white);
		DrawingUtils.Text(msg, pos, TextAnchor.UpperLeft, Color.black, false);
	}
	
	private void DrawScreenRepresentation(GraphArea graphArea)
	{
		Rect rt = graphArea.screenDrawingRect;
		Color bgMain = _isReadOnly ? BgColorDisabled : BgColorEnabled;
		Color bgOutline = _isReadOnly ? BgOutlineDisabled : BgOutlineEnabled;
		Color bgOffscreen = _isReadOnly ? BgOffscreenDisabled : BgOffscreenEnabled;
		DrawingUtils.Clip(Expand(graphArea.fullDrawingRect, 1));
		
		graphArea.DrawBgRect(bgMain, bgOutline, bgOffscreen);
		GUI.color = Color.white;
		
		if (rt.Contains(Event.current.mousePosition))
		{
		}
		
		if (_graph != null)
		{
			_tooltip.tooltipTarget = null;
			foreach (GraphItem item in _graph.graphItems)
			{
				Vector3 pos = GetDrawingPosition(item, graphArea.screenDrawingRect);
				DrawNode(item, pos);
				DrawNodeDirections(item, pos);
			}
			
			DrawLinks(rt);
		}
		
		// Halt drag if necessary:
		if (_dragInfo.active && Event.current.type == EventType.MouseUp && Event.current.button == 0)
		{
			_dragInfo.active = false;
		}
		
		// If drag is active
		if (_dragInfo.active)
		{
			GraphItem dragItem = _graph.graphItems.Find(i => i == _dragInfo.source);
			if (dragItem != null)
			{
				Vector3 pos = GetDrawingPosition(dragItem, graphArea.screenDrawingRect);
				float linkSize = _elementRadius / 2f;
				Vector3 offset = GetDirectionalOffset(_dragInfo.direction) * (_elementRadius + linkSize);
				Vector3 arrowStart = pos + offset;
				Vector3 arrowEnd = _mousePos;
				DrawArrow(arrowStart, arrowEnd, Color.white);
			}
		}
		
		if (Application.isPlaying)
		{
			if (_graph is UIControlGraph)
			{
				UIControlGraph controlGraph = _graph as UIControlGraph;

				GraphItem gi = controlGraph.graphItems.Find(item => item.uiObject.component == controlGraph.activeChild);
				if (gi != null)
				{
					UIObject uiObj = gi.uiObject;
					if (uiObj != null)
					{
						GUILayout.BeginHorizontal();
						bool prevWordWrapState = EditorStyles.textField.wordWrap;
						EditorStyles.textField.wordWrap = true;
						float btnWidth = 60f;
						float maxWidth = Mathf.Min(Screen.width - btnWidth, Screen.width * 0.7f);
						GUILayout.TextArea("Active Control: " + EB.UIUtils.GetFullName(uiObj.gameObject), GUILayout.MaxWidth(maxWidth));
						EditorStyles.textField.wordWrap = prevWordWrapState;
						if (GUILayout.Button("Ping", GUILayout.Width(btnWidth)))
						{
							EditorGUIUtility.PingObject(uiObj.gameObject);
						}
						GUILayout.EndHorizontal();
					}
				}
			}
		}
	}
	
	private void DrawNode(GraphItem item, Vector3 drawPos)
	{
		Color fill = _isReadOnly ? NodeFillDisabled : NodeFillEnabled;
		Color outline = _isReadOnly ? NodeOutlineDisabled : NodeOutlineEnabled;

		if (_graph is UIControlGraph)
		{
			UIControlGraph controlGraph = _graph as UIControlGraph;
			if (controlGraph.activeChild == item.uiObject.component)
			{
				outline = NodeOutlineActive;
			}
		}

		// Check tooltip range
		bool inRange = false;
		if ((_mousePos - drawPos).sqrMagnitude < _elementRadius * _elementRadius)
		{
			_tooltip.tooltipTarget = item;
			_tooltip.usingTooltipLinkDirection = false;
			// Check for drag release.
			if (!_isReadOnly &&
			    Event.current.type == EventType.MouseUp &&
			    Event.current.button == 0 &&
			    _dragInfo.active &&
			    _dragInfo.dragType == DragInfo.DragType.Link &&
			    _dragInfo.source != item)
			{
				_graph.AssignLink(_dragInfo.source, _dragInfo.direction, item);
				_dragInfo.dragType = DragInfo.DragType.Link;
			}
			// Check for ping click
			else if (Event.current.type == EventType.MouseUp &&
			         Event.current.button == 0)
			{
				EditorGUIUtility.PingObject(item.uiObject.gameObject);
			}
			inRange = true;
		}
		DrawingUtils.Circle(drawPos, _elementRadius, fill, inRange ? NodeOutlineHighlighted : outline);
	}
	
	private void DrawNodeDirections(GraphItem item, Vector3 drawPos)
	{
		Color fill = _isReadOnly ? NodeFillDisabled : NodeFillEnabled;
		Color outline = _isReadOnly ? NodeOutlineDisabled : NodeOutlineEnabled;
		foreach (var d in EB.Util.GetEnumValues<UIGraph.Link>())
		{
			float linkSize = _elementRadius / 2f;
			Vector3 offset = GetDirectionalOffset(d) * (_elementRadius + linkSize);
			Vector3 curDirectionPos = drawPos + offset;
			// Check tooltip range
			bool inRange = false;
			if ((_mousePos - curDirectionPos).sqrMagnitude < linkSize * linkSize)
			{
				_tooltip.tooltipTarget = item;
				_tooltip.tooltipLinkDirection = d;
				_tooltip.usingTooltipLinkDirection = true;
				inRange = true;
				// Check for drag start.
				if (!_isReadOnly && Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					_dragInfo.source = item;
					_dragInfo.dragType = DragInfo.DragType.Link;
					_dragInfo.direction = d;
					_dragInfo.active = true;
					// As we start dragging, remove any existing link.
					_graph.AssignLink(item, d, null);
				}
				// Check for drag release.
				if (!_isReadOnly &&
				    Event.current.type == EventType.MouseUp &&
				    Event.current.button == 0 &&
				    _dragInfo.dragType == DragInfo.DragType.Link &&
				    _dragInfo.source != item)
				{
					_graph.AssignLink(_dragInfo.source, _dragInfo.direction, item);
					_dragInfo.dragType = DragInfo.DragType.Link;
				}
			}
			
			// Draw rectangles to represent directions.
			float s = Mathf.Round(linkSize);
			float h = s / 2f;
			Rect r = new Rect(curDirectionPos.x - h, curDirectionPos.y - h, s, s);
			DrawingUtils.Quad(Expand(r, 1f), inRange ? NodeOutlineHighlighted : outline);
			DrawingUtils.Quad(r, fill);
		}
	}
	
	private Vector3 GetDrawingPosition(GraphItem item, Rect guiRect)
	{
		Vector3 pos = Vector3.zero;
		
		if (_graphElements != null)
		{
			GraphElement el = _graphElements.Find(e => e.item == item);
			
			if (el != null)
			{
				pos.x = guiRect.x + (el.pos.x * guiRect.width);
				pos.y = guiRect.y + ((1f - el.pos.y) * guiRect.height);
			}
		}
		
		return pos;
	}
	
	// Draw arrows to represent links between nodes.
	private void DrawLinks(Rect guiRect)
	{
		ArrowInfo highlighted = null;
		List<ArrowInfo> unhighlighted = new List<ArrowInfo>();
		List<ArrowInfo> parentHighlights = new List<ArrowInfo>();
		
		foreach (GraphItem item in _graph.graphItems)
		{
			Vector3 pos = GetDrawingPosition(item, guiRect);
			// Draw directionalOffsets
			foreach (var d in EB.Util.GetEnumValues<UIGraph.Link>())
			{
				float linkSize = _elementRadius / 2f;
				Vector3 offset = GetDirectionalOffset(d) * (_elementRadius + linkSize);
				Vector3 startPos = pos + offset;
				GraphItem linked = _graph.GetLink(item, d);
				
				bool linkHighlighted = false;
				bool parentHighlighted = false;
				if (_tooltip.tooltipTarget == item && _tooltip.usingTooltipLinkDirection && _tooltip.tooltipLinkDirection == d)
				{
					linkHighlighted = true;
				}
				else if (_tooltip.tooltipTarget == item && !_tooltip.usingTooltipLinkDirection)
				{
					parentHighlighted = true;
				}
				
				if (linked != null)
				{
					var linkedItem = _graph.graphItems.Find(i => i == linked);
					if (linkedItem != null)
					{
						Vector3 linkedPos = GetDrawingPosition(linkedItem, guiRect);
						linkedPos -= offset;
						if (linkHighlighted)
						{
							highlighted = new ArrowInfo(startPos, linkedPos);
						}
						else if (parentHighlighted)
						{
							parentHighlights.Add(new ArrowInfo(startPos, linkedPos));
						}
						else
						{
							unhighlighted.Add(new ArrowInfo(startPos, linkedPos));
						}
					}
				}
			}
		}
		
		if (highlighted == null && parentHighlights.Count < 1)
		{
			foreach (ArrowInfo info in unhighlighted)
			{
				DrawArrow(info.start, info.end, _isReadOnly ? ArrowNeutralColorDisabled : ArrowNeutralColorEnabled);
			}
		}
		else if (parentHighlights.Count > 0)
		{
			foreach (ArrowInfo info in unhighlighted)
			{
				DrawArrow(info.start, info.end, _isReadOnly ? ArrowUnhighlightedDisabled : ArrowUnhighlightedEnabled);
			}
			foreach (ArrowInfo info in parentHighlights)
			{
				DrawArrow(info.start, info.end, ArrowHighlighted);
			}
		}
		else
		{
			foreach (ArrowInfo info in unhighlighted)
			{
				DrawArrow(info.start, info.end, _isReadOnly ? ArrowUnhighlightedDisabled : ArrowUnhighlightedEnabled);
			}
			DrawArrow(highlighted.start, highlighted.end, ArrowHighlighted);
		}
	}
	
	private Vector3 GetDirectionalOffset(UIGraph.Link d)
	{
		switch (d)
		{
		case UIGraph.Link.Up:
			return Vector3.down;
		case UIGraph.Link.Down:
			return Vector3.up;
		case UIGraph.Link.Left:
			return Vector3.left;
		case UIGraph.Link.Right:
			return Vector3.right;
		}
		
		return Vector3.zero;
	}
	
	private void DrawArrow(Vector3 start, Vector3 end, Color c)
	{
		DrawingUtils.Line(start, end, c);
		
		float arrowPointAngle = 20f;
		
		float lineLength = (end - start).magnitude;
		float arrowSize = 15f;
		float arrowStart = lineLength - arrowSize;
		float arrowRatio = arrowStart / lineLength;
		
		Vector3 tipStart = Vector3.Lerp(start, end, arrowRatio);
		Vector3 tipToEnd = end - tipStart;
		float tipLen = tipToEnd.magnitude;
		float angle = Mathf.Atan(tipToEnd.y / tipToEnd.x);
		float angleInDeg = angle * Mathf.Rad2Deg;
		
		// Draw the first arrow tip...
		float tipAngle = angleInDeg + arrowPointAngle;
		float tipAngleInRad = tipAngle * Mathf.Deg2Rad;
		float y = Mathf.Sin(tipAngleInRad) * tipLen;
		float x = Mathf.Cos(tipAngleInRad) * tipLen;
		
		Vector3 arrowPoint1;
		if (start.x <= end.x)
		{
			arrowPoint1 = end - new Vector3(x, y, 0f);
		}
		else
		{
			arrowPoint1 = end + new Vector3(x, y, 0f);
		}
		// DrawingUtils.Line(arrowPoint1, end, c);
		
		// Draw the other arrow tip
		tipAngle = angleInDeg - arrowPointAngle;
		tipAngleInRad = tipAngle * Mathf.Deg2Rad;
		y = Mathf.Sin(tipAngleInRad) * tipLen;
		x = Mathf.Cos(tipAngleInRad) * tipLen;
		
		Vector3 arrowPoint2;
		if (start.x <= end.x)
		{
			arrowPoint2 = end - new Vector3(x, y, 0f);
		}
		else
		{
			arrowPoint2 = end + new Vector3(x, y, 0f);
		}
		// DrawingUtils.Line(arrowPoint2, end, c);
		
		DrawingUtils.Triangle(end, arrowPoint1, arrowPoint2, c);
	}
	
	/// Report //////////////////////////////////////////////////////////////
	/// Class logging method.
	/////////////////////////////////////////////////////////////////////////
	private void Report(string msg)
	{
#if DEBUG_UI_GRAPH_EDITOR_DISPLAY_CLASS
		EB.Debug.Log(string.Format("[{0}] FocusableControlDisplay > {1} > {2}",
			Time.frameCount,
			_graph.gameObject.name,
			msg));
#endif
	}
	
	private void RebuildControlList()
	{
		try
		{
			if (_graph == null || _graph.graphItems == null)
			{
				return;
			}
			foreach (GraphItem item in _graph.graphItems)
			{
				if (item.uiObject.gameObject == null)
				{
					return;
				}
			}
	
			// Rebuild control list:
			_graphElements = new List<GraphElement>();
			foreach (GraphItem item in _graph.graphItems)
			{
				item.uiPosition = _graph.GetUiPosition(item.uiObject.gameObject);
				_graphElements.Add(new GraphElement(item, item.uiPosition));
			}
	
			foreach (var el in _graphElements)
			{
				el.displayName = el.item.uiObject.gameObject.name;
				if (el.displayName.Length > 10)
				{
					el.displayName = el.displayName.Substring(0, 10);
				}
				el.displayName += " " + el.pos.ToString();
			}
	
			if (moveCloseItemsApart)
			{
				// Make sure no elements are overlapping
				MoveGraphItemsApart();
			}
		}
		catch (System.Exception e)
		{
			errors = e.Message;
		}
	}
}
