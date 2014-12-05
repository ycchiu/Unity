using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Print")]
public class SequenceAction_Print : Action
{
    [EB.Sequence.Property]
	public string Message = string.Empty;
	
	[EB.Sequence.PropertyAttribute]
	public int Test = 1;
	
	[Variable(ExpectedType=typeof(float))]
	public Variable V = Variable.Null;
	
	[EntryAttribute]
	public void Test2()
	{
		var myfloat = V.GetValue<float>();
		Activate();
	}
	
    [Entry]
    public void Print()
    {
		EB.Debug.Log(Message);
		Finished.Invoke();
    }

    [Trigger]
    public Trigger Finished = new Trigger();

}
