using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour {
	public GameObject target;

	public float attackTimer;
	public float coolDown;

	// Use this for initialization
	void Start () {
		attackTimer = 0;
		coolDown = 2.0f;
	}
	
	// Update is called once per frame
	void Update () {
		if(attackTimer >0)
		if(Input.GetKeyUp(KeyCode.F)) {
			Attack();                                                
		}
	}

	private void Attack() {
		float distance = Vector3.Distance(target.transform.position, transform.position);
		Vector3 dir = (target.transform.position - transform.position);

		float direction = Vector3.Dot (dir, transform.forward);
		Debug.Log("Direction =" + direction);

		if(distance < 2.5f && direction > 0) {
			EnemyHealth eh = (EnemyHealth)target.GetComponent("EnemyHealth");
			eh.AdjuestCurtHealth(-10);
		}
	}
}
