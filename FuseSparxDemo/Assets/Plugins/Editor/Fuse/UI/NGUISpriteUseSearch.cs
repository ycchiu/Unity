#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FindSpriteResult = NGUISpriteUseFinder.FindSpriteResult;

// This is a unity editor monobehaviour used to perform searches for sprites by the NGUISpriteUseFinder class.
[ExecuteInEditMode]
public class NGUISpriteUseSearch : MonoBehaviour
{
	public NGUISpriteUseFinder window;
	public string atlasName
	{
		set
		{
			if (state == State.Done)
			{
				_atlasName = value;
			}
		}
		get
		{
			return _atlasName;
		}
	}
	private string _atlasName;
	
	public string spriteName
	{
		set
		{
			if (state == State.Done)
			{
				_spriteName = value;
			}
		}
		get
		{
			return _spriteName;
		}
	}
	private string _spriteName;
	
	public string searchPath
	{
		set
		{
			if (state == State.Done)
			{
				_searchPath = value;
			}
		}
		get
		{
			return _searchPath;
		}
	}
	private string _searchPath;
	
	public bool isSearchingForUnused
	{
		get
		{
			return string.IsNullOrEmpty(spriteName);
		}
	}
	
	private static NGUISpriteUseSearch _searchInstance = null;
	
	public void BeginSearch()
	{
		_searchInstance = this;
		state = State.StartGettingAssetPaths;
		
		// Setup ...
		if (isSearchingForUnused)
		{
			results = null;
			spriteUses = new List<NGUISpriteUseFinder.SpriteUseInfo>();
			
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
			
			GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(atlasPath, typeof(GameObject));
			if (go != null)
			{
				UIAtlas atlas = go.GetComponent<UIAtlas>();
				if (atlas != null)
				{
					foreach (var sprite in atlas.spriteList)
					{
						spriteUses.Add(new NGUISpriteUseFinder.SpriteUseInfo(sprite.name));
					}
				}
			}
		}
		
		EditorApplication.update += OnUpdate;
	}
	
	public static void OnUpdate()
	{
		if (_searchInstance != null)
		{
			_searchInstance.Pump();
		}
	}
	
	public enum State
	{
		StartGettingAssetPaths,
		GettingAssetPaths,
		StartSearching,
		Searching,
		Done
	}
	private State state
	{
		set
		{
			_state = value;
		}
		get
		{
			return _state;
		}
	}
	private State _state = State.Done;
	
	public void Pump()
	{
		switch (state)
		{
		case State.StartGettingAssetPaths:
			StartGettingAssetPaths();
			break;
		case State.GettingAssetPaths:
			GetAssetPaths();
			break;
		case State.StartSearching:
			StartSearching();
			break;
		case State.Searching:
			Search();
			break;
		default:
			break;
		}
	}
	
	string[] assetPathsArray;
	private List<NGUISpriteUseFinder.SpriteUseInfo> spriteUses;
	List<string> prefabAssetPaths = null;
	int assetPathIdx = 0;
	private void StartGettingAssetPaths()
	{
		window.searchStatus = "Getting asset paths ...";
		window.searchResults = null;
		
		assetPathsArray = AssetDatabase.GetAllAssetPaths();
		prefabAssetPaths = new List<string>();
		
		assetPathIdx = 0;
		state = State.GettingAssetPaths;
	}
	
	private void GetAssetPaths()
	{
		if (assetPathsArray == null)
		{
			HaltSearch();
			return;
		}
		
		// Grab prefab asset paths.
		System.DateTime yieldTime = System.DateTime.Now.AddMilliseconds(30f);
		for (; assetPathIdx < assetPathsArray.Length && System.DateTime.Now < yieldTime; ++assetPathIdx)
		{
			string path = assetPathsArray[assetPathIdx];
			if (path.EndsWith(".prefab"))
			{
				prefabAssetPaths.Add(path);
			}
		}
		
		if (assetPathIdx >= assetPathsArray.Length)
		{
			state = State.StartSearching;
		}
	}
	
	private List<FindSpriteResult> results = null;
	private int prefabIndex = 0;
	private void StartSearching()
	{
		window.searchStatus = "Searching ...";
		results = new List<FindSpriteResult>();
		prefabIndex = 0;
		
		state = State.Searching;
	}

	private void HaltSearch()
	{
		window.searchStatus = "";
		window.searchResults = results;
		window.spriteUses = spriteUses;
		if (spriteUses != null)
		{
			window.searchResults = null;
		}
		state = State.Done;
		EditorApplication.update -= OnUpdate;
		window.Unregister(this);
		GameObject.DestroyImmediate(gameObject);
	}

	private void Search()
	{
		// Sanity check ...
		if (results == null || prefabAssetPaths == null)
		{
			HaltSearch();
			return;
		}
		
		System.DateTime yieldTime = System.DateTime.Now.AddMilliseconds(30f);
		// For each prefab ...
		for (; prefabIndex < prefabAssetPaths.Count && System.DateTime.Now < yieldTime; ++prefabIndex)
		{
			string path = prefabAssetPaths[prefabIndex];
			if (!path.StartsWith(searchPath))
			{
				continue;
			}
			GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
			if (go != null)
			{
				GameObject instance = GameObject.Instantiate(go) as GameObject;
				
				UISprite[] sprites = EB.Util.FindAllComponents<UISprite>(instance);
				foreach (UISprite sprite in sprites)
				{
					if (sprite.atlas != null && sprite.atlas.name == atlasName)
					{
						string currentSpriteName = sprite.spriteName;
						if (isSearchingForUnused)
						{
							var entry = spriteUses.Find(item => item.spriteName == currentSpriteName);
							if (entry != null)
							{
								entry.useCount ++;
								entry.usingPrefabs.Add(go);
							}
							else
							{
								entry = new NGUISpriteUseFinder.SpriteUseInfo(currentSpriteName);
								spriteUses.Add(entry);
								entry.useCount = 1;
								entry.usingPrefabs.Add(go);
							}
						}
						else if (spriteName == currentSpriteName)
						{
							results.Add(new FindSpriteResult(sprite.atlas.name, currentSpriteName, instance.name, sprite.gameObject.name, path));
						}
					}
				}

				GameObject.DestroyImmediate(instance);
				window.searchStatus = "Searched " + prefabIndex + " / " + prefabAssetPaths.Count + " prefabs ...";
			}
		}
		
		if (prefabIndex >= prefabAssetPaths.Count)
		{
			HaltSearch();
		}
	}
}
#endif