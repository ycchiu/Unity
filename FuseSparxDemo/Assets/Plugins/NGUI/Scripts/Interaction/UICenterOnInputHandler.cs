using UnityEngine;
using System.Collections.Generic;

public class UICenterOnInputHandler : UICenterOnChild
{
	public bool recenterOnEnable = true;

	protected override void OnEnable()
	{
		if (recenterOnEnable)
		{
			base.OnEnable();
		}
	}

	protected override List<Transform> GetCenterOnItems()
	{
		Component[] inputHandlers = EB.Util.FindAllComponents(gameObject, typeof(ControllerInputHandler));
		List<Transform> children = new List<Transform>();
		
		foreach (Component handler in inputHandlers)
		{
			if (handler.gameObject != gameObject)
			{
				children.Add(handler.transform);
			}
		}
		
		return children;
	}
}
