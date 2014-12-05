using UnityEngine;
using System.Collections;

public class Transition_Intro_AlphaFromZero : EBUI_Transition {
	
	/*************************************************/
	public override void StartTransition() 
	{						
		gameObject.GetComponent<UIRect>().alpha = 0f;
		_tweener = TweenAlpha.Begin(gameObject, info.duration, 1f);
		_tweener.delay = info.delay;
		_tweener.method = UITweener.Method.Linear;
		EventDelegate.Add(_tweener.onFinished, StopTransition);
	}
}
