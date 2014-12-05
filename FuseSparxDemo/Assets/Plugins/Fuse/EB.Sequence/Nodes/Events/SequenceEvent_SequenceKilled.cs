using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Events/Sequence Killed")]
public class SequenceEvent_SequenceKilled : EB.Sequence.Runtime.Event
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Sequence = Variable.Null;
	
	public override bool CheckActivate (GameObject instigator, object target)
	{
		if ( Sequence.IsNull )
		{
			return instigator == Parent.gameObject;
		}
		
		if ( Utils.Filter(Sequence, instigator) )	
		{
			return true;
		}
		
		return false;
	}
}
