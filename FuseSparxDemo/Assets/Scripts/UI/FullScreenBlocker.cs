// #define DEBUG_FULL_SCREEN_BLOCKER_CLASS
using UnityEngine;
using System.Collections;
using WindowLayer = WindowManager.WindowLayer;


/////////////////////////////////////////////////////////////////////////////
/// FullScreenBlocker
/////////////////////////////////////////////////////////////////////////////
/// This window is always loaded and therefore should be as lightweight as 
/// possible!
/// 
/// The purpose of this blocker is to provide a very simple, optionally 
/// instant blocker that blocks both input and visuals. This can be used as a
/// Loading Screen of sorts, when using the actual loading screen would not
/// be desirable.
/////////////////////////////////////////////////////////////////////////////
public class FullScreenBlocker : Window
{
	private float currentAlpha = 0f;
	private float deltaAlpha = 1f;
	private bool alphaUpdating = false;
	private bool isOpening = false;
	private UISprite blocker = null;

	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	public static FullScreenBlocker Instance { get; private set; }

	public void SetColor(Color rgb)
	{
		rgb.a = currentAlpha;
		blocker.color = rgb;
	}

	public void Show(float fadeTime = 0f)
	{
		Report("Show (" + fadeTime.ToString() + ")");
		if (fadeTime <= 0f)
		{
			currentAlpha = 1f;
			alphaUpdating = false;
			state = State.Open;
			windowManager.NotifyStackChange(WindowLayer.Loading);
			UpdateAlpha();
		}
		else
		{
			deltaAlpha = 1f / fadeTime;
			alphaUpdating = true;
			isOpening = true;
		}

		state = State.Open;
		windowManager.NotifyStackChange(WindowLayer.Loading);
	}

	public void Hide(float fadeTime = 0f)
	{
		Report("Hide (" + fadeTime.ToString() + ")");
		if (fadeTime <= 0f)
		{
			currentAlpha = 0f;
			alphaUpdating = false;
			state = State.Closed;
			windowManager.NotifyStackChange(WindowLayer.Loading);
			UpdateAlpha();
		}
		else
		{
			deltaAlpha = 1f / fadeTime;
			alphaUpdating = true;
			isOpening = false;
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region Window Overrides
	// Override WindowReady to add a callback to ShowWindow to immediately hide.
	protected override void WindowReady()
	{
		ShowWindow(delegate() {
			Hide();
		});
	}

	protected override void SetupWindow()
	{
		Instance = this;
		blocker = EB.Util.FindComponent<UISprite>(gameObject);

		base.SetupWindow();
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	// Updates visuals and keeps track of animating currentAlpha.
	private void UpdateAlpha()
	{
		if (alphaUpdating)
		{
			float frameDelta = deltaAlpha * Time.deltaTime;
			if (isOpening)
			{
				currentAlpha += frameDelta;
				if (currentAlpha >= 1f)
				{
					currentAlpha = 1f;
					alphaUpdating = false;
				}
			}
			else
			{
				currentAlpha -= frameDelta;
				if (currentAlpha <= 0f)
				{
					currentAlpha = 0f;
					alphaUpdating = false;
					state = State.Closed;
					windowManager.NotifyStackChange(WindowLayer.Loading);
				}
			}
		}

		blocker.alpha = currentAlpha;
	}

	private void Update()
	{
		if (alphaUpdating)
		{
			UpdateAlpha();
		}
	}

	/// Report //////////////////////////////////////////////////////////////
	/// Class logging method.
	/////////////////////////////////////////////////////////////////////////
	private void Report(string msg)
	{
#if DEBUG_FULL_SCREEN_BLOCKER_CLASS
		EB.Debug.Log(string.Format("[{0}] FullScreenBlocker > {1}",
		                           Time.frameCount,
		                           msg));
#endif
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
		UILogger.Instance.Log(string.Format("[{0}] FullScreenBlocker > {1}",
		                                    Time.frameCount,
		                                    msg));
#endif
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
