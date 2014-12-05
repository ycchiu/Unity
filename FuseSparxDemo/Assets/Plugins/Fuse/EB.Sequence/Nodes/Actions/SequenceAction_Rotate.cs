using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Rotate")]
public class SequenceAction_Rotate : Action
{
	[EB.Sequence.Property]
	public float X = 0.0f;

	[EB.Sequence.Property]
	public float Y = 0.0f;
	
	[EB.Sequence.Property]
	public float Z = 0.0f;
		
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[Entry]
	public void Rotate()
	{
		GameObject go = (GameObject)Target.Value;
		if ( go == null )
		{
			EB.Debug.LogError("No Target to rotate");
			return;
		}
		
		go.transform.rotation = Quaternion.Euler(X,Y,Z);
	}
	
}
