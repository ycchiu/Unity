using UnityEngine;
using System.Collections.Generic;
using WindowInfo = WindowManager.WindowInfo;
using WindowLayer = WindowManager.WindowLayer;
using WindowInitInfo = WindowManager.WindowInitInfo;

public class GenericPopup : Window
{
	public class ButtonInfo
	{
		public string label;
		
		public ButtonInfo(string label)
		{
			this.label = label;
		}
	}
	
	public class InitInfo
	{
		public string titleText;
		public string bodyText;
		public bool hasCloseButton;
		public List<ButtonInfo> buttons;
	}
	
	public const int CloseButtonIndex = -1;
	
	private List<GameObject> buttons;

	protected override void SetupWindow()
	{
		base.SetupWindow();

		// Get initialization data, or handle missing data without falling over.
		InitInfo initInfo = null;
		if (windowInfo.initData == null)
		{
			EB.Debug.LogError("GenericPopup > SetupWindow > initData is missing.");
			initInfo = GetDefaultBehaviour();
		}
		else
		{
			initInfo = windowInfo.initData.data as InitInfo;
			if (initInfo == null)
			{
				EB.Debug.LogError("GenericPopup > SetupWindow > initInfo data structure missing.");
				initInfo = GetDefaultBehaviour();
			}
		}

		GameObject buttonsContainer = EB.Util.GetObjectExactMatch(gameObject, "ButtonsContainer");
		buttons = EB.ArrayUtils.ToList<GameObject>(EB.Util.GetObjects(buttonsContainer, "Button"));
		buttons.Remove(buttonsContainer);
		// Ensure the buttons are numerically ordered.
		buttons.Sort(delegate(GameObject x, GameObject y) {
			return x.name.CompareTo(y.name);
		});
		
		SetupGenericPopup(initInfo);
	}

	private void SetupGenericPopup(InitInfo initInfo)
	{
		EB.UIUtils.SetLabelContents(gameObject, "LabelTitle", initInfo.titleText);
		EB.UIUtils.SetLabelContents(gameObject, "LabelBody", initInfo.bodyText);

		// Close button setup:
		GameObject closeButton = EB.Util.GetObjectExactMatch(gameObject, "CloseButton");
		closeButton.SetActive(initInfo.hasCloseButton);
		if (initInfo.hasCloseButton)
		{
			GameObject interactive = EB.Util.FindComponent<BoxCollider>(closeButton).gameObject;
			UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
				WindowInitInfo closingData = new WindowInitInfo();
				closingData.sourceName = windowInfo.name;
				closingData.data = CloseButtonIndex;
				CloseWindow(closingData);
			};
		}

		// Active buttons:
		int i;
		for (i = 0; i < initInfo.buttons.Count && i < buttons.Count; ++i)
		{
			GameObject btn = buttons[i];
			GameObject interactive = EB.Util.FindComponent<BoxCollider>(btn).gameObject;
			ButtonInfo data = initInfo.buttons[i];
			int currentIndex = i;
			EB.UIUtils.SetLabelContents(btn, "Label", data.label);
			UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
				WindowInitInfo closingData = new WindowInitInfo();
				closingData.sourceName = windowInfo.name;
				closingData.data = currentIndex;
				CloseWindow(closingData);
			};
		}

		// Inactive buttons:
		for (; i < buttons.Count; ++i)
		{
			buttons[i].SetActive(false);
		}

		AlignUIElements alignment = EB.Util.FindComponent<AlignUIElements>(gameObject);
		alignment.Reposition();
	}

	private InitInfo GetDefaultBehaviour()
	{
		InitInfo initInfo = new InitInfo();
		initInfo.titleText = "Missing";
		initInfo.bodyText = "Missing";
		initInfo.buttons = new List<ButtonInfo>();
		initInfo.hasCloseButton = true;
		
		return initInfo;
	}
}
