using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/Locator",VariableType=typeof(GameObject))]
public class SequenceVariable_Locator : Variable
{
	[EB.Sequence.Property(Hint="Scene")]
	public string targetScene = string.Empty;
			
	[EB.Sequence.Property(Description="Locator Name",Hint="Locator")]
	public string LocatorName = string.Empty;
	
	public override object Value 
	{
		get
		{
//			EB.Debug.Log("Name: " + LocatorName );
			return GameObject.Find(LocatorName);
		}
		set 
		{
			throw new Exception(this,"Invalid Operation");
		}
	}
}
