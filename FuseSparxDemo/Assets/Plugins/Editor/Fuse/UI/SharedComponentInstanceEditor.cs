using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SharedComponentInstance))]
public class SharedComponentInstanceEditor : Editor
{
	private bool serializedPropertiesFoldOpen = false;

	public override void OnInspectorGUI() 
	{
		SharedComponentInstance sharedComponentInstance = target as SharedComponentInstance;

		GameObject oldPrefabRef = sharedComponentInstance.prefab;
		DrawDefaultInspector();

		if (oldPrefabRef != sharedComponentInstance.prefab)
		{
			sharedComponentInstance.Load();
		}

		{
			bool prevWordWrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = true;
			serializedPropertiesFoldOpen = EditorGUILayout.Foldout(serializedPropertiesFoldOpen, "Serialized Properties");
			if (serializedPropertiesFoldOpen)
			{
				bool textFieldWordWrap = EditorStyles.textField.wordWrap;
				EditorStyles.textField.wordWrap = true;
				sharedComponentInstance.serializedProps = EditorGUILayout.TextArea(sharedComponentInstance.serializedProps, GUILayout.MinHeight(128.0f));
				EditorStyles.textField.wordWrap = textFieldWordWrap;
			}
			EditorGUILayout.HelpBox("You can directly edit properties in the child instance.  " +
				"The component must be registered with the SharedComponent attached to the child instance.  " +
				"Any other changes you make to it or any further children will not be saved.", 
				MessageType.Info);
			EditorStyles.label.wordWrap = prevWordWrap;
		}
		
		EditorGUILayout.BeginHorizontal();
		{
			sharedComponentInstance.autoSaveProperties = EditorGUILayout.Toggle("Auto Save Properties", sharedComponentInstance.autoSaveProperties);
			if (!sharedComponentInstance.autoSaveProperties)
			{
				GUI.color = Color.green;
				if (GUILayout.Button("Save"))
				{
					string prevSerializedProperties = sharedComponentInstance.serializedProperties;
					sharedComponentInstance.Save();
					if (prevSerializedProperties != sharedComponentInstance.serializedProperties)
					{
						EditorUtility.SetDirty(sharedComponentInstance);
					}
				}
				GUI.color = Color.yellow;
				if (GUILayout.Button("Revert"))
				{
					sharedComponentInstance.Load();
				}
			}
		}
		GUILayout.EndHorizontal();
		
		GUI.color = Color.red;
		if (GUILayout.Button ("Reset to Prefab"))
		{
			if (EditorUtility.DisplayDialog("Reverting to Prefab", "Are you sure you want to remove all changes and revert to the prefab?", "Yes", "Wait a minute..."))
			{
				sharedComponentInstance.serializedProperties = string.Empty;
				sharedComponentInstance.Load();
				sharedComponentInstance.Save();
				EditorUtility.SetDirty(sharedComponentInstance);
			}
		}
		GUI.color = Color.white;
	}
}
