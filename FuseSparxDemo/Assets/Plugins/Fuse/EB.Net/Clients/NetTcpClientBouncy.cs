#if !UNITY_IPHONE || UNITY_EDITOR
#define SUPPORT_SSL
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace EB.Net
{	
	public class TcpClientBouncy : ITcpClient
	{
		#region ITcpClient implementation
		System.Net.Sockets.TcpClient 		_client;

		System.Net.Sockets.NetworkStream 	_net;
		NetworkFailure						_error = NetworkFailure.None;

#if SUPPORT_SSL
		TlsProtocolHandler 					_handler;
		MyTlsAuthentication					_auth;
		MyTlsClient							_tlsClient;

		public NetworkFailure Error { get { return _error; } }

		
		class MyTlsAuthentication : TlsAuthentication
		{
			public void NotifyServerCertificate (Certificate serverCertificate)
			{
				var certs = serverCertificate.GetCerts();
				foreach( var cert in certs )
				{
					var encoding = cert.GetDerEncoded();
					var base64   = Encoding.ToBase64String(encoding);	
					if (_validCerts.IndexOf(base64) >= 0)
					{
						return;
					}
				}
				throw new TlsFatalAlert(AlertDescription.bad_certificate);
			}
	
			public TlsCredentials GetClientCredentials (CertificateRequest certificateRequest)
			{
				return null;
			}
		}

			
		class MyTlsClient : DefaultTlsClient
		{
			TlsAuthentication _auth;
			
			public MyTlsClient( TlsAuthentication auth ) 
			{
				_auth = auth;
			}
			
			public override TlsAuthentication GetAuthentication ()
			{
				return _auth;
			}
		}
#endif
		
		System.IO.Stream					_stream;
		bool								_secure;
		System.IAsyncResult					_async;
		byte[] 								_readBuffer;
		int									_readCount;
		int									_readOffset;
		
#if UNITY_WEBPLAYER
		static List<string>					_prefetchedIps = new List<string>();
#endif
		
		
#if SUPPORT_SSL			
		static List<string> _validCerts = new List<string>();
#endif
		
		public static void AddCertificate( byte[] certData ) 
		{
#if SUPPORT_SSL
			try 
			{
				var parser = new Org.BouncyCastle.X509.X509CertificateParser();
				var cert = parser.ReadCertificate(certData);
				if (cert != null)
				{
					EB.Debug.Log("Adding cert " + cert.SubjectDN.ToString() );
					_validCerts.Add( Encoding.ToBase64String(cert.GetEncoded()));
				}
			}
			catch (System.Exception ex)
			{
				EB.Debug.LogError("Failed to load certificate! " + ex);
			}
#endif
		}		
		
		private static int RefCount = 0;
		
		public int Available { get { return _readCount-_readOffset; } }
				
		public TcpClientBouncy(bool secure)
		{
			_client = new System.Net.Sockets.TcpClient();
			_client.NoDelay = true;
			_secure = secure;
			_readBuffer = new byte[4*1024];
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
			EB.Debug.Log("Try connect " + ip);
#if UNITY_WEBPLAYER
			lock(_prefetchedIps)
			{
				if (_prefetchedIps.Contains(ip.ToString()) == false)
				{
					if (Security.PrefetchSocketPolicy(ip.ToString(), 8843, connectTimeout)==false)
					{
						EB.Debug.LogError("failed to prefetch security policy for " + ip);
						return false;
					}
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
				_error = NetworkFailure.CannotConnectToHost;
				return false;
			}
						
			_net = _client.GetStream();
			_stream = _net;
			
#if SUPPORT_SSL			
			if (_secure)
			{
				EB.Debug.Log("doing ssl connect");
				try {
					var random = new System.Random();
					var bytes = new byte[20];
					random.NextBytes(bytes);
					
					var secureRandom = new SecureRandom(bytes);
					
					_auth = new MyTlsAuthentication();
					_tlsClient = new MyTlsClient(_auth);
					_handler = new TlsProtocolHandler(_net, secureRandom);
					_handler.Connect(_tlsClient);
					_stream = _handler.Stream;
					if (_stream == null)
					{
						EB.Debug.LogError("stream is null");
						_error = NetworkFailure.SecureConnectionFailed;
						return false;
					}
				}
				catch (System.Exception ex)
				{
					EB.Debug.LogError("ssl connect failed " + ex);
					_error = NetworkFailure.SecureConnectionFailed;
					return false;
				}
				
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
				_error = NetworkFailure.DNSLookupFailed;
				return false;
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
				}
			}
			else if (Available == 0)
			{
				//Debug.Log("BeginRead");
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
			var async = _stream.BeginWrite(buffer, offset, count, null, null);
			_stream.EndWrite(async);
			//_stream.Write(buffer,offset,count);
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
					return false;
				}
				
#if SUPPORT_SSL				
				if (_secure)
				{
					return _handler != null && _handler.Stream != null;
				}
#endif
				
				return true;
			}
		}
		
		public bool DataAvailable
		{
			get
			{
				//CheckRead();
				//return Available > 0;
				if (_net != null)
				{
					return _net.DataAvailable;
				}
				return false;
			}
		}
		
		public System.DateTime LastTime {get;private set;}
		#endregion
		
	}
}
#endif