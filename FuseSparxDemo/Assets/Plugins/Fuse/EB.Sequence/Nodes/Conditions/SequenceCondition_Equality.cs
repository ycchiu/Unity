using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Conditions/Equality")]
public class SequenceCondition_Equality : Condition
{
	[Variable(ExpectedType=typeof(float))]
	public Variable A = Variable.Null;
	
	[Variable(ExpectedType=typeof(float))]
	public Variable B = Variable.Null;
	
	public override bool Update ()
	{
		object objA = A.Value;
		object objB = B.Value;

		if ( objA!= null && objB != null )
		{
			float numA = (float)objA;
			float numB = (float)objB;

			//EB.Debug.Log("Equality test: A:"+numA+" B:"+numB);
		
			if (numA == numB)
			{
				Check1.Invoke();
			}

			if (numA > numB )
			{
				Check2.Invoke();
			}
			
			if (numA < numB )
			{
				Check3.Invoke();
			}

			if (numA >= numB )
			{
				Check4.Invoke();
			}

			if (numA <= numB )
			{
				Check5.Invoke();
			}
			
			if (numA!=numB)
			{
				Check6.Invoke();			
			}
		}
		else
		{
			EB.Debug.Log("Equality test failed, item A or B is null - Parent:"+Parent.name);
		}
		return false;
	}
	
	[Trigger(EditorName="A == B")]
	public Trigger Check1 = new Trigger();

	[Trigger(EditorName="A > B")]
	public Trigger Check2 = new Trigger();

	[Trigger(EditorName="A < B")]
	public Trigger Check3 = new Trigger();

	[Trigger(EditorName="A >= B")]
	public Trigger Check4 = new Trigger();

	[Trigger(EditorName="A <= B")]
	public Trigger Check5 = new Trigger();

	[Trigger(EditorName="A != B")]
	public Trigger Check6 = new Trigger();
}


