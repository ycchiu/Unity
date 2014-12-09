using UnityEngine;
using System.Collections;
using System;

public class GameSettings : MonoBehaviour {

	void Awake () {
		DontDestroyOnLoad(this);
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SaveCharacterData () {
		//the one "pc" in CharacterGenerator.cs
		GameObject pc = GameObject.Find("pc"); 

		PlayerCharacter pcClass = pc.GetComponent<PlayerCharacter>();

		//PlayerPrefs.DeleteAll(); //clean all old data

		PlayerPrefs.SetString("Player Name", pcClass.Name);

		for(int cnt = 0; cnt < Enum.GetValues(typeof(AttributeName)).Length; cnt++) {
			PlayerPrefs.SetInt( ((AttributeName)cnt).ToString() + " - Base Value", pcClass.GetPrimaryAttribute(cnt).BaseValue );
			PlayerPrefs.SetInt( ((AttributeName)cnt).ToString() + " - Exp To Level", pcClass.GetPrimaryAttribute(cnt).ExpToLevel );
		}
	}

	public void LoadCharacterData () {
	}
}
