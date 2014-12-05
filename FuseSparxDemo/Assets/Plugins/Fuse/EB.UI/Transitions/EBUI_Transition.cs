using UnityEngine;

public abstract class EBUI_Transition : MonoBehaviour {
	
	public enum TransitionTypes
	{
		None,
		SlideIn_FromLeft,
		SlideIn_FromTop,
		SlideIn_FromRight,
		SlideIn_FromBottom,
		SlideOut_ToLeft,
		SlideOut_ToTop,
		SlideOut_ToRight,
		SlideOut_ToBottom,
		BounceIn_FromLeft,
		BounceIn_FromTop,
		BounceIn_FromRight,
		BounceIn_FromBottom,
		Intro_AlphaFromZero,
		Outro_AlphaToZero
	};
	
	// SR This has to map to the enum list above
	public static string[] transitionScripts = 
	{"Transition_Default",
	 "Transition_Slide_Intro_FromLeft",
	 "Transition_Slide_Intro_FromTop",
	 "Transition_Slide_Intro_FromRight",
	 "Transition_Slide_Intro_FromBottom",
	 "Transition_Slide_Outro_ToLeft",
	 "Transition_Slide_Outro_ToTop",
	 "Transition_Slide_Outro_ToRight",
	 "Transition_Slide_Outro_ToBottom",
	 "Transition_Bounce_Intro_FromLeft",
	 "Transition_Bounce_Intro_FromTop",
	 "Transition_Bounce_Intro_FromRight",
	 "Transition_Bounce_Intro_FromBottom",
	 "Transition_Intro_AlphaFromZero",
	 "Transition_Outro_AlphaToZero"
	};
	
	[System.Serializable]
	public class TransitionPropertyInfo 
	{
		public TransitionTypes transitionType = TransitionTypes.None;
		public float 		   duration = 0.5F;
		public float 		   delay = 0.0F;
	}

	public TransitionPropertyInfo info;
	
	public EBUI_TransitionManager.transitionIDCallback callback = null;
	private string _id;
	protected UITweener _tweener;
	
	public void Run(TransitionPropertyInfo i, EBUI_TransitionManager.transitionIDCallback  cb, string id)
	{
		info = i;
		callback = cb;
		_id = id;
		StartTransition();
	}
	
	// Use this for initialization
	public virtual void StartTransition() {}
	
	// Use this to clean up the transition.
	public virtual void StopTransition() 
	{
#if TM_DEBUG
		Debug.Log(string.Format ("Stopping transition (and cleaning up) on {0} whose parent is {1}", name, transform.parent.name));	
#endif
		// forcefully finish the tween
		_tweener.tweenFactor = 1f;

		if (callback != null)
		{
			callback(_id);
		}
		Destroy(this);
	}
}

