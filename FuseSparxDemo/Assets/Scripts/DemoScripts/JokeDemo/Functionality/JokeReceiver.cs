using UnityEngine;
using System.Collections;

public class JokeReceiver : MonoBehaviour 
{
	UILabel label;

	void Start () 
	{
		label = GetComponent ("UILabel") as UILabel;
		EB.Sparx.JokeManager.instance.jokeReceivers += OnJokeRequested;
	}

	void OnDestroy()
	{
		EB.Sparx.JokeManager.instance.jokeReceivers -= OnJokeRequested;
	}

	void OnJokeRequested(string joke)
	{
		label.text = joke;
	}
}