using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;

/////////////////////////////////////////////////////////////////////////////
/// WindowInputBlocker
/////////////////////////////////////////////////////////////////////////////
/// Manages blocking of input based on layers and windows.
/////////////////////////////////////////////////////////////////////////////
public class WindowInputBlocker : MonoBehaviour
{
	public const string rootName = "EBUI_Root";
	public const string uiCameraName = "Camera";
	public const string assetPath = "UI/Framework/WindowInputBlocker";
	
	[System.NonSerialized]
	public int depthPerLayer = 20;
	[System.NonSerialized]
	public int numLayers = 1;
	
	private int baseDepth = 0;
	private List<WindowInfo> blockingWindows;
	private UIPanel[] panels;
	private bool isDisplayingWidgets;
	private BoxCollider blockingCollider;
	
	/// Public Interface ////////////////////////////////////////////////////
	
	/// CreateInputBlocker //////////////////////////////////////////////////
	/// Static initialization class. Creates and returns the input blocker 
	/// instance.
	/////////////////////////////////////////////////////////////////////////
	public static WindowInputBlocker CreateInputBlocker()
	{
		GameObject uiRoot = GameObject.Find(rootName);
		GameObject uiCamera = EB.Util.GetObjectExactMatch(uiRoot, uiCameraName);
		
		GameObject inputBlockerContainer = (GameObject)Instantiate(EB.Assets.Load(assetPath));
		inputBlockerContainer.name = "WindowInputBlocker";
		Transform t = inputBlockerContainer.transform;
		t.parent = uiCamera.transform;
		t.localPosition = Vector3.zero;
		t.localScale = Vector3.one;

		WindowInputBlocker windowInputBlocker = inputBlockerContainer.GetComponent<WindowInputBlocker>();
		windowInputBlocker.ResetScale();
		
		return windowInputBlocker;
	}
	
	/// BlockWindow /////////////////////////////////////////////////////////
	/// Begins or ends blocking the window specified.
	/////////////////////////////////////////////////////////////////////////
	public void BlockWindow(WindowInfo windowInfo, bool blockingEnabled)
	{
		if (blockingEnabled && !blockingWindows.Contains(windowInfo))
		{
			blockingWindows.Add(windowInfo);
		}
		else if (!blockingEnabled && blockingWindows.Contains(windowInfo))
		{
			blockingWindows.Remove(windowInfo);
		}
		
		UpdateDepth();
	}
	
	/// ResetScale //////////////////////////////////////////////////////////
	/// This is intended to make the scale of the blocker match the screen
	/// dimensions for the current device.
	/////////////////////////////////////////////////////////////////////////
	public void ResetScale()
	{
		Debug.LogWarning ("ResetScale not implemented.");
	}
	
	/// SetBaseDepth ////////////////////////////////////////////////////////
	/// Always block input up to below this base depth.
	/////////////////////////////////////////////////////////////////////////
	public void SetBaseDepth(int depth)
	{
		baseDepth = depth;
		UpdateDepth();
	}

	public WindowManager.WindowLayer GetBlockedLayer()
	{
		int blockedLayerAsInt = (baseDepth + 1) / depthPerLayer;
		WindowManager.WindowLayer layer = (WindowManager.WindowLayer)blockedLayerAsInt;
		return layer;
	}

	public void DisplayWidgets(bool display)
	{
		isDisplayingWidgets = display;
		GameObject debug = EB.Util.GetObjectExactMatch(gameObject, "DebugBlocker");
		foreach (UIWidget w in EB.Util.FindAllComponents<UIWidget>(debug))
		{
			w.enabled = display;
		}
	}
	
	public bool IsDisplayingWidgets
	{
		get
		{
			return isDisplayingWidgets;
		}
		set
		{
			if (value != isDisplayingWidgets)
			{
				DisplayWidgets(value);
			}
		}
	}

	/// IsBlockingLayer /////////////////////////////////////////////////////
	/// Check if the input blocker is blocking the layer specified.
	/////////////////////////////////////////////////////////////////////////
	public bool IsBlockingLayer(WindowManager.WindowLayer layer)
	{
		if (panels == null || panels.Length <= 0)
		{
			EB.Debug.LogError("No panels were found on the WindowInputBlocker!");
			return false;
		}
		
		UIPanel panel = panels[0];
		int layerDepth = depthPerLayer * (int)layer;
		
		return (layerDepth <= panel.depth);
	}
	
	/// Private Implementation //////////////////////////////////////////////

	/// CalculateBlockingWindowDepth ////////////////////////////////////////
	/// Based on which windows have been flagged as blocking, figures out 
	/// what depth to block up to.
	/// 
	/// Currently this just blocks everything all the time - transitions need
	/// to do this to be safe.
	/////////////////////////////////////////////////////////////////////////
	private int CalculateBlockingWindowDepth()
	{
		if (blockingWindows.Count == 0)
		{
			return 0;
		}
		else
		{
			return (depthPerLayer * numLayers);
		}
	}
	
	/// UpdateDepth /////////////////////////////////////////////////////////
	/// Internal method to reposition the blocker.
	/////////////////////////////////////////////////////////////////////////
	private void UpdateDepth()
	{
		int currentDepth = Mathf.Max(baseDepth, CalculateBlockingWindowDepth());

		blockingCollider.enabled = (Mathf.Abs(currentDepth) > 0);
		
		foreach (UIPanel panel in panels)
		{
			panel.depth = currentDepth;
		}
	}
	
	/// Monobehaviour Implementation ////////////////////////////////////////
	private void Awake()
	{
		blockingWindows = new List<WindowInfo>();
		panels = EB.Util.FindAllComponents<UIPanel>(gameObject);
		if (panels == null || panels.Length < 1)
		{
			panels = new UIPanel[1];
			panels[0] = gameObject.AddComponent<UIPanel>();
		}
		blockingCollider = EB.Util.FindComponent<BoxCollider>(gameObject);

		DisplayWidgets(false);
		UpdateDepth();
	}
}
