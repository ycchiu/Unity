using UnityEngine;
using System.Collections;

public interface UIDependency
{
	EB.Action onReadyCallback
	{
		get;
		set;
	}
	
	EB.Action onDeactivateCallback
	{
		get;
		set;
	}
	
	bool IsReady();
}
