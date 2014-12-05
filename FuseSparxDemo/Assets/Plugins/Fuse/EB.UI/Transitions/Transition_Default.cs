using UnityEngine;
using System.Collections;

// Need a meta class which will be part of the UI stuff..

public class Transition_Default : EBUI_Transition {
	
	
	/*************************************************/
	public override void StartTransition() 
	{	
		StopTransition();
	}

}
