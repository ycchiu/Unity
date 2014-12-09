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

	private void CalculateModValue() {
		_modValue = 0;

		if (_mods.Count > 0) {
			foreach(ModifyingAttribute att in _mods){ 
				_modValue += (int)(att.attribute.AdjustedBaseValue * att.ratio);
				Debug.Log("============   att.attribute = " + att.attribute.AdjustedBaseValue + "  ratio = " + att.ratio + "  modValue =" + _modValue);
			}
			Debug.Log("\n");
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
