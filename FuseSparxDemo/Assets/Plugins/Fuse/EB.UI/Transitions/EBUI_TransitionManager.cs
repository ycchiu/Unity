using UnityEngine;
using System.Collections.Generic;

public class EBUI_TransitionManager : MonoBehaviour {
					
	public static EBUI_TransitionManager Instance { get; private set; }

	public delegate void transitionCallback();
	public delegate void transitionIDCallback(string id);
	
//	int transitionCounter=0;
//	transitionCallback callback;
	
	bool isAnimating = false;
	
	public bool IsAnimating
	{
		get
		{
			return isAnimating;
		}
	}
	
	public void Awake()
	{
		Instance = this;
	}

	/*****************************************/
	private void StartSingleTransition( GameObject go, EBUI_Transition.TransitionPropertyInfo info, transitionIDCallback cb, string id )
	{		
		string script = EBUI_Transition.transitionScripts[ (int)info.transitionType ];
		
		if (string.IsNullOrEmpty(script)==false)
		{
			EBUI_Transition t = (EBUI_Transition)go.AddComponent(script);
			// Set the callback!
			t.Run(info,cb,id);
		}
	}

	/*****************************************/
	public void RunIntroTransitions( GameObject scr, EB.Action callback)
	{
		EBUI_TransitionInfo[] transitions = scr.GetComponentsInChildren<EBUI_TransitionInfo>();				
	
		_transitions[scr.name] = new TransitionRecord{_callback = callback, _counter = transitions.Length};
		
		if(transitions.Length == 0)
			OnTransitionsComplete(scr.name);
		
		isAnimating=true;
		for (int i=0; i<transitions.Length; i++)
		{
#if TM_DEBUG
			Debug.Log(string.Format("Running intro transition on {0}", scr));
#endif
			StartSingleTransition(transitions[i].gameObject, transitions[i].transition_intro, OnTransitionsComplete, scr.name);
		}
	}
	
	/*****************************************/
	public void RunOutroTransitions( GameObject scr, EB.Action callback)
	{
		EBUI_TransitionInfo[] transitions = scr.GetComponentsInChildren<EBUI_TransitionInfo>();				
		
		_transitions[scr.name] = new TransitionRecord{_callback = callback, _counter = transitions.Length};
		
		if(transitions.Length == 0)
			OnTransitionsComplete(scr.name);
		
		isAnimating=true;
		for (int i=0; i<transitions.Length; i++)
		{
#if TM_DEBUG
			Debug.Log(string.Format("Running outro transition on {0}", scr));
#endif
			StartSingleTransition(transitions[i].gameObject, transitions[i].transition_outro, OnTransitionsComplete, scr.name);
		}
	}
	/*****************************************/
	public void OnTransitionsComplete(string id)
	{		
		// should use tryget() here
		TransitionRecord trec;
		if (_transitions.TryGetValue(id, out trec))
		{
			if ( trec._counter>0)
				 trec._counter--;
			
			if ( trec._counter<=0)
			{
			//	EB.Debug.Log(string.Format("Transition set for {0} is complete ", id));
			
				if (trec._callback != null)
				{
					trec._callback();
				}
				
				_transitions.Remove(id);
			}
		}
		if (_transitions.Count == 0)
		{
			isAnimating=false;
		}
	}
	
	
	private class TransitionRecord
	{
		public 	EB.Action 				_callback;
		public 	int						_counter;
	}
	
	public void Clear()
	{
		// HACK: Just in case...
		// Will this break anything else (i.e. any missing transition end callbacks)?
		_transitions.Clear();
		isAnimating=false;
	}
	
	public void HackClear()
	{
		isAnimating=false;
	}
	
	private Dictionary<string, TransitionRecord> 	_transitions = new Dictionary<string, TransitionRecord>();
}
