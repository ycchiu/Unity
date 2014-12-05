using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Selector : MonoBehaviour {

	public static int guiCounter = 0;
	public List<string> scenes;

	bool selectedServer;
	List<EB.Sparx.Discovery.Server> _envs = new List<EB.Sparx.Discovery.Server>();
	EB.Sparx.Discovery _discovery = null;
	
	void Start() 
	{
		_envs.Add(new EB.Sparx.Discovery.Server("localhost","https://localhost") );
		_envs.Add(new EB.Sparx.Discovery.Server("Dev","https://api.sandbox.sparx.io") );
		
		_discovery = new EB.Sparx.Discovery("sandbox");
		_discovery.On("server", delegate(EB.Sparx.Discovery.Server s) {
			_envs.Add(s);
		});
		_discovery.Start();
	}
	
	void OnDestroy()
	{
		if (_discovery != null)
		{
			_discovery.Dispose();
		}
	}

	void OnGUI() {
		float width = Screen.width / 1.5f;
		float height = Screen.height / 7;
		
		GUILayout.BeginArea( new Rect(0,0,Screen.width,Screen.height) );  
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		if (selectedServer) 
		{
			if (scenes.Count > 0) 
			{
				foreach(string demo in scenes)
				{
					if (GUILayout.Button(demo, GUILayout.Width(width/scenes.Count), GUILayout.Height(height)))
					{
						Debug.Log("Loading demo scene: " + demo);
						Application.LoadLevel(demo);
					}
				}
			}
			else 
			{
				GUILayout.Label("No scenes are assigned!");
			}
		} 
		else 
		{
			foreach(var server in _envs)
			{
				if (GUILayout.Button(server.Name, GUILayout.Width(width/_envs.Count), GUILayout.Height(height)))
				{
					Setup.ApiEndPoint = server.Url;
					selectedServer = true;
				}
			}
		}
		
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();		

		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
}