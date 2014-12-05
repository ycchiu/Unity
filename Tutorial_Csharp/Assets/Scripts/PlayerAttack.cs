using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour {
	public GameObject target;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyUp(KeyCode.F)) {
			Attack();                                                
		}
	}

	private void Attack() {
		float distance = Vector3.Distance(target.transform.position, transform.position);
		Debug.Log("Distance = " + distance);

		if(distance < 2.5 ) {
			EnemyHealth eh = (EnemyHealth)target.GetComponent("EnemyHealth");
			eh.AdjuestCurtHealth(-10);
		}
	}
}
