using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public delegate void Callback();
public delegate void Callback<Type1>(ref Type1 Argument1);
public delegate void Callback<Type1, Type2>(ref Type1 Argument1, ref Type2 Argument2);
public delegate void Callback<Type1, Type2, Type3>(ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3);
public delegate void Callback<Type1, Type2, Type3, Type4>(ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4);
public delegate void Callback<Type1, Type2, Type3, Type4, Type5>(ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4, ref Type5 Argument5);

internal static class MessengerInternal 
{
    public static Dictionary<KeyValuePair<GameObject, int>, Delegate> s_EventRegistry = new Dictionary<KeyValuePair<GameObject, int>, Delegate>();
    public static Dictionary<int, HashSet<Delegate>> s_BroadcastEventRegistry = new Dictionary<int, HashSet<Delegate>>();
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	private static float LastTime = 0;
	private static HashSet<int> OrphanedMessages = new HashSet<int>();
	
    public static void OnListenerAdding(KeyValuePair<GameObject, int> Key, Delegate Listener)
	{		
//		if (OrphanedMessages.Contains(Key.Value))
//			EB.Debug.Log("had this orphan");
		
		Delegate RegisteredDelegate = null;
        if (false == s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
		{
            s_EventRegistry.Add(Key, Listener);
        }

		if ((null != RegisteredDelegate) && (RegisteredDelegate.GetType() != Listener.GetType()))
		{
            throw new Exception(string.Format("Attempting to add listener with inconsistent signature for event {0}.  Current listeners have type {1} and listener being added has type {2}.", Key.Value, RegisteredDelegate.GetType().Name, Listener.GetType().Name));
        }
		
		if (null != RegisteredDelegate)
		{
			s_EventRegistry[Key] = Delegate.Combine(RegisteredDelegate, Listener);
		}
		
		HashSet<Delegate> Listeners = null;
		
		if (false == s_BroadcastEventRegistry.TryGetValue(Key.Value, out Listeners))
		{
			Listeners = new HashSet<Delegate>();
			s_BroadcastEventRegistry.Add(Key.Value, Listeners);
		}

		Listeners.Add(Listener);
		
    }

    public static void OnListenerRemoving(KeyValuePair<GameObject, int> Key, Delegate Listener) 
	{		
		Delegate RegisteredDelegate = null;
        if (true == s_EventRegistry.TryGetValue(Key, out RegisteredDelegate)) 
		{
            if (null == RegisteredDelegate)
			{
                throw new Exception(string.Format("Attempting to remove listener for event {0} but current listener is null.", Key.Value));
            } 
			else if (RegisteredDelegate.GetType() != Listener.GetType()) 
			{
                throw new Exception(string.Format("Attempting to remove listener with inconsistent signature for event {0}.  Current listeners have type {1} and listener being removed has type {2}.", Key.Value, RegisteredDelegate.GetType(), Listener.GetType()));
            }
			
			Delegate NewDelegate = Delegate.Remove(RegisteredDelegate, Listener);
			
			if (null == NewDelegate)
			{
				s_EventRegistry.Remove(Key);
			}
			else
			{
				s_EventRegistry[Key] = NewDelegate;
			}
			
			HashSet<Delegate> Listeners = null;
		
			if (s_BroadcastEventRegistry.TryGetValue(Key.Value, out Listeners))
			{
				Listeners.Remove(Listener);
			}
			
			// We don't remove the broadcast key ever since its just a message index, not worth removing
        } 
		else 
		{
			throw new Exception(string.Format("Attempting to remove listener for unknown event {0}.", Key.Value));
        }
    }
	
	public static void ShowDebugInfo(bool UseTime)
	{
		if (Time.time - LastTime > 5.0f || !UseTime)
		{
			int count = 0;
			foreach(KeyValuePair<int, HashSet<Delegate>> item in s_BroadcastEventRegistry)
			{
				count += item.Value.Count;
				foreach(Delegate thing in item.Value)
				{
					MonoBehaviour RealTarget = thing.Target as MonoBehaviour;
					if (null == RealTarget)
					{
						EB.Debug.LogWarning("NULL THING:"+item.Key);
						OrphanedMessages.Add(item.Key);
					}
					else if (!UseTime)
					{
						EB.Debug.Log("Listener("+thing.Target+"):"+item.Key);
					}
				}
			}
			
			EB.Debug.Log("MSGS:"+count);
			LastTime = Time.time;
		}
	}
	
	public static void OnBroadcastToAll(string EventName, ref ArrayList CallbacksToFire)
	{
//		ShowDebugInfo(true);
		
		HashSet<Delegate> ListDelegates = null;
		int HashCode = EventName.GetHashCode();
		if (true == MessengerInternal.s_BroadcastEventRegistry.TryGetValue(HashCode, out ListDelegates))
		{
			CallbacksToFire = new ArrayList(ListDelegates.Count);
			
			foreach(Delegate Item in ListDelegates)
			{
				MonoBehaviour RealTarget = Item.Target as MonoBehaviour;
				if (null != RealTarget && RealTarget.enabled)
				{
					CallbacksToFire.Add(Item);
				}
			}
			
			if (CallbacksToFire.Count == 0)
			{
				CallbacksToFire = null;
			}
		}		
	}
}

public static class Messenger 
{
	public static void ShowDebugInfo()
	{
		MessengerInternal.ShowDebugInfo(false);	
	}
	
    public static void AddListener(GameObject GameObject, string EventName, Callback Handler) 
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());		
        MessengerInternal.OnListenerAdding(Key, Handler);
    }

    public static void RemoveListener(GameObject GameObject, string EventName, Callback Handler) 
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());		
        MessengerInternal.OnListenerRemoving(Key, Handler);
    }	
		
	public static bool Broadcast(GameObject GameObject, string EventName, bool IgnoreChildren) 
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback RegisteredCallback = RegisteredDelegate as Callback;
				if (null != RegisteredCallback)
				{
					RegisteredCallback();
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren)
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName);
				}
			}
		}
		
		return (Handled);
    }

	public static bool Broadcast(GameObject GameObject, string EventName)
	{
		return (Broadcast(GameObject, EventName, false));
	}

	public static bool BroadcastToAllListeners(string EventName)
	{
		bool Handled = false;

		ArrayList CallbacksToFire = null;
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);

		if (null != CallbacksToFire)
		{
			// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
			foreach (Callback CallbackToFire in CallbacksToFire)
			{
				CallbackToFire();			
			}
			Handled = true;
		}

		return (Handled);
	}
}

public static class Messenger<Type1>
{
	public static void AddListener(GameObject GameObject, string EventName, Callback<Type1> Handler) 
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());		
        MessengerInternal.OnListenerAdding(Key, Handler);
    }

	public static void RemoveListener(GameObject GameObject, string EventName, Callback<Type1> Handler) 
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());		
        MessengerInternal.OnListenerRemoving(Key, Handler);
    }	
	
	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, bool IgnoreChildren) 
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback<Type1> RegisteredCallback = RegisteredDelegate as Callback<Type1>;
				if (null != RegisteredCallback)
				{
					RegisteredCallback(ref Argument1);
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren)
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName, ref Argument1);
				}
			}
		}
		
		return (Handled);
    }

	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1)
	{
		return (Broadcast(GameObject, EventName, ref Argument1, false));
	}

	public static bool BroadcastToAllListeners(string EventName, ref Type1 Argument1)
	{
		bool Handled = false;
		ArrayList CallbacksToFire = null;
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);

		// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
		if (null != CallbacksToFire)
		{
			foreach (Callback<Type1> CallbackToFire in CallbacksToFire)
			{
				CallbackToFire(ref Argument1);
			}
			Handled = true;
		}

		return (Handled);
	}
}

public static class Messenger<Type1, Type2>
{
	public static void AddListener(GameObject GameObject, string EventName, Callback<Type1, Type2> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerAdding(Key, Handler);
	}

	public static void RemoveListener(GameObject GameObject, string EventName, Callback<Type1, Type2> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerRemoving(Key, Handler);
	}
	
	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, bool IgnoreChildren)
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback<Type1, Type2> RegisteredCallback = RegisteredDelegate as Callback<Type1, Type2>;
				if (null != RegisteredCallback)
				{
					RegisteredCallback(ref Argument1, ref Argument2);
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren)
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName, ref Argument1, ref Argument2);
				}
			}
		}

		return (Handled);
	}

	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2)
	{
		return (Broadcast(GameObject, EventName, ref Argument1, ref Argument2, false));
	}

	public static bool BroadcastToAllListeners(string EventName, ref Type1 Argument1, ref Type2 Argument2)
	{
		bool Handled = false;
		
		ArrayList CallbacksToFire = null;
		
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);
		
		// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
		if (null != CallbacksToFire)
		{
			foreach (Callback<Type1, Type2> CallbackToFire in CallbacksToFire)
			{
				CallbackToFire(ref Argument1, ref Argument2);
			}
			Handled = true;
		}
		
		return (Handled);
	}
}

public static class Messenger<Type1, Type2, Type3>
{
	public static void AddListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerAdding(Key, Handler);
	}

	public static void RemoveListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerRemoving(Key, Handler);
	}
	
	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, bool IgnoreChildren)
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback<Type1, Type2, Type3> RegisteredCallback = RegisteredDelegate as Callback<Type1, Type2, Type3>;
				if (null != RegisteredCallback)
				{
					RegisteredCallback(ref Argument1, ref Argument2, ref Argument3);
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren)
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName, ref Argument1, ref Argument2, ref Argument3);
				}
			}
		}

		return (Handled);
	}

	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3)
	{
		return (Broadcast(GameObject, EventName, ref Argument1, ref Argument2, ref Argument3, false));
	}

	public static bool BroadcastToAllListeners(string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3)
	{
		bool Handled = false;

		ArrayList CallbacksToFire = null;
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);

		// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
		if (null != CallbacksToFire)
		{
			foreach (Callback<Type1, Type2, Type3> CallbackToFire in CallbacksToFire)
			{
				CallbackToFire(ref Argument1, ref Argument2, ref Argument3);
			}
			Handled = true;
		}

		return (Handled);
	}
}

public static class Messenger<Type1, Type2, Type3, Type4>
{
	public static void AddListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3, Type4> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerAdding(Key, Handler);
	}

	public static void RemoveListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3, Type4> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerRemoving(Key, Handler);
	}
	
	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4, bool IgnoreChildren)
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback<Type1, Type2, Type3, Type4> RegisteredCallback = RegisteredDelegate as Callback<Type1, Type2, Type3, Type4>;
				if (null != RegisteredCallback)
				{
					RegisteredCallback(ref Argument1, ref Argument2, ref Argument3, ref Argument4);
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren) 
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName, ref Argument1, ref Argument2, ref Argument3, ref Argument4);
				}
			}
		}

		return (Handled);
	}

	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4)
	{
		return (Broadcast(GameObject, EventName, ref Argument1, ref Argument2, ref Argument3, ref Argument4, false));
	}

	public static bool BroadcastToAllListeners(string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4)
	{
		bool Handled = false;

		ArrayList CallbacksToFire = null;
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);

		// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
		if (null != CallbacksToFire)
		{
			foreach (Callback<Type1, Type2, Type3, Type4> CallbackToFire in CallbacksToFire)
			{
				CallbackToFire(ref Argument1, ref Argument2, ref Argument3, ref Argument4);
			}
			Handled = true;
		}

		return (Handled);
	}
}

public static class Messenger<Type1, Type2, Type3, Type4, Type5>
{
	public static void AddListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3, Type4, Type5> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerAdding(Key, Handler);
	}

	public static void RemoveListener(GameObject GameObject, string EventName, Callback<Type1, Type2, Type3, Type4, Type5> Handler)
	{
		KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
		MessengerInternal.OnListenerRemoving(Key, Handler);
	}
	
	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4, ref Type5 Argument5, bool IgnoreChildren)
	{
		bool Handled = false;

		if (null != GameObject)
		{
			KeyValuePair<GameObject, int> Key = new KeyValuePair<GameObject, int>(GameObject, EventName.GetHashCode());
			Delegate RegisteredDelegate;
			if (true == MessengerInternal.s_EventRegistry.TryGetValue(Key, out RegisteredDelegate))
			{
				Callback<Type1, Type2, Type3, Type4, Type5> RegisteredCallback = RegisteredDelegate as Callback<Type1, Type2, Type3, Type4, Type5>;
				if (null != RegisteredCallback)
				{
					RegisteredCallback(ref Argument1, ref Argument2, ref Argument3, ref Argument4, ref Argument5);
					Handled = true;
				}
				else
				{
					throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", Key.Value));
				}
			}

			if (false == IgnoreChildren)
			{
				foreach (Transform ChildTransform in GameObject.transform)
				{
					Broadcast(ChildTransform.gameObject, EventName, ref Argument1, ref Argument2, ref Argument3, ref Argument4, ref Argument5);
				}
			}
		}

		return (Handled);
	}

	public static bool Broadcast(GameObject GameObject, string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4, ref Type5 Argument5)
	{
		return (Broadcast(GameObject, EventName, ref Argument1, ref Argument2, ref Argument3, ref Argument4, ref Argument5, false));
	}

	public static bool BroadcastToAllListeners(string EventName, ref Type1 Argument1, ref Type2 Argument2, ref Type3 Argument3, ref Type4 Argument4, ref Type5 Argument5)
	{
		bool Handled = false;

		ArrayList CallbacksToFire = null;
		MessengerInternal.OnBroadcastToAll(EventName, ref CallbacksToFire);

		// A handler may remove its listener from the registry, so we don't want to fire callbacks while iterating through it.
		if (null != CallbacksToFire)
		{
			foreach (Callback<Type1, Type2, Type3, Type4, Type5> CallbackToFire in CallbacksToFire)
			{
				CallbackToFire(ref Argument1, ref Argument2, ref Argument3, ref Argument4, ref Argument5);
			}
			Handled = true;
		}

		return (Handled);
	}
}