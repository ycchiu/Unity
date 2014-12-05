#pragma strict

var bulletSpeed	: float = 15.0;
var heightLimit : float = 9.0;

var explosion	: Transform;
var sceneManager : GameObject;

function Start () {

}

function Update () 
{
	transform.Translate(0, bulletSpeed * Time.deltaTime, 0);
	
	if(transform.position.y >= heightLimit) {
		Destroy (gameObject);
	}
}

function OnTriggerEnter (other : Collider)
{
	if(other.gameObject.tag == "astroid") {
		other.transform.position.y = 9;
		other.transform.position.x = Random.Range(-6, 6);
		
		if( explosion ) {
			Instantiate (explosion, transform.position, transform.rotation);
		}
		
		//Tell scene manager that we destroyed on enemy, and add a point to the score
		sceneManager.transform.GetComponent(script_sceneManager).AddScore();
		
		Destroy (gameObject);
	}
}