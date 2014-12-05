using UnityEngine;
using System.Collections;

public class script_player : MonoBehaviour {

	public var lives : int = 3;
	
	float speed = 8.0;
	float vert_max = 0.0;
	float vert_min = -0.0;
	var horz_max : float = 0.0;
	var horz_min : float = -0.0;
	var numberOfShields	: int = 3;
	var shieldKeyInput : KeyCode;
	
	var projectTile : Transform;
	var socketProjectTile : Transform;
	var shieldMesh : Transform;
	
	//Private Variables
	private var ifShieldOn : boolean = false;
	private var shieldClone : Component;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
