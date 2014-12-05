using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Fire Game Event")]
public class SequenceAction_FireGameEvent : Action
{
	[EB.Sequence.Property]
	public string GameEventName = "";
	
	[Trigger(EditorName="Out")]
	public Trigger Out = new Trigger();
	
	[Entry]
	public void FireGameEvent()
	{
		//EB.Debug.Log("SEQUENCE ACTION: FIRE GAME EVENT - " + GameEventName + " from: " + Parent.name + " at: " + Time.time);
		
		EB.Sequence.Runtime.Event.Activate(null, GameEventName, typeof(SequenceEvent_GameEvent));	
		
		Out.Invoke();
	}
}
