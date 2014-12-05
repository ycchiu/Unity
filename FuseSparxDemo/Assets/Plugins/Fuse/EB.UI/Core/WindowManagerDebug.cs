using UnityEngine;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;

public class WindowManagerDebug : MonoBehaviour
{
	public const string AssetPath = "UI/Framework/WindowManagerDebug";
	
	public WindowManager windowManager;
	
	private string lastTxt = "";
	
	public void Show(bool show)
	{
		gameObject.SetActive(show);
	}
	
	public bool IsShowing()
	{
		return gameObject.activeSelf;
	}
	
	private void Start()
	{
		UIEventListener.Get(EB.Util.GetObjectExactMatch(gameObject, "BtnOpen")).onClick += delegate (GameObject go) {
			
		};
		UIEventListener.Get(EB.Util.GetObjectExactMatch(gameObject, "BtnClose")).onClick += delegate (GameObject go) {
			gameObject.SetActive(false);
		};
		
		UIPanel[] panels = EB.Util.FindAllComponents<UIPanel>(gameObject);
		
		// UIPanels for this component goes way out in front of layers to make sure it is on top.
		int depthOffset = WindowManager.PanelDepthPerLayer * (WindowManager.GetWindowLayers().Length + 1);
		foreach (UIPanel panel in panels)
		{
			panel.depth += depthOffset;
		}
	}
	
	private void Update()
	{
		if (windowManager != null)
		{
			WriteDebugContents();
		}
#if UNITY_EDITOR
		if (Application.isEditor && Input.GetKeyDown(KeyCode.P))
		{
			Debug.Break();
		}
#endif
	}

	string DisplayWindowInfo (WindowInfo windowInfo, int index)
	{
		string result = "";
		string winName = windowInfo.name;
		var state = windowInfo.window.state;
		string isActive = (state == Window.State.Open || state == Window.State.Animating) ? "c0c0c0" : "808080";
		
		int activeWidgetCount = 0;
		foreach (UIWidget w in EB.Util.FindAllComponents<UIWidget>(windowInfo.screenObject))
		{
			if (w.gameObject.activeInHierarchy)
			{
				activeWidgetCount ++;
			}
		}
		
		result = string.Format("[{0}] [{1}] {2} ({3}) ({4})[-]", isActive, index, winName, state.ToString(), activeWidgetCount);
		if (windowInfo.layer == WindowManager.WindowLayer.BusyBlocker)
		{
			BusyBlockerManager.BlockerFlag[] flags = EB.Util.GetEnumValues<BusyBlockerManager.BlockerFlag>();
			foreach (BusyBlockerManager.BlockerFlag flag in flags)
			{
				if (BusyBlockerManager.Instance.CheckBlocker(flag))
				{
					result += (" + " + flag.ToString ());
				}
			}
		}

		return result;
	}

	private void WriteDebugContents()
	{
		List<string> debugText = new List<string>();
		
		debugText.Add(string.Format("[ffffff]WindowManager({0})[-]", windowManager.enabled ? "enabled" : "[808080]disabled[-]"));
		
		WindowLayer[] layers = WindowManager.GetWindowLayers();
		for (int i = 0; i < layers.Length; ++i)
		{
			EB.Collections.Stack<WindowInfo> stack = windowManager.GetStack(layers[i]);
			if (stack.Count > 0)
			{
				debugText.Add("[ffffff]" + layers[i].ToString() + "[-]");
			}
			else
			{
				debugText.Add("[606060]" + layers[i].ToString() + "[-]");
			}
			for (int j = 0; j < stack.Count; ++j)
			{
				debugText.Add(DisplayWindowInfo(stack[j], j));
			}
		}
		debugText.Add("[ffffff]Preloaded[-]");
		GameObject preloadContainer = WindowManager.Instance.GetPreloadContainer();
		foreach (Transform t in preloadContainer.transform)
		{
			debugText.Add("[606060]" + t.gameObject.name + "[-]");
		}

		string final = "";
		for (int i = 0; i < debugText.Count; ++i)
		{
			final += debugText[i];
			if (i < debugText.Count - 1)
			{
				final += "\n";
			}
		}
		
		if (lastTxt != final)
		{
			lastTxt = final;
			EB.UIUtils.SetLabelContents(gameObject, "DebugLabel", final);
		}
	}
}
