using UnityEngine;
using System.Collections;

namespace EB
{
	// a event emitter similar to Node.js's event emitter
	public class EventEmitter : System.IDisposable
	{
		public struct Handle
		{
			public System.Delegate C;
			public EventEmitter P;
			public string N;
			
			public void Clear()
			{
				if (P != null) 
				{
					P.Remove(N,C);
					P= null;
				}
			}
		}
		
		private Hashtable _events = new Hashtable();
		private ArrayList _tmp = new ArrayList(16);
		
		public virtual void Dispose()
		{
			lock(_events)
			{
				_events.Clear();
			}
		}
		
		private Handle Add( string name, System.Delegate cb )
		{
			lock(_events)
			{
				var tmp = (ArrayList)_events[name];
				if ( tmp == null )
				{
					tmp = new ArrayList();
					_events[name] = tmp;
				}
				tmp.Add(cb);
				//Debug.Log("Added handler for " + name);
			}
			return new Handle{ C=cb,P=this,N=name };
		}
		
		private void Call( string name, params object[] pars )
		{
			lock(_tmp)
			{
				_tmp.Clear();
				lock(_events)
				{
					var tmp = (ArrayList)_events[name];
					if (tmp != null)
					{
						foreach( var t in tmp )
						{
							_tmp.Add(t);
						}
					}
					else
					{
						EB.Debug.Log("EE: no handlers for "  + name);
					}
				}
				
				foreach( System.Delegate cb in _tmp )
				{
					//_current = cb;
					cb.DynamicInvoke(pars);
				}
			}
		}
		
		public void Clear()
		{
			lock(_events)
			{
				//Debug.Log("EE: clear all ");
				_events.Clear();
			}
		}
		
		public void RemoveAll( string name )
		{
			lock(_events)
			{
				var tmp = (ArrayList)_events[name];
				if (tmp != null)
				{
					tmp.Clear();
				}
			}
		}
		
		public void Remove( string name, System.Delegate cb )
		{
			lock(_events)
			{
				var tmp = (ArrayList)_events[name];
				if (tmp != null)
				{
					if (!tmp.Contains(cb))
					{
						EB.Debug.LogError("Warning callback not found for name: " + name);
					}
					tmp.Remove(cb);
				}
			}
		}
		
		public void Remove( Handle handle )
		{
			Remove(handle.N, handle.C);
		}
		
		public Handle On( string name, Action callback )
		{
			return Add( name, callback );
		}
		
		public Handle On<Arg1>( string name, Action<Arg1> callback )
		{
			return Add( name, callback );
		}
		
		public Handle On<Arg1, Arg2>( string name, Action<Arg1,Arg2> callback )
		{
			return Add( name, callback );
		}
		
		public Handle On<Arg1, Arg2, Arg3>( string name, Action<Arg1,Arg2, Arg3> callback )
		{
			return Add( name, callback );
		}
		
		public Handle Once( string name, Action callback )
		{
			Handle h = default(Handle);
			h = Add( name, (Action)delegate(){
				h.Clear();
				callback();
			});
			return h;
		}
		
		public Handle Once<Arg1>( string name, Action<Arg1> callback )
		{
			Handle h = default(Handle);
			h = Add( name, (Action<Arg1>)delegate(Arg1 arg1){
				h.Clear();
				callback(arg1);
			});
			return h;
		}
		
		public Handle Once<Arg1, Arg2>( string name, Action<Arg1,Arg2> callback )
		{
			Handle h = default(Handle);
			h = Add( name, (Action<Arg1,Arg2>)delegate(Arg1 arg1, Arg2 arg2){
				h.Clear();
				callback(arg1, arg2);
			});
			return h;
		}
		
		public Handle Once<Arg1, Arg2, Arg3>( string name, Action<Arg1,Arg2, Arg3> callback )
		{
			Handle h = default(Handle);
			h = Add( name, (Action<Arg1,Arg2, Arg3>)delegate(Arg1 arg1, Arg2 arg2, Arg3 arg3){
				h.Clear();
				callback(arg1, arg2, arg3);
			});
			return h;
		}
		
		public void Emit( string name )
		{
			Call(name);
		}
		
		public void Emit<Arg1>( string name, Arg1 arg1 )
		{
			Call(name, arg1);
		}
		
		public void Emit<Arg1,Arg2>( string name, Arg1 arg1, Arg2 arg2 )
		{
			Call(name, arg1, arg2);
		}
		
		public void Emit<Arg1,Arg2,Arg3>( string name, Arg1 arg1, Arg2 arg2, Arg3 arg3 )
		{
			Call(name, arg1, arg2, arg3);
		}
		
		
	}
}
