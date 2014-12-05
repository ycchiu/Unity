using System;
using System.Collections;

namespace EB
{
	public class Buffer
	{
		byte[] 	_buffer;	// working buffer
		int		_offset;	// offset start
		int		_capacity;	// size of buffer to work with
		int		_length;	// # of bytes read/written
		
		public  int Length
		{
			get
			{
				return _length;
			}
		}
		
		public  int Capacity
		{
			get
			{
				return _capacity;
			}
		}
		
		public byte this[int index]
		{
		    get
		    {
		        return _buffer[_offset+index];
		    }
		    set
		    {
				if ( index >= _capacity )
				{
					throw new System.IndexOutOfRangeException("Buffer Overflow");
				}
		        _buffer[_offset+index] = value;
		    }
		}
		
		public Buffer( int capacity )	
		{
			_buffer 	= new byte[capacity];
			_offset 	= 0;
			_capacity	= capacity;
			_length  	= 0;
		}
		
		public Buffer( byte[] bytes, bool setLength )
		{
			_buffer 	= bytes;
			_offset 	= 0;
			_capacity 	= bytes.Length;
			_length		= setLength ? _capacity : 0;
		}
		
		public Buffer( ArraySegment<byte> segment, bool setLength )
		{
			_buffer 	= segment.Array;
			_offset 	= segment.Offset;
			_capacity	= segment.Count;
			_length		= setLength ? _capacity : 0;
		}
		
		public void Reset()
		{
			_length = 0;
		}
		
		public byte ReadByte()
		{
			return this[_length++];
		}
		
		public int Digest()
		{
			return Hash.FNV32( _buffer, _offset, _length, (int)Hash.HASH_INIT_32); 
		}
		
		public void WriteBuffer( Buffer other )
		{
			for ( int i = 0; i < other.Length; ++i )
			{
				WriteByte( other[i] ); 
			}
		}
		
		public void WriteByte(byte value)
		{
			this[_length++] = value;
		}
		
		
		public void Sign( Hmac hmac )
		{
			hmac.Update(_buffer, _offset, Length);
			var digest = hmac.Final();
			WriteBytes(digest);
		}
		
		public bool Verify( Hmac hmac )
		{
			var ds =hmac.DigestSize;
			if (_capacity < ds)
			{
				return false;
			}
			
			hmac.Update(_buffer, _offset, _capacity-ds);
			var digest = hmac.Final();
			
			var bufferIndex = _offset + _capacity-digest.Length;
			for( int i = 0; i < digest.Length; ++i, ++bufferIndex )
			{
				if (_buffer[bufferIndex] != digest[i] )
				{
					return false;
				}
			}
			// remove the digest from the end of the block
			_capacity -= digest.Length;
			return true;;
		}
		
		public void WriteBytes(byte[] array)
		{
			var count = array.Length;
			if ( count + _length > _capacity ) 
			{
				throw new System.ArgumentOutOfRangeException("WriteBytes buffer overflow length: " + Length + " count:" + count + " capacity" + _capacity);
			}
			
			var index = _offset + _length;
			for ( int i = 0; i < count; ++i )
			{
				_buffer[index++] = array[i];
			}
			_length += array.Length;
		}
		
		public ArraySegment<byte> ReadBytes( int count )
		{
			if ( count + Length > _capacity ) 
			{
				throw new System.ArgumentOutOfRangeException("ReadBytes buffer underun length: " + Length + " count:" + count + " capacity" + _capacity);
			}
			
			var result = new ArraySegment<byte>( _buffer, _offset + _length, count );
			_length += count;
			return result;
		}
		
		public Buffer Slice( int start, int end, bool setLength )
		{
			return new Buffer( new ArraySegment<byte>( _buffer, _offset+start, end-start), setLength );  
		}
		
		public Buffer Slice( int start )
		{
			return Slice(start, Length, true);
		}
		
		public Buffer Slice()
		{
			return Slice(0, Length, true);
		}
		
		public Buffer Clone()
		{
			return Slice(0, Length, false);
		}
		
		public Buffer Remaining()
		{
			return Slice(Length,Capacity, false);
		}
				
		public ArraySegment<byte> ToArraySegment(bool all)
		{
			return new ArraySegment<byte>( _buffer, _offset, all ? _capacity : _length ); 
		}
		
		// uint16
		public UInt16 ReadUInt16LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 2 );
			}
			
			var result = BitConverter.ToUInt16( _buffer, _offset + _length );
			_length += 2;
			return result;
		}
		
		public UInt16 ReadUInt16BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 2 );
			}
			
			var result = BitConverter.ToUInt16( _buffer, _offset + _length );
			_length += 2;
			return result;
		}
		
		public void WriteUInt16LE( UInt16 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteUInt16BE( UInt16 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		// int16
		public Int16 ReadInt16LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 2 );
			}
			
			var result = BitConverter.ToInt16( _buffer, _offset + _length );
			_length += 2;
			return result;
		}
		
		public Int16 ReadInt16BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 2 );
			}
			
			var result = BitConverter.ToInt16( _buffer, _offset + _length );
			_length += 2;
			return result;
		}
		
		public void WriteInt16LE( Int16 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteInt16BE( Int16 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		// uint32
		public UInt32 ReadUInt32LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToUInt32( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public UInt32 ReadUInt32BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToUInt32( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public void WriteUInt32LE( UInt32 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteUInt32BE( UInt32 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		// int32
		public Int32 ReadInt32LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToInt32( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public Int32 ReadInt32BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToInt32( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public void WriteInt32LE( Int32 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteInt32BE( Int32 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		//uint64
		public UInt64 ReadUInt64LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToUInt64( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public UInt64 ReadUInt64BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToUInt64( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public void WriteUInt64LE( UInt64 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteUInt64BE( UInt64 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		//int64
		public Int64 ReadInt64LE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToInt64( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public Int64 ReadInt64BE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToInt64( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public void WriteInt64LE( Int64 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteInt64BE( Int64 value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		// float
		public float ReadFloatLE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToSingle( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public float ReadFloatBE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 4 );
			}
			
			var result = BitConverter.ToSingle( _buffer, _offset + _length );
			_length += 4;
			return result;
		}
		
		public void WriteFloatLE( float value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteFloatBE( float value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		// double
		public double ReadDoubleLE()
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToDouble( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public double ReadDoubleBE()
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(_buffer, _offset + _length, 8 );
			}
			
			var result = BitConverter.ToDouble( _buffer, _offset + _length );
			_length += 8;
			return result;
		}
		
		public void WriteDoubleLE( double value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteDoubleBE( double value )
		{
			var bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			WriteBytes(bytes);
		}
		
		public void WriteString( string value )
		{
			var bytes = Encoding.GetBytes(value);
			WriteBytes( bytes );
			WriteByte( 0 ); // null terminator
		}
		
		public string ReadString()
		{
			int start = _length;
			for (;_length < _capacity; ++_length)
			{
				// find null termination
				if (this[_length] == 0 )
				{
					break;
				}
			}
			var result = Encoding.GetString( _buffer, _offset + start, _length - start ); 
			_length += 1; // skip the null
			return result;
		}
		
		public string ToHexString()
		{
			return Encoding.ToHexString(_buffer, _offset, Length);
		}
		
		public override string ToString ()
		{
			return string.Format ("[Buffer: Length={0}, Capacity={1}, Offset={2}]", Length, Capacity, _offset);
		}
		
	}
}
