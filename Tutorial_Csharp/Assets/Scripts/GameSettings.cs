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

		for(int cnt = 0; cnt < Enum.GetValues(typeof(VitalName)).Length; cnt++) {
			PlayerPrefs.SetInt( ((VitalName)cnt).ToString() + " - Base Value", pcClass.GetVital(cnt).BaseValue );
			PlayerPrefs.SetInt( ((VitalName)cnt).ToString() + " - Exp To Level", pcClass.GetVital(cnt).ExpToLevel );
			PlayerPrefs.SetInt( ((VitalName)cnt).ToString() + " - Current Value", pcClass.GetVital(cnt).CurValue );
			PlayerPrefs.SetString(((VitalName)cnt).ToString() + " - Mods", pcClass.GetVital(cnt).GetModifyingAttributesString());
		}

		for(int cnt = 0; cnt < Enum.GetValues(typeof(SkillName)).Length; cnt++) {
			PlayerPrefs.SetInt( ((SkillName)cnt).ToString() + " - Base Value", pcClass.GetSkill(cnt).BaseValue );
			PlayerPrefs.SetInt( ((SkillName)cnt).ToString() + " - Exp To Level", pcClass.GetSkill(cnt).ExpToLevel );
			PlayerPrefs.SetString( ((SkillName)cnt).ToString() + " - Mods", pcClass.GetSkill(cnt).GetModifyingAttributesString());
		}
	}

	public void LoadCharacterData () {
		//the one "pc" in CharacterGenerator.cs
		GameObject pc = GameObject.Find("pc"); 
		
		PlayerCharacter pcClass = pc.GetComponent<PlayerCharacter>();
		
		//PlayerPrefs.SetString("Player Name", pcClass.Name); //just set the player name
		pcClass.Name = PlayerPrefs.GetString("Player Name", "Name Me"); //2nd params: default name

		//---->> Debug.Log(pcClass.Name);


		for(int cnt = 0; cnt < Enum.GetValues(typeof(AttributeName)).Length; cnt++) {
			pcClass.GetPrimaryAttribute(cnt).BaseValue = PlayerPrefs.GetInt( ((AttributeName)cnt).ToString() + " - Base Value", 0 );  //2nd params: default if the Attribute is not found
			pcClass.GetPrimaryAttribute(cnt).ExpToLevel = PlayerPrefs.GetInt( ((AttributeName)cnt).ToString() + " - Exp To Level", 0 );
		
			Debug.Log("AttributeName Base Value : " + cnt + "-" + pcClass.GetPrimaryAttribute(cnt).BaseValue);
			Debug.Log("AttributeName Exp Level : " + cnt + "-" + pcClass.GetPrimaryAttribute(cnt).ExpToLevel);
		}

		for(int cnt = 0; cnt < Enum.GetValues(typeof(VitalName)).Length; cnt++) {
			pcClass.GetVital(cnt).BaseValue = PlayerPrefs.GetInt( ((VitalName)cnt).ToString() + " - Base Value", 0 );
			pcClass.GetVital(cnt).ExpToLevel = PlayerPrefs.GetInt( ((VitalName)cnt).ToString() + " - Exp To Level", 0 );
			pcClass.GetVital(cnt).CurValue = PlayerPrefs.GetInt( ((VitalName)cnt).ToString() + " - Current Value", 0 );

			Debug.Log("AttributeName Base Value : " + cnt + "-" + pcClass.GetVital(cnt).BaseValue);
			Debug.Log("AttributeName Exp Level : " + cnt + "-" + pcClass.GetVital(cnt).ExpToLevel);
			Debug.Log("AttributeName CurValue : " + cnt + "-" + pcClass.GetVital(cnt).BaseValue);

			//pcClass.GetVital(cnt).GetModifyingAttributesString() = PlayerPrefs.GetString(((VitalName)cnt).ToString() + " - Mods", "");
		}
		/*
		for(int cnt = 0; cnt < Enum.GetValues(typeof(SkillName)).Length; cnt++) {
			PlayerPrefs.SetInt( ((SkillName)cnt).ToString() + " - Base Value", pcClass.GetSkill(cnt).BaseValue );
			PlayerPrefs.SetInt( ((SkillName)cnt).ToString() + " - Exp To Level", pcClass.GetSkill(cnt).ExpToLevel );
			PlayerPrefs.SetString( ((SkillName)cnt).ToString() + " - Mods", pcClass.GetSkill(cnt).GetModifyingAttributesString());
		}
		*/
	}
}
