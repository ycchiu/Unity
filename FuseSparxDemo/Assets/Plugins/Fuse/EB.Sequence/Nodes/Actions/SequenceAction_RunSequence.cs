using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Run Sequence")]
public class SequenceAction_RunSequence : Action
{                                                               
	[EB.Sequence.Property(Hint="Sequence",NonEditable=true)]
	public string sequenceName = string.Empty;
	
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Sequence = Variable.Null;
	
	GameObject sequence;
	bool isRun = false;
	
	[Entry]
	public void RunSequence()
	{
		var prefab = Resources.Load("Prefab_Sequences/" + sequenceName);//EB.Assets.Load("Bundles/Prefab_Sequences/" + sequenceName);
	   if(prefab != null)
	   {
	        sequence = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
	        sequence.name = prefab.name;
			
			if (GameObject.Find("Sequences") == null)
			{
				GameObject seqs = new GameObject("Sequences");
			}
			sequence.transform.parent = GameObject.Find("Sequences").transform;
	       	//EB.Debug.Log("Instantiated sequence " + sequence.name);
	       	isRun = true;
			Started.Invoke();
			
			if (Sequence.IsNull)
			{
				return;
			}
			else
			{
				//SequenceVariable_Sequence theSequence = Sequence.Value as SequenceVariable_Sequence;
				Sequence.Value = sequence;
			}
			
	   }
	   else
	   {
	       EB.Debug.LogError("Unable to find sequence " + sequenceName + " in Prefab_Sequences");
	   }
	}
	       
	public override bool Update()
	{  
		if(sequence == null && isRun)
		{
			Finished.Invoke();
	        return false;
	    }
	    return true;
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
	
	[Trigger]
	public Trigger Started = new Trigger();
}