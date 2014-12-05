using UnityEngine;
using System.Collections;
using EB.Rendering;

public class GenericPoolSingleton : MonoBehaviour 
{
	static GenericPoolSingleton _instance;

	public GenericPoolManager<GenericPool<GenericTrailRendererInstance>, GenericTrailRendererInstance> 		trailPool;
	public GenericPoolManager<GenericPool<DynamicPointLightInstance>, DynamicPointLightInstance> 	lightPool;

	public static GenericPoolSingleton Instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject pool = new GameObject("GenericPoolSingleton");
				_instance = pool.AddComponent<GenericPoolSingleton>();
				_instance.Init();
				DontDestroyOnLoad(pool.gameObject);
			}
			return _instance;
		}
	}

	public static bool IsInitialized()
	{
		return _instance != null;
	}

	public void Init() 
	{
		trailPool = new GenericPoolManager<GenericPool<GenericTrailRendererInstance>, GenericTrailRendererInstance>();
		lightPool = new GenericPoolManager<GenericPool<DynamicPointLightInstance>, DynamicPointLightInstance>();
		trailPool.Start();
		lightPool.Start();
	}
	
	public void Update() 
	{
		if (_instance != null)
		{
			trailPool.Update();
			lightPool.Update();
		}
	}
}
