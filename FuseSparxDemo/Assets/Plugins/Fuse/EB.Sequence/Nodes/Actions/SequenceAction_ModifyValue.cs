using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Modify Value")]
public class SequenceAction_ModifyValue : Action
{
	[Variable(ExpectedType=typeof(float))]
	public Variable Value = Variable.Null;
	
	[EB.Sequence.Property]
	public float IncDecVal = 1.0F;
	
	[Entry]
	public void Increment()
	{
		Modify( IncDecVal );
		Activate();
	}
	
	[Entry]
	public void Decrement()
	{
		Modify( -IncDecVal );
		Activate();
	}
	
	[Entry]
	public void Zero()
	{
		Value.Value = (float)0;
		Activate();
	}
	
	public override bool Update ()
	{
		Finished.Invoke();
		return false;
	}
	
	private void Modify( float by )
	{
		float value = (float)Value.Value;
		
		EB.Debug.Log("Original Value:" + value);
		
		value += by;
		Value.Value = value;
		
		EB.Debug.Log("Modified Value:" + value);
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}
