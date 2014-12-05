#pragma strict

var astroidSpeed : float = 6.0;
var destroyLimit : float = -9;
var defaultAstroidY : float = 8;

var explosion	: Transform;
var sceneManager : GameObject;

function Start () {

}

function Update () 
{
	transform.Translate(Vector3.down * astroidSpeed * Time.deltaTime);
	
	if( transform.position.y <= destroyLimit ) {
		ResetAstroid();
	}
}

function OnTriggerEnter (other : Collider )
{
	if( explosion ) {
		Instantiate (explosion, transform.position, transform.rotation);
	}
		
	if( other.gameObject.tag == "shield") {
		ResetAstroid();
	}
	
	if( other.gameObject.tag == "Player") {
		
		other.GetComponent(script_player).lives -= 1;
		
		//tell sceneManager that player lose one life
		sceneManager.transform.GetComponent(script_sceneManager).SubstractLife();
		
	}
}

function ResetAstroid()
{
	transform.position.y = defaultAstroidY;
	transform.position.x = Random.Range(-6, 6);
}