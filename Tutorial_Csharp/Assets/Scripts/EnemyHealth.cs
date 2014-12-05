using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour {
	public int maxHealth = 100;
	public int curHealth = 100;
	public float healthBarLength;
	
	// Use this for initialization
	void Start () {
		healthBarLength = Screen.width/2;
	}
	
	// Update is called once per frame
	void Update () {
		AdjuestCurtHealth(0);
	}
	
	void OnGUI() {
		GUI.Box(new Rect(10, 40, healthBarLength, 20), curHealth + "/" + maxHealth);
	}
	
	public void AdjuestCurtHealth(int adj) 
	{
		curHealth += adj;
		
		if(maxHealth < 1) {
			maxHealth = 1;
		}
		
		if( curHealth < 0){
			curHealth = 0;
		}
		if(curHealth > maxHealth) {
			curHealth = maxHealth;
		}
		healthBarLength = (Screen.width/2) * (curHealth/(float)maxHealth);
		
	}
}
