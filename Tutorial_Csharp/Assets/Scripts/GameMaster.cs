using UnityEngine;
using System.Collections;

public class GameMaster : MonoBehaviour {
	public GameObject playerCharacter;
	public GameObject gameSettings;    //the prefab_GameSettings and CharacterGeneration
	public Camera mainCamera;

	public float zOffset;
	public float yOffset;
	public float xRotation;

	private GameObject _pcCached;
	private PlayerCharacter _pcScript;

	// Use this for initialization
	void Start () {

		//                                       Vector3.zero = the (0,0,0) in the world. Quaternion.identity = facing forward
		_pcCached = Instantiate(playerCharacter, Vector3.zero, Quaternion.identity) as GameObject; 
		_pcCached.name = "pc";

		_pcScript = _pcCached.GetComponent<PlayerCharacter>();

		zOffset = -2.5f;
		yOffset = 2.0f;
		xRotation = 20.0f;

		mainCamera.transform.position = new Vector3 (_pcCached.transform.position.x, 
		                                             _pcCached.transform.position.y + yOffset, 
		                                             _pcCached.transform.position.z + zOffset);
		mainCamera.transform.Rotate (xRotation, 0, 0);

		LoadCharacter ();

	}

	public void LoadCharacter () {
		GameObject gs = GameObject.Find ("__GameSettings");

		if (gs == null) {
			gs = Instantiate(gameSettings, Vector3.zero, Quaternion.identity) as GameObject;
			gs.name = "__GameSettings";
		}

		GameSettings gsScript = gs.GetComponent<GameSettings> ();
		//GameSettings gsScript = GameObject.Find ("__GameSettings").GetComponent<GameSettings> ();
		gsScript.LoadCharacterData();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
