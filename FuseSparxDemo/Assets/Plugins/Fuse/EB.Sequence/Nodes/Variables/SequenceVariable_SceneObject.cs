using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/Scene Object",VariableType=typeof(UnityEngine.GameObject))]
public class SequenceVariable_SceneObject : Variable
{
	[EB.Sequence.Property]
	public string Name = string.Empty;
	
	public override object Value 
	{
		get 
		{
			return UnityEngine.GameObject.Find(Name);
		}
	}
}
