using UnityEngine;
using System.Collections;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Load Bundle")]
public class SequenceAction_LoadBundle : Action
{
	[EB.Sequence.Property]
	public string BundleName = string.Empty;
	
	[Entry]
	public void Load()
	{
		EB.Assets.LoadBundle(BundleName);
		Activate();
	}
	
	public override bool Update ()
	{
		if ( !EB.Assets.IsBundleLoaded(BundleName) )
		{
			return true;
		}
		
		Finished.Invoke();
		return false;
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}
