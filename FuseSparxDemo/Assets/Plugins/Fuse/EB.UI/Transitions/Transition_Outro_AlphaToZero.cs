using UnityEngine;
using System.Collections;

public class Transition_Outro_AlphaToZero : EBUI_Transition
{
	/*************************************************/
	public override void StartTransition() 
	{						
		_tweener = TweenAlpha.Begin(gameObject, info.duration, 0f);
		_tweener.delay = info.delay;
		_tweener.method = UITweener.Method.Linear;
		EventDelegate.Add(_tweener.onFinished, StopTransition);
	}
}

