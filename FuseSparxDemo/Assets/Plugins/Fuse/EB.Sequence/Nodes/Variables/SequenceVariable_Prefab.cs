using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/Prefab", VariableType=typeof(UnityEngine.GameObject))]
public class SequenceVariable_Prefab : Variable
{
	[EB.Sequence.Property]
	public UnityEngine.GameObject Prefab = null;
	
	public override object Value 
	{
		get 
		{
			return Prefab;
		}
		set 
		{
			throw new Exception(this,"Invalid Operation");
		}
	}
	
	
}
