#pragma strict
// Ispector Variables
var waitTime : float = 2.0; //in seconds


//function Start () {
//
//}

function Update () 
{
	// player don't want to wait
	if (Input.GetKeyDown("space")) {
		Application.LoadLevel ("scene_game");	//start the game
	}
	else {
		WaitTime();
	}
}

function OnGUI ()
{
	//make a group of the buttons on the screen
	GUI.BeginGroup (Rect(Screen.width /2 - 100, Screen.height / 2 - 100, 200, 200));
	
	// Make a box to see the group on screen
	GUI.Box (Rect (0, 0, 200, 200), "Instruction");
	
	// Instruction for player
	GUI.Label (Rect (10, 30, 140, 40),"Arro Keys : Move");
	GUI.Label (Rect (10, 60, 150, 70),"Spacebar : Shoot Bullets");
	GUI.Label (Rect (10, 90, 160, 100),"Esc : Quit the Game");
	
	GUI.EndGroup ();
}

function WaitTime ()
{
	yield WaitForSeconds (waitTime);
	Application.LoadLevel ("scene_game");
}