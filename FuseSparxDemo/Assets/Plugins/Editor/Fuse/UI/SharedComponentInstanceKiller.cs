using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SharedComponentInstanceKiller : ScriptableWizard
{
	private SharedComponentInstanceKillerWorker _worker = null;

	private string searchPath = "Assets/Resources/UI/";

	[MenuItem("EBG/UI/Kill Shared Component Instances")]
	static void FindSprite()
	{
		ScriptableWizard.DisplayWizard<SharedComponentInstanceKiller>("Shared Component Instance Killer");
	}

	private void OnEnable()
	{
		EditorApplication.update += OnEditorUpdate;
	}
	
	private void OnDisable()
	{
		EditorApplication.update -= OnEditorUpdate;
	}

	System.DateTime lastGUIRefreshTime = System.DateTime.Now;

	private void OnEditorUpdate()
	{
		// Repaint 10 times a second.
		if ((System.DateTime.Now - lastGUIRefreshTime).TotalMilliseconds > 100)
		{
			lastGUIRefreshTime = System.DateTime.Now;
			Repaint();
		}
	}

	private void OnGUI()
	{
		if (_worker == null)
		{
			searchPath = EditorGUILayout.TextField("Search Path:", searchPath);
			if (GUILayout.Button("Begin Search"))
			{
				GameObject workerContainer = new GameObject("SharedComponentInstanceKillerWorker");
				_worker = workerContainer.AddComponent<SharedComponentInstanceKillerWorker>();
				_worker.SearchPath = searchPath;
				_worker.Initialize();
			}
		}
		else if (_worker.HasWorkToDo)
		{
			GUILayout.Label(_worker.GetProgressText());
		}
		else // Worker exists and is done
		{
			GUILayout.Label(_worker.GetProgressText());
			if (GUILayout.Button("Reset"))
			{
				GameObject.DestroyImmediate(_worker.gameObject);
				_worker = null;
			}
		}
	}
}
