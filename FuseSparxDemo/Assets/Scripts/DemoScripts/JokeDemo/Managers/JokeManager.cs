using UnityEngine;
using System.Collections;

namespace EB.Sparx 
{
	public class JokeManager : SubSystem 
	{
		public static JokeManager instance;

		public JokeUpdated jokeReceivers;
		public delegate void JokeUpdated(string result);

		JokeAPI _api;

		public override void Initialize(Config config) 
		{
			instance = this;
			_api = new JokeAPI (Hub.ApiEndPoint);
		}

		public override void Connect() 
		{

		}

		public override void Disconnect(bool isLogout) 
		{

		}

		public void RequestJoke()
		{
			_api.RequestJoke (OnJoke);
		}

		void OnJoke(string error, Hashtable data) 
		{
			if (error == "" || error == null) 
			{
				jokeReceivers(data["joke"] as string);
			}
			else 
			{
				jokeReceivers(error);
			}
		}
	}
}