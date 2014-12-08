using UnityEngine;
using System.Collections;

public class BaseStat : MonoBehaviour {	
	private int _baseValue;			// the base value of this stat
	private int _buffValue;			// the amount fo th buff to this stat
	private int _expToLevel;		// the total amount of exp needed to raise this skill
	private float _levelModifier;	// the modifier applied tothe exp needed to rails this skill

	public BaseStat () {
		_baseValue = 0;
		_buffValue = 0;
		_levelModifier = 1.1f;
		_expToLevel = 100;
	}


	public int BaseValue {
		get{ return _baseValue; }
		set{ _baseValue = value; }
	}

	public int BuffValue {
		get{ return _buffValue; }
		set{ _buffValue = value; }
	}

	public int ExpToLevel {
		get{ return _expToLevel; }
		set{ _expToLevel = value; }
	}

	public float LevelModifier {
		get{ return _levelModifier; }
		set{ _levelModifier = value; }
	}


	private int CalculateExpToLevel() {
		return (int)(_expToLevel * _levelModifier);
	}

	public void LevelUp() {
		_expToLevel = CalculateExpToLevel ();
		_baseValue++;
	}

	public int AdjustedValue() {
		return _baseValue + _buffValue;
	}
}
