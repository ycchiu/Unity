using UnityEngine;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;
using WindowInitInfo = WindowManager.WindowInitInfo;

public class GenericPopupTestScreen : Window
{
	private GenericPopup.InitInfo popupInitData;
	
	protected override void SetupWindow()
	{
		base.SetupWindow();
		
		GameObject openPopupButton = EB.Util.GetObjectExactMatch(gameObject, "OpenPopupButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(openPopupButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			// This is how we pass customizable initialization data to the opening window:
			WindowInitInfo initInfo = new WindowInitInfo();
			initInfo.sourceName = windowInfo.name;
			// The target window supplies us with this data structure which we fill out.
			popupInitData = new GenericPopup.InitInfo();
			popupInitData.titleText = "Test Generic Popup";
			popupInitData.bodyText = "This is an example of a generic popup.";
			popupInitData.buttons = new List<GenericPopup.ButtonInfo>();
			popupInitData.hasCloseButton = true;
			popupInitData.buttons.Add(new GenericPopup.ButtonInfo("test button 0"));
			popupInitData.buttons.Add(new GenericPopup.ButtonInfo("test button 1"));
			initInfo.data = popupInitData;
			// Finally, pass the initInfo and a callback into the Open method.
			WindowManager.Instance.Open(WindowLayer.Popup, "GenericPopup", initInfo, EB.SafeAction.Wrap<WindowInfo>(this, OnPopupClosed));
		};

		GameObject closeScreenButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		interactive = EB.Util.FindComponent<BoxCollider>(closeScreenButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};
	}

	// This gets called with the result of the Popup. Note that it does not get called
	// until any outro transition has been completed.
	private void OnPopupClosed(WindowInfo closingInfo)
	{
		int buttonIndex = (int)closingInfo.closingData.data;

		string result;
		if (buttonIndex == GenericPopup.CloseButtonIndex)
		{
			result = "GenericPopup exited via close button press.";
		}
		else if (popupInitData != null &&
				 popupInitData.buttons != null &&
				 buttonIndex < popupInitData.buttons.Count)
		{
			result = string.Format("GenericPopup closed by button labeled '{0}'.", popupInitData.buttons[buttonIndex].label);
		}
		else
		{
			result = "Something went wrong! button index: " + buttonIndex;
		}
		
		EB.UIUtils.SetLabelContents(gameObject, "LabelResultsBody", result);
	}
}
