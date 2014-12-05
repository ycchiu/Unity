using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace EB
{
	public class FNV64_Digest : IDigest
	{
		ulong _hash = Hash.HASH_INIT_64;
		
		#region IDigest implementation
		public int GetDigestSize ()
		{
			return 8;
		}

		public int GetByteLength ()
		{
			return 1;
		}

		public void Update (byte b)
		{
			_hash = _hash * Hash.HASH_PRIME_64;
			_hash = _hash ^ b;
		}

		public void BlockUpdate (byte[] input, int inOff, int length)
		{
			for ( int i = 0; i < length; ++i )
			{
				_hash = _hash * Hash.HASH_PRIME_64;
				_hash = _hash ^ input[i+inOff];
			}
		}

		public int DoFinal (byte[] output, int outOff)
		{
			var bytes = System.BitConverter.GetBytes(_hash);
			System.Array.Copy(bytes, 0, output, outOff, bytes.Length);
			return GetDigestSize();
		}

		public void Reset ()
		{
			_hash = Hash.HASH_INIT_64;
		}

		public string AlgorithmName {
			get {
				return "FVN64";
			}
		}
		#endregion
	}
	
	public class FNV32_Digest : IDigest
	{
		uint _hash = Hash.HASH_INIT_32;
		
		#region IDigest implementation
		public int GetDigestSize ()
		{
			return 4;
		}

		public int GetByteLength ()
		{
			return 1;
		}

		public void Update (byte b)
		{
			_hash = _hash * Hash.HASH_PRIME_32;
			_hash = _hash ^ b;
		}

		public void BlockUpdate (byte[] input, int inOff, int length)
		{
			for ( int i = 0; i < length; ++i )
			{
				_hash = _hash * Hash.HASH_PRIME_32;
				_hash = _hash ^ input[i+inOff];
			}
		}

		public int DoFinal (byte[] output, int outOff)
		{
			var bytes = System.BitConverter.GetBytes(_hash);
			System.Array.Copy(bytes, 0, output, outOff, bytes.Length);
			return GetDigestSize();
		}

		public void Reset ()
		{
			_hash = Hash.HASH_INIT_32;
		}

		public string AlgorithmName {
			get {
				return "FVN32";
			}
		}
		#endregion
	}
	
	public class Digest
	{
		private IDigest _digest;
		
		public IDigest Implementation { get { return _digest; } } 
		
		public int DigestSize { get { return _digest.GetDigestSize(); } }
		
		public Digest( IDigest digest )
		{
			_digest = digest;
		}
		
		public Digest Reset()
		{
			_digest.Reset();
			return this;
		}
				
		public Digest Update( byte[] data )
		{
			Update( data, 0, data.Length );
			return this;
		}
		
		public Digest Update( byte[] data, int offset, int length )
		{
			_digest.BlockUpdate(data, offset, length);
			return this;
		}
		
		public byte[] Final()
		{
			var digest = new byte[DigestSize];
			_digest.DoFinal(digest, 0);
			return digest;
		}
		
		public byte[] Hash( byte[] data )
		{
			Update(data);
			return Final();
		}
		
		public static Digest Sha1()
		{
			return new Digest( new Org.BouncyCastle.Crypto.Digests.Sha1Digest() ); 
		}
		
		public static Digest MD5()
		{
			return new Digest( new Org.BouncyCastle.Crypto.Digests.MD5Digest() ); 
		}
		
		public static Digest FNV32()
		{
			return new Digest( new FNV32_Digest() ); 
		}
		
		public static Digest FNV64()
		{
			return new Digest( new FNV64_Digest() ); 
		}
	}
	
	public class Hmac 
	{
		private KeyParameter _key;
		private Digest _digest;
		private IMac _mac;
		
		public int DigestSize { get { return _digest.DigestSize; } }
		public byte[] Key { get { return _key.GetKey(); } }
		
		public Hmac( Digest digest, byte[] key )
		{
			_digest = digest;
			_key = new KeyParameter(key);
			_mac = new Org.BouncyCastle.Crypto.Macs.HMac(_digest.Implementation);
			_mac.Init(_key);
		}
		
		public void Reset()
		{
			_mac.Reset();
		}
		
		public void Update( byte[] data )
		{
			Update( data, 0, data.Length );
		}
		
		public void Update( byte[] data, int offset, int length )
		{
			_mac.BlockUpdate(data, offset, length);
		}
		
		public byte[] Final()
		{
			var digest = new byte[DigestSize];
			_mac.DoFinal(digest, 0);
			return digest;
		}
		
		public byte[] Hash( byte[] data )
		{
			Update(data);
			return Final();
		}
		
		public static Hmac Sha1(byte[] key)
		{
			return new Hmac( Digest.Sha1(), key);  
		}
		
		public static Hmac MD5(byte[] key)
		{
			return new Hmac( Digest.MD5(), key);
		}
		
	}
	
	public static class Crypto
	{				
		public static byte[] CreateKey( string str )
		{
			return Encoding.GetBytes(str);
		}
		
		public static byte[] RandomBytes( int length )
		{
			var random = new System.Random( Time.Now );
			var bytes = new byte[length];
			random.NextBytes(bytes);
			return bytes;
		}
	}
	
	
}



