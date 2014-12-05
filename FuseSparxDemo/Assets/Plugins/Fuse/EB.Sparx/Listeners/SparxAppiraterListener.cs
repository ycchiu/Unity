using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface AppiraterListener
	{
		void OnDisplay();
		void OnRated();
		void OnDeclined();
		void OnRemind();
	}
	
}

