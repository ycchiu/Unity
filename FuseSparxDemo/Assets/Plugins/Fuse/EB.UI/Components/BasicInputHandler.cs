using UnityEngine;
using System.Collections;

public class BasicInputHandler : MonoBehaviour, ControllerInputHandler
{
	public GameObject activateWhenFocused;

	
	protected bool isEnabled = true;
	private UIEventListener _eventListener;
	private BoxCollider _boxCollider;
	
	public UIEventListener EventListener
	{
		set
		{
			_eventListener = value;
		}
		get
		{
			if (_eventListener == null)
			{
				_eventListener = EB.Util.FindComponent<UIEventListener>(gameObject);
			}
			return _eventListener;
		}
	}
	
	public BoxCollider ListenerBoxCollider
	{
		get
		{
			if(_boxCollider == null)
				_boxCollider = EventListener.GetComponent<BoxCollider>();
			
			return _boxCollider;
		}
	}
	
	public void SetFocus(bool isFocused)
	{
		activateWhenFocused.SetActive(isFocused);
	}

	public void SetEnabled(bool isEnabled)
	{
		this.isEnabled = isEnabled;
		EventListener.enabled = isEnabled;
		
		if(ListenerBoxCollider != null)
			ListenerBoxCollider.enabled = isEnabled;
	}
	
	public bool HandleInput(FocusManager.UIInput input)
	{
		if (isEnabled)
		{
			
			if (input == FocusManager.UIInput.Action && EventListener != null)
			{
				EventListener.onClick(EventListener.gameObject);
				return true;
			}
		}

		return false;
	}
	
	public bool IsEnabled()
	{
		return isEnabled;
	}
	
	private void Awake()
	{
		SetFocus(false);
	}
}
