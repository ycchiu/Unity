#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/////////////////////////////////////////////////////////////////////////////
/// Window
/////////////////////////////////////////////////////////////////////////////
[ExecuteInEditMode]
public class SharedComponentInstanceKillerWorker : EditorWorker
{
	public string SearchPath = "";
	public List<string> PrefabPaths = null;

	/////////////////////////////////////////////////////////////////////////
	#region FindingPrefabs_WorkerState
	private class FindingPrefabs_WorkerState : WorkerState
	{
		public string SearchPath = "";

		private string[] _assetPathsArray;
		private List<string> _prefabPaths;
		private int _assetPathsIndex = 0;

		public override void Initialize()
		{
			base.Initialize();
			_assetPathsArray = AssetDatabase.GetAllAssetPaths();
			Debug.Log ("_assetPathsArray created with len: " + _assetPathsArray.Length);
			_prefabPaths = new List<string>();
		}

		public override void DoWork()
		{
			// Debug.Log ("FindingPrefabs_WorkerState > Do work " + _assetPathsIndex + " / " + _assetPathsArray.Length);
			if (!IsDone())
			{
				string path = _assetPathsArray[_assetPathsIndex];
				// Debug.Log(string.Format("Checking path: '{0}'", path));
				if (path.StartsWith(SearchPath) && path.EndsWith(".prefab"))
				{
					_prefabPaths.Add(path);
				}
				++_assetPathsIndex;
			}
		}

		public override bool IsDone()
		{
			return _assetPathsIndex >= _assetPathsArray.Length;
		}

		public override string GetStatus()
		{
			return string.Format("Searching for prefabs: {0}/{1}", _assetPathsIndex, _assetPathsArray.Length);
		}

		public override WorkerState GetNextWorkerState()
		{
			Debug.Log ("FindingPrefabs_WorkerState > Making next state ...");
			var nextState = new SearchingThroughPrefabsForSharedComponents_WorkerState();
			nextState.PrefabPaths = _prefabPaths;
			nextState.Initialize();
			return nextState;
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region SearchingThroughPrefabsForSharedComponents_WorkerState
	private class SearchingThroughPrefabsForSharedComponents_WorkerState : WorkerState
	{
		public List<string> PrefabPaths;

		private int _prefabPathIndex = 0;
		private List<GameObject> _prefabsWithSharedComponentInstances;

		public override void Initialize()
		{
			base.Initialize();
			_prefabsWithSharedComponentInstances = new List<GameObject>();
		}

		public override void DoWork()
		{
			if (!IsDone())
			{
				string path = PrefabPaths[_prefabPathIndex];
				GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
				if (prefab != null)
				{
					if (EB.Util.FindComponent<SharedComponentInstance>(prefab) != null)
					{
						_prefabsWithSharedComponentInstances.Add(prefab);
					}
				}

				++_prefabPathIndex;
			}
		}
		
		public override bool IsDone()
		{
			return _prefabPathIndex >= PrefabPaths.Count;
		}

		public override string GetStatus()
		{
			return string.Format("Searching for Prefabs with Shared Component Instances: {0}/{1}", _prefabPathIndex, PrefabPaths.Count);
		}
		
		public override WorkerState GetNextWorkerState()
		{
			var nextState = new FindingSharedComponentsWithInstances_WorkerState();
			nextState.PrefabsWithSharedComponentInstances = _prefabsWithSharedComponentInstances;
			nextState.Initialize();
			return nextState;
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	/////////////////////////////////////////////////////////////////////////
	#region FindingSharedComponentsWithInstances_WorkerState
	private class FindingSharedComponentsWithInstances_WorkerState : WorkerState
	{
		public List<GameObject> PrefabsWithSharedComponentInstances;

		private List<GameObject> _prefabsWithActiveInstances;
		private int _prefabIndex = 0;

		public override void Initialize()
		{
			base.Initialize();
			_prefabsWithActiveInstances = new List<GameObject>();
		}

		public override void DoWork()
		{
			if (!IsDone())
			{
				GameObject prefab = PrefabsWithSharedComponentInstances[_prefabIndex];
				if (prefab != null)
				{
					SharedComponentInstance[] sharedComponents = EB.Util.FindAllComponents<SharedComponentInstance>(prefab);
					bool hasSharedComponentInstances = false;
					foreach (var sc in sharedComponents)
					{
						if (sc.prefab == null)
						{
							continue;
						}
						GameObject go = sc.gameObject;
						foreach (Transform child in go.transform)
						{
							if (child.gameObject.GetComponent<SharedComponent>() != null)
							{
								hasSharedComponentInstances = true;
							}
						}
					}
					if (hasSharedComponentInstances)
					{
						_prefabsWithActiveInstances.Add(prefab);
					}
				}
				
				++_prefabIndex;
			}
		}
		
		public override bool IsDone()
		{
			return _prefabIndex >= PrefabsWithSharedComponentInstances.Count;
		}

		public override string GetStatus()
		{
			return string.Format("Searching for Shared Component Instances: {0}/{1}", _prefabIndex, PrefabsWithSharedComponentInstances.Count);
		}

		public override WorkerState GetNextWorkerState()
		{
			var nextState = new DestroyingSharedComponentInstances_WorkerState();
			nextState.PrefabsWithActiveSharedComponentInstances = _prefabsWithActiveInstances;
			nextState.Initialize();
			return nextState;
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region DestroyingSharedComponentInstances_WorkerState
	private class DestroyingSharedComponentInstances_WorkerState : WorkerState
	{
		public List<GameObject> PrefabsWithActiveSharedComponentInstances;

		private enum DestructionState
		{
			Uninitialized,
			DestroyingSharedComponents,
			WaitingForDestructionOfSharedComponents,
			Cleanup
		}

		private GameObject _currentPrefab = null;
		private int _prefabIndex = 0;
		private DestructionState destructionState = DestructionState.Uninitialized;
		private List<GameObject> _currentlyDestroyingSharedComponentInstances;

		public override void Initialize()
		{
			base.Initialize();

			Debug.Log("DestroyingSharedComponentInstances_WorkerState initialized with " + PrefabsWithActiveSharedComponentInstances.Count + " items.");
		}
		
		public override void DoWork()
		{
			if (!IsDone())
			{
				if (_currentPrefab == null)
				{
					GameObject prefab = PrefabsWithActiveSharedComponentInstances[_prefabIndex];
					_currentPrefab = prefab;
					destructionState = DestructionState.DestroyingSharedComponents;
				}
				else // _currentPrefab exists ...
				{
					HandleDestructionState();
				}
			}
		}

		private void HandleDestructionState()
		{
			switch (destructionState)
			{
				case DestructionState.DestroyingSharedComponents:
				{
					DestroySharedComponentInstances(_currentPrefab);
					destructionState = DestructionState.WaitingForDestructionOfSharedComponents;
					break;
				}

				case DestructionState.WaitingForDestructionOfSharedComponents:
				{
					bool waitForDestruction = false;
					foreach (var instance in _currentlyDestroyingSharedComponentInstances)
					{
						if (instance != null)
						{
							waitForDestruction = true;
						}
					}
					if (!waitForDestruction)
					{
						destructionState = DestructionState.Cleanup;
					}
					break;
				}

				case DestructionState.Cleanup:
				{
					_prefabIndex ++;
					_currentPrefab = null;
					destructionState = DestructionState.Uninitialized;
					break;
				}
			}
		}

		public override bool IsDone()
		{
			return _prefabIndex >= PrefabsWithActiveSharedComponentInstances.Count;
		}
		
		public override string GetStatus()
		{
			string msg = string.Format("Instantiating / Destroying Shared Component Instances: {0}/{1} ({2})", _prefabIndex, PrefabsWithActiveSharedComponentInstances.Count, destructionState);
			return msg;
		}
		
		public override WorkerState GetNextWorkerState()
		{
			return null;
		}

		private void DestroySharedComponentInstances(GameObject container)
		{
			_currentlyDestroyingSharedComponentInstances = new List<GameObject>();
			SharedComponentInstance[] scis = EB.Util.FindAllComponents<SharedComponentInstance>(_currentPrefab);
			foreach (var sci in scis)
			{
				if (sci != null)
				{
					_currentlyDestroyingSharedComponentInstances.Add(sci.instance);
					sci.PrefabCleanupOldInstances();
				}
			}
		}
		
		private void ApplyPrefabInstance(GameObject originalPrefab, GameObject instanceToApply)
		{
			string displayName = originalPrefab.name;
			PrefabUtility.ReplacePrefab(instanceToApply, originalPrefab, ReplacePrefabOptions.ConnectToPrefab);
			Debug.Log ("Removed shared components from " + displayName, PrefabUtility.GetPrefabParent(instanceToApply));
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////
	
	public override string GetProgressText()
	{
		if (Application.isPlaying)
		{
			return "Worker cannot run while application is playing!";
		}
		else
		{
			if (_workerState != null)
			{
				return _workerState.GetStatus();
			}
			else
			{
				return "No active worker.";
			}
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		if (PrefabPaths != null)
		{
			List<GameObject> prefabGameObjects = new List<GameObject>();
			foreach (string path in PrefabPaths)
			{
				GameObject prefab = Resources.LoadAssetAtPath<GameObject>(path);
				if (prefab != null)
				{
					prefabGameObjects.Add(prefab);
				}
				else
				{
					Debug.LogError(string.Format("Couldn't load prefab at path: '{0}'", path));
				}
			}

			var initialState = new FindingSharedComponentsWithInstances_WorkerState();
			initialState.PrefabsWithSharedComponentInstances = prefabGameObjects;
			_workerState = initialState;
			_workerState.Initialize();
		}
		else // Search for prefabs.
		{
			var initialState = new FindingPrefabs_WorkerState();
			initialState.SearchPath = SearchPath;
			_workerState = initialState;
			_workerState.Initialize();
		}
		Debug.Log ("> SharedComponentInstanceKillerWorker: _workerState assigned as:" + _workerState.ToString());
	}
}
#endif
