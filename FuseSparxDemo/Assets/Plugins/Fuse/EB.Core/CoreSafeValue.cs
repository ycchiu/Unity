using UnityEngine;
using System;
using System.Collections;

namespace EB
{
		// safe value, a salted + hash memory checking value
	public class SafeValue
    {
        private byte[] _v;
        private long _h;
        private long _r;

        public static bool Breach = false;

        public SafeValue( byte[] v )
        {
            _v = v;
            _r = System.DateTime.Now.Ticks;
            _h = Hash.FNV64(_v, _r);
        }

        public byte[] Get()
        {
            // check to see if our memory was hacked!
            long hash = Hash.FNV64(_v, _r);
            if (hash != _h)
            {
                Breach = true;
                return new byte[_v.Length];
            }
            return _v;
        }
    }

    public struct SafeBool
    {
        private SafeValue _v;

        public static implicit operator SafeBool(bool value)
        {
            SafeBool v = new SafeBool();
            v.Value = value;
            return v;
        }

        public static implicit operator bool(SafeBool rhs)
        {
            return rhs.Value;
        }

        public bool Value
        {
            get { return _v != null ? System.BitConverter.ToBoolean(_v.Get(), 0) : false; }
            set { _v = new SafeValue(System.BitConverter.GetBytes(value)); }
        }

		#region ISerializable implementation
		public object Serialize()
		{
			return Value;
		}

		void Deserialize (object src)
		{
			bool value = false;
			if ( bool.TryParse(src.ToString(), out value))
			{
				Value = value;
			}
		}
		#endregion

        public override bool Equals(object obj)
        {
            if (obj is bool)
            {
                return Value == (bool)obj;
            }	
            else if (obj is SafeBool)
            {
                return Value == ((SafeBool)obj).Value;
            }
            return obj.Equals(Value);
        }

        public override int GetHashCode()
        {
            return Value ? 1 : 0;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public struct SafeInt //: EB.ISerializable
    {
        private SafeValue _v;

        public static implicit operator SafeInt(int value )
        {
            SafeInt v = new SafeInt();
            v.Value = value;
            return v;
        }

        public static implicit operator int(SafeInt rhs)
        {
            return rhs.Value;
        }

        public static SafeInt operator +(SafeInt a, SafeInt b)
        {
            return a.Value + b.Value;
        }

        public static SafeInt operator -(SafeInt a, SafeInt b)
        {
            return a.Value - b.Value;
        }

        public static SafeInt operator --(SafeInt a)
        {
            return a.Value - 1;
        }

        public static SafeInt operator ++(SafeInt a)
        {
            return a.Value + 1;
        }

        public int Value 
        {
            get { return _v != null ? BitConverter.ToInt32(_v.Get(), 0) : 0; }
            set { _v = new SafeValue(BitConverter.GetBytes(value));  }
        }
		
        public override bool Equals(object obj)
        {
            if (obj is int)
            {
                return Value == (int)obj;
            }
            else if (obj is SafeInt)
            {
                return Value == ((SafeInt)obj).Value;
            }
            return obj.Equals(Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

		#region ISerializable implementation
		public object Serialize()
		{
			return Value;
		}

		void Deserialize (object src)
		{
			int value = 0;
			if ( int.TryParse(src.ToString(), out value))
			{
				Value = value;
			}
		}
		#endregion
    }

    public struct SafeFloat //: EB.ISerializable
    {
        private SafeValue _v;

        public static implicit operator SafeFloat(float value)
        {
            SafeFloat v = new SafeFloat();
            v.Value = value;
            return v;
        }

        public static implicit operator float(SafeFloat rhs)
        {
            return rhs.Value;
        }

        public static SafeFloat operator +(SafeFloat a, SafeFloat b)
        {
            return a.Value + b.Value;
        }

        public static SafeFloat operator -(SafeFloat a, SafeFloat b)
        {
            return a.Value - b.Value;
        }

        public static SafeFloat operator --(SafeFloat a)
        {
            return a.Value - 1;
        }

        public static SafeFloat operator ++(SafeFloat a)
        {
            return a.Value + 1;
        }

        public float Value
        {
            get { return _v != null ? BitConverter.ToSingle(_v.Get(), 0) : 0; }
            set { _v = new SafeValue(BitConverter.GetBytes(value)); }
        }

		public object Serialize()
		{
			return Value;
		}

		void Deserialize (object src)
		{
			float value = 0;
			if ( float.TryParse(src.ToString(), out value))
			{
				Value = value;
			}
		}

        public override bool Equals(object obj)
        {
            if (obj is float)
            {
                return Value == (float)obj;
            }
            else if (obj is SafeFloat)
            {
                return Value == ((SafeFloat)obj).Value;
            }
            return obj.Equals(Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public static class SafeAction
    {
        public static Action Wrap(MonoBehaviour b, Action cb)
        {
            Action action = delegate()
            {
                if (b != null)
                {
                    cb();
                }
            };
            return action;
        }

        public static Action<T> Wrap<T>(MonoBehaviour b, Action<T> cb)
        {
            Action<T> action = delegate(T obj)
            {
                if ( b != null )
                {
                    cb(obj);
                }
            };
            return action;
        }

        public static Action<T, U> Wrap<T, U>(MonoBehaviour b, Action<T, U> cb)
        {
            Action<T,U> action = delegate(T obj1, U obj2)
            {
                if (b != null)
                {
                    cb(obj1, obj2);
                }
            };
            return action;
        }

		public static Action<T, U, V> Wrap<T, U, V>(MonoBehaviour b, Action<T, U, V> cb)
		{
			Action<T,U, V> action = delegate(T obj1, U obj2, V obj3)
			{
				if (b != null)
				{
					cb(obj1, obj2, obj3);
				}
			};
			return action;
		}
    }
}
