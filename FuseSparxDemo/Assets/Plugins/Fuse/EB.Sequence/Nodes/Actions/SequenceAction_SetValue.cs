using UnityEngine;
using System.Collections.Generic;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Set Value")]
public class SequenceAction_SetValue : Action
{	
	[EB.Sequence.Property]
	public int value;
		
	[Variable(ExpectedType=typeof(float))]
	public Variable Target = Variable.Null;
	
	[Entry]
	public void Set()
	{	
		Target.Value = (float)value;
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}

