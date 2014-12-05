using UnityEngine;
using System.Collections;

// Need a meta class which will be part of the UI stuff..

public class Transition_Bounce_Intro_FromRight : EBUI_Transition {
	
	/*************************************************/
	public override void StartTransition() 
	{						
		// Store off the Z
		float originalZ = gameObject.transform.localPosition.z;

		// Store off the Z
		gameObject.transform.localPosition = new Vector3(Screen.width*2.0F,0.0F,originalZ);

		_tweener = TweenPosition.Begin (gameObject, info.duration, new Vector3(0f, 0f, originalZ));
		_tweener.method = UITweener.Method.EaseOut;
		_tweener.delay = info.delay;
		EventDelegate.Add (_tweener.onFinished, StopTransition);
	}

}
