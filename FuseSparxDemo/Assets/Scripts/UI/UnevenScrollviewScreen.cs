using UnityEngine;
using System.Collections;

public class UnevenScrollviewScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();

		GameObject interactiveContainer = EB.Util.GetObjectExactMatch(gameObject, "Interactive");

		GameObject btn = EB.Util.GetObjectExactMatch(interactiveContainer, "TestButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(btn).gameObject;
		UIEventListener.Get(interactive).onClick = delegate(GameObject go) {
			Debug.Log ("'" + go.name + "' clicked.");
		};

		btn = EB.Util.GetObjectExactMatch(interactiveContainer, "CloseScreenButton");
		interactive = EB.Util.FindComponent<BoxCollider>(btn).gameObject;
		UIEventListener.Get(interactive).onClick = delegate(GameObject go) {
			CloseWindow();
		};

		GameObject scrollViewContainer = EB.Util.GetObjectExactMatch(interactiveContainer, "ScrollView");
		GameObject[] scrollViewItems = EB.Util.GetObjects(scrollViewContainer, "Item");
		Debug.Log ("There are " + scrollViewItems.Length + " items:");
		foreach (GameObject item in scrollViewItems)
		{
			Debug.Log (EB.UIUtils.GetFullName(item));
			interactive = EB.Util.FindComponent<BoxCollider>(item).gameObject;
			UIEventListener.Get(interactive).onClick = delegate(GameObject go) {
				Debug.Log ("'" + go.name + "' clicked.");
			};
		}
	}
}
