using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

namespace EB.Net
{
#if UNITY_IPHONE	
	public class TcpClientNS : ITcpClient
	{
		private IntPtr _handle;		
		private bool _secure = false;

		NetworkFailure						_error = NetworkFailure.None;
		
		#region P/Invoke
		const string DLL_NAME = "__Internal";
		
		[DllImport(DLL_NAME)]
		static extern IntPtr 	_NSTcpClientCreate(bool secure);
		
		[DllImport(DLL_NAME)]
		static extern void 		_NSTcpClientDestory(IntPtr client);
		
		[DllImport(DLL_NAME)]
		static extern void 		_NSTcpClientConnect(IntPtr client, string host, int port);
		
		[DllImport(DLL_NAME)]
		static extern bool		_NSTcpClientConnected(IntPtr client);
		
		[DllImport(DLL_NAME)]
		static extern bool		_NSTcpClientError(IntPtr client);
		
		[DllImport(DLL_NAME)]
		static extern bool		_NSTcpClientDataAvailable(IntPtr client);
		
		[DllImport(DLL_NAME)]
		static extern int 		_NSTcpClientRead(IntPtr client, IntPtr buffer, int offset, int count);
		
		[DllImport(DLL_NAME)]
		static extern int 		_NSTcpClientWrite(IntPtr client, byte[] buffer, int offset, int count);
				
		[DllImport(DLL_NAME)]
		static extern IntPtr	_NSCreatePool();
		
		[DllImport(DLL_NAME)]
		static extern IntPtr 	_NSDestroyPool( IntPtr pool );		
		
		[DllImport(DLL_NAME)]
		static extern void 		_NSImportCertificate(IntPtr data, int dataLength);
		
		#endregion

		public NetworkFailure Error { get { return _error; } }
		
		public static void AddCertificate( byte[] certData ) 
		{
			var ptr = Marshal.AllocHGlobal(certData.Length);
			Marshal.Copy(certData, 0, ptr, certData.Length);
			_NSImportCertificate(ptr, certData.Length);
			Marshal.FreeHGlobal(ptr);
		}
		
		IntPtr _buffer;
		const int kBufferSize = 2048;
		
		#region ITcpClient implementation
		public TcpClientNS(bool secure)
		{
			_secure = secure;
			
			_buffer = Marshal.AllocHGlobal(kBufferSize);
		}
		
		bool TryConnect (string host, System.Net.IPAddress ip, int port, int connectTimeout)
		{			
			DestroyClient();
			_handle = _NSTcpClientCreate(_secure);
			
			var timeout = System.DateTime.Now + System.TimeSpan.FromMilliseconds(connectTimeout);
			_NSTcpClientConnect(_handle,ip.ToString(),port);
			
			while( Connected == false && System.DateTime.Now < timeout ) 
			{
				System.Threading.Thread.Sleep(100);
			}
			
			return Connected;
		}
		
		// can only call connect once per tcp client
		public bool Connect (string host, int port, int connectTimeout)
		{
			var addreses = DNS.Lookup(host);
			if (addreses.Length == 0 )
			{
				EB.Debug.LogError("failed to lookup " + host);
			}
			
			foreach( var ip in addreses )
			{
				if (TryConnect(host, ip, port, connectTimeout))
				{
					DNS.StoreLast(host,ip);
					return true;
				}
			}
			
			return false;
		}
		
		void DestroyClient()
		{
			if (_handle != IntPtr.Zero)
			{				
				_NSTcpClientDestory(_handle);
				_handle = IntPtr.Zero;
			}
		}

		public void Close ()
		{
			DestroyClient();
			
			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
				_buffer = IntPtr.Zero;
			}
		}

		public int Read (byte[] buffer, int index, int count)
		{
			count = Mathf.Min(count,kBufferSize);
			var read = _NSTcpClientRead(_handle,_buffer,0,count);
			if ( read > 0 )
			{
				Marshal.Copy(_buffer, buffer, index, count);
				LastTime = System.DateTime.Now;
				//Debug.Log("NSRead: " + Encoding.ToHexString(buffer, index, count, 1) );
			}
			else if ( read < 0 )
			{
				// read failure
				throw new System.IO.IOException("Read Failed!");
			}
			return read;
		}

		public void Write (byte[] buffer, int index, int count)
		{
			while( count > 0 )
			{
				int w = _NSTcpClientWrite(_handle,buffer,index,count);
				if ( w < 0 )
				{
					throw new System.IO.IOException("Write Failed!");
				}	
			 	else if (w == 0)
				{
					// wait
					System.Threading.Thread.Sleep(100);
				}
				else
				{
					count -= w;
					index += w;
				}
			}			
		}

		public int ReadTimeout {
			get {
				return 0;
			}
			set {
				
			}
		}

		public int WriteTimeout {
			get {
				return 0;
			}
			set {
				
			}
		}

		public System.DateTime LastTime {
			get; private set;
		}


		public bool Connected {
			get
			{
				if (_NSTcpClientError(_handle))
				{
					return false;
				}
				
				return _NSTcpClientConnected(_handle);
			}
		}
		
		public bool DataAvailable {
			get {
				return _NSTcpClientDataAvailable(_handle);
			}
		}
		#endregion
	}
#endif
}

