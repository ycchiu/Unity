using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NGUISpriteUseFinder : ScriptableWizard
{
	public class FindSpriteResult
	{
		public string atlasName;
		public string spriteName;
		public string prefabName;
		public string gameObjectName;
		public string prefabPath;
		
		public FindSpriteResult(string atlas, string sprite, string prefab, string gameObj, string prefabPath)
		{
			atlasName = atlas;
			spriteName = sprite;
			prefabName = prefab;
			gameObjectName = gameObj;
			this.prefabPath = prefabPath;
			
			string suffix = "(Clone)";
			if (prefabName.EndsWith(suffix))
			{
				prefabName = prefabName.Substring(0, prefabName.Length - suffix.Length);
			}
		}
	}

	public class SpriteUseInfo
	{
		public SpriteUseInfo(string name)
		{
			spriteName = name;
			useCount = 0;
			usingPrefabs = new List<GameObject>();
		}

		public string spriteName;
		public int useCount;
		public List<GameObject> usingPrefabs;
	}
	
	public List<FindSpriteResult> searchResults = null;
	public List<SpriteUseInfo> spriteUses = null;
	public string searchStatus
	{
		set
		{
			_searchStatus = value;
			Repaint();
		}
		get
		{
			return _searchStatus;
		}
	}
	private string _searchStatus = "";
	
	private GameObject searchObject = null;
	private NGUISpriteUseSearch searcher = null;
	
	private string atlasName = "";
	private bool atlasFound = false;
	private UIAtlas atlas = null;
	private string spriteName = "";
	private string searchPath = "Resources/UI/";
	private bool spriteFound = false;
	private Vector2 scrollPos = Vector2.zero;
	
	[MenuItem("NGUI/Find Uses of Atlas Sprite")]
	static void FindSprite()
	{
		ScriptableWizard.DisplayWizard<NGUISpriteUseFinder>("Find Uses of Atlas Sprite");
	}
	
	public void Unregister(NGUISpriteUseSearch s)
	{
		if (s == searcher)
		{
			searcher = null;
			searchObject = null;
		}
	}

	private void DoSearch()
	{
		if (atlasFound && spriteFound)
		{
			searchObject = new GameObject("NGUI Sprite Finder");
			searchObject.AddComponent<NGUISpriteUseSearch>();
			searcher = searchObject.GetComponent<NGUISpriteUseSearch>();
			searcher.window = this;
			searcher.atlasName = atlasName;
			searcher.spriteName = spriteName;
			searcher.searchPath = searchPath;
			searcher.BeginSearch();
		}
	}
	
	private void DoSearchForUnused()
	{
		searchObject = new GameObject("NGUI Sprite Finder");
		searchObject.AddComponent<NGUISpriteUseSearch>();
		searcher = searchObject.GetComponent<NGUISpriteUseSearch>();
		searcher.window = this;
		searcher.atlasName = atlasName;
		searcher.spriteName = "";
		searcher.searchPath = searchPath;
		searcher.BeginSearch();
	}
	
	private void UpdateAtlasName()
	{
		atlasFound = false;
		atlas = null;
		
		string[] assetPaths = AssetDatabase.GetAllAssetPaths();
		
		string atlasNameFull = atlasName + ".prefab";
		string atlasPath = "";
		foreach (string path in assetPaths)
		{
			if (path.EndsWith(atlasNameFull))
			{
				atlasPath = path;
				break;
			}
		}
		
		if (!string.IsNullOrEmpty(atlasPath))
		{
			GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(atlasPath, typeof(GameObject));
			if (go != null)
			{
				atlas = go.GetComponent<UIAtlas>();
				if (atlas != null)
				{
					atlasFound = true;
				}
			}
		}
	}
	
	
	private void UpdateSpriteName()
	{
		if (atlas != null)
		{
			spriteFound = (atlas.spriteList.Find(item => item.name == spriteName) != null);
		}
		else
		{
			spriteFound = false;
		}
	}
	
	private void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		string newAtlasName = EditorGUILayout.TextField("Match Atlas:", atlasName);
		if (atlasName != newAtlasName)
		{
			atlasName = newAtlasName;
			UpdateAtlasName();
			UpdateSpriteName();
		}
		if (atlasFound)
		{
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.green;
			GUILayout.Label("Y", style, GUILayout.MaxWidth(20f));
		}
		else
		{
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.red;
			GUILayout.Label("N", style, GUILayout.MaxWidth(20f));
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		string newSpriteName = EditorGUILayout.TextField("Match Sprite:", spriteName);
		if (newSpriteName != spriteName)
		{
			spriteName = newSpriteName;
			UpdateSpriteName();
		}
		if (spriteFound)
		{
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.green;
			GUILayout.Label("Y", style, GUILayout.MaxWidth(20f));
		}
		else
		{
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.red;
			GUILayout.Label("N", style, GUILayout.MaxWidth(20f));
		}
		EditorGUILayout.EndHorizontal();

		searchPath = EditorGUILayout.TextField("Search Path:", searchPath);

		if (atlasFound && string.IsNullOrEmpty(searchStatus) && GUILayout.Button("Get Sprite Use Counts"))
		{
			DoSearchForUnused();
		}
		if (string.IsNullOrEmpty(searchStatus) && atlasFound && spriteFound && GUILayout.Button("Search"))
		{
			DoSearch();
		}
		NGUIEditorTools.DrawSeparator();
		
		if (!string.IsNullOrEmpty(searchStatus))
		{
			GUILayout.Label(searchStatus);
		}
		else
		{
			if (searchResults != null)
			{
				Print("Prefab", "Game Object Name", false);
				foreach (FindSpriteResult result in searchResults)
				{
					if (Print(result.prefabName, result.gameObjectName, true))
					{
						string path = result.prefabPath;
						GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
						Selection.activeGameObject = go;
					}
				}
			}
			else if (spriteUses != null)
			{
				spriteUses.Sort(delegate(SpriteUseInfo x, SpriteUseInfo y) {
					return x.spriteName.CompareTo(y.spriteName);
				});
				
				Print("Sprite Name", "Use Count", false);
				GUIStyle scrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
				scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.horizontalScrollbar, scrollbarStyle, null);
				foreach (var entry in spriteUses)
				{
					if (Print(entry.spriteName, entry.useCount.ToString(), true))
					{
						foreach (var prefab in entry.usingPrefabs)
						{
							Debug.Log (prefab.name, prefab);
						}
					}
				}
				GUILayout.EndScrollView();
			}
		}
	}

	bool Print(string a, string b, bool button)
	{
		bool retVal = false;

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label(a, GUILayout.Width(160f));
			if (b == "0")
			{
				GUI.contentColor = Color.red;
				GUILayout.Label(b, GUILayout.Width(160f));
				GUI.contentColor = Color.white;
			}
			else
			{
				GUILayout.Label(b, GUILayout.Width(160f));
			}

			if (button)
			{
				retVal = GUILayout.Button("Select", GUILayout.Width(60f));
			}
			else
			{
				GUILayout.Space(60f);
			}
		}
		GUILayout.EndHorizontal();
		return retVal;
	}
}
