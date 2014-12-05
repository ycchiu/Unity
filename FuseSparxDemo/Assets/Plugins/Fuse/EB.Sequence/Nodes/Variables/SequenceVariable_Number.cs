using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/Number", VariableType=typeof(float))]
public class SequenceVariable_Number : Variable
{
	[EB.Sequence.Property]
	public float IntialValue = 0.0f;
	
	[EB.Sequence.Property]
	public string Name = string.Empty;
	
	private float? _value = null;
	
	public bool IsGlobal { get { return string.IsNullOrEmpty(Name) == false; } }
	
	public override object Value 
	{
		get 
		{
			if ( IsGlobal )
			{
				return (float)SequenceVariableManager.Instance.GetNumber( Name, IntialValue); 
			}
			else
			{
				return _value ?? IntialValue;
			}
			
		}
		set 
		{
			if ( IsGlobal )
			{
				SequenceVariableManager.Instance.SetVariable(Name, (float)value ); 
			}
			else
			{
				_value = (float)value;
				ValueChanged();
			}
		}
	}
	
	public override void Init ()
	{
		base.Init ();
		if ( IsGlobal )
		{
			EB.Debug.LogWarning("SequenceVariable_Number Sequence: "+Parent.name+" Name:"+Name);
			
			if (SequenceVariableManager.Instance==null)
			{
				EB.Debug.LogWarning ("SEQUENCE VAR MANAGER IS NULL!");
			}
			SequenceVariableManager.Instance.AddNode(this, Name);
		}
	}
	
	public override void Dispose ()
	{
		if ( IsGlobal )
		{
			SequenceVariableManager.Instance.RmvNode(this, Name);
		}
		base.Dispose ();
	}
	
	[Entry]
	public void Inc()
	{
		Value = (float)Value + 1;
	}
	
	[Entry]
	public void Dec()
	{
		Value = (float)Value - 1;
	}
	
}
