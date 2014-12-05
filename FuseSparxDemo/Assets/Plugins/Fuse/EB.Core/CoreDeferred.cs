using UnityEngine;
using System.Collections;

namespace EB
{
	// deferred callbacks
	// a thread safe wrapper moslty used to invoke callbacks between threads
	public class Deferred : System.IDisposable
	{
		struct Info		
		{
			public System.Delegate Callback;
			public  object[] Args;
		}
		ArrayList _list;
		ArrayList _copy;
		
		
		public Deferred( int capacity )
		{
			_list = new ArrayList(capacity);
			_copy = new ArrayList(capacity);
		}
		
		public void Defer( System.Delegate callback, params object[] args )
		{
			lock(_list)
			{
				_list.Add( new Info(){ Callback=callback,Args=args }); 		
			}
		}
		
		public void Dispose ()
		{
			lock(_list)
			{
				_list.Clear();
			}
		}
		
		public void Dispatch()
		{
			lock(_list)
			{
				if (_list.Count==0)
				{
					return;
				}
				
				_copy.Clear();
				foreach( var cb in _list )
				{
					_copy.Add(cb);
				}
				_list.Clear();
			}
		
			foreach( Info info in _copy )
			{
				info.Callback.DynamicInvoke(info.Args);
			}
			_copy.Clear();
		}
	}
}

