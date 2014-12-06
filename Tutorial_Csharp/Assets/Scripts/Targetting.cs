using UnityEngine;
using System.Collections.Generic;

public class Targetting : MonoBehaviour {
	public List<Transform> targets;
	public Transform selectedTarget;

	// Use this for initialization
	void Start () {
		targets = new List<Transform>();
		selectedTarget = null;

		AddAllEnemies();
	}

	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.Tab)) {
			TargetEnemy();
		}
	}


	public void AddAllEnemies() {
		GameObject[] go = GameObject.FindGameObjectsWithTag("Enemy");

		foreach( GameObject enemyObj in go) {
			AddTarget(enemyObj.transform);
		}
	}


	private void AddTarget( Transform enemy) {
		targets.Add(enemy);
	}

	private void TargetEnemy() {
		selectedTarget = targets[0];
	}
}
