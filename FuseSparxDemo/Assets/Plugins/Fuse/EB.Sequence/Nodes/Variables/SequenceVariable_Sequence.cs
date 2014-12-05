using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/Sequence", VariableType=typeof(UnityEngine.GameObject))]
public class SequenceVariable_Sequence : Variable
{
	[EB.Sequence.Property(Hint="Sequence",NonEditable=true)]
	public string SequenceId = string.Empty;
	
	private GameObject sequenceObject;
	
	public override object Value 
	{
		get 
		{
			if (sequenceObject == null)
			{
				sequenceObject = GameObject.Find(SequenceId);
				
				if (sequenceObject == null)
				{
					EB.Debug.Log("SEQUENCEVARIABLE_SEQUENCE ERROR - CANNOT FIND SEQUENCE "+SequenceId);
					return null;
				}
			}
			
			return sequenceObject;
			
//			GameObject go = GameObject.Find(SequenceId);
//			
//			EB.Debug.Log(SequenceId);
//			
//			if (go != null )
//			{
//				var sq = go.GetComponent<EB.Sequence.Component>();
//				
//				if (sq==null)
//				{
//					throw new Exception(this, "ERROR - SEQUENCE VARIABLE NOT A VALID SEQUENCE - SEQUENCE ID:"+SequenceId);
//				}
//				
//			}
//			else
//			{
//				EB.Debug.Log("SEQUNVARIABLE_SEQUENCE ERROR - CANNOT FIND SEQUENCE "+SequenceId);
//			}
//			
//			return go;
		}		
		set
		{
			sequenceObject = (GameObject) value;
			SequenceId = sequenceObject.name;
		}
	}

}
