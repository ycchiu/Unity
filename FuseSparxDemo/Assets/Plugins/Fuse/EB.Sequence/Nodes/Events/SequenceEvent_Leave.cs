using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Events/Leave")]
public class SequenceEvent_Leave : Event
{
	[Variable(ExpectedType=typeof(UnityEngine.GameObject))]
	public Variable Source = Variable.Null;
	
	[Variable(ExpectedType=typeof(UnityEngine.GameObject))]
	public Variable Target = Variable.Null;
	
	[Variable(ExpectedType=typeof(UnityEngine.GameObject), Direction=Direction.Out)]
	public Variable SourceOut = Variable.Null;
	
	[Variable(ExpectedType=typeof(UnityEngine.GameObject), Direction=Direction.Out)]
	public Variable TargetOut = Variable.Null;

	public override bool CheckActivate (UnityEngine.GameObject instigator, object target)
	{
		if ( CheckFilter(instigator,Source) && CheckFilter(target as UnityEngine.GameObject,Target) )
		{
			SourceOut.Value = instigator;
			TargetOut.Value = target;
			return true;
		}
		
		return false;
	}
	
	bool CheckFilter( UnityEngine.GameObject go, Variable filter )
	{
		if ( filter.IsNull ) 
		{
			return true;
		}
		
		if ( Utils.Contains( filter.Value, go ) ) 
		{
			return true;	
		}
		
		return false;
	}
	
}
