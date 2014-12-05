using UnityEngine;
using System.Collections;

namespace EB
{
	public class Coroutines  
	{
		class IntervalHandle
		{	
			public bool Running = true;
			public Coroutine Couroutine = null;
		}
		
		private static CoreCoroutines _this = null;
		
		public static Coroutine Run( IEnumerator function )
		{
			if ( _this == null )
			{
				GameObject go = new GameObject("COROUTINES");
				go.hideFlags = HideFlags.HideAndDontSave;
				GameObject.DontDestroyOnLoad(go);
				_this = go.AddComponent<CoreCoroutines>();
			}
			return _this.StartCoroutine(function);
		}
		
		public static object SetUpdate( EB.Action cb )
		{
			IntervalHandle handle = new IntervalHandle();
			Run (_SetUpdate(handle,cb));
			return handle;
		}
		
		public static void ClearUpdate( object handle )
		{
			if ( handle != null && handle is IntervalHandle )
			{
				((IntervalHandle)handle).Running = false;
			}
		}
		
		public static object SetInterval( EB.Action cb, int ms )
		{
			IntervalHandle handle = new IntervalHandle();
			Run (_SetInterval(handle,cb,ms));
			return handle;
		}
		
		public static void ClearInterval( object handle )
		{
			if ( handle != null && handle is IntervalHandle )
			{
				((IntervalHandle)handle).Running = false;
			}
		}
		
		static IEnumerator _SetUpdate( IntervalHandle handle, EB.Action cb )
		{
			while(true)
			{
				if (handle.Running)
				{
					cb();
				}
				else 
				{
					yield break;
				}
				
				yield return 1;
			}
		}
		
		static IEnumerator _SetInterval( IntervalHandle handle, EB.Action cb, int ms )
		{
			var seconds = ms / 1000.0f;
			while(true)
			{
				var end = Time.realtimeSinceStartup + seconds;
				while ( Time.realtimeSinceStartup < end )
				{
					yield return 1;
				}
				
				if ( handle.Running )
				{
					cb();
				}
				else
				{
					yield break;
				}
			}
		}
		
		public static object SetTimeout( EB.Action cb, int ms )
		{
			IntervalHandle handle = new IntervalHandle();
			handle.Couroutine = Run(_SetTimeout(handle,cb,ms));
			return handle;
		}
		
		public static void ClearTimeout( object handle )
		{
			if (handle != null && handle is IntervalHandle)
			{
				((IntervalHandle)handle).Running = false;
			}
		}
		
		static IEnumerator _SetTimeout( IntervalHandle handle, EB.Action cb, int ms )
		{
			var seconds = ms / 1000.0f;
			yield return new WaitForSeconds(seconds);
			if (handle.Running) {
				cb();
			}
		}
		
		public static Coroutine NextFrame( EB.Action cb )
		{
			return Run(_NextFrame(cb));
		}
		
		static IEnumerator _NextFrame( EB.Action cb )
		{
			yield return 1;
			cb();
		}
		
		public static Coroutine EndOfFrame( EB.Action cb )
		{
			return Run(_EndOfFrame(cb));
		}
		
		static IEnumerator _EndOfFrame( EB.Action cb )
		{
			yield return new WaitForEndOfFrame();
			cb();
		}
	}	
}


// for unity
public class CoreCoroutines : MonoBehaviour {}

