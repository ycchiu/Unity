using UnityEngine;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;
using WindowInitInfo = WindowManager.WindowInitInfo;

public class MatchWidgetSizeTestScreen : Window
{
	private GenericPopup.InitInfo popupInitData;
	
	protected override void SetupWindow()
	{
		base.SetupWindow();

		GameObject closeScreenButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(closeScreenButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};

		introTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, CustomIntroTransition);
		outroTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, CustomOutroTransition);
	}
	
	/// DefaultIntroTransition //////////////////////////////////////////////
	/// Defines the UI's default intro transition implementation.
	/////////////////////////////////////////////////////////////////////////
	protected static void CustomIntroTransition(WindowInfo windowInfo, EB.Action completionCallback)
	{
		TweenPosition tweenPosition = EB.Util.FindComponent<TweenPosition>(windowInfo.screenObject);
		tweenPosition.PlayForward();
		tweenPosition.SetOnFinished(delegate() {
			completionCallback();
		});
	}
	
	/// DefaultIntroTransition //////////////////////////////////////////////
	/// Defines the UI's default outro transition implementation.
	/////////////////////////////////////////////////////////////////////////
	protected static void CustomOutroTransition(WindowInfo windowInfo, EB.Action completionCallback)
	{
		TweenPosition tweenPosition = EB.Util.FindComponent<TweenPosition>(windowInfo.screenObject);
		tweenPosition.PlayReverse();
		tweenPosition.SetOnFinished(delegate() {
			completionCallback();
		});
	}
}
