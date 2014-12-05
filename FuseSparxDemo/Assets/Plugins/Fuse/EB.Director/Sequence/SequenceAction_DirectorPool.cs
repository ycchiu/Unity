using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Director Pool")]
public class SequenceAction_DirectorPool : Action
{
	[EB.Sequence.Property]
	public GameObject[] DirectorData = null;
	
	[Entry]
	public void Populate()
	{	
		GameObject _directorParent = GameObject.Find("Directors");
		
		if (_directorParent != null) GameObject.Destroy(_directorParent);
		
		_directorParent = new GameObject("Directors");
		
		int count = 0;
		
		DirectorInformation.LocalToWorldDirectors.Clear();
		
		foreach(GameObject dir in DirectorData)	
		{
			if (dir != null)
			{
				GameObject newDir = (GameObject) GameObject.Instantiate(dir);
				newDir.transform.parent = _directorParent.transform;
				newDir.name = dir.name;
				newDir.GetComponent<EB.Director.Component>().enabled = false;
				
				//EB.Debug.Log("Added Director: " + newDir.name + " to pool.");
				
				count++;
			}
		}	
		
		//EB.Debug.Log("Added: " + count + " directors to the pool.");
		
		Out.Invoke();
	}

	[Trigger]
	public Trigger Out = new Trigger();
}
