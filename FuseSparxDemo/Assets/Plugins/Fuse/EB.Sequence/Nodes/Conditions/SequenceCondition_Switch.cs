using EB.Sequence.Runtime;
using EB.Sequence;

[MenuItem(Path="Conditions/Switch")]
public class SequenceCondition_Switch : Condition
{
	[Variable(ExpectedType=typeof(object))]
	public Variable Value = Variable.Null;
	
	[EB.Sequence.Property]
	public string[] TestValues = new string[0];
	
	public override void Init ()
	{
		base.Init ();
		
		Triggers = new Trigger[TestValues.Length];
		for ( int i = 0; i < Triggers.Length; ++i )
		{
			Triggers[i] = new Trigger();
		}
		
	}
		
	public override bool Update ()
	{
		string value = Value.Value.ToString();
		
		for ( int i = 0; i < TestValues.Length; ++i )
		{
			if ( TestValues[i] == value )
			{
				Triggers[i].Invoke();
				return false;
			}
		}
		
		Default.Invoke();
		
		return false;
	}
	
	[Trigger(Show=false)]
	public Trigger[] Triggers = new Trigger[0];
			
	[Trigger(EditorName="Default")]
	public Trigger Default = new Trigger();

}