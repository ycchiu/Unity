using System.Collections.Generic;
using UnityEngine;

public class ModifiedStat : BaseStat {
	private List<ModifyingAttribute> _mods;		//A listof Attributes that modify this stat
	private int _modValue;						//The amount added to the baseValue from the modifiers

	public ModifiedStat() {
		_mods = new List<ModifyingAttribute> ();
		_modValue = 0;
	}

	public void AddModifier(ModifyingAttribute mod) {
		_mods.Add (mod);
	}

	public void Update() {
		CalculateModValue();
	}

	public string GetModifyingAttributesString () {
		string temp;

		for(int cnt = 0; cnt < _mods.Count; cnt++) {
			UnityEngine.Debug.Log (">> ModifiedStat.cs,  _mods[" + cnt + "]: " + _mods[cnt].attribute.Name + " = " + _mods[cnt] );
		}

		return "";
	}

	public new int AdjustedBaseValue {
		get{ return BaseValue + _modValue; }
	}



	private void CalculateModValue() {
		_modValue = 0;

		if (_mods.Count > 0) {
			foreach(ModifyingAttribute att in _mods){ 
				_modValue += (int)(att.attribute.AdjustedBaseValue * att.ratio);
			}
		}
	}
}

public struct ModifyingAttribute {
	public Attribute attribute;
	public float ratio;

	public ModifyingAttribute(Attribute att, float r) {
		attribute = att;
		ratio = r;
	}
}
