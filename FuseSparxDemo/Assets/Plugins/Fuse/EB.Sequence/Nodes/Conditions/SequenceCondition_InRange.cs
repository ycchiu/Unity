using UnityEngine;
using System.Collections;
using EB.Sequence.Runtime;
using EB.Sequence;

[MenuItem(Path="Conditions/In Range")]
public class SequenceCondition_InRange : Condition 
{
	[EB.Sequence.Property]
	public float Distance = 0.0f;
	
	[EB.Sequence.PropertyAttribute]
	public bool OnlyXZ = true;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable A = Variable.Null;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable B = Variable.Null;
	   
	public override bool Update()
	{
		GameObject goA = (GameObject)A.Value;
		GameObject goB = (GameObject)B.Value;
		
		if ( goA == null )
		{
			EB.Debug.LogError("SequenceCondition_Distance: A is null");
			return false;
		}
		
		if ( goB == null )
		{
			EB.Debug.LogError("SequenceCondition_Distance: B is null");
			return false;
		}
		
		var difference = goA.transform.position - goB.transform.position;
		
		if ( OnlyXZ )
		{
			difference.y = 0;
		}
		
		float distance = difference.magnitude;
		if ( distance <= Distance )
		{
			InRange.Invoke();
		}
		else
		{
			OutOfRange.Invoke();
		}
		return false;
	}
	
	[Trigger]
	public Trigger InRange = new Trigger();
	
	[Trigger]
	public Trigger OutOfRange = new Trigger();
}
