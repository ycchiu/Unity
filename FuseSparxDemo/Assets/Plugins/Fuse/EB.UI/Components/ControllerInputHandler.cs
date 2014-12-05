using UnityEngine;
using System.Collections.Generic;

public interface ControllerInputHandler
{
	void SetFocus(bool isFocused);
	// Return true to indicate that input was handled.
	bool HandleInput(FocusManager.UIInput input);
	// Return true to indicate that this handler is enabled and able to accept input.
	bool IsEnabled();
	// Used to enable or disable input for tutorials.
	void SetEnabled(bool isEnabled);
	// This is part of all monobehaviours, so does not need a manual implementation.
	GameObject gameObject { get; }
}
