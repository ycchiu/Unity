using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Events/Game Event")]
public class SequenceEvent_GameEvent : Event
{
	[EB.Sequence.Property]
	public string eventName = string.Empty;
	
	[Variable(ExpectedType=typeof(UnityEngine.GameObject), Direction=Direction.Out)]
	public Variable Instigator = Variable.Null;
	
	public override bool CheckActivate (UnityEngine.GameObject instigator, object target)
	{
		if ( target.ToString() == eventName )			
		{
			Instigator.Value = instigator;
			return true;
		}
		return false;
	}	
}
