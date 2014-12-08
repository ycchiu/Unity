using UnityEngine;
using System.Collections;
using System;				//added to access the enum class

public class BaseCharacter : MonoBehaviour {
	private string _name;
	private int _level;
	private uint _freeExp;

	private Attribute[] _primaryAttribute;
	private Vital[] _vital;
	private Skill[] _skill;

	public void Awake() {
		_name = string.Empty;
		_level = 0;
		_freeExp = 0;

		_primaryAttribute = new Attribute[Enum.GetValues(typeof(AttributeName)).Length];
		_vital = new Vital[Enum.GetValues(typeof(VitalName)).Length];
		_skill = new Skill[Enum.GetValues(typeof(SkillName)).Length];

		SetupPrimaryAttributes();
		SetupVitals();
		SetupSkills();
	}


	public string Name {
		set{ _name = value; }
		get{ return _name; }
	}

	public int Level {
		set{ _level = value; }
		get{ return _level; }
	}

	public uint FreeExp {
		set{ _freeExp = value; }
		get{ return _freeExp; }
	}

	public void AddExp(uint exp) {
		_freeExp += exp;
	}

	//Take Ave all of the players skills and assign that as the player level
	public void CalculateLevel() {
	}

	public Attribute GetPrimaryAttribute( int index ) {
		return _primaryAttribute[index];
	}

	public Skill GetSkill(int index) {
		return _skill[index];
	}

	public Vital GetVital(int index) {
		return _vital[index];
	}

	public void StatUpdate() {
		for(int cnt =0; cnt < _vital.Length; cnt++) {
			_vital[cnt].Update();
		}

		for(int cnt =0; cnt < _skill.Length; cnt++) {
			_skill[cnt].Update();
		}
	}





	private void SetupPrimaryAttributes() {
		for( int cnt = 0; cnt < _primaryAttribute.Length; cnt++ ) {
			_primaryAttribute[cnt] = new Attribute();
		}
	}
	
	private void SetupVitals() {
		for( int cnt = 0; cnt < _vital.Length; cnt++ ) {
			_vital[cnt] = new Vital();
		}
	}
	
	private void SetupSkills() {
		for( int cnt = 0; cnt < _skill.Length; cnt++ ) {
			_skill[cnt] = new Skill();
		}
	}


	private void SetupVitalModifier() {
		//health
		GetVital((int)VitalName.Health).AddModifier( new ModifyingAttribute( GetPrimaryAttribute((int)AttributeName.Constitution), 0.5f) );

		//energy
		GetVital((int)VitalName.Energy).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Constitution), 1.0f) );

		//mana
		GetVital((int)VitalName.Mana).AddModifier( new ModifyingAttribute( GetPrimaryAttribute((int)AttributeName.Willpower), 1.0f) );
	}

	private void SetupSkillModifier() {
		//Melee Offence
		GetSkill((int)SkillName.Melee_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Might), 0.33f) );
		GetSkill((int)SkillName.Melee_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Nimbleness), 0.33f) );

		//Melee Defence
		GetSkill((int)SkillName.Melee_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Speed), 0.33f) );
		GetSkill((int)SkillName.Melee_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Constitution), 0.33f) );

		//Magic offence
		GetSkill((int)SkillName.Magic_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Concentration), 0.33f) );
		GetSkill((int)SkillName.Magic_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Willpower), 0.33f) );

		//Magic Defence
		GetSkill((int)SkillName.Magic_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Concentration), 0.33f) );
		GetSkill((int)SkillName.Magic_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Willpower), 0.33f) );

		//Ranged Offence
		GetSkill((int)SkillName.Ranged_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Concentration), 0.33f) );
		GetSkill((int)SkillName.Ranged_Offence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Speed), 0.33f) );

		//Ranged Defence
		GetSkill((int)SkillName.Ranged_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Speed), 0.33f) );
		GetSkill((int)SkillName.Ranged_Defence).AddModifier( new ModifyingAttribute(GetPrimaryAttribute((int)AttributeName.Nimbleness), 0.33f) );
	}


}
