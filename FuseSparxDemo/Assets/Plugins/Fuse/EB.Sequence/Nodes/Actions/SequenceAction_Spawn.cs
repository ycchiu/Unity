using UnityEngine;
using System.Collections.Generic;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Spawn")]
public class SequenceAction_Spawn : Action
{		
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Where = Variable.Null;
	
	[Variable(ExpectedType=typeof(GameObject), Direction=Direction.Out)]
	public Variable Spawned = Variable.Null;
	
	[EB.Sequence.PropertyAttribute]
	public GameObject Prefab = null;
	
	[EB.Sequence.PropertyAttribute]
	public int Number = 1;
	
	[Entry]
	public void TestMe()
	{
		Finished.Invoke();
	}
	
	public override bool Update()
	{
		
		return false;
	}
	
	
	[Entry]
	public void Spawn()
	{	
		var locator = Where.GetValue<GameObject>();
		if (!locator)
		{
			Debug.LogError("Missing locator!");
		}
		
		Spawned.Value  = (GameObject)Object.Instantiate(Prefab, locator.transform.position, locator.transform.rotation);
		
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}

