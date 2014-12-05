using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Set Position")]
public class SequenceAction_SetPosition : Action 
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[EB.Sequence.Property]
	public Vector3 _newPosition = Vector3.zero;
	
	[EB.Sequence.Property]
	public Vector3 _newRotation = Vector3.zero;
	
	[EB.Sequence.Property]
	public bool _local = false;
	
	[EB.Sequence.PropertyAttribute]
	public bool _setRotation = false;
	
	
	[Entry]
	public void Start()
	{
		if (Target.Value != null)
		{
			GameObject target = (GameObject)Target.Value;
			
			if(_local)
			{
				target.transform.localPosition = _newPosition;
			}
			else
			{
				target.transform.position = _newPosition;
			}
			
			if (_setRotation)
			{
				target.transform.localEulerAngles = _newRotation;
			}
		}
		Finished.Invoke();
	}

	[Trigger]
	public Trigger Finished = new Trigger();
}
