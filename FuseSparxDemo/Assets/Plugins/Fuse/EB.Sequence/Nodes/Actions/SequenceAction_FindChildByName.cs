using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Find Child By Name")]
public class SequenceAction_FindChildByName : Action
{
	[EB.Sequence.Variable(ExpectedType=typeof(GameObject))]
	public Variable Parent = new Variable();
	
	[EB.Sequence.Variable(ExpectedType=typeof(GameObject))]
	public Variable Child = new Variable();
	
	[EB.Sequence.Property]
	public string Name = "";
	
	[Trigger(EditorName="Out")]
	public Trigger Out = new Trigger();
	
	[Entry]
	public void In()
	{
		GameObject parent = Parent.GetValue<GameObject>();
		
		if (parent != null)
		{
			Child.Value = EB.Util.GetObjectExactMatch(Parent.GetValue<GameObject>(), Name);
		}
		
		Out.Invoke();
	}
}
