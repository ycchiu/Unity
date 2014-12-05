using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Send Message")]
public class SequenceAction_SendMessage : Action
{
	[EB.Sequence.Property]
	public string functionName = string.Empty;

	[Variable(ExpectedType=typeof(GameObject))]
	public Variable target = Variable.Null;
	
	[Variable]
	public Variable arg = Variable.Null;
	
	[Entry]
	public void Fire()
	{
		GameObject go = (GameObject)target.Value;
		
		if ( go != null && arg.Value != null )
		{
			go.SendMessage(functionName, arg.Value, SendMessageOptions.RequireReceiver);			
		}
		else if (go != null)
		{
			go.SendMessage(functionName, SendMessageOptions.RequireReceiver);
		}
		else	
		{
			if (go == null)
			{
				EB.Debug.LogWarning("GameObject is null at: " + this.ToString());
			}
			else if (arg.Value == null)
			{
				EB.Debug.LogWarning("Argument is null at: " + this.ToString());
			}
		}
	
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
	
}
