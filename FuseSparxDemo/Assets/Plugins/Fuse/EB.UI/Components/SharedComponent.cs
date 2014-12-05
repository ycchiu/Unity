using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SharedComponent : MonoBehaviour
{
	// Specifies which monobehaviours in the shared component should be serialized.
	public List<MonoBehaviour> serializedComponents;
}
