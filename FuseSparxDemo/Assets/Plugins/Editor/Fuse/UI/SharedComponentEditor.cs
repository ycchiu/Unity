using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SharedComponent))]
public class SharedComponentEditor : Editor
{
	SharedComponent sc;

	private void DrawCustomInspector()
	{
		var prefabType = UnityEditor.PrefabUtility.GetPrefabType(sc.gameObject);
		GUI.color = new Color(.5f, .5f, 1f);
		GUILayout.Label("PrefabType: " + prefabType);
		GUI.color = Color.white;
	}

	public override void OnInspectorGUI() 
	{
		sc = target as SharedComponent;

		DrawDefaultInspector();

		DrawCustomInspector();
		
		if (sc.transform.parent == null)
		{
			GUILayout.BeginHorizontal();
			GUI.color = Color.green;
			if (GUILayout.Button ("Instantiate"))
			{
				GameObject parent = null;
				UIPanel [] panels = NGUITools.FindActive<UIPanel>();
				if (panels.Length > 0) parent = panels[0].gameObject;
				while (parent.transform != null)
				{
					UIPanel nextUp = EB.Util.FindComponentUpwards<UIPanel>(parent.transform.parent.gameObject);
					if (nextUp == null)
					{
						break;
					}
					else
					{
						parent = nextUp.gameObject;
					}
				}
				if (parent == null)
				{
					EditorUtility.DisplayDialog("No UIPanel root found",
						"Cannot instantiate this widget without a root UIPanel.",
						"OK");
				}
				else
				{
					GameObject instance = new GameObject("New" + target.name + "Instance", typeof(SharedComponentInstance));
					instance.transform.parent = parent.transform;
					instance.transform.localScale = Vector3.one;
					instance.transform.localPosition = Vector3.zero;
					instance.transform.localRotation = Quaternion.identity;
					SharedComponentInstance sci = instance.GetComponent<SharedComponentInstance>();
					sci.prefab = sc.gameObject;

					createdInstance = instance;
					EditorApplication.update += OnEditorUpdate;
					editorUpdateCountdown = 20;
				}
			}
			GUI.color = Color.white;
			GUILayout.EndHorizontal();
		}
	}

	private GameObject createdInstance;
	private int editorUpdateCountdown;
	private void OnEditorUpdate()
	{
		--editorUpdateCountdown;
		if (editorUpdateCountdown == 15)
		{
			if (createdInstance != null)
			{
				SharedComponentInstance sci = createdInstance.GetComponent<SharedComponentInstance>();
				if (sci != null)
				{
					sci.Load();
				}
			}
		}
		else if (editorUpdateCountdown == 5)
		{
			EditorApplication.update -= OnEditorUpdate;
			SharedComponentInstance sci = createdInstance.GetComponent<SharedComponentInstance>();
			if (sci != null)
			{
				sci.Save();
			}
		}
		else if (editorUpdateCountdown == 0)
		{
			EditorApplication.update -= OnEditorUpdate;
		}
	}
}

