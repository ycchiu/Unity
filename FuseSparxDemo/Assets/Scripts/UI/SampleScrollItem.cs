using UnityEngine;
using System.Collections;

public class SampleScrollItem : MonoBehaviour, ControllerInputHandler
{
	private UISprite background;
	private UISprite selected;
	private GameObject highlight;
	private UILabel infoLabel;
	private UITextureRef textureRef;
	private DynamicScrollViewScreen.ScrollItemData dataRef;
	private bool isEnabled = true;
	
	public void SetFocus(bool isFocused)
	{
		highlight.SetActive(isFocused);
	}

	public bool HandleInput(FocusManager.UIInput input)
	{
		if (input == FocusManager.UIInput.Action)
		{
			OnClick(gameObject);
			return true;
		}

		return false;
	}

	public bool IsEnabled()
	{
		return isEnabled;
	}
	
	public void SetEnabled(bool isEnabled)
	{
		this.isEnabled = isEnabled;
	}
	
	public void SetData(DynamicScrollViewScreen.ScrollItemData data)
	{
		dataRef = data;
		background.color = data.bgColor;
		infoLabel.text = data.name;
		textureRef.baseTexturePath = "UI/Streaming/" + data.imagePath;

		if (selected)
		{
			selected.enabled = data.isSelected;
		}
	}

	private void Awake()
	{
		GameObject container = EB.Util.GetObjectExactMatch(gameObject, "ScrollViewInfoLabel");
		infoLabel = EB.Util.FindComponent<UILabel>(container);
		
		container = EB.Util.GetObjectExactMatch(gameObject, "Background");
		background = EB.Util.FindComponent<UISprite>(container);
		
		container = EB.Util.GetObjectExactMatch(gameObject, "ImageTex");
		textureRef = EB.Util.FindComponent<UITextureRef>(container);

		container = EB.Util.GetObjectExactMatch(gameObject, "Selected");
		selected = EB.Util.FindComponent<UISprite>(container);

		highlight = EB.Util.GetObjectExactMatch(gameObject, "Highlight");
		highlight.SetActive(false);

		if (selected)
		{
			selected.enabled = false;
		}

		GameObject interactive = EB.Util.FindComponent<BoxCollider>(gameObject).gameObject;
		UIEventListener.Get(interactive).onClick += OnClick;
	}

	private void OnClick(GameObject go)
	{
		if (!isEnabled)
		{
			return;
		}
		// Demonstrate persistent selection: Since this object can get 
		// recycled at any time, we need to apply changes to the data 
		// object reference we stored in SetData.
		dataRef.isSelected = !dataRef.isSelected;
		selected.enabled = dataRef.isSelected;
	}
}