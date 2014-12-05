#if !UNITY_IPHONE || UNITY_EDITOR
#define SUPPORT_SSL
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EB.Net
{	
	public class TcpClientMono : ITcpClient
	{
		#region ITcpClient implementation
		System.Net.Sockets.TcpClient 		_client;
		
		System.Net.Sockets.NetworkStream 	_net;
		NetworkFailure						_error = NetworkFailure.None;
	
#if SUPPORT_SSL
		System.Net.Security.SslStream 		_ssl;
#endif
		
		public NetworkFailure Error { get { return _error; } }
		
		System.IO.Stream					_stream;
		bool								_secure;
		System.IAsyncResult					_async;
		byte[] 								_readBuffer;
		int									_readCount;
		int									_readOffset;
		
#if UNITY_WEBPLAYER
		static List<string>					_prefetchedIps = new List<string>();
#endif
		
		
		private static int RefCount = 0;
		
		public int Available { get { return _readCount-_readOffset; } }
		
#if SUPPORT_SSL			
		static List<string> _validCerts = new List<string>();
#endif
		
		public static void AddCertificate( byte[] certData ) 
		{
#if SUPPORT_SSL
			try 
			{
				var cert = new X509Certificate2(certData);
				var hash = Encoding.ToHexString(cert.GetCertHash());
				_validCerts.Add( hash );
				EB.Debug.Log("got cert hash: " + hash);
			}
			catch (System.Exception ex)
			{
				EB.Debug.LogError("Failed to load certificate! " + ex);
			}
#endif
		}
		
		public TcpClientMono(bool secure)
		{
			_client = new System.Net.Sockets.TcpClient();
			_client.NoDelay = true;
			_secure = secure;
			_readBuffer = new byte[4096];
			_readOffset = 0;
			_readCount = 0;
			
			++RefCount;
			//Debug.Log("TcpClients: Construct " + RefCount);
#if !SUPPORT_SSL
			if (_secure)
			{
				throw new System.IO.IOException("secure unsupported");
			}
#endif
		}
		
		bool TryConnect(string hostname, System.Net.IPAddress ip, int port, int connectTimeout)
		{
			EB.Debug.Log("Try connect " + ip + " " + port);
#if UNITY_WEBPLAYER
			var needsPrefetch = false;
			lock(_prefetchedIps)
			{
				needsPrefetch = _prefetchedIps.Contains(ip.ToString()) == false;
			}

			if (needsPrefetch)
			{
				EB.Debug.Log("Prefetching security policy for " + ip);
				if (Security.PrefetchSocketPolicy(ip.ToString(), 8843, connectTimeout)==false)
				{
					EB.Debug.LogError("failed to prefetch security policy for " + ip);
					return false;
				}
				EB.Debug.Log("GOT security policy for " + ip);
				lock(_prefetchedIps)
				{
		    		_prefetchedIps.Add(ip.ToString());
				}
			}
#endif

			var async 	= _client.BeginConnect( ip, port, null, null);			
			var timeout	= System.DateTime.Now + System.TimeSpan.FromMilliseconds(connectTimeout);
			while (async.IsCompleted==false && System.DateTime.Now < timeout)
			{
				System.Threading.Thread.Sleep(100);
			}
			
			if (!async.IsCompleted)
			{
				return false;
			}
			_client.EndConnect(async);
			
			if (_client.Connected == false )
			{
				EB.Debug.LogError("Failed to connect to " + ip);
				return false;
			}
						
			_net = _client.GetStream();
			_stream = _net;
			
#if SUPPORT_SSL			
			if (_secure)
			{
				_ssl = new System.Net.Security.SslStream( _stream, true, RemoteCertificateValidationCallback, null);
				try
				{
					_ssl.AuthenticateAsClient( hostname ); 
				}
				catch (System.Exception e) 
				{
					EB.Debug.LogError("Failed to authenticate: " + e);
					return false;
				}
				_stream = _ssl;
			}
#endif
			EB.Debug.Log("Connected to  " + ip);
			
			LastTime = System.DateTime.Now;
			
			return true;
		}
		
		
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

		public void Close ()
		{
			--RefCount;
			//Debug.Log("TcpClients: Destroy " + RefCount);
#if SUPPORT_SSL	
			try
			{
			
				if (_ssl!=null)
				{
					_ssl.Dispose();
				}
			}
			catch
			{
				
			}
			_ssl = null;
#endif
			
			try 
			{
				if (_client != null)
				{
					var disposable = (System.IDisposable)_client;
					disposable.Dispose();
				}
				
			}
			catch
			{
			}
			_client = null;
		}
			
#if SUPPORT_SSL			
		bool RemoteCertificateValidationCallback( System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{	
			if ( sslPolicyErrors == SslPolicyErrors.None )
			{
				return true;
			}

			var hash = Encoding.ToHexString(certificate.GetCertHash());
			EB.Debug.Log("checking cert hash: " + hash + " - " + Encoding.ToHexString(certificate.GetPublicKey()) );
			foreach( var cert in _validCerts )
			{	
				if (hash == cert)
				{
					return true;
				}
			}
			EB.Debug.LogError("Cant find certificate in pinned certificate!");
			
			return false;
		}		
#endif	
		
		private void CheckRead()
		{
			if (_stream == null)
			{
				return;
			}
			
			if (_async != null)
			{
				if (_async.IsCompleted)
				{
					_readCount = _stream.EndRead(_async);
					_readOffset = 0;
					//Debug.Log("EndRead {0}", _readCount);
					_async = null;
					
					if (_readCount > 0)
					{
						LastTime = System.DateTime.Now;
					}
					else if (_readCount == 0 )
					{
						_readCount = -1;
					}
				}
			}
			else if (Available == 0)
			{
				_async = _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, null, null);
			}
		}
		

		public int Read (byte[] buffer, int offset, int count)
		{
			CheckRead();
			
			int read = 0;
			if ( Available > 0 )
			{
				read = Mathf.Min( count, Available );
				System.Array.Copy(_readBuffer, _readOffset, buffer, offset, read);
				_readOffset += read;
			}
			
			return read;
		}

		public void Write (byte[] buffer, int offset, int count)
		{
			_stream.Write(buffer,offset,count);
		}

		public int ReadTimeout {
			get {
				return _client.ReceiveTimeout;
			}
			set {
				_client.ReceiveTimeout = value;
			}
		}

		public int WriteTimeout {
			get {
				return _client.SendTimeout;
			}
			set {
				_client.SendTimeout = value;
			}
		}
		
		public bool Connected
		{
			get
			{
				if (_client.Connected==false)	
				{
					EB.Debug.Log("Client Is Disconnected");
					return false;
				}

				if (_readCount < 0)
				{
					EB.Debug.Log("Socket Is Disconnected");
					return false;
				}
				
#if SUPPORT_SSL				
				if (_ssl != null)
				{
					return _ssl.IsAuthenticated;
				}
#endif
				
				return true;
			}
		}
		
		public bool DataAvailable
		{
			get
			{
				CheckRead();
				return Available > 0;
			}
		}
		
		public System.DateTime LastTime {get;private set;}
		#endregion
		
	}
}
#endif