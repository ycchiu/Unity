using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public class ThreadPool 
	{
		readonly int _maxThreads;
		
		public class AsyncTask
		{
			public System.Exception exception;
			public bool done;
			public System.Action<object> action;
			public object state;
		}
		
		Queue<AsyncTask> _queue;
		System.Threading.AutoResetEvent _event;
		bool _running = false;
		
		public ThreadPool(int maxThreads)
		{
			_maxThreads = maxThreads;
			_queue = new Queue<AsyncTask>(16);
			_event = new System.Threading.AutoResetEvent(false);
			Start();
		}
		
		public AsyncTask Queue( System.Action<object> action, object state )
		{
			AsyncTask task = new AsyncTask();
			task.action = action;
			task.state = state;
			
			lock(_queue)
			{
				_queue.Enqueue(task);
			}
			
			Wakeup();
			return task;
		}
		
		public void Wait()
		{
			// join the queue until its done
			while( JoinForOne() )
			{
			}
		}
		
		bool JoinForOne()
		{
			AsyncTask task = null;
					
			lock(_queue)
			{
				if ( _queue.Count > 0 )
				{
					task = _queue.Dequeue();
				}
			}
			
			if ( task == null )
			{
				return false;
			}
			
			try
			{
				task.action(task.state);
			}
			catch(System.Exception e ) 
			{
				task.exception = e;
			}
			task.done = true;
			return true;
		}
		
		void _Thread()
		{
			try 
			{
				System.Threading.Thread.CurrentThread.Name = "threadpool";
			}catch {}
			
			while (_running)
			{
				if (!JoinForOne())
				{
					_event.WaitOne();
				}
			}
		}
		
		void Start()
		{
			if ( _running == false )
			{
				_running = true;
				for ( int i = 0; i < _maxThreads; ++i )
				{
					var thread = new System.Threading.Thread(this._Thread, 256*1024);
					thread.Start();
				}
			}
		}
		
		void Wakeup()
		{
			// wake up threads
			try
			{
				_event.Set();
			}
			catch {}
		}
		
	}
}


