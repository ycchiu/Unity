using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Events/Sequence Started")]
public class SequenceEvent_SequenceStarted : EB.Sequence.Runtime.Event
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Sequence = Variable.Null;
	
	public override bool CheckActivate (GameObject instigator, object target)
	{
		if ( Sequence.IsNull )
		{
			if ( instigator == Parent.gameObject )
			{
				//Debug.Log("Active: " + instigator);
				return true;
			}
			return false;
		}
		
		if ( Utils.Filter(Sequence, instigator) )	
		{
			return true;
		}
		
		return false;
	}
}
