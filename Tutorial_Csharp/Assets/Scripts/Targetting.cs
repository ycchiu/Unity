using UnityEngine;
using System.Collections.Generic;

public class Targetting : MonoBehaviour {
	private Transform myTransform;

	public List<Transform> targets;
	public Transform selectedTarget;

	// Anything happened before script
	void Awake() {
		myTransform = this.transform;
	}

	// Use this for initialization
	void Start () {
		targets = new List<Transform>();
		selectedTarget = null;

		AddAllEnemies();
	}

	// Update is called once per frame
	void Update () {
		if(Input.GetKeyUp(KeyCode.Tab)) {
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
		if (selectedTarget == null) 
		{
			SortTargetByDistance ();
			selectedTarget = targets [0];
		} 
		else 
		{
			int index = targets.IndexOf(selectedTarget);
			if(index < targets.Count-1) {
				index += 1;
			}
			else {
				index = 0;
			}

			UnselectTarget();
			selectedTarget = targets [index];
		}

		if (selectedTarget != null) {
			SelectTarget();
		}
	}

	private void SelectTarget() {
		selectedTarget.renderer.material.color = Color.red;

		PlayerAttack pa = (PlayerAttack) GetComponent("PlayerAttack");

		pa.target = selectedTarget.gameObject;
	}

	private void UnselectTarget() {
		selectedTarget.renderer.material.color = Color.white;
		selectedTarget = null;
	}

	private void SortTargetByDistance() {
		targets.Sort ( delegate (Transform t1, Transform t2){
			return Vector3.Distance(t1.position, myTransform.position).CompareTo(Vector3.Distance(t2.position
			                                                                                      , myTransform.position));		
		});
	}

}
