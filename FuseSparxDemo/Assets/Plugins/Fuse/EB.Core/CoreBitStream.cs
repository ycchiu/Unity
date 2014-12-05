#define PACK_BOOL
//#define USE_FARSEER
using System;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public partial class BitStream : System.IDisposable
	{
		private Buffer 	_buffer;
		private int    	_bufferStart;
		
		private int 	_bitIndex = -1;
		private int 	_bitShift = -1;
		
		public bool isReading { get;private set; }
		public bool isWriting { get { return !isReading; } }
		
		public bool DataAvailable
		{
			get
			{
				return _buffer.Length < _buffer.Capacity;
			}
		}
				
		public BitStream( int capacity )
		{
			isReading 		= false;
			_buffer 		= new Buffer(capacity);
		}
		
		public BitStream( byte[] data ) 
		{
			isReading 	= true;
			_buffer		= new Buffer( data, false );
			_buffer.Reset();
		}
		
		public BitStream( Buffer buffer, bool write )
		{
			isReading = !write;
			_buffer	  = buffer;
			_bufferStart = buffer.Length;
		}
		
		public Buffer Slice()
		{
			if (isWriting )
			{
				var start = _bufferStart;
				_bufferStart = _buffer.Length;
				return _buffer.Slice(start, _buffer.Length, true);
			}
			throw new System.Exception("Can't call to buffer when writing");
		}
		
		public void Reset()
		{
			_buffer.Reset();
			_bufferStart = _buffer.Length;
		}
		
		public int Reserve()
		{
			if ( isWriting )
			{
				var index = _buffer.Length;
				_buffer.WriteByte(0);
				return index;
			}
			throw new System.Exception("Can't reserve space while reading");
		}
		
		public void Poke( int index, byte data )
		{
			if ( isWriting )
			{
				_buffer[index] = data;
				return;
			}
			throw new System.Exception("Can't poke while reading");
		}
		
#if PACK_BOOL
		public void ResetBoolFlag()
		{
			_bitIndex = -1;
		}
		
		public void Serialize( ref bool data )
		{
			if (isReading)
			{
				if (_bitIndex == -1)
				{
					_bitIndex = _buffer.Length;
					_buffer.ReadByte();
					_bitShift = 0;
				}
				
				var b = _buffer[_bitIndex];
				var m = 1 << _bitShift;
				
				data = (b&m) != 0;
				
				_bitShift++;
				if (_bitShift == 8)
				{
					_bitIndex = -1;
				}
			}
			else
			{
				// see if we can use the current bit
				if ( _bitIndex == -1 )
				{
					_bitIndex = _buffer.Length;
					_buffer.WriteByte(0);
					_bitShift = 0;
				}
				
				var b  = _buffer[_bitIndex];
				if (data)
				{
					b |= (byte)(1 << _bitShift);
				}
				
				_buffer[_bitIndex] = b;
				
				_bitShift++;
				if (_bitShift == 8)
				{
					//Debug.Log("wrapping bit mask");
					_bitIndex = -1;
				}
			}
		}
#else
		public void Serialize( ref bool data )
		{
			if (isReading)
			{
				data = _buffer.ReadByte() != 0;
			}
			else
			{
				_buffer.WriteByte( data ? (byte)1 : (byte)0 );
			}
		}
#endif
		
		public void Serialize( ref byte data )
		{
			if (isReading)
			{
				data = _buffer.ReadByte();
			}
			else
			{
				_buffer.WriteByte(data);
			}
		}
		
		public void Serialize( ref SafeInt data )
		{
			if ( isReading )
			{
				data = _buffer.ReadInt32LE();
			}
			else
			{
				_buffer.WriteInt32LE( data ); 
			}
		}
		
		public void Serialize( ref SafeFloat data )
		{
			if ( isReading )
			{
				data = _buffer.ReadFloatLE();
			}
			else
			{
				_buffer.WriteFloatLE( data ); 
			}
		}
		
		public void Serialize( ref byte[] data )
		{
			if (isReading)
			{
				int size = _buffer.ReadByte();
				
				if ( size == byte.MaxValue )
				{
					// read 16-bit length
					size = _buffer.ReadUInt16LE();
				}
				
				var src 	= _buffer.ReadBytes(size);
				data 		= new byte[size];
				System.Array.Copy( src.Array, src.Offset, data, 0, size ); 
			}
			else
			{
				// check size
				int size = data.Length;
				if ( size >= ushort.MaxValue )
				{
					throw new System.ArgumentException("Byte array too large for bitstream " + size );
				}
				else if ( size >= byte.MaxValue )
				{
					_buffer.WriteByte( byte.MaxValue);
					_buffer.WriteUInt16LE( (ushort)size );
				}
				else
				{
					_buffer.WriteByte( (byte)size );
				}
				_buffer.WriteBytes( data ); 
			}
		}
		
		public void Serialize( ref Buffer data )
		{
			if (isReading)
			{
				int size = _buffer.ReadByte();
				
				if ( size == byte.MaxValue )
				{
					// read 16-bit length
					size = _buffer.ReadUInt16LE();
				}
				
				data = new Buffer( _buffer.ReadBytes(size), false ); 
			}
			else
			{
				// check size
				int size = data.Length;
				if ( size >= ushort.MaxValue )
				{
					throw new System.ArgumentException("Byte array too large for bitstream " + size );
				}
				else if ( size >= byte.MaxValue )
				{
					_buffer.WriteByte( byte.MaxValue);
					_buffer.WriteUInt16LE( (ushort)size );
				}
				else
				{
					_buffer.WriteByte( (byte)size );
				}
				_buffer.WriteBuffer( data ); 
			}
		}
		
		public void Serialize( ref ushort data )
		{
			if (isReading)
			{
				data = _buffer.ReadUInt16LE();
			}
			else
			{
				_buffer.WriteUInt16LE( data );
			}
		}
		
		public void Serialize( ref short data )
		{
			if (isReading)
			{
				data = _buffer.ReadInt16LE();
			}
			else
			{
				_buffer.WriteInt16LE( data );
			}
		}
		
		public void Serialize( ref uint data )
		{
			if (isReading)
			{
				data = _buffer.ReadUInt32LE();
			}
			else
			{
				_buffer.WriteUInt32LE( data );
			}
		}
		
		public void Serialize( ref int data )
		{
			if (isReading)
			{
				data = _buffer.ReadInt32LE();
			}
			else
			{
				_buffer.WriteInt32LE( data );
			}
		}
		
		public void Serialize( ref ulong data )
		{
			if (isReading)
			{
				data = _buffer.ReadUInt64LE();
			}
			else
			{
				_buffer.WriteUInt64LE( data );
			}
		}
		
		public void Serialize( ref long data )
		{
			if (isReading)
			{
				data = _buffer.ReadInt64LE();
			}
			else
			{
				_buffer.WriteInt64LE( data );
			}
		}
		
		public void Serialize( ref float data )
		{
			if (isReading)
			{
				data = _buffer.ReadFloatLE();
			}
			else
			{
				_buffer.WriteFloatLE( data );
			}
		}
		
		public void Serialize( ref double data )
		{
			if (isReading)
			{
				data = _buffer.ReadDoubleLE();
			}
			else
			{
				_buffer.WriteDoubleLE( data );
			}
		}
		
		public void Serialize( ref string data )
		{
			if (isReading)
			{
				data = _buffer.ReadString();
			}	
			else
			{
				_buffer.WriteString(data);
			}
		}
		
		
		
		public void Serialize( ref UnityEngine.Vector2 data )
		{
			Serialize( ref data.x );
			Serialize( ref data.y );
		}
		
		public void Serialize( ref UnityEngine.Vector3 data )
		{
			Serialize( ref data.x );
			Serialize( ref data.y );
			Serialize( ref data.z );
		}
		
		public void Serialize( ref UnityEngine.Vector4 data )
		{
			Serialize( ref data.x );
			Serialize( ref data.y );
			Serialize( ref data.z );
			Serialize( ref data.w );
		}
		
		public void Serialize( ref UnityEngine.Quaternion data )
		{
			if ( isReading )
			{
				var angles = default(UnityEngine.Vector3);
				Serialize(ref angles);
				data = UnityEngine.Quaternion.Euler(angles);
			}
			else
			{
				var angles = data.eulerAngles;
				Serialize( ref angles );
			}
		}
		
#if USE_FARSEER
		public void Serialize( ref Microsoft.Xna.Framework.FVector2 data )
		{
			Serialize(ref data.X);
			Serialize(ref data.Y);
		}
		
		public void Serialize( ref Microsoft.Xna.Framework.FVector3 data )
		{
			Serialize(ref data.X);
			Serialize(ref data.Y);
			Serialize(ref data.Z);
		}
#endif
		
		#region IDisposable implementation
		public void Dispose ()
		{
			
		}
		#endregion
		
	}
}

