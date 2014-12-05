using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Get Gacha Token Count")]
public class SequenceAction_GetGachaTokenCount : Action 
{
	[Variable(ExpectedType=typeof(float))]
	public Variable Count = Variable.Null;
	
	[EB.Sequence.Property]
	public string tokenName = string.Empty;
	
	[Entry]
	public void Sync()
	{
		if(!string.IsNullOrEmpty(tokenName))
		{
			if(!Count.IsNull)
			{
				int tokenCount = SparxHub.Instance.GachaManager.GetTokenCount( tokenName );	
				Count.Value = (float)tokenCount;
			
			}

			Out.Invoke();
		}
		else
		{
			if(!Count.IsNull)
			{
				Count.Value = 0f;
			}
			Out.Invoke();
		}
	}
	
	
	[Trigger(EditorName="Out")]
	public Trigger Out = new Trigger();
}
