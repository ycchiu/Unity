using UnityEngine;
using System.Collections;

public class SharedComponentExampleScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();
		
		GameObject closeScreenButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(closeScreenButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};
	}
}
