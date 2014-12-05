#pragma strict

// Main menu

//function Start () {
//
//}
//
//function Update () {
//
//}

function OnGUI ()
{
	GUI.BeginGroup(Rect(Screen.width/2-100, Screen.height/2-100, 200, 100));
	
	GUI.Box (Rect(0, 0, 200, 300), "You Won");
	
	GUI.Label(Rect(10, 30, 100, 50), "Your Score: " + PlayerPrefs.GetInt("score"));
	
	if( GUI.Button( Rect(60, 60, 80, 30), "Main Menu") ) {
		Application.LoadLevel("scene_menu");
	}
	
	GUI.EndGroup();
}