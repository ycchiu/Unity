using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Conditions/Game Object Equality")]
public class SequenceCondition_GameObjectEquality : Condition
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable A = Variable.Null;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable B = Variable.Null;
	
	public override bool Update ()
	{
		object objA = A.Value;
		object objB = B.Value;

		if ( objA!= null && objB != null )
		{
			GameObject goA = (GameObject)objA;
			GameObject goB = (GameObject)objB;

			//EB.Debug.Log("Equality test: A:"+numA+" B:"+numB);
		
			if (goA == goB)
			{
				Check1.Invoke();
			}

			if (goA != goB)
			{
				Check2.Invoke();
			}
		}
		else
		{
			EB.Debug.Log("Equality test failed, game object A or B is null - Parent:"+Parent.name);
		}
		return false;
	}
	
	[Trigger(EditorName="A == B")]
	public Trigger Check1 = new Trigger();

	[Trigger(EditorName="A != B")]
	public Trigger Check2 = new Trigger();
}


