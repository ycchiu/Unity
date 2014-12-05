#pragma strict
//*************************
//*     Shield Script     *
//*************************


// Inspector Variables
var shieldStrength : int = 2;


function Start () {

}

function Update () 
{
	if( shieldStrength == 0 ) {
		Destroy (gameObject);
	}
}

function OnTriggerEnter ( other : Collider ) 
{
	if (other.tag == "astroid") {
		shieldStrength -= 1;
	}
}