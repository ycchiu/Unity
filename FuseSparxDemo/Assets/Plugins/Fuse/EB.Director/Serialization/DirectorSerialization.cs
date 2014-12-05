using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace EB.Director.Serialization
{		
	[System.Serializable]
	public class KeyFrame
	{
		public int frame;
		public BlendMode mode = BlendMode.Linear;
		public byte[] data = new byte[0];
	
		public KeyFrame Clone()
		{
			KeyFrame clone = new KeyFrame();
			clone.frame = frame;
			clone.mode = mode;
			clone.data = (byte[])data.Clone();
			return clone;
		}
		
		void AddRange( List<byte> b, byte[] d ) 
		{
			foreach( var bb in d )
			{
				b.Add(bb);
			}
		}
		
		public bool BoolValue 
		{
			get
			{
				if ( data.Length > 0 ) return data[0] != 0;
				return default(bool);
			}
			set
			{
				data = new byte[1] { value ? (byte)1 : (byte)0 };
			}
		}
		
		public byte ByteValue 
		{
			get
			{
				if ( data.Length > 0 ) return data[0];
				return default(byte);
			}
			set
			{
				data = new byte[1] { value };
			}
		}
		
		public float FloatValue
		{
			get
			{
				if ( data.Length > 0 ) return BitConverter.ToSingle(data,0);
				return 0;
			}
			set
			{
				data = System.BitConverter.GetBytes( value ); 
			}
		}
		
		public Vector2 Vector2Value
		{
			get
			{
				if ( data.Length > 0 ) return new Vector2( BitConverter.ToSingle(data,0), BitConverter.ToSingle(data,4) );
				return Vector2.zero;
			}
			set
			{
				List<byte> tmp = new List<byte>(2 * 4);
				AddRange( tmp, BitConverter.GetBytes(value.x) );
				AddRange( tmp, BitConverter.GetBytes(value.y) );
				data = tmp.ToArray();
			}
		}
		
		public Vector3 Vector3Value 
		{
			get
			{
				if ( data.Length > 0 ) return new Vector3( BitConverter.ToSingle(data,0), BitConverter.ToSingle(data,4), BitConverter.ToSingle(data,8) );
				return Vector3.zero;
			}
			set
			{
				List<byte> tmp = new List<byte>(3 * 4);
				AddRange( tmp, BitConverter.GetBytes(value.x) );
				AddRange( tmp, BitConverter.GetBytes(value.y) );
				AddRange( tmp, BitConverter.GetBytes(value.z) );
				data = tmp.ToArray();
			}
		}
		
		public Vector4 Vector4Value
		{
			get
			{
				if ( data.Length > 0 ) return new Vector4( BitConverter.ToSingle(data,0), BitConverter.ToSingle(data,4), BitConverter.ToSingle(data,8), BitConverter.ToSingle(data,12) );
				return Vector4.zero;
			}
			set
			{
				List<byte> tmp = new List<byte>(4 * 4);
				AddRange( tmp, BitConverter.GetBytes(value.x) );
				AddRange( tmp, BitConverter.GetBytes(value.y) );
				AddRange( tmp, BitConverter.GetBytes(value.z) );
				AddRange( tmp, BitConverter.GetBytes(value.w) );
				data = tmp.ToArray();
			}
		}
		
		public Color ColorValue
		{
			get
			{
				if ( data.Length > 0 ) return new Color( BitConverter.ToSingle(data,0), BitConverter.ToSingle(data,4), BitConverter.ToSingle(data,8), BitConverter.ToSingle(data,12) );
				return new Color(0,0,0,0);
			}
			set
			{
				List<byte> tmp = new List<byte>(4 * 4);
				AddRange( tmp, BitConverter.GetBytes(value.r) );
				AddRange( tmp, BitConverter.GetBytes(value.g) );
				AddRange( tmp, BitConverter.GetBytes(value.b) );
				AddRange( tmp, BitConverter.GetBytes(value.a) );
				data = tmp.ToArray();
			}
		}
		
		public QuatPos QuatPosValue
		{
			get
			{
				if ( data.Length > 0 )
				{
					QuatPos p;
					p.quat = new Quaternion( BitConverter.ToSingle(data,0), BitConverter.ToSingle(data,4), BitConverter.ToSingle(data,8), BitConverter.ToSingle(data,12) );
					p.pos = new Vector3( BitConverter.ToSingle(data,16), BitConverter.ToSingle(data,20), BitConverter.ToSingle(data,24) );   
					return p;
				};
				return default(QuatPos);
			}
			set
			{
				List<byte> tmp = new List<byte>(7 * 4);
				AddRange( tmp, BitConverter.GetBytes(value.quat.x) );
				AddRange( tmp, BitConverter.GetBytes(value.quat.y) );
				AddRange( tmp, BitConverter.GetBytes(value.quat.z) );
				AddRange( tmp, BitConverter.GetBytes(value.quat.w) );
				AddRange( tmp, BitConverter.GetBytes(value.pos.x) );
				AddRange( tmp, BitConverter.GetBytes(value.pos.y) );
				AddRange( tmp, BitConverter.GetBytes(value.pos.z) );
				data = tmp.ToArray();
			}
		}
		
		
		private static System.Text.Encoding _encoding = new System.Text.UTF8Encoding();
		
		public string StringValue
		{
			get
			{
				if ( data.Length > 0 ) 
				{
					return _encoding.GetString(data);
				}
				return string.Empty;
			}
			set
			{
				data = _encoding.GetBytes(value);
			}
		}
	}
	
	[System.Serializable]
	public class Track
	{
		public string name { get { return type.ToString(); } }
		
		public bool restoreState = true;
		
		public TrackType type = TrackType.None;
		public SpaceType space = SpaceType.World;
		public List<KeyFrame> frames = new List<KeyFrame>();
		public string target = string.Empty;
		
		public void Add( KeyFrame kf )
		{
			frames.RemoveAll(delegate(KeyFrame kf1){
				return kf1.frame == kf.frame;	
			});
			
			frames.Add(kf);
			Sort();
		}
		
		public void Rmv( KeyFrame kf )
		{
			frames.Remove(kf);
		}
		
		public void Sort()
		{
			frames.Sort(delegate(KeyFrame kf1, KeyFrame fk2){
				return	kf1.frame - fk2.frame;
			});
		}
	}
	
	[System.Serializable]
	public class Group
	{
		public int id = -1;
		public string name = string.Empty;
		public string targetName = string.Empty;
		public GroupType type = GroupType.None;
		public List<Track> tracks = new List<Track>();
	}

	
}