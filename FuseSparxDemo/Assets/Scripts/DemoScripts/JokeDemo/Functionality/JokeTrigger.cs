using UnityEngine;
using System.Collections;

public class JokeTrigger : MonoBehaviour 
{
	public void OnClick() {
		EB.Sparx.JokeManager.instance.RequestJoke ();
	}
}