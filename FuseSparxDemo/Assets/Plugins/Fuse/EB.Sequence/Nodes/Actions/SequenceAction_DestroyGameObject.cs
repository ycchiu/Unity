using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Destroy GameObject")]
public class SequenceAction_DestroyGameObject : Action
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[Entry]
	public void Destroy()
	{
		GameObject go = (GameObject)Target.Value;
		if ( go == null )
		{
			EB.Debug.LogWarning("No Target to destroy. Sequence: " + this.Parent.name);
			Out.Invoke();
			return;
		}
		
		//EB.Debug.Log("DESTROYING: " + go.name + " at: " + Time.time);
		GameObject.Destroy(go);
		Out.Invoke();
	}
	
	[Trigger(EditorName="Out")]
	public Trigger Out = new Trigger();
	
}
