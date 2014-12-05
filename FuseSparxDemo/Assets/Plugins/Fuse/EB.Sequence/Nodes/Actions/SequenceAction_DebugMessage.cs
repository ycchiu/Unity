using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Print Debug Message")]
public class SequenceAction_DebugMessage : Action
{
	[EB.Sequence.Property]
	public string Text = string.Empty;
	
	[Entry]
	public void Fire()
	{
		EB.Debug.LogWarning(Text);
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
	
}
