using UnityEngine;
using System.Collections;

public class GameMaster : MonoBehaviour {
	public GameObject playerCharacter;
	public Camera mainCamera;

	public float zOffset;
	public float yOffset;
	public float xRotation;

	private GameObject _pcCached;

	// Use this for initialization
	void Start () {
		_pcCached = Instantiate(playerCharacter, Vector3.zero, Quaternion.identity) as GameObject;

		zOffset = -2.5f;
		yOffset = 2.0f;
		xRotation = 20.0f;

		mainCamera.transform.position = new Vector3 (_pcCached.transform.position.x, 
		                                             _pcCached.transform.position.y + yOffset, 
		                                             _pcCached.transform.position.z + zOffset);
		mainCamera.transform.Rotate (xRotation, 0, 0);

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
