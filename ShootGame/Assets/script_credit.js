#pragma strict
//Credit Script

// Inspector Variables

//function Start () {
//
//}
//
//function Update () {
//
//}

function OnGUI ()
{
	//make a group of the buttons on the screen
	GUI.BeginGroup (Rect(Screen.width /2 - 100, Screen.height / 2 - 100, 200, 200));
	
	// Make a box to see the group on screen
	GUI.Box (Rect (0, 0, 200, 200), "Credits");
	
	// Instruction for player
	GUI.Label (Rect (10, 30, 200, 50),"Designer		: Eric Walker");
	GUI.Label (Rect (10, 60, 200, 80),"Developer	: Eric Walker");
	GUI.Label (Rect (10, 90, 200, 110),"Artists		: Eric Walker");
	GUI.Label (Rect (10, 120, 200, 130),"Sount		: Eric Walker");
	
	//Add Button here, return the game
	if(GUI.Button(Rect(60, 175, 80, 30), "Back")) {
		Application.LoadLevel("scene_menu");
	}
	
	GUI.EndGroup ();
}