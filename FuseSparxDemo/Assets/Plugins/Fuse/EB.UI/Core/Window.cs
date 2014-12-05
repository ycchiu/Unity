// #define DEBUG_WINDOW_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;
using WindowInitInfo = WindowManager.WindowInitInfo;

/////////////////////////////////////////////////////////////////////////////
/// Window
/////////////////////////////////////////////////////////////////////////////
/// The base class for windows to implement. These can be screens, popups, 
/// overlays, etc.
/////////////////////////////////////////////////////////////////////////////
public class Window : MonoBehaviour
{
	public enum State
	{
		Uninitialized,
		WaitingForDependencies,
		ReadyToShow,
		Animating,
		Open,
		Closed,
		Destroying
	}
	
	[System.NonSerialized]
	public WindowManager windowManager;
	[System.NonSerialized]
	public WindowInputBlocker windowInputBlocker;
	[System.NonSerialized]
	public WindowInfo windowInfo;
	
	/////////////////////////////////////////////////////////////////////////
	#region State
	public State state
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
			
			bool isActive;
			switch (value)
			{
				case State.Closed:
				case State.Uninitialized:
				case State.WaitingForDependencies:
				case State.ReadyToShow:
					isActive = false;
					break;
				default:
					isActive = true;
					break;
			}
			if (isActive != wasActive)
			{
				wasActive = isActive;
				string layerName = isActive ? "GUI" : "GUIDisabled";
				EB.Util.SetLayerRecursive(gameObject, LayerMask.NameToLayer(layerName));

				// Panels need a refresh for change of layer.
				UIPanel[] panels = EB.Util.FindAllComponents<UIPanel>(gameObject);
				foreach (UIPanel panel in panels)
				{
					if (panel.widgetsAreStatic)
					{
						panel.singleFrameUpdate = true;
					}
				}
			}
		}
	}
	private State _state;
	private bool wasActive = true;
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	protected object windowData;
	protected EB.Action<WindowInfo, EB.Action> introTransition;
	protected EB.Action<WindowInfo, EB.Action> outroTransition;
	
	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	
	/// Initialize //////////////////////////////////////////////////////////
	/// Called when the window is opened.
	/// 
	/// initData contains optional setup information passed in by the opener.
	/// 
	/// closeData contains any information passed by the child screen that
	/// was closed, causing this screen to reopen.
	/// 
	/// windowData provides access to the temporary storage for this window.
	/// It can be used to store state information about the window while sub-
	/// windows are open, etc.
	/////////////////////////////////////////////////////////////////////////
	public void Initialize(WindowInfo windowInfo, object windowData)
	{
		Report("Initialize");
		if (windowInfo == null)
		{
			EB.Debug.LogError("Window.Initialize on '{0}' was passed no windowInfo!", gameObject.name);
		}
		state = State.Uninitialized;
		
		introTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, DefaultIntroTransition);
		outroTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, DefaultOutroTransition);
		
		// Save references to data.
		this.windowData = windowData;
		this.windowInfo = windowInfo;
		
		state = State.WaitingForDependencies;

		// Block UI for initialization.
		windowInputBlocker.BlockWindow(windowInfo, true);
		InitializeEndPointCalls();
	}
	
	/// CloseWindow /////////////////////////////////////////////////////////
	/// Closes this window.
	/////////////////////////////////////////////////////////////////////////
	public void CloseWindow(WindowInitInfo closingData = null)
	{
		Report("CloseWindow");
		windowInfo.closingData = closingData;
		windowManager.Close(windowInfo.layer, windowInfo.name, closingData);
	}
	
	/// CloseWindowExternal /////////////////////////////////////////////////
	/// Used to close the window in preparation for another window opening 
	/// over it. This should only be called by the window display manager.
	/////////////////////////////////////////////////////////////////////////
	public void CloseWindowExternal(EB.Action windowClosed = null)
	{
		Report("CloseWindowExternal");
		// This will be fired either after a delay or immediately based on the logic below.
		EB.Action closeComplete = delegate() {
			if (windowClosed != null)
			{
				windowClosed();
			}
			
			DestroyWindow();
		};
		
		if (state == State.Open)
		{
			HideWindow(EB.SafeAction.Wrap(this, closeComplete));
		}
		else // Window is not active. No need to hide.
		{
			closeComplete();
		}
	}
	
	/// HideWindow //////////////////////////////////////////////////////////
	/// Hides the window. This appears as though the window is closing to the
	/// user, but it is not destroyed. This is expected to be called only by
	/// the WindowManager.
	/////////////////////////////////////////////////////////////////////////
	public void HideWindow(EB.Action WindowHidden = null)
	{
		Report("HideWindow");
		state = State.Animating;
		windowInputBlocker.BlockWindow(windowInfo, true);
		OnOutroTransitionBegin();
		if (windowInfo.transitionOutBeginCallback != null)
		{
			windowInfo.transitionOutBeginCallback(windowInfo);
		}
		outroTransition(windowInfo, EB.SafeAction.Wrap(this, delegate() {
			Report("HideWindow > Outro transitions complete.");
			state = State.Closed;
			OnOutroTransitionComplete();
			if (windowInfo.transitionOutEndCallback != null)
			{
				windowInfo.transitionOutEndCallback(windowInfo);
			}
			if (WindowHidden != null)
			{
				WindowHidden();
			}
			windowInputBlocker.BlockWindow(windowInfo, false);
			windowManager.NotifyWindowClose(windowInfo);
		}));
	}
	
	/// ShowWindow //////////////////////////////////////////////////////////
	/// Shows the window. This appears as though the window is opening to the
	/// user, but it is not being created; instead it is just being displayed
	/// again. This is expected to be called only by the WindowManager, or
	/// by the Window itself to indicate initialization is complete.
	/////////////////////////////////////////////////////////////////////////
	public void ShowWindow(EB.Action WindowShown = null)
	{
		Report("ShowWindow");
		gameObject.SetActive(true);
		state = State.Animating;
		OnIntroTransitionBegin();
		if (windowInfo.transitionInBeginCallback != null)
		{
			windowInfo.transitionInBeginCallback(windowInfo);
		}
		
		introTransition(windowInfo, EB.SafeAction.Wrap(this, delegate() {
			Report("ShowWindow > Intro transitions complete.");
			state = State.Open;
			OnIntroTransitionComplete();
			if (windowInfo.transitionInEndCallback != null)
			{
				windowInfo.transitionInEndCallback(windowInfo);
			}
			if (WindowShown != null)
			{
				WindowShown();
			}
			windowInputBlocker.BlockWindow(windowInfo, false);
			windowManager.NotifyWindowOpen(windowInfo);
			windowManager.NotifyStackChange(windowInfo.layer);
		}));
	}
	
	/// CanGoBack ///////////////////////////////////////////////////////////
	/// Defines if a back button press (either at the UI or OS level) can be 
	/// handled by this window.
	/////////////////////////////////////////////////////////////////////////
	public virtual bool CanGoBack()
	{
		return (state == State.Open);
	}
	
	/// OnBackClicked ///////////////////////////////////////////////////////
	/// Defines how this window handles a back button input (only applicable
	/// if CanGoBack returns true).
	/////////////////////////////////////////////////////////////////////////
	public virtual void OnBackClicked()
	{
		CloseWindow();
	}
	
	/// OnAddedToStack //////////////////////////////////////////////////////
	/// Called when the window is added to a stack layer. This will happen 
	/// before SetupWindow and ShowWindow. It can be used to do non-window 
	/// dependent initialization.
	/////////////////////////////////////////////////////////////////////////
	public virtual void OnAddedToStack()
	{
		Report("OnAddedToStack");
	}
	
	/// OnPreloadComplete ///////////////////////////////////////////////////
	/// Called when the window gets preloaded. This will not be called if the
	/// window is not preloaded. Additionally, shared component instances and
	/// streamed textures are not guaranteed to be available when this method
	/// is called. It can be used for caching other GameObject references, 
	/// etc.
	/////////////////////////////////////////////////////////////////////////
	public virtual void OnPreloadComplete()
	{
		Report("OnPreloadComplete");
	}
	
	/// HandleBroadcastEvent ////////////////////////////////////////////////
	/// Called when an event has been broadcast to all open windows.
	/////////////////////////////////////////////////////////////////////////
	public virtual void HandleBroadcastEvent(string eventName, object parameter)
	{
	}
	
	/// WindowReEntryExternal ///////////////////////////////////////////////
	/// Called by the window manager. This is here to allow window base class
	/// code which will not be overridden when WindowReEntry is.
	/////////////////////////////////////////////////////////////////////////
	public void WindowReEntryExternal()
	{
		Report("WindowReEntryExternal");
		windowInputBlocker.BlockWindow(windowInfo, true);
		WindowReEntry();
	}
	
	/// WindowReEntry ///////////////////////////////////////////////////////
	/// Called when a window is going to be re-entered after another has been
	/// opened and later closed above it on the stack. This allows for screen
	/// data to be refreshed if necessary. Call base.WindowReEntry() to
	/// allow the intro transition to take place.
	/////////////////////////////////////////////////////////////////////////
	public virtual void WindowReEntry()
	{
		// Override and delay this call if there is anything else that should
		// be done before displaying the window to the user.
		ShowWindow();
	}
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Overridable Protected Implementation
	/////////////////////////////////////////////////////////////////////////
	
	/// InitializeEndPointCalls /////////////////////////////////////////////
	/// Access SparxHub to make any requests that are required for this 
	/// window to be displayed. If you need to override this call, fire off
	/// OnEndPointCallsReady() once the process is completed.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void InitializeEndPointCalls()
	{
		OnEndPointCallsReady();
	}
	
	/// InitializeEndPointCalls /////////////////////////////////////////////
	/// Called once SparxHub endpoint requests are completed. 
	/////////////////////////////////////////////////////////////////////////
	protected void OnEndPointCallsReady()
	{
		WaitForUIDependencies();
	}
	
	/// SetupWindow /////////////////////////////////////////////////////////
	/// Happens after GameObject creation and SharedComponentInstance 
	/// instantiation, and receiving endpoint callbacks, but before all 
	/// textures may have completed loading, and before beginning to 
	/// transition onto the screen.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void SetupWindow()
	{
		Report("SetupWindow");
	}
	
	/// WindowReady /////////////////////////////////////////////////////////
	/// Called when all of the window's known dependencies are ready, 
	/// including that UITextureRefs have been loaded. 
	/////////////////////////////////////////////////////////////////////////
	protected virtual void WindowReady()
	{
		Report("WindowReady");
		// Override and delay this call if there is anything else that should
		// be done before displaying the window to the user.
		ShowWindow();
	}
	
	/// OnIntroTransitionBegin //////////////////////////////////////////////
	/// Called when the window is about to transition in. 
	/////////////////////////////////////////////////////////////////////////
	protected virtual void OnIntroTransitionBegin()
	{
		Report("OnIntroTransitionBegin");
	}
	
	/// OnOutroTransitionBegin //////////////////////////////////////////////
	/// Called when the window is about to transition out.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void OnOutroTransitionBegin()
	{
		Report("OnOutroTransitionBegin");
	}
	
	/// OnIntroTransitionComplete ///////////////////////////////////////////
	/// Called when the window has transitioned in. Custom animations can be
	/// kicked off here.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void OnIntroTransitionComplete()
	{
		Report("OnIntroTransitionComplete");
	}
	
	/// OnIntroTransitionComplete ///////////////////////////////////////////
	/// Called when the window has transitioned out.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void OnOutroTransitionComplete()
	{
		Report("OnOutroTransitionComplete");
		// Turn gameObject off to prevent rendering.
		gameObject.SetActive(false);
	}
	
	/// TeardownWindow //////////////////////////////////////////////////////
	/// Called when the window is about to be flagged for gameObject 
	/// destruction. You can use this call to release any resources used by
	/// the window.
	/////////////////////////////////////////////////////////////////////////
	protected virtual void TeardownWindow()
	{
		Report("TeardownWindow");
	}
	
	/// DefaultIntroTransition //////////////////////////////////////////////
	/// Defines the UI's default intro transition implementation.
	/////////////////////////////////////////////////////////////////////////
	protected static void DefaultIntroTransition(WindowInfo windowInfo, EB.Action completionCallback)
	{
		int typesRemaining = 2;
		EB.Action waitForAllTransitionTypes = EB.SafeAction.Wrap(windowInfo.window, delegate() {
			-- typesRemaining;
			if (typesRemaining <= 0)
			{
				completionCallback();
			}
		});

		UITransition uiTransition = windowInfo.screenObject.GetComponent<UITransition>();
		if (uiTransition != null)
		{
			uiTransition.TransitionIn(waitForAllTransitionTypes);
		}
		else
		{
			--typesRemaining;
		}
		EBUI_TransitionManager.Instance.RunIntroTransitions(windowInfo.screenObject, waitForAllTransitionTypes);
	}
	
	/// DefaultIntroTransition //////////////////////////////////////////////
	/// Defines the UI's default outro transition implementation.
	/////////////////////////////////////////////////////////////////////////
	protected static void DefaultOutroTransition(WindowInfo windowInfo, EB.Action completionCallback)
	{
		int typesRemaining = 2;
		EB.Action waitForAllTransitionTypes = EB.SafeAction.Wrap(windowInfo.window, delegate() {
			-- typesRemaining;
			if (typesRemaining <= 0)
			{
				completionCallback();
			}
		});
		
		UITransition uiTransition = windowInfo.screenObject.GetComponent<UITransition>();
		if (uiTransition != null)
		{
			uiTransition.TransitionOut(waitForAllTransitionTypes);
		}
		else
		{
			--typesRemaining;
		}
		EBUI_TransitionManager.Instance.RunOutroTransitions(windowInfo.screenObject, waitForAllTransitionTypes);
	}
	
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	/////////////////////////////////////////////////////////////////////////
	
	/// WaitForUIDependencies ///////////////////////////////////////////////
	/// Listens for child UIDependency instances to declare they are ready.
	/////////////////////////////////////////////////////////////////////////
	private void WaitForUIDependencies()
	{
		Report("WaitForUIDependencies");

		List<UIDependency> deps = EB.UIUtils.GetUIDependencies(this);
		EB.UIUtils.WaitForUIDependencies(EB.SafeAction.Wrap(this, OnUIDependenciesReady), deps);
	}

	private void OnUIDependenciesReady()
	{
		Report("OnUIDependenciesReady");
		if (state != State.ReadyToShow)
		{
			InitializeGraphs();
			SetupWindow();
			state = State.ReadyToShow;
			WindowReady();
		}
	}

	private void InitializeGraphs()
	{
		Component[] graphs = EB.Util.FindAllComponents(gameObject, typeof(UIControlGraph));

		foreach (Component graph in graphs)
		{
			UIControlGraph controlGraph = graph as UIControlGraph;
			controlGraph.Initialize();
		}
	}
	
	/// DestroyWindow ///////////////////////////////////////////////////////
	/// Destroys the window gameobject and notifies listeners that it is now
	/// closed and cleaned up.
	/////////////////////////////////////////////////////////////////////////
	private void DestroyWindow()
	{
		state = State.Destroying;
		Report("DestroyWindow");
		// Save stored data.
		if(windowInfo != null)
		{
			if (windowManager != null)
			{
				windowManager.SaveWindowData(windowInfo, windowData);
			}
			if (windowInfo.closeCallback != null)
			{
				windowInfo.closeCallback(windowInfo);
			}
		}	
		
		TeardownWindow();
		
		NGUITools.Destroy(gameObject);
	}
	
	/// Report //////////////////////////////////////////////////////////////
	/// Class logging method.
	/////////////////////////////////////////////////////////////////////////
	private void Report(string msg)
	{
#if DEBUG_WINDOW_CLASS
		EB.Debug.Log(string.Format("[{0}] Window > {1} > {2}",
			Time.frameCount,
			(windowInfo == null) ? gameObject.name : windowInfo.name,
			msg));
#endif
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
		UILogger.Instance.Log(string.Format("[{0}] Window > {1} > {2}",
		                                    Time.frameCount,
		                                    (windowInfo == null) ? gameObject.name : windowInfo.name,
		                                    msg));
#endif
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
