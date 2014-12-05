using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/New Random")]
public class SequenceAction_NewRandom : Action
{
    [EB.Sequence.Property]
    public int[] PossibleOutcomes = new int[0];

	[Variable(ExpectedType=typeof(float))]
	public Variable Number = Variable.Null;

    [Entry]
    public void PickANewOne()
    {
		if (Number != null)
		{
	      	int lastNumber = (int)((float)Number.Value);
			
			int newNumber = lastNumber;
			
			while (newNumber == lastNumber)
			{
				newNumber = Random.Range(0, PossibleOutcomes.Length);
			}
			
			Number.Value = (float)newNumber;
			
			Finished.Invoke();
		}
		else
		{
			EB.Debug.LogError("Number is null. Parent: " + this.ToString());
		}
    }

    [Trigger]
    public Trigger Finished = new Trigger();

}
