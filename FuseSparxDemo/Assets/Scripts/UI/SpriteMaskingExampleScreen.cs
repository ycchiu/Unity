using UnityEngine;
using System.Collections;
using WindowInfo = WindowManager.WindowInfo;

public class SpriteMaskingExampleScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();

		GameObject btn, interactive, interactiveContainer;

		interactiveContainer = EB.Util.GetObjectExactMatch(gameObject, "Interactive");

		btn = EB.Util.GetObjectExactMatch(interactiveContainer, "CloseScreenButton");
		interactive = EB.Util.FindComponent<BoxCollider>(btn).gameObject;
		UIEventListener.Get(interactive).onClick = delegate(GameObject go) {
			CloseWindow();
		};

		introTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, DefaultIntroTransition);
		outroTransition = EB.SafeAction.Wrap<WindowInfo, EB.Action>(this, DefaultOutroTransition);
	}

	/// DefaultIntroTransition //////////////////////////////////////////////
	/// Defines the UI's default intro transition implementation.
	/////////////////////////////////////////////////////////////////////////
	protected static new void DefaultIntroTransition(WindowInfo windowInfo, EB.Action completionCallback)
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
	protected static new void DefaultOutroTransition(WindowInfo windowInfo, EB.Action completionCallback)
	{
		TweenPosition tweenPosition = EB.Util.FindComponent<TweenPosition>(windowInfo.screenObject);
		tweenPosition.PlayReverse();
		tweenPosition.SetOnFinished(delegate() {
			completionCallback();
		});
	}
}
