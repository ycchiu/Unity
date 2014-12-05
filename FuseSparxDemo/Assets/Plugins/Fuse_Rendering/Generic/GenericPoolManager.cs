using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB.Rendering
{
	public class GenericPoolManager<T, U>  
		where T : GenericPool<U> , new() 
		where U : GenericPoolType
	{
		public enum Persistence{Always, Temporary};
		
		public void Start()
		{
			_pools[0] = new Dictionary<string, T>(); 	// Persistent pools
			_pools[1] = new Dictionary<string, T>();	// Temp pools
		}

		public void Register(GameObject psobject, int count, Persistence ePersistence)
		{
			if (!_pools[(int)ePersistence].ContainsKey(psobject.name))
			{
				T t = new T();
				t.Init(psobject, count);
				_pools[(int)ePersistence][psobject.name] = t;
			}	
		}

		public List<GameObject> GetAllInReady(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			
			T pool = Find(name);
			if (pool != null)
			{
				return pool.GetAllInReady();
			}
			
			return null;
		}

		public GameObject Use(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
				
			T pool = Find(name);
			if (pool != null)
			{

				return pool.Use();
			}

			return null;
		}
		
		private T Find(string name)
		{
			T pool = null;
			for (int i=0; i<_pools.Length; ++i)
			{
				if (_pools[i] != null)
				{
					if (_pools[i].TryGetValue(name, out pool))
					{
						return pool;
					}
				}
			}
			return null;
		}
		
		public void Recycle(GameObject go)
		{
			T pool = Find(go.name);
			if (pool != null)
			{
				pool.Recycle(go);
			}
		}

		public void PlayAt(string name, Vector3 pos)
		{
			GameObject go = Use(name);
			U trInst = go.GetComponent<U>();
			if (go && trInst != null)
			{
				go.transform.position = pos;
				trInst.Play();
			}
		}

		public void Update()
		{
			for (int i=0; i<_pools.Length; ++i)
			{
				foreach (T pool in _pools[i].Values)
				{
					pool.Update();
				}
			}
		}

		public void Retire()
		{
			for (int i=0; i<_pools.Length; ++i)
			{
				foreach (T pool in _pools[i].Values)
				{
					pool.Retire();
				}
			}
		}

		public void Clean(Persistence ePersistence)
		{
			foreach (KeyValuePair<string, T> kvp in _pools[(int)ePersistence])
			{
				kvp.Value.Clean();
			}

			_pools[(int)ePersistence].Clear();
		}
		
		public void CleanAll()
		{
			Clean(Persistence.Always);
			Clean(Persistence.Temporary);
		}
		
		private Dictionary<string, T>[]	_pools = new Dictionary<string, T>[2];
	}

	public class GenericPool<U> where U:GenericPoolType
	{

		public virtual void OnRecycle(GameObject go)
		{
			go.transform.parent = GenericPoolSingleton.Instance.transform;
		}
		public virtual void OnInit(GameObject go)
		{
			go.transform.parent = GenericPoolSingleton.Instance.transform;
		}

		public virtual void Init(GameObject psObject, int count)
		{
			_ready = new List<GameObject>(count);
			_active = new List<GameObject>(count);
			
			for (int i=0; i<count; ++i)
			{
				GameObject go = (GameObject)(GameObject.Instantiate(psObject));

				OnInit(go);

				_ready.Add(go);
			}
		}
		
		public GameObject Use()
		{
			if (_ready.Count > 0)
			{
				GameObject go = _ready[_ready.Count-1];
				_active.Add(go);
				_ready.RemoveAt(_ready.Count-1);
				
				return go;
			}
			
			return null;
		}

		public List<GameObject> GetAllInReady()
		{
			if (_ready != null && _ready.Count > 0)
			{
				return _ready;
			}
			return null;
		}
		
		public virtual void Recycle(GameObject go)
		{
			if (_active.Contains(go))
			{
				_ready.Add(go);
				_active.Remove(go);
				OnRecycle(go);
			}
		}
		
		public virtual void Update()
		{
			for (int i=_active.Count-1; i>=0; --i)
			{
				if (_active[i] == null)
					continue;
				U tri = _active[i].GetComponent<U>();
				
				if (tri != null && !tri.IsPlaying)
				{
					Recycle(_active[i]);
				}
			}
		}
		
		// Shut down all systems
		public virtual void Retire()
		{
			for (int i=_active.Count-1; i>=0; --i)
			{
				if (_active[i] != null)
				{
					U tri = _active[i].GetComponent<U>();
					tri.Stop();
					Recycle(_active[i]);
				}
			}
			_active.Clear();
		}

		// Free the structures
		public void Clean()
		{
			// Make sure they're all in the ready list
			Retire();

			// Destroy them all
			int count = _ready.Count;
			for (int i=0; i<count; ++i)
			{
				Object.Destroy(_ready[i]);
			}
		}
		
		public string					_name;
		
		public List<GameObject> 	_ready;
		public List<GameObject>	_active;
	}
}
