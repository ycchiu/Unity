using UnityEngine;
using System.Collections;
using System;

public class CharacterGenerator : MonoBehaviour {
	private const int STARTING_POINTS = 70;				//The amount player can used to customize skills/attritubes
	private const int MIN_STARTING_ATTRIBUTE_VALUE = 10;	
	private const int STARTING_VALUE = 50;					//The init value for each attritubes
	
	private const int OFFSET = 5;
	private const int LINE_HEIGHT = 20;
	private const int STAT_LABEL_WIDTH = 100;
	private const int BASE_VALUE_LABEL_WIDTH = 30;
	private const int BUTTON_WIDTH = 20;
	private const int POSITION_Y = 40;


	private PlayerCharacter _toon;
	private int _pointsLeft;

	public GUIStyle myStyle;
//	public GUISkin mySkin;
	public GameObject playerPrefab;



	// Use this for initialization
	void Start () {
		GameObject pc = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		pc.name = "pc";

		_toon = pc.GetComponent<PlayerCharacter>();
//		_toon = new PlayerCharacter();
//		_toon.Awake();

		_pointsLeft = STARTING_POINTS;
		for(int cnt = 0; cnt < Enum.GetValues(typeof(AttributeName)).Length; cnt++) 
		{
			_toon.GetPrimaryAttribute(cnt).BaseValue = STARTING_VALUE;
		}

		_toon.StatUpdate();
	}
	
	// Update is called once per frame
	void Update () {
	}

	void OnGUI() {
		DisplayName();
		DisplayPointsLeft();
		DisplayAttributes();
		DisplayVitals();
		DisplaySkills();

		//Add an "Create" button
		DisplayCreateButton();
	}

	private void DisplayName() {
		GUI.Label(new Rect(10, 10, 50, 25), "Name");
		_toon.Name = GUI.TextField(new Rect(65, 10, 100, 25), _toon.Name);
	}

	private void DisplayAttributes() {
		for(int cnt = 0; cnt < Enum.GetValues(typeof(AttributeName)).Length; cnt++) 
		{
			GUI.Label( new Rect(OFFSET, POSITION_Y + cnt * LINE_HEIGHT, STAT_LABEL_WIDTH, LINE_HEIGHT), ((AttributeName)cnt).ToString() );
			GUI.Label( new Rect(OFFSET+STAT_LABEL_WIDTH, POSITION_Y + cnt * LINE_HEIGHT, BASE_VALUE_LABEL_WIDTH, LINE_HEIGHT), _toon.GetPrimaryAttribute(cnt).AdjustedBaseValue.ToString() );

			//Buttons to +/- value of atttibutes
			if(GUI.Button(new Rect(OFFSET+STAT_LABEL_WIDTH+BASE_VALUE_LABEL_WIDTH, POSITION_Y + cnt * LINE_HEIGHT, BUTTON_WIDTH, BUTTON_WIDTH), "+", myStyle) && _pointsLeft > 0) {
				_toon.GetPrimaryAttribute(cnt).BaseValue++;
				_pointsLeft--;

				_toon.StatUpdate(); //when press button, update the skill value
			}
			if(GUI.Button(new Rect(OFFSET+STAT_LABEL_WIDTH+BASE_VALUE_LABEL_WIDTH + BUTTON_WIDTH, POSITION_Y + cnt * LINE_HEIGHT, BUTTON_WIDTH, BUTTON_WIDTH), "-") && _toon.GetPrimaryAttribute(cnt).BaseValue > MIN_STARTING_ATTRIBUTE_VALUE && _pointsLeft < STARTING_POINTS) {
				_toon.GetPrimaryAttribute(cnt).BaseValue--;
				_pointsLeft++;

				_toon.StatUpdate(); //when press button, update the skill value
			}
		}
	}

	private void DisplayVitals() {
		for(int cnt = 0; cnt < Enum.GetValues(typeof(VitalName)).Length; cnt++) 
		{
			GUI.Label( new Rect(OFFSET, POSITION_Y + (cnt+8) * LINE_HEIGHT, STAT_LABEL_WIDTH, LINE_HEIGHT), ((VitalName)cnt).ToString() );
			GUI.Label( new Rect(OFFSET+STAT_LABEL_WIDTH, POSITION_Y + (cnt+8) * LINE_HEIGHT, BASE_VALUE_LABEL_WIDTH, LINE_HEIGHT), _toon.GetVital(cnt).AdjustedBaseValue.ToString() );
		}
	}

	private void DisplaySkills() {
		for(int cnt = 0; cnt < Enum.GetValues(typeof(SkillName)).Length; cnt++) 
		{
			GUI.Label( new Rect(OFFSET + STAT_LABEL_WIDTH + BASE_VALUE_LABEL_WIDTH + BUTTON_WIDTH*2 + OFFSET*2, POSITION_Y + cnt * LINE_HEIGHT, STAT_LABEL_WIDTH, LINE_HEIGHT), ((SkillName)cnt).ToString().Replace("_", " ") );
			GUI.Label( new Rect(OFFSET + STAT_LABEL_WIDTH + BASE_VALUE_LABEL_WIDTH + BUTTON_WIDTH*2 + STAT_LABEL_WIDTH + OFFSET*2, POSITION_Y + cnt * LINE_HEIGHT, BASE_VALUE_LABEL_WIDTH, LINE_HEIGHT), _toon.GetSkill(cnt).AdjustedBaseValue.ToString() );
		}
	}

	private void DisplayPointsLeft() {
		GUI.Label(new Rect(255, 10, 100, 25), "Points Left: "+_pointsLeft );
	}

	private void DisplayCreateButton () {
		if( GUI.Button(new Rect(Screen.width/2 - 100, POSITION_Y + 10 * LINE_HEIGHT, STAT_LABEL_WIDTH + 60,	LINE_HEIGHT), "Create") )
		{
			GameSettings gsScript = GameObject.Find("__GameSettings").GetComponent<GameSettings>();


			//Change the current value of the vitals to the max modified value of that vital
			gsScript.SaveCharacterData();

			Application.LoadLevel("FirstTestScene");
		}
	}

}
