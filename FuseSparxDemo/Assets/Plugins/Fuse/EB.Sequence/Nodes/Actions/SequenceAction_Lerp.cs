using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Lerp")]
public class SequenceAction_Lerp : Action 
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable From = Variable.Null;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable To = Variable.Null;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[EB.Sequence.Property]
	public float Duration = 1.0f;
	
	[EB.Sequence.Property]
	public bool useScale = false;

	[EB.Sequence.Property]
	public float scaleTarget = 1.0F;

	private float _t = 0.0f;
	
	[Entry]
	public void Start()
	{
		_t = 0.0f;
		Duration = Mathf.Max( Duration, 0.01f );
		this.Activate();
	}
	
	public override bool Update()
	{
		_t += Time.deltaTime;
		
		float t = Mathf.Clamp01( _t / Duration );
		Lerp(t);
		if ( t > 0.99f )
		{
			Finished.Invoke();
			return false;
		}
		
		return true;
	}
	
	void Lerp( float t )
	{
		GameObject from = (GameObject)From.Value;
		GameObject to = (GameObject)To.Value;
		GameObject target = (GameObject)Target.Value;
				
		if ( target != null && from != null && to != null )
		{
			target.transform.position = Vector3.Lerp( from.transform.position, to.transform.position, t );
			target.transform.rotation = Quaternion.Slerp( from.transform.rotation, to.transform.rotation, t );
		}
	
		if (useScale)
		{
			if (target != null && from != null )
			{
				target.transform.localScale = Vector3.Lerp ( from.transform.localScale, new Vector3(scaleTarget,scaleTarget,scaleTarget), t);
			}
			else
			{
				EB.Debug.Log ("SequenceAction_Lerp - scale cannot be used - target or from missing");
			}
		}
	}
	
	
	[Trigger]
	public Trigger Finished = new Trigger();
}
