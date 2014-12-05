using UnityEngine;
using System.Collections;

namespace EB.Collections
{
	public class Queue<T> : System.Collections.Generic.List<T>
	{
	    public Queue()
	    {
	
	    }
	
	    public Queue(int capacity)
	        : base(capacity)
	    {
	
	    }
	
	    public void Enqueue(T obj)
	    {
	        this.Add(obj);
	    }
	
	    public T Peek()
	    {
	        return this[0];
	    }
	
	    public T Dequeue()
	    {
	        T value = this[0];
	        RemoveAt(0);
	        return value;
	    }
	}

	public class Stack<T> : System.Collections.Generic.List<T>
	{
	    public Stack() { }
	
	    public void Push(T obj)
	    {
	        this.Add(obj);
	    }
	
	    public T Peek()
	    {
	        return this[this.Count - 1];
	    }
	
	    public T Pop()
	    {
	        T value = this[this.Count - 1];
	        this.RemoveAt(this.Count - 1);
	        return value;
	    }
	}

	// Basic Tuple since mono doesn't support .net tuples.
	public class Tuple<T1, T2>
	{
		public T1 Item1;
		public T2 Item2;

		public Tuple(T1 item1, T2 item2)
		{
			Item1 = item1;
			Item2 = item2;
		}
	}
	
	public class CircularBuffer : System.Collections.IEnumerable
	{
		private object[] _buffer;
		private int _head = 0;
		private int _tail = 0;
		
		public CircularBuffer( int size )
		{
			_buffer = new object[size];
		}
		
		public void Clear()
		{
			_head = _tail = 0;
		}
		
		public int Count
		{
			get
			{
				return ((_tail+_buffer.Length)-_head) % _buffer.Length;
			}
		}
		
		public object[] ToArray()
		{
			var array = new object[Count];
			for ( int i = 0; i < array.Length; ++i )
			{
				var idx = (_head+i)%_buffer.Length;
				array[i] = _buffer[idx];
			}
			return array;
		}
		
		public void Push( object obj )
		{
			_buffer[_tail] = obj;
			_tail = (_tail+1)%_buffer.Length;
			if ( _tail == _head )
			{
				_head = (_head+1)%_buffer.Length;;
			}
		}
		
		public System.Collections.IEnumerator GetEnumerator ()
		{
			int i = _head;
			while ( i != _tail )
			{
				yield return _buffer[i];
				i = (i+1)%_buffer.Length;
			}
		}
		
	}	
}

namespace System.Collections.Generic
{
	public static class List
	{
		// moko: In-place array shuffle extension method for generic IList 
		public static void Shuffle<T>(this System.Collections.Generic.IList<T> list)  
		{  
			int n = list.Count;  
			while (n > 1) {  
				n--;  
				int k = UnityEngine.Random.Range(0, n);  
				T value = list[k];  
				list[k] = list[n];  
				list[n] = value;  
			}  
		}
	}

}


