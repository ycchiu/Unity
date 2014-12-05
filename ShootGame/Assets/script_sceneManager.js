#pragma strict

//Inspector Variables
var gameTime : float = 10;
static var score : int = 0;
static var lives : int = 3;
var labelRight : float = 100;


//Private Variabls


function Start () 
{
	InvokeRepeating ("CountDown", 1.0, 1.0);
}

function Update () 
{
	if (lives <= 0 ) {
		Application.LoadLevel ("scene_lost");
		PlayerPrefs.SetInt("score", score);
		lives = 3;
		score = 0;
	}
	
	 if (gameTime <= 0 ) {
		Application.LoadLevel ("scene_win");
		PlayerPrefs.SetInt ("score", score);
		lives = 3;
		score = 0;
	}
}


function AddScore()
{
	score += 1;
}

function SubstractLife()
{
	lives -= 1;
}

function CountDown()
{
	if (--gameTime == 0) {
		CancelInvoke ("CountDown");
	}
}


function OnGUI()
{
	GUI.Label(Rect(10, 10, 150, 20), "Player Score: " + score);
	GUI.Label(Rect(10, 30, 200, 20), "Player Lives: " + lives);
	GUI.Label(Rect(Screen.width-labelRight, 10, 100, 20), "Counter: " + gameTime);
}