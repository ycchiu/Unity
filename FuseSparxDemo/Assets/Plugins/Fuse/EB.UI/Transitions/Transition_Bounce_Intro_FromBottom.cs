using UnityEngine;
using System.Collections;

// Need a meta class which will be part of the UI stuff..

public class Transition_Bounce_Intro_FromBottom : EBUI_Transition {

	/*************************************************/
	public override void StartTransition() 
	{						
		// Store off the Z
		float originalZ = gameObject.transform.localPosition.z;
	
		gameObject.transform.localPosition = new Vector3(0.0f,-Screen.height*2.0f,originalZ);

		_tweener = TweenPosition.Begin (gameObject, info.duration, new Vector3(0.0f, 0.0f, originalZ));
		_tweener.delay = info.delay;
		_tweener.method = UITweener.Method.BounceOut;
		EventDelegate.Add (_tweener.onFinished, StopTransition);
	}

}
