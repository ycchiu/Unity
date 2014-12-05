using UnityEngine;
using System.Collections;

public class FocusableControlSampleScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();
		
		GameObject interactive;
		
		for (int btnIdx = 1; btnIdx <= 3; ++btnIdx)
		{
			GameObject button = EB.Util.GetObjectExactMatch(gameObject, "TestButton" + btnIdx);
			interactive = EB.Util.FindComponent<BoxCollider>(button).gameObject;
			int index = btnIdx;
			UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
				Debug.Log("TestButton " + index + " pressed.");
			};
		}
		
		GameObject closeScreenButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		interactive = EB.Util.FindComponent<BoxCollider>(closeScreenButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};
	}
}
