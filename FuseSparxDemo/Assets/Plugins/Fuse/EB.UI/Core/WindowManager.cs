// #define DEBUG_WINDOW_MANAGER_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/////////////////////////////////////////////////////////////////////////////
/// WindowManager
/////////////////////////////////////////////////////////////////////////////
/// Documentation: 
/// http://wiki.kabam.com/display/EBG/Using+the+WindowManager+class
/////////////////////////////////////////////////////////////////////////////
public class WindowManager : MonoBehaviour
{
	/////////////////////////////////////////////////////////////////////////
	#region Public Enums
	/////////////////////////////////////////////////////////////////////////
	// These layers are in order of relative depth.
	public enum WindowLayer
	{
		Background,
		Screen,
		Overlay,
		Loading,
		Popup,
		BusyBlocker,
		ToolTip
	}
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	// Helper function.
	public static WindowLayer[] GetWindowLayers()
	{
		return (WindowLayer[])System.Enum.GetValues(typeof(WindowLayer));
	}
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Data Structures
	/////////////////////////////////////////////////////////////////////////
	// A window can be a screen, popup, overlay, etc.
	public class WindowInfo
	{
		public string name { get; private set; }
		public WindowLayer layer { get; private set; }
		// initData is passed into the window when it is opened.
		public WindowInitInfo initData;
		// closingData is passed into the window when it is returned to by a child screen.
		public WindowInitInfo closingData;
		// Optional callback to fire when closing.
		public EB.Action<WindowInfo> closeCallback;
		// Optional callbacks to fire when transitions begin.
		public EB.Action<WindowInfo> transitionInBeginCallback;
		public EB.Action<WindowInfo> transitionOutBeginCallback;
		public EB.Action<WindowInfo> transitionInEndCallback;
		public EB.Action<WindowInfo> transitionOutEndCallback;
		// Reference to Screen's GameObject. This will be null until asynchronous asset loading is completed.
		public GameObject screenObject;
		// Reference to Window component. This will be null until asynchronous asset loading is completed.
		public Window window;
		// Reference to UIControlGraph component. This will be null until asynchronous asset loading is completed.
		public UIControlGraph controlGraph;

		public void GetLayerPanelDepths(out int minDepth, out int maxDepth)
		{
			int layerIdx = (int)layer;
			minDepth = WindowManager.PanelDepthPerLayer * layerIdx;
			const int reservedForInputBlocker = 1;
			maxDepth = (WindowManager.PanelDepthPerLayer * (layerIdx + 1)) - 1 - reservedForInputBlocker;
		}
		
		public WindowInfo(string name, WindowLayer layer, WindowInitInfo initData, WindowInitInfo closingData, EB.Action<WindowInfo> closeCallback)
		{
			this.name = name;
			this.layer = layer;
			this.initData = initData;
			this.closingData = closingData;
			this.closeCallback = closeCallback;
		}
	}
	
	// Used to pass information into a window when it is opening.
	public class WindowInitInfo
	{
		public string sourceName;
		public object data;
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	public const int PanelDepthPerLayer = 20;
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Variables
	/////////////////////////////////////////////////////////////////////////
	public static WindowManager Instance {get; private set;}
	public string uiPath = "UI/";
	public WindowInputBlocker windowInputBlocker {get; private set;}
	public event EB.Action<WindowLayer> StackChangeNotification;
	public event EB.Action<WindowInfo> WindowOpenNotification;
	public event EB.Action<WindowInfo> WindowCloseNotification;
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Variables
	/////////////////////////////////////////////////////////////////////////
	private Dictionary<WindowLayer, EB.Collections.Stack<WindowInfo>> windowStacks;
	private Dictionary<string, object> storedWindowData;
	// One container per layer:
	private Dictionary<WindowLayer, GameObject> layerContainers;
	// NGUI required elements.
	private GameObject uiCamera;
	private GameObject uiRoot;
	private GameObject uiPreloadContainer;
	private List<string> loadingScreenReasons = new List<string>();
	private List <string> deferredScreens = new List<string>(); // when we need to open multiple screens later
	private List<Window> preloadedWindows = new List<Window>();
	private List<WindowInitInfo> deferredScreensInfo = new List<WindowInitInfo>();
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	public GameObject GetUiRoot()
	{
		return uiCamera;
	}
	
	public GameObject GetPreloadContainer()
	{
		return uiPreloadContainer;
	}
	
	public bool IsPersistent(WindowLayer layer)
	{
		// TODO: Make this data driven.
		return (layer == WindowLayer.Overlay || layer == WindowLayer.BusyBlocker);
	}

	public string GetLayerPath(WindowLayer layer)
	{
		return uiPath + layer.ToString() + "s/";
	}

	public string GetWindowPath(WindowLayer layer, string windowName)
	{
		return GetLayerPath(layer) + windowName;
	}
	
	public string GetWindowPath(WindowInfo winInfo)
	{
		return GetWindowPath(winInfo.layer, winInfo.name);
	}
	
	public void BroadcastToWindows(string eventName, object parameter = null)
	{
		WindowLayer[] layers = GetWindowLayers();
		for (int layerIdx = 0; layerIdx < layers.Length; ++layerIdx)
		{
			EB.Collections.Stack<WindowInfo> stack = windowStacks[layers[layerIdx]];
			foreach (WindowInfo windowInfo in stack)
			{
				windowInfo.window.HandleBroadcastEvent(eventName, parameter);
			}
		}
	}

	/// PreloadWindow ///////////////////////////////////////////////////////
	/// Does the initial work of loading, instantiating and setting up a 
	/// window. When Open is called with the same window name, it will be 
	/// pulled from the preloaded list and moved to the requested layer.
	/////////////////////////////////////////////////////////////////////////
	public void PreloadWindow(WindowLayer layer, string name, EB.Action preloadComplete)
	{
		Report(string.Format("PreloadWindow > {0} ", name));

		string path = GetWindowPath(layer, name);
		LoadWindowObject(layer, name, path, EB.SafeAction.Wrap(this, delegate(Window preloadedWindow) {
			if (preloadedWindow != null)
			{
				preloadedWindows.Add(preloadedWindow);
				preloadedWindow.OnPreloadComplete();
			}
			if (preloadComplete != null)
			{
				preloadComplete();
			}
		}));
	}

	/// Open ////////////////////////////////////////////////////////////////
	/// Opens a window on the specified layer. This is not guaranteed to
	/// happen immediately, due to transitions, implementation details, etc.
	/// The initData is passed to the window once it opens.
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo Open(WindowLayer layer, string name, WindowInitInfo initData = null, EB.Action<WindowInfo> closeCallback = null)
	{
		Report(string.Format("Open > {0} > {1}", layer.ToString(), name));

		// Check in preload ...
		bool preloaded = false;
		WindowInfo newWindowInfo = new WindowInfo(name, layer, initData, null, closeCallback);
		for (int i = 0; i < preloadedWindows.Count; ++i)
		{
			Window w = preloadedWindows[i];
			if (w.windowInfo.name == name)
			{
				w.windowInfo = newWindowInfo;
				Report(string.Format("Opening preloaded screen > {0} > {1}", layer.ToString(), name));
				preloadedWindows.RemoveAt(i);
				CompleteOpenWindowInternal(layer, w.windowInfo, w);
				preloaded = true;
				break;
			}
		}

		if (!preloaded)
		{
			newWindowInfo = new WindowInfo(name, layer, initData, null, closeCallback);
			OpenWindowInternal(layer, newWindowInfo);
		}

		return newWindowInfo;
	}
	
	/// Replace /////////////////////////////////////////////////////////////
	/// Replaces the window at the top of the stack with the one requested.
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo Replace(WindowLayer layer, string name, WindowInitInfo initData = null, EB.Action<WindowInfo> closeCallback = null)
	{
		// TODO: Is this still required if close/open can be called back to back?
		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];

		WindowInfo newWindowInfo;

		if (stack.Count > 0)
		{
			WindowInfo replaceWindowInfo = stack.Peek();
			newWindowInfo = new WindowInfo(name, layer, initData, null, closeCallback);
			replaceWindowInfo.window.CloseWindowExternal(EB.SafeAction.Wrap(this, delegate() {
				EB.Assets.Unload(GetWindowPath(replaceWindowInfo));
				stack.Remove(replaceWindowInfo);
				
				OpenWindowInternal(layer, newWindowInfo);
				// GA: Note: there is a chance that the requested window was not opened. What do we do?
				NotifyStackChange(layer);
			}));
		}
		else
		{
			newWindowInfo = Open(layer, name, initData, closeCallback);
		}

		return newWindowInfo;
	}
	
	/// CloseTop ////////////////////////////////////////////////////////////////
	/// Closes the top window in the specified layer stack
	/////////////////////////////////////////////////////////////////////////////
	public void CloseTop(WindowLayer layer, WindowInitInfo closingData = null)
	{
		Report(string.Format("CloseTop > {0}", layer.ToString()));
		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];
		
		if (stack.Count > 0)
		{
			WindowInfo removeWindowInfo = stack.Peek();
			removeWindowInfo.window.CloseWindowExternal(EB.SafeAction.Wrap(this, delegate() {
				EB.Assets.Unload(GetWindowPath(removeWindowInfo));
				stack.Remove(removeWindowInfo);
				NotifyStackChange(layer);
			}));
			
			NotifyStackChange(layer);
		}
	}
	
	/// Close ///////////////////////////////////////////////////////////////
	/// Closes the highest window on the stack with a matching name. This is
	/// not guaranteed to happen immediately, due to transitions, 
	/// implementation details, etc. The closingData is passed to the window
	/// underneath once it reopens.
	/////////////////////////////////////////////////////////////////////////
	public void Close(WindowLayer layer, string name, WindowInitInfo closingData = null)
	{
		Report(string.Format("Close > {0} > {1}", layer.ToString(), name));
		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];
		
		// Locate the window in this stack (if it is in there).
		// Start looking at the top of the stack and work backwards.
		for (int i = stack.Count - 1; i >= 0; --i)
		{
			WindowInfo windowInfo = stack[i];
			if (windowInfo.name == name)
			{
				// Pass closing info to the parent window:
				if (i >= 1)
				{
					WindowInfo parentWindow = stack[i - 1];
					parentWindow.closingData = closingData;
				}
				Window w = windowInfo.window;
				if (w != null)
				{
					w.CloseWindowExternal(EB.SafeAction.Wrap(this, delegate() {
						EB.Assets.Unload(GetWindowPath(windowInfo));
						stack.Remove(windowInfo);
						NotifyStackChange(layer);
					}));
					
					NotifyStackChange(layer);
					break;
				}
			}
		}
	}
	
	/// CloseAll ////////////////////////////////////////////////////////////
	/// Closes all windows on the layer.
	/////////////////////////////////////////////////////////////////////////
	public void CloseAll(WindowLayer layer, EB.Action onCloseAllCallback = null)
	{
		// Create a callback to track as each window on the layer closes.
		// Once all are closed, update the blocker and notify of the change.
		int windowsClosing = 1;
		EB.Action SingleWindowClosed = EB.SafeAction.Wrap(this, delegate(){
			--windowsClosing;
			if (windowsClosing < 1)
			{
				NotifyStackChange(layer);
				if (onCloseAllCallback != null)
				{
					onCloseAllCallback();
				}
			}
		});

		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];
		for (int i = 0; i < stack.Count; i++) 
		{
			WindowInfo removeWindowInfo = stack[i];
			Window w = removeWindowInfo.window;
			++windowsClosing;
			w.CloseWindowExternal(EB.SafeAction.Wrap(this, delegate() {
				EB.Assets.Unload(GetWindowPath(removeWindowInfo));
				SingleWindowClosed();
			}));
		}
		stack.Clear();

		// Final call to make sure the loop is completed before we fire the callback.
		SingleWindowClosed();
		
		NotifyStackChange(layer);
	}
	
	/// CloseAll ////////////////////////////////////////////////////////////
	/// Closes all windows on all layers.
	/////////////////////////////////////////////////////////////////////////
	public void CloseAll(EB.Action onCloseAllCallback = null)
	{
		int layersClosing = 1;
		EB.Action SingleLayerClosed = EB.SafeAction.Wrap (this, delegate()
		{
			--layersClosing;
			if (layersClosing < 1)
			{
				if (onCloseAllCallback != null)
				{
					onCloseAllCallback();
				}
			}
		});
		foreach (WindowLayer layer in GetWindowLayers())
		{
			// gross, but what can you do? We need to keep the loading screen up when
			// transitioning between levels or in the disconnect flow. If you want to take
			// the loading screen down, explicitly take it down yourself!
			if(layer != WindowLayer.Loading && layer != WindowLayer.BusyBlocker)
			{
				++layersClosing;
				CloseAll(layer, SingleLayerClosed);
			}
		}
		// Final call to ensure we completed the loop before firing our onCloseAllCallback
		SingleLayerClosed();
		
		// TODO:Make sure we complete the preloaded window unloads before calling onCloseAllCallback
		for (int i = 0; i < preloadedWindows.Count; ++i)
		{
			Window w = preloadedWindows[i];
			WindowInfo removeWindowInfo = w.windowInfo;
			if (removeWindowInfo != null)
			{
				w.CloseWindowExternal(EB.SafeAction.Wrap(this, delegate() {
					EB.Assets.Unload(GetWindowPath(removeWindowInfo));
				}));
			}
		}
		preloadedWindows.Clear();
	}

	/// GetWindow ////////////////////////////////////////////////////////////
	/// Retrieves the WindowInfo for the specified screen and window layer
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo GetWindow(WindowLayer layer, string windowName)
	{
		return GetStack (layer).Find(delegate(WindowManager.WindowInfo obj)
		{
			return obj.name.Equals(windowName);
		});
	}

	/// IsInStack ///////////////////////////////////////////////////////////
	/// Checks if the given window is somewhere in the stack.
	/////////////////////////////////////////////////////////////////////////
	public bool IsInStack(WindowLayer layer, string windowName)
	{
		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];
		for (int i = 0; i < stack.Count; ++i)
		{
			if (stack[i].name == windowName)
			{
				return true;
			}
		}
		
		return false;
	}
	
	/// Peek ////////////////////////////////////////////////////////////////
	/// Peeks at the stack requested, returning the name of the window at the
	/// top of the stack. Returns null if there is no window on the layer 
	/// specified.
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo Peek(WindowLayer layer)
	{
		EB.Collections.Stack<WindowInfo> stack = windowStacks[layer];
		if (stack.Count > 0)
		{
			return stack.Peek();
		}
		
		return null;
	}
	
	/// GetStack ////////////////////////////////////////////////////////////
	/// Get the stack for the layer requested.
	/////////////////////////////////////////////////////////////////////////
	public EB.Collections.Stack<WindowInfo> GetStack(WindowLayer layer)
	{
		return windowStacks[layer];
	}
	
	/// GetTopWindow ////////////////////////////////////////////////////////
	/// Get the topmost active window on the topmost active layer.
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo GetTopWindow(bool allowPersistent = true)
	{
		WindowLayer[] layers = GetWindowLayers();
		for (int i = layers.Length - 1; i >= 0; --i)
		{
			if (layers[i] == WindowLayer.Loading || layers[i] == WindowLayer.BusyBlocker)
			{
				continue;
			}
			if (!allowPersistent && IsPersistent(layers[i]))
			{
				continue;
			}
			WindowInfo windowInfo = Peek(layers[i]);
			if (windowInfo != null)
			{
				return windowInfo;
			}
		}
		
		return null;
	}
	
	/// HandleBackButton ////////////////////////////////////////////////////
	/// Passes a back button press to the topmost window.
	/////////////////////////////////////////////////////////////////////////
	public void HandleBackButton()
	{
		WindowInfo topWindow = GetBackButtonHandler();
		if (topWindow != null)
		{
			topWindow.window.OnBackClicked();
		}
	}
	
	/// CanPressBackButton //////////////////////////////////////////////////
	/// Checks if the topmost window is able to handle a back button press at
	/// this time.
	/////////////////////////////////////////////////////////////////////////
	public bool CanPressBackButton()
	{
		WindowInfo windowInfo = GetBackButtonHandler();
		// Window must exist.
		if (windowInfo == null)
		{
			return false;
		}
		// Window must be on screen or popup layers.
		if (windowInfo.layer != WindowLayer.Screen && windowInfo.layer != WindowLayer.Popup)
		{
			return false;
		}
		// Window must be active and not be mid-transition.
		if (windowInfo.window.state != Window.State.Open)
		{
			return false;
		}
		
		return windowInfo.window.CanGoBack();
	}
	
	/// SaveWindowData //////////////////////////////////////////////////////
	/// Saves persistent data for the window specified.
	/////////////////////////////////////////////////////////////////////////
	public void SaveWindowData(WindowInfo windowInfo, object storedData)
	{
		string key = windowInfo.name;
		
		if (storedData != null)
		{
			storedWindowData[key] = storedData;
		}
		else
		{
			if (storedWindowData.ContainsKey(key))
			{
				storedWindowData.Remove(key);
			}
		}
	}

	/// GetWindowData ///////////////////////////////////////////////////////
	/// Retrieves persistent data for the window specified.
	/////////////////////////////////////////////////////////////////////////
	public object GetWindowData(WindowInfo windowInfo)
	{
		if (storedWindowData.ContainsKey(windowInfo.name))
		{
			return storedWindowData[windowInfo.name];
		}
		
		return null;
	}

	/// NotifyWindowOpen ////////////////////////////////////////////////////
	/// 
	/////////////////////////////////////////////////////////////////////////
	public void NotifyWindowOpen(WindowInfo windowInfo)
	{
		NotifyStackChange(windowInfo.layer);
		if (WindowOpenNotification != null)
		{
			WindowOpenNotification(windowInfo);
		}
	}
	
	/// NotifyWindowClose ///////////////////////////////////////////////////
	/// 
	/////////////////////////////////////////////////////////////////////////
	public void NotifyWindowClose(WindowInfo windowInfo)
	{
		NotifyStackChange(windowInfo.layer);
		if (WindowCloseNotification != null)
		{
			WindowCloseNotification(windowInfo);
		}
	}
	
	/// NotifyStackChange ///////////////////////////////////////////////////
	/// Wrapper for stack notification event firing.
	/////////////////////////////////////////////////////////////////////////
	public void NotifyStackChange(WindowLayer layer)
	{
		enabled = true;
		UpdateInputBlocker();
		if (StackChangeNotification != null)
		{
			try
			{
				StackChangeNotification(layer);
			}
			catch (System.Exception e)
			{
				EB.Debug.LogError("NotifyStackChange > Exception in event listener!\n" + e.ToString());
			}
		}
	}

	/// ShowError ///////////////////////////////////////////////////////////
	/// Wrapper to display the Error popup
	/////////////////////////////////////////////////////////////////////////
	public void ShowError(string errorDescription, EB.Action<WindowInfo> callback = null)
	{
		WindowInitInfo initInfo = new WindowInitInfo();
		initInfo.sourceName = "WindowManager";
		initInfo.data = errorDescription;
		Open(WindowLayer.Popup, "Error_Popup", initInfo, callback);
	}

	/// ShowLoadingScreen ///////////////////////////////////////////////////
	/// Wrapper to display the Loading Screen. Returns the loading screen's 
	/// windowInfo object if successful. The call can fail if you try to open
	/// the loading screen before it is ready, so check the result during 
	/// game setup.
	/////////////////////////////////////////////////////////////////////////
	public WindowInfo ShowLoadingScreen(bool show, string reason)
	{
		WindowInfo loadingScreenWindowInfo = GetWindow(WindowLayer.Loading, "Loading_Screen");

		if (loadingScreenWindowInfo == null)
		{
			EB.Debug.LogError("ShowLoadingScreen > Loading screen not found!");
			return null;
		}
		if (loadingScreenWindowInfo.window == null)
		{
			EB.Debug.LogError("ShowLoadingScreen > Loading screen window not ready!");
			return null;
		}
		if (loadingScreenWindowInfo.window.state == Window.State.Uninitialized)
		{
			EB.Debug.LogWarning("ShowLoadingScreen > Loading screen window not initialized!");
			return null;
		}
		if (loadingScreenWindowInfo.window.state == Window.State.WaitingForDependencies)
		{
			EB.Debug.LogWarning("ShowLoadingScreen > Loading screen window waiting for dependencies!");
			return null;
		}

		if (show)
		{
			if (!loadingScreenReasons.Contains(reason))
			{
				if (loadingScreenReasons.Count == 0)
				{
					loadingScreenWindowInfo.window.ShowWindow();
					NotifyStackChange(WindowLayer.Loading);
				}
				loadingScreenReasons.Add(reason);
			}
		}
		else
		{
			loadingScreenReasons.Remove(reason);
			if (loadingScreenReasons.Count == 0)
			{
				loadingScreenWindowInfo.window.HideWindow();
				NotifyStackChange(WindowLayer.Loading);
			}
		}

		return loadingScreenWindowInfo;
	}

	/// IsLoadingScreenOpen /////////////////////////////////////////////////
	public bool IsLoadingScreenOpen()
	{
		WindowInfo loadingScreenWindowInfo = GetWindow(WindowLayer.Loading, "Loading_Screen");
		if (loadingScreenWindowInfo != null)
		{
			return loadingScreenWindowInfo.window.state == Window.State.Open;
		}
		return false;
	}

	/// GetLoadingScreenReasons /////////////////////////////////////////////
	/// For debug purposes.
	/////////////////////////////////////////////////////////////////////////
	public List<string> GetLoadingScreenReasons()
	{
		return loadingScreenReasons;
	}

	/// HaveDeferredScreens /////////////////////////////////////////////////
	/// Do we want to build a stack of screens?
	/////////////////////////////////////////////////////////////////////////
	public bool HaveDeferredScreens()
	{
		return deferredScreens.Count > 0;
	}

	/// AddDeferredScreen ///////////////////////////////////////////////////
	/// When we want to build a stack of screens to be opened later
	/////////////////////////////////////////////////////////////////////////
	public void AddDeferredScreen(string screenName, WindowInitInfo initInfo)
	{
		deferredScreens.Add(screenName);
		deferredScreensInfo.Add(initInfo);
	}

	/// OpenDeferredScreens	/////////////////////////////////////////////////
	/// Build the stack of deferred screens
	/////////////////////////////////////////////////////////////////////////
	public void OpenDeferredScreens()
	{
		int topScreenIdx = deferredScreens.Count - 1;
		for (int idx = 0; idx < deferredScreens.Count; ++idx)
		{
			string screenName = deferredScreens[idx];
			WindowInitInfo initInfo = deferredScreensInfo[idx];
			WindowInfo opened = Open(WindowLayer.Screen, screenName, initInfo);
			bool isTopScreen = (idx == topScreenIdx);
			if (!isTopScreen)
			{
				// Disable window objects until they are opened.
				opened.window.gameObject.SetActive(false);
			}
		}
		deferredScreens.Clear();
		deferredScreensInfo.Clear();
	}

	/// ClearDeferredScreens ////////////////////////////////////////////////
	/// Reset the deferred screen stack.
	/////////////////////////////////////////////////////////////////////////
	public void ClearDeferredScreens()
	{
		deferredScreens.Clear();
		deferredScreensInfo.Clear();
	}

	/// StoreScreensAsDeferred //////////////////////////////////////////////
	/// Store the current screen stack as the deferred stack.
	/////////////////////////////////////////////////////////////////////////
	public void StoreScreensAsDeferred(bool preserveInitInfo = false)
	{
		deferredScreens.Clear();
		deferredScreensInfo.Clear();
		foreach (WindowInfo info in windowStacks[WindowLayer.Screen])
		{
			deferredScreens.Add(info.name);
			if (preserveInitInfo)
			{
				deferredScreensInfo.Add (info.initData);
			}
			else
			{
				deferredScreensInfo.Add (null);
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
		Instance = this;
		storedWindowData = new Dictionary<string, object>();
		CreateWindowStacks();
		CreateUiRoot();
	}

	private void OnUIRootReady()
	{
		CreateWindowInputBlocker();
		CreateWindowLayerObjects();
		InitializeLoadingScreen();
		InitializeBusyBlocker();
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	/////////////////////////////////////////////////////////////////////////
	
	/// CreateWindowInputBlocker ////////////////////////////////////////////
	/// WindowInputBlocker Initialization
	/////////////////////////////////////////////////////////////////////////
	private void CreateWindowInputBlocker()
	{
		windowInputBlocker = WindowInputBlocker.CreateInputBlocker();
		windowInputBlocker.numLayers = GetWindowLayers().Length;
	}
	
	// CreateWindowStacks ///////////////////////////////////////////////////
	/// Populates the dictionary windowStacks with the WindowLayer
	/////////////////////////////////////////////////////////////////////////
	private void CreateWindowStacks()
	{
		windowStacks = new Dictionary<WindowLayer, EB.Collections.Stack<WindowInfo>>();
		WindowLayer[] layers = GetWindowLayers();
		foreach (WindowLayer layer in layers)
		{
			windowStacks[layer] = new EB.Collections.Stack<WindowInfo>();
		}
	}
	
	// CreateUiRoot /////////////////////////////////////////////////////////
	/// Creates NGUI's root object for us to attach windows to.
	/////////////////////////////////////////////////////////////////////////
	private void CreateUiRoot()
	{
		EB.Assets.LoadAsync("UI/Framework/EBUI_Root", typeof(GameObject), EB.SafeAction.Wrap(this, delegate(Object loadedAsset) {
			GameObject uiRootPrefab = loadedAsset as GameObject;
			uiRoot = GameObject.Instantiate(uiRootPrefab) as GameObject;
			uiRoot.name = "EBUI_Root";
			uiCamera = EB.Util.GetObjectExactMatch(uiRoot, "Camera");
			DontDestroyOnLoad(uiRoot);
			DontDestroyOnLoad(uiCamera);
			uiRoot.transform.parent = gameObject.transform;
			uiPreloadContainer = new GameObject("PreloadContainer");
			uiPreloadContainer.transform.parent = gameObject.transform;
			uiPreloadContainer.transform.localPosition = Vector3.zero;
			uiPreloadContainer.transform.localScale = Vector3.one;
			EB.Util.SetLayerRecursive(uiPreloadContainer, LayerMask.NameToLayer("GUIDisabled"));
			OnUIRootReady();
		}));
	}
	
	/// CreateWindowLayerObjects ////////////////////////////////////////////
	/// Object initialization.
	/////////////////////////////////////////////////////////////////////////
	private void CreateWindowLayerObjects()
	{
		layerContainers = new Dictionary<WindowLayer, GameObject>();
		WindowLayer[] layers = WindowManager.GetWindowLayers();
		int count = 0;
		foreach (WindowLayer layer in layers)
		{
			// Create a container for every layer:
			GameObject container = new GameObject(count + ". " + layer.ToString());
			layerContainers[layer] = container;
			container.layer = LayerMask.NameToLayer("GUI");
			Transform t = container.transform;
			t.parent = uiCamera.transform;
			t.localScale = Vector3.one;
			t.localPosition = Vector3.zero;
			
			++count; 
		}
	}

	/// InitializeLoadingScreen /////////////////////////////////////////////
	/// The loading screen always exists, it is disabled until required. This
	/// is done so that we don't spend a frame waiting for the loading screen
	/// to show up.
	/////////////////////////////////////////////////////////////////////////
	private void InitializeLoadingScreen()
	{
		WindowInfo loadingInfo = Open(WindowLayer.Loading, "Loading_Screen");
		EB.Coroutines.Run(WaitForWindowToLoad(loadingInfo, EB.SafeAction.Wrap<WindowInfo>(this, delegate(WindowInfo windowInfo) {
			loadingInfo.window.Initialize(loadingInfo, GetWindowData(loadingInfo));
		})));
		WindowInfo fullScreenBlockerInfo = Open(WindowLayer.Loading, "FullScreenBlocker");
		EB.Coroutines.Run(WaitForWindowToLoad(loadingInfo, EB.SafeAction.Wrap<WindowInfo>(this, delegate(WindowInfo windowInfo) {
			fullScreenBlockerInfo.window.Initialize(fullScreenBlockerInfo, GetWindowData(fullScreenBlockerInfo));
		})));
	}

	/// InitializeBusyBlocker ///////////////////////////////////////////////
	/// As with the loading screen, this screen always exists.
	/////////////////////////////////////////////////////////////////////////
	private void InitializeBusyBlocker()
	{
		WindowInfo blockerInfo = Open(WindowLayer.BusyBlocker, BusyBlockerManager.Config.BlockerUiName);
		EB.Coroutines.Run(WaitForWindowToLoad(blockerInfo, EB.SafeAction.Wrap<WindowInfo>(this, delegate(WindowInfo windowInfo) {
			blockerInfo.window.Initialize(blockerInfo, GetWindowData(blockerInfo));
		})));
	}

	/// WaitForWindowToLoad /////////////////////////////////////////////////
	/// Custom initialization helper function. Wait for the screen object to
	/// exist before firing the callback.
	/////////////////////////////////////////////////////////////////////////
	private static IEnumerator WaitForWindowToLoad(WindowInfo windowInfo, EB.Action<WindowInfo> onLoadedCallback)
	{
		while (windowInfo.screenObject == null)
		{
			yield return null;
		}

		onLoadedCallback(windowInfo);
	}
	
	/// LoadWindowObject ////////////////////////////////////////////////////
	/// Does the asynchronous work of loading a window from its prefab.
	/////////////////////////////////////////////////////////////////////////
	private void LoadWindowObject(WindowLayer layer, string windowName, string path, EB.Action<Window> readyCallback)
	{
		EB.Assets.LoadAsync(path, typeof(GameObject), EB.SafeAction.Wrap(this, delegate(Object loadedAsset) {
			GameObject windowPrefab = loadedAsset as GameObject;
			if (windowPrefab == null)
			{
				EB.Debug.LogError("Request to open window at '{0}' which does not exist!", path);
				readyCallback(null);
			}
			else
			{
				GameObject windowInstance = NGUITools.AddChild(uiPreloadContainer, windowPrefab);
				windowInstance.name = windowName;
				Window window = EB.Util.FindComponent<Window>(windowInstance);
				if (window != null)
				{
					window.state = Window.State.Uninitialized;
					// Placeholder data to assign a name.
					window.windowInfo = new WindowInfo(windowName, layer, null, null, null);
					window.windowInfo.screenObject = windowInstance;
					AddMissingPanel(windowInstance);
					readyCallback(window);
				}
				else
				{
					EB.Debug.LogError("No Window component found on '{0}' loaded from '{1}'", windowName, path);
				}
			}
		}));
	}
	
	/// OpenWindowInternal ////////////////////////////////////////////////
	/// Kick off the async load of the window prefab asset.
	///////////////////////////////////////////////////////////////////////
	private void OpenWindowInternal(WindowLayer layer, WindowInfo windowInfo)
	{
		if (windowInfo == null)
		{
			EB.Debug.LogError("WindowManager.OpenWindowInternal is missing the windowInfo param.");
		}

		string path = GetWindowPath(windowInfo);
		LoadWindowObject(layer, windowInfo.name, path, EB.SafeAction.Wrap<Window>(this, delegate(Window loadedWindow) {
			if (loadedWindow != null)
			{
				CompleteOpenWindowInternal(layer, windowInfo, loadedWindow);
			}
		}));
	}

	/// CompleteOpenWindowInternal ////////////////////////////////////////
	/// After async load of the window is completed, does setup.
	///////////////////////////////////////////////////////////////////////
	private void CompleteOpenWindowInternal(WindowLayer layer, WindowInfo windowInfo, Window newWindow)
	{
		EB.Collections.Stack<WindowInfo> stack = GetStack(layer);
		stack.Push(windowInfo);

		// Move to the layer requested.
		GameObject instance = newWindow.gameObject;
		instance.transform.parent = layerContainers[layer].transform;
		instance.transform.localPosition = Vector3.zero;
		instance.transform.localRotation = Quaternion.identity;
		instance.transform.localScale = Vector3.one;

		if (newWindow == null)
		{
			EB.Debug.LogError("Request to open window named '{0}' on '{1}' which has no window component on it.", windowInfo.name, layer.ToString());
		}
		windowInfo.screenObject = instance;
		windowInfo.window = newWindow;
		windowInfo.controlGraph = (UIControlGraph) instance.GetComponent(typeof(UIControlGraph));
		if (windowInfo.controlGraph == null)
		{
			windowInfo.controlGraph = instance.AddComponent<UIControlGraph>();
		}

		newWindow.windowManager = this;
		newWindow.windowInputBlocker = windowInputBlocker;
		newWindow.state = Window.State.Uninitialized;
		newWindow.OnAddedToStack();
		UpdatePanelDepths(windowInfo);

		if (!IsPersistent(layer))
		{
			// If there is currently an active window on the layer, hide it.
			int topOfStackIndex = stack.Count - 1;
			int windowToCloseIndex = topOfStackIndex - 1;
			if (windowToCloseIndex >= 0)
			{
				Window windowToHide = stack[windowToCloseIndex].window;
				if (windowToHide.state == Window.State.Open)
				{
					windowToHide.HideWindow();
				}
			}
		}
		NotifyStackChange(layer);
	}

//	private void CheckStack()
//	{
//		foreach (WindowLayer layer in GetWindowLayers())
//		{
//			Debug.Log("Layer : " + layer);
//			EB.Collections.Stack<WindowInfo> stack = GetStack(layer);
//
//			foreach(WindowInfo wi in stack)
//			{
//				if (wi != null)
//				{
//					Debug.Log("wi : " + wi.name);
//				}
//				else
//				{
//					Debug.Log("wi : null");
//				}
//			}
//		}
//	}

	/// Update //////////////////////////////////////////////////////////////
	/// The update function checks every frame to see if there are windows at
	/// the top of the layer stacks waiting to be activated.
	/////////////////////////////////////////////////////////////////////////
	private void Update()
	{
		bool mustKeepUpdating = false;
		foreach (WindowLayer layer in GetWindowLayers())
		{
			// Loading layer is just for the loading screen, and not handled like the rest.
			if (layer == WindowLayer.Loading || layer == WindowLayer.BusyBlocker)
			{
				continue;
			}
			EB.Collections.Stack<WindowInfo> stack = GetStack(layer);
			WindowInfo topWindowInfo = Peek(layer);
			
			if (topWindowInfo != null)
			{
				Window topWindow = topWindowInfo.window;
				if (topWindow == null)
				{
					continue;
				}
				// Does the top window need to be activated?
				if (topWindow.state == Window.State.Closed || topWindow.state == Window.State.Uninitialized)
				{
					bool readyForTransition = true;
					if (!IsPersistent(layer))
					{
						for (int i = 0; i < stack.Count - 1; ++i)
						{
							Window currentWindow = stack[i].window;
							if (currentWindow.state == Window.State.Open)
							{
								currentWindow.HideWindow();
								readyForTransition = false;
								mustKeepUpdating = true;
								break;
							}
							else if (currentWindow.state != Window.State.Closed &&
								currentWindow.state != Window.State.Uninitialized)
							{
								readyForTransition = false;
								mustKeepUpdating = true;
								break;
							}
						}
					}
					
					if (readyForTransition)
					{
						if (topWindow.state == Window.State.Closed)
						{
							topWindow.WindowReEntryExternal();
						}
						else // This is the first entry into this screen.
						{
							topWindow.gameObject.SetActive(true);
							topWindow.Initialize(topWindowInfo, GetWindowData(topWindowInfo));
						}
						NotifyStackChange(layer);
					}
					
				}
				else if (topWindow.state == Window.State.Animating || topWindow.state == Window.State.WaitingForDependencies)
				{
					mustKeepUpdating = true;
				}
			}
		}
		if (!mustKeepUpdating)
		{
			enabled = false;
		}
	}
	
	/// UpdateInputBlocker //////////////////////////////////////////////////
	/// This is here to tell the input blocker about which is the highest
	/// active layer. No other layers should be receiving input under it.
	/// Exception: Persistent layers do not block input to layers below them.
	/////////////////////////////////////////////////////////////////////////
	private void UpdateInputBlocker()
	{
		WindowLayer[] layers = WindowManager.GetWindowLayers();
		
		int depth = 0;
		// Find the highest depth layer and block under that.
		for (int i = layers.Length - 1; i >= 0; --i)
		{
			WindowLayer layer = layers[i];
			if (!IsPersistent(layer))
			{
				WindowInfo topWindow = Peek(layer);
				if (topWindow != null && (topWindow.window.state != Window.State.Destroying && topWindow.window.state != Window.State.Closed))
				{
					depth = (i * PanelDepthPerLayer) - 1;
					break;
				}
			}
		}
		windowInputBlocker.SetBaseDepth(depth);
	}
	
	/// UpdatePanelDepths ///////////////////////////////////////////////////
	/// Assigns depth to the window by running through each UIPanel it 
	/// contains and increasing its depth to be between the values available
	/// to the window. If there isn't a panel on the root of the window, one
	/// will be added.
	/////////////////////////////////////////////////////////////////////////
	private void UpdatePanelDepths(WindowInfo windowInfo)
	{
		// We need a panel at the window root.
		AddMissingPanel(windowInfo.screenObject);
		// Assign depth.
		List<UIPanel> windowPanels = EB.ArrayUtils.ToList<UIPanel>(EB.Util.FindAllComponents<UIPanel>(windowInfo.screenObject));

		int layerMinDepth, layerMaxDepth;
		windowInfo.GetLayerPanelDepths(out layerMinDepth, out layerMaxDepth);
		foreach (UIPanel panel in windowPanels)
		{
			panel.depth = Mathf.Clamp(layerMinDepth + panel.depth, layerMinDepth, layerMaxDepth);
		}
	}

	/// AddMissingPanel /////////////////////////////////////////////////////
	/// If there is no panel on this gameObject, add one.
	/////////////////////////////////////////////////////////////////////////
	private void AddMissingPanel(GameObject windowContainer)
	{
		if (windowContainer.GetComponent<UIPanel>() == null)
		{
			windowContainer.AddComponent<UIPanel>();
		}
	}
	
	/// GetBackButtonHandler ////////////////////////////////////////////////
	/// Get the window that will handle input for the back button.
	/////////////////////////////////////////////////////////////////////////
	private WindowInfo GetBackButtonHandler()
	{
		WindowLayer[] layers = GetWindowLayers();
		WindowInfo handler = null;
		// Find the top window that is not persistent.
		for (int i = layers.Length - 1; i >= 0 && handler == null; --i)
		{
			if (layers[i] == WindowLayer.Loading ||
			    layers[i] == WindowLayer.BusyBlocker)
			{
				continue;
			}
			if (IsPersistent(layers[i]))
			{
				continue;
			}
			handler = Peek(layers[i]);
		}
		
		return handler;
	}
	
	private void Report(string msg)
	{
#if DEBUG_WINDOW_MANAGER_CLASS
		EB.Debug.Log(string.Format("[{0}] WindowManager > {1}", Time.frameCount, msg));
#endif
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
		UILogger.Instance.Log(string.Format("[{0}] WindowManager > {1}", Time.frameCount, msg));
#endif
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
