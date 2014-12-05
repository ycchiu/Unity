using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace EB
{
	public class CircularBuffer<T>
	{
		private T[] _buffer;
		private int _latestTailIndex = 0;
		private int _latestHeadIndex = 0;
		public int bufferSize { get; private set; }
		private int _count;
		public int Count
		{
			get { return _count; }
			set { _count = value; }
		}
		
		public CircularBuffer(int size)
		{
			bufferSize = size;
			_latestTailIndex = 0;
			_latestHeadIndex = 0;
			_buffer = new T[bufferSize];
		}

		public void Reset()
		{
			_latestHeadIndex = 0;
			_latestTailIndex = 0;
			_count = 0;
		}

		public bool Enqueue(T item)
		{
			if (_count == bufferSize)
			{
				Debug.LogWarning("Enqueue called on Full Buffer");
				return false;
			}

			_buffer[_latestHeadIndex] = item;

			_latestHeadIndex++;

			if (_latestHeadIndex == bufferSize)
			{
				_latestHeadIndex = 0;
			}
			
			_count++;

			return true;
		}

		public T Dequeue()
		{
			if(_count == 0) 
			{
				Debug.LogWarning("Dequeue called on Empty Buffer");
				return default(T);
			}

			var poped = _buffer[_latestTailIndex];

			_buffer[_latestTailIndex] = default(T);

			_latestTailIndex++;

			if (_latestTailIndex == bufferSize)
			{
				_latestTailIndex = 0;
			}

			_count--;

			return poped;
		}

		public T this[int i] 	
		{ 		
			get 		
			{ 
				if(i > _count) 
				{
					Debug.LogError("Item out of range, returning head index.");
					return _buffer[_latestHeadIndex];
				}

				int current = _latestHeadIndex - i - 1;
				current = current >= 0 ? current : current + bufferSize;
				return _buffer[current]; 		
			} 	
			
			set 		
			{ 			
				_buffer[i] = value;
				
			}
		}

		public T Peek()
		{
			if(_count == 0) 
			{
				Debug.LogWarning("Peek on empty Buffer");
				return default(T);
			}
			else 
			{
				return _buffer[_latestTailIndex]; 
			}
		} 	

	}
}
