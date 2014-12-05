// #define DEBUG_FOCUS_MANAGER_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;

/////////////////////////////////////////////////////////////////////////////
/// FocusManager
/////////////////////////////////////////////////////////////////////////////
/// 
/////////////////////////////////////////////////////////////////////////////
public class FocusManager : MonoBehaviour
{
	/// Public Enums ////////////////////////////////////////////////////////
	public enum UIInput
	{
		Up,
		Down,
		Left,
		Right,
		Action,
		Back
	}
	
	/// Public Data Structures //////////////////////////////////////////////
	
	/// Public Variables ////////////////////////////////////////////////////
	public static FocusManager Instance {get; private set;}
	
	public WindowManager windowManager
	{
		get
		{
			return _windowManager;
		}
		set
		{
			if (_windowManager != null)
			{
				_windowManager.StackChangeNotification -= OnStackChanged;
			}
			_windowManager = value;
			if (_windowManager != null)
			{
				_windowManager.StackChangeNotification += OnStackChanged;
			}
		}
	}
	private WindowManager _windowManager;

	/// Private Variables ///////////////////////////////////////////////////
	private UIControlGraph activeControlGraph = null;
	
	/// Public Interface ////////////////////////////////////////////////////
	private void SetActiveControlGraph(UIControlGraph graph)
	{
		if (activeControlGraph != graph)
		{
			if (activeControlGraph != null)
			{
				activeControlGraph.SetFocus(false);
			}
			activeControlGraph = graph;
			if (activeControlGraph != null)
			{
				activeControlGraph.SetFocus(true);
			}
		}
	}

	public void SetActiveControl(ControllerInputHandler inputHandler)
	{
		if (activeControlGraph != null)
		{
			activeControlGraph.SetActiveControl(inputHandler);
		}
	}

	public void HandleInput(UIInput input)
	{
		Report("HandleInput: " + input.ToString());
		WindowInfo topWindow = windowManager.GetTopWindow(false);
		// No windows open?
		if (topWindow == null)
		{
			return;
		}
		// Top window is blocked? Ignore this input.
		if (windowManager.windowInputBlocker.IsBlockingLayer(topWindow.layer))
		{
			return;
		}

		UIControlGraph controlGraph = topWindow.controlGraph;
		if (controlGraph != null)
		{
			if (!SendInputToControlGraph(controlGraph, input))
			{
				if (input == UIInput.Back && WindowManager.Instance.CanPressBackButton())
				{
					WindowManager.Instance.HandleBackButton();
				}
			}
		}
		else
		{
			EB.Debug.Log("FocusManager > cannot send input to > " + topWindow.name + " because it is missing a control graph.");
		}
	}
	
	/// Private Implementation //////////////////////////////////////////////
	private bool SendInputToControlGraph(UIControlGraph graph, UIInput input)
	{
		Report("SendInputToDistributor");
		SetActiveControlGraph(graph);

		bool handled = false;
		if (graph != null)
		{
			handled = graph.HandleInput(input);
		}
		return handled;
	}
	
	private void OnStackChanged(WindowManager.WindowLayer layer)
	{
		WindowInfo topWindow = windowManager.GetTopWindow(false);
		// No windows open?
		if (topWindow == null)
		{
			return;
		}
		// Top window is blocked? Ignore this input.
		if (windowManager.windowInputBlocker.IsBlockingLayer(topWindow.layer))
		{
			return;
		}

		UIControlGraph controlGraph = topWindow.controlGraph;
		SetActiveControlGraph(controlGraph);
	}

	/// Monobehaviour Implementation ////////////////////////////////////////
	private void Awake()
	{
		Instance = this;
	}

	// Debug input handling ...
	private void Update()
	{
#if UNITY_ANDROID
		// Handle OS Back button
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (windowManager.CanPressBackButton())
			{
				windowManager.HandleBackButton();
			}
		}
#endif
		// Keyboard input simulation of controller
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			HandleInput(FocusManager.UIInput.Down);
		}
		else if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			HandleInput(FocusManager.UIInput.Up);
		}
		
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			HandleInput(FocusManager.UIInput.Left);
		}
		else if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			HandleInput(FocusManager.UIInput.Right);
		}
		
		if (Input.GetKeyDown(KeyCode.X))
		{
			HandleInput(FocusManager.UIInput.Action);
		}
		else if (Input.GetKeyDown(KeyCode.Z))
		{
			HandleInput(FocusManager.UIInput.Back);
		}
#endif
	}

	/// Report //////////////////////////////////////////////////////////////
	/// Class logging method.
	/////////////////////////////////////////////////////////////////////////
	private void Report(string msg)
	{
#if DEBUG_FOCUS_MANAGER_CLASS
		EB.Debug.Log(string.Format("[{0}] FocusManager > {1} > {2}",
		                           Time.frameCount,
		                           gameObject.name,
		                           msg));
#endif
	}
}
