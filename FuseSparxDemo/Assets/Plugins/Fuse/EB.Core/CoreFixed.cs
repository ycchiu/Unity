using UnityEngine;
using System.Collections;

namespace EB
{
	public static class Fixed
	{
		public static short ToFixed16(float value, int bits)
		{
			var mult = 1 << bits;
			return (short)(value * mult);
		}
		
		public static float FromFixed16( short value, int bits)
		{
			var mult = 1 << bits;
			return ((float)value) / mult;
		}
		
		public static byte ToFixed8(float value, int bits)
		{
			var mult = 1 << bits;
			return (byte)(value * mult);
		}
		
		public static float FromFixed8( byte value, int bits)
		{
			var mult = 1 << bits;
			return ((float)value) / mult;
		}
	}
	
	public partial class BitStream
	{
		public void SerializeFixed8( ref float value, int bits )
		{
			if (this.isReading)
			{
				byte b = _buffer.ReadByte();
				value = Fixed.FromFixed8(b, bits);
			}
			else
			{
				var b = Fixed.ToFixed8(value,bits);
				_buffer.WriteByte(b);
			}
		}
		
		public void SerializeFixed16( ref float value, int bits )
		{
			if (this.isReading)
			{
				var b = _buffer.ReadInt16LE();
				value = Fixed.FromFixed16(b, bits);
			}
			else
			{
				var b = Fixed.ToFixed16(value,bits);
				_buffer.WriteInt16LE(b);
			}
		}
		
		public void SerializeUInt24( ref uint value )
		{
			if (this.isReading)
			{
				value = 0;
				value += (uint)_buffer.ReadByte() << 16;
				value += (uint)_buffer.ReadByte() << 8;
				value += (uint)_buffer.ReadByte();
			}
			else
			{
				if ( value > 0xFFFFFF )
				{
					throw new System.Exception("uint too big to store in 24-bits");
				}
				
				var b = ( value & 0xFF0000 ) >> 16;
				_buffer.WriteByte( (byte)b);
				b = ( value & 0xFF00 ) >> 8;
				_buffer.WriteByte( (byte)b);
				b = ( value & 0xFF );
				_buffer.WriteByte( (byte)b);
			}
		}
	}
	
}

