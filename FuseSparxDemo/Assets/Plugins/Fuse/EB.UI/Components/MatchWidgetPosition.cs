using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MatchWidgetPosition : MonoBehaviour
{
	/////////////////////////////////////////////////////////////////////////
	#region Public Enumerations
	/////////////////////////////////////////////////////////////////////////
	public enum MatchAnchor
	{
		Left,
		Right,
		Top,
		Bottom,
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	public UIWidget widgetToMatch;
	public Vector3 offset;
	public MatchAnchor matchAnchor;

	public void Reposition()
	{
		Vector3 zeroInWorldSpace = transform.TransformPoint(Vector3.zero);
		Vector3 offsetInWorldSpace = transform.TransformPoint(offset);
		Vector3 offsetDeltaInWorldSpace = offsetInWorldSpace - zeroInWorldSpace;

		Bounds b = EB.UIUtils.GetWidgetWorldBounds(widgetToMatch);
		Vector3 pos = transform.position;

		switch (matchAnchor)
		{
		case MatchAnchor.Top:
			pos.y = b.max.y;
			break;
		case MatchAnchor.Bottom:
			pos.y = b.min.y;
			break;
		case MatchAnchor.Left:
			pos.x = b.min.x;
			break;
		case MatchAnchor.Right:
			pos.x = b.max.x;
			break;
		}
		transform.position = pos + offsetDeltaInWorldSpace;
	}

	/////////////////////////////////////////////////////////////////////////
	#region Monobehaviour Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Awake()
	{
		Reposition();
	}

	private void OnEnable()
	{
		if (widgetToMatch != null)
		{
			widgetToMatch.onChange += OnMatchedWidgetChange;
		}
	}
	
	private void OnDisable()
	{
		if (widgetToMatch != null)
		{
			widgetToMatch.onChange -= OnMatchedWidgetChange;
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	private void OnMatchedWidgetChange()
	{
		Reposition();
	}
}
