using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	// wrapper around a 64-bit id
	public class Id
	{
		public static Id Null 		= new Id(0);
		public static Id Offline 	= new Id(-1);
	
	    private long _value = 0;
		
		
		public long Value { get { return _value; } }
	    public bool Valid { get { return _value != 0; } }
		
		public Id() 
		{
			_value = 0;
		}
	
	    public Id( object value )
	    {
			var str = (value != null) ? value.ToString() : string.Empty;
			if ( !long.TryParse(str, out _value) )
			{
				_value = 0;
			}
	    }
	
	    public override bool Equals(object obj)
	    {
	        if ( obj != null )
	        {
	            var other = new Id(obj);
	            return other._value == _value;
	        }
	        return false;
	    }
		
		public override int GetHashCode()
		{
			return Hash.StringHash(ToString());
		}
		
	    public override string ToString()
	    {
	        return _value.ToString();
	    }
	
		public static bool operator ==(Id a, Id b)
		{
			var id1 = a ?? Null;
			var id2 = b ?? Null;
			return id1._value == id2._value;
		}
		
		public static bool operator !=(Id a, Id b)
		{
			return !(a == b);
		}
	}

}
