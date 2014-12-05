#pragma strict

function Start () {

}

function Update () {

}


function OnGUI ()
{
	//make a group of the buttons on the screen
	GUI.BeginGroup (Rect(Screen.width /2 - 50, Screen.height / 2 - 50, 100, 175));
	
	// Make a box to see the group on screen
	GUI.Box (Rect (0, 0, 100, 170), "Main Menu");
	
	// Add buttons for game nevigation
	if ( GUI.Button (Rect(10, 30, 80, 30), "Start Game") ) {
		Application.LoadLevel("scene_load");
	}
	
	if ( GUI.Button (Rect(10, 65, 80, 30), "Credit") ) {
		Application.LoadLevel("scene_credit");
	}
	
	if ( GUI.Button (Rect(10, 100, 80, 30), "Exit") ) {
		Application.Quit();
	}
	
	if ( GUI.Button (Rect(10, 135, 80, 30), "Homepage") ) {
		Application.OpenURL("http://www.walkerboystudio.com");
	}
	
	
	GUI.EndGroup ();
	
	
}