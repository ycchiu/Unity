﻿using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour {
	public Transform target;
	public int moveSpeed;
	public int rotationSpeed;
	public int maxDistance;

	private Transform myTransform;

	// Anything happened before script
	void Awake() {
		myTransform = this.transform;
	}

	// Use this for initialization
	void Start () {
		GameObject gObj = GameObject.FindGameObjectWithTag("Player");
		target = gObj.transform;

		maxDistance = 2;
	}
	
	// Update is called once per frame
	void Update () {
		Debug.DrawLine(target.position, myTransform.position, Color.yellow);

		//Look at target
		myTransform.rotation = Quaternion.Slerp (myTransform.rotation, 
		                                         Quaternion.LookRotation(target.position - myTransform.position), 
		                                         rotationSpeed * Time.deltaTime);

		if(Vector3.Distance(target.position, myTransform.position) > maxDistance) {
			//Move towards target
			myTransform.position += myTransform.forward * moveSpeed * Time.deltaTime;
		}
	}
	
}
