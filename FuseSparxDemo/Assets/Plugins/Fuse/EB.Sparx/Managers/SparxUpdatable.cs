using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface Updatable
	{
		void Update();
		bool UpdateOffline {get;}
	}
}