using UnityEngine;
using System.Collections.Generic;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Set Value to Number")]
public class SequenceAction_SetValueToNumber : Action
{		
	[Variable(ExpectedType=typeof(float))]
	public Variable SetThisNumber = Variable.Null;
	
	[Variable(ExpectedType=typeof(float))]
	public Variable ToThisNumber = Variable.Null;
	
	[EB.Sequence.Property]
	public bool clamp = false;
	
	[EB.Sequence.Property]
	public float minValue = 0f;
	
	[EB.Sequence.Property]
	public float maxValue = 0f;
	
	[Entry]
	public void Set()
	{	
		float newValue = (float)ToThisNumber.Value;
		
		if (clamp) newValue = Mathf.Clamp(newValue, minValue, maxValue);
		
		//EB.Debug.LogWarning("NEW VALUE IS " + newValue);
		SetThisNumber.Value = newValue;
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}

