// #define DEBUG_BUSY_BLOCKER_MANAGER_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BusyBlocker = EB.UI.BusyBlocker;

/////////////////////////////////////////////////////////////////////////////
/// BlockingUiAction
/////////////////////////////////////////////////////////////////////////////
public static class BlockingUiAction
{
	public static EB.Action Wrap(MonoBehaviour b, BusyBlockerManager.BlockerFlag flag, EB.Action cb)
	{
		BusyBlockerManager.Instance.AddBlocker(flag);
		EB.Action action = delegate()
		{
			BusyBlockerManager.Instance.RemoveBlocker(flag);
			if (b != null)
			{
				cb();
			}
		};
		return action;
	}
	
	public static EB.Action<T> Wrap<T>(MonoBehaviour b, BusyBlockerManager.BlockerFlag flag, EB.Action<T> cb)
	{
		BusyBlockerManager.Instance.AddBlocker(flag);
		EB.Action<T> action = delegate(T obj)
		{
			BusyBlockerManager.Instance.RemoveBlocker(flag);
			if ( b != null )
			{
				cb(obj);
			}
		};
		return action;
	}
	
	public static EB.Action<T, U> Wrap<T, U>(MonoBehaviour b, BusyBlockerManager.BlockerFlag flag, EB.Action<T, U> cb)
	{
		BusyBlockerManager.Instance.AddBlocker(flag);
		EB.Action<T,U> action = delegate(T obj1, U obj2)
		{
			BusyBlockerManager.Instance.RemoveBlocker(flag);
			if (b != null)
			{
				cb(obj1, obj2);
			}
		};
		return action;
	}
}

/////////////////////////////////////////////////////////////////////////////
/// BusyBlockerManager
/////////////////////////////////////////////////////////////////////////////
/// Controls display of the busy blocker based on bit flags. There is no
/// reference counting.
/////////////////////////////////////////////////////////////////////////////
public class BusyBlockerManager : MonoBehaviour
{
	/// Public Enums ////////////////////////////////////////////////////////
	/// These are ordered by importance, so if multiple are active 
	/// simultaneously, the top most reason will be displayed.
	public enum BlockerFlag
	{
		FileLoad,
		ServerTransaction,
		ServerCommunication,
		Transitions,
		UiAnimation,
		
		NonBlockingCommunication,
	}
	
	/// Public Variables ////////////////////////////////////////////////////
	public class BusyBlockerManagerConfig
	{
		public Dictionary<BlockerFlag, float> DisplayDelayPerBlockerFlag = new Dictionary<BlockerFlag, float>();
		public string BlockerUiName = "BusyBlocker";
	}
	static public BusyBlockerManagerConfig Config = new BusyBlockerManagerConfig();
	
	public static BusyBlockerManager Instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject go = new GameObject("BusyBlockerManager");
				DontDestroyOnLoad(go);
				_instance = go.AddComponent<BusyBlockerManager>();
			}
			return _instance;
		}
	}

	
	/// A list of callbacks that will be invoked when the blocker is fully loaded, or is already fully loaded.  This is called whenever a flag is added to the busy blocker
	public event EB.Action onBlockerLoaded;
	
	/// A list of callbacks that will be invoked when the blocker is fully unloaded (no flags are set)
	public event EB.Action onBlockerUnloaded;
	
	public BusyBlocker BusyBlockerInstance
	{
		get
		{
			return _BusyBlockerInstance;
		}
		set
		{
			if (_BusyBlockerInstance != null)
			{
				_BusyBlockerInstance.OnBlockerOpened -= OnBlockerOpened;
				_BusyBlockerInstance.OnBlockerClosed -= OnBlockerClosed;
			}
			
			_BusyBlockerInstance = value;
			
			if (_BusyBlockerInstance != null)
			{
				_BusyBlockerInstance.OnBlockerOpened += OnBlockerOpened;
				_BusyBlockerInstance.OnBlockerClosed += OnBlockerClosed;
			}
		}
	}
	private BusyBlocker _BusyBlockerInstance = null;
	
	/// Private Variables ///////////////////////////////////////////////////
	private static BusyBlockerManager _instance = null;
	private int activeBlockerFlags = 0;
	
	/// Public Interface ////////////////////////////////////////////////////
	
	public void AddBlocker(BlockerFlag flag)
	{
		Report(string.Format("Add blocker '{0}'", flag.ToString()));
		AddFlag(flag);
		
		CheckBlockerStatus();
	}
	
	public void RemoveBlocker(BlockerFlag flag)
	{
		Report(string.Format("Remove blocker '{0}'", flag.ToString()));
		RemoveFlag(flag);
		
		CheckBlockerStatus();
	}
	
	public bool CheckBlocker(BlockerFlag flag)
	{
		return HasFlag(flag);
	}

	public static bool IsNonBlockingFlag(BlockerFlag flag)
	{
		switch (flag)
		{
		case BlockerFlag.NonBlockingCommunication:
			return true;
		}
		return false;
	}

	public static bool IsVisualBlocker(BlockerFlag flag)
	{
		switch (flag)
		{
			case BlockerFlag.FileLoad:
			case BlockerFlag.ServerTransaction:
			case BlockerFlag.ServerCommunication:
			case BlockerFlag.NonBlockingCommunication:
				return true;
			case BlockerFlag.Transitions:
			case BlockerFlag.UiAnimation:
				return false;
			default:
				EB.Debug.LogError("BlockerFlag enum must be added to IsVisualBlocker switch.");
				break;
		}
		return true;
	}
	
	public void GetBlockingReason(out BlockerFlag activeFlag, out bool isActive)
	{
		BlockerFlag[] flags = EB.Util.GetEnumValues<BlockerFlag>();
		// Needs a default value:
		activeFlag = BlockerFlag.ServerTransaction;
		isActive = false;
		
		foreach (BlockerFlag blockerFlag in flags)
		{
			if (HasFlag(blockerFlag))
			{
				activeFlag = blockerFlag;
				isActive = true;
				break;
			}
		}
	}

	public bool IsFlagActive(BlockerFlag flag)
	{
		return HasFlag(flag);
	}
	
	public bool IsBlocking()
	{
		bool result = false;
		
		BlockerFlag[] flags = EB.Util.GetEnumValues<BlockerFlag>();
		foreach (BlockerFlag blockerFlag in flags)
		{
			if (HasFlag(blockerFlag) && !IsNonBlockingFlag(blockerFlag))
			{
				result = true;
				break;
			}
		}
		
		return result;
	}
	
	public float GetDisplayDelay(BlockerFlag flag)
	{
		float delay = 0f;
		
		BusyBlockerManager.Config.DisplayDelayPerBlockerFlag.TryGetValue(flag, out delay);
		
		return delay;
	}
	
	public override string ToString()
	{
		string details = "";
		foreach (BlockerFlag flag in EB.Util.GetEnumValues<BlockerFlag>())
		{
			bool flagBlocked = HasFlag(flag);
			details += string.Format("{0}:{1}\n", flag.ToString(), flagBlocked.ToString());
		}
		
		return details;
	}

	/// Private Implementation //////////////////////////////////////////////
	private void CheckBlockerStatus()
	{
		BusyBlockerInstance.UpdateBlocker();
	}
	
	private void OnBlockerOpened()
	{
		if (onBlockerLoaded != null)
		{
			onBlockerLoaded();
		}
	}
	
	private void OnBlockerClosed()
	{
		if (onBlockerUnloaded != null)
		{
			onBlockerUnloaded();
		}
	}
	
	// Bit twiddling methods follow...
	private int GetBit(BlockerFlag flag)
	{
		return 1 << (int)flag;
	}
	
	private void AddFlag(BlockerFlag flag)
	{
		if (!HasFlag(flag))
		{
			activeBlockerFlags ^= GetBit(flag);
		}
	}
	
	private bool HasFlag(BlockerFlag flag)
	{
		return (activeBlockerFlags & GetBit(flag)) != 0;
	}
	
	private bool HasAnyFlags()
	{
		return activeBlockerFlags != 0;
	}
	
	private void RemoveFlag(BlockerFlag flag)
	{
		if (HasFlag(flag))
		{
			activeBlockerFlags ^= GetBit(flag);
		}
	}

	private void Report(string msg)
	{
#if DEBUG_BUSY_BLOCKER_MANAGER_CLASS
		Debug.Log(string.Format("[{0}] BusyBlockerManager > {1}", Time.frameCount, msg));
#endif
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
		UILogger.Instance.Log(string.Format("[{0}] BusyBlockerManager > {1}", Time.frameCount, msg));
#endif
	}
}
