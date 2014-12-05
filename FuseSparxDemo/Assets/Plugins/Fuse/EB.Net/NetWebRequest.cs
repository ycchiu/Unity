#if UNITY_WEBPLAYER && !UNITY_EDITOR
#define USE_WWW
#endif

#if !USE_WWW
#define USE_GZIP
#endif

#if USE_WWW
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Net
{
	public class WebRequest
	{
		string _url;
		WWW _www;
		System.DateTime _start;
		int _timeMs = 0;

		public string url 
		{
			get
			{
				return _url;
			}
		}
		
		public string text
		{
			get	
			{
				return _www.text;
			}
		}
		
		public string error
		{
			get
			{
				return _www.error;
			}
		}
		
		public Dictionary<string,string> responseHeaders 
		{ 
			get 
			{ 
				return _www.responseHeaders; 
			} 
		}
		
		public int statusCode
		{
			get
			{
				EB.Debug.LogError("WebRequest > Headers Error: {0} - {1}", _www.responseHeaders, _www.error);
				return System.Convert.ToInt16(!string.IsNullOrEmpty(_www.error));
			}
		}
		
		public int responseTime
		{
			get
			{
				return _timeMs;
			}
		}
		
		public NetworkFailure failure 
		{
			get {
				return NetworkFailure.None;
			}
		}

		public EB.Action<WebRequest> OnComplete { private get; set }
		
		public bool isDone
		{
			get
			{
				return _www.isDone;
			}
		}
		
		public WebRequest( string url ) 
		{
			_start = System.DateTime.Now;
			_url = url;
			_www = new WWW(url);
		}
		
		public WebRequest( string url, byte[] postData, Hashtable headers ) 
		{
			_start = System.DateTime.Now;
			_url = url;
			_www = new WWW(url,postData, headers);
		}
		
		public void Start()
		{
			EB.Coroutines.Run(_Wait());
		}

		IEnumerate _Wait()
		{
			while(_www.isDone==false) {
				yield return 1;
			}
			_timeMs = (int)(System.DateTime.Now - _start).TotalMilliseconds;

			if (OnComplete != null)
			{
				OnComplete(this);
			}

			yield break;
		}
		
		
	}
	
}

#else
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace EB.Net
{
	public class WebRequest
	{
		#if UNITY_WEBPLAYER
		const int NUM_THREAD = 8;
		#else
		const int NUM_THREAD = 1;
		#endif
		private string _url;
		private EB.Uri _uri;
		private Dictionary<string,string> _requestHeaders = new Dictionary<string, string>();
		private Dictionary<string,string> _responseHeaders = new Dictionary<string, string>();
		private string _method;
		private byte[] _body;
		private string _error;
		private bool   _done;
		private List<byte> _data = new List<byte>();
		private System.DateTime _requestSent;
		private System.DateTime _headersReceived;

		private NewRelicPlugin.Timer _timer;

		private long _bytesSent = 0;
		private long _bytesRecieved = 0;
		
		public void Dispose() {}
		
		private static Dictionary<string,List<ITcpClient>> _connections 	= new Dictionary<string, List<ITcpClient>>();

		private Net.NetworkFailure _failure = Net.NetworkFailure.None;

		private const int kReadTimeout = 25;
		
		private static int _randomSeed = 0;
		private static int _connectionFailures = 0;
		private static ThreadPool _pool;
		
		public string url { get { return _url; } }
		public Dictionary<string,string> responseHeaders { get { return _responseHeaders; } }
		public bool isDone { get { return _done; } }

		public int statusCode 				{ get;set; }
		public string statusText 			{ get;set; }
		public byte[] bytes 				{ get { return _data.ToArray(); } }
		public int size 					{ get { return _data.Count; } }

		public Net.NetworkFailure failure 	{ get{ return _failure;} }

		public int responseTime 
		{ 
			get 
			{ 
				if ( statusCode == 200 )
				{
					return (int)((_headersReceived-_requestSent).TotalMilliseconds);  
				}
				return -1;
			}
		}

		public NewRelicPlugin.Timer NRTimer { get { return _timer; } }
		
		public string text
		{
			get	
			{
				if ( _data != null && _data.Count > 0 )
				{
					return System.Text.Encoding.UTF8.GetString(_data.ToArray());
				}
				else
				{
					return null;
				}
			}
		}
		
		public string error
		{
			get
			{
				if ( _error != null )
				{
					return _error;
				}
				else if ( statusCode != 200 )
				{
					return string.Format("Non-200 error ({0})", statusCode);
				}
				else
				{
					return null;
				}
			}
		}

		public EB.Action<WebRequest> OnComplete { private get; set; }

		private string connectionKey
		{
			get
			{
				return _uri.HostAndPort;
			}
		}

		public long bytesSent 		{ get { return _bytesSent; } }
		public long bytesRecieved 	{ get { return _bytesRecieved; } }
		
		public WebRequest( string url ) 
		{
			// parse url
			url = GetUrl(url);
			_url = url;
			_uri = new EB.Uri(url);
			_method = "GET";
		}
		
		public WebRequest( string url, byte[] postData, Hashtable headers ) 
		{
			// parse url
			url = GetUrl(url);
			_url = url;
			_uri = new EB.Uri(url);
			_method = "POST";
			
			foreach( DictionaryEntry header in headers )
			{
				_requestHeaders[header.Key.ToString()] = header.Value.ToString();
			}
			_requestHeaders["Content-Length"] = postData.Length.ToString();
			_body = postData;
			_bytesSent = postData.LongLength;
		}
		
		private string GetUrl( string url )
		{
			return url;
		}
		
		public void Start()  
		{
			if ( _randomSeed == 0 )
			{
				_randomSeed = Random.Range(0, System.Int32.MaxValue);
			}
			
			// setup callbacks
			if (_pool == null) 
			{
				_pool = new ThreadPool(NUM_THREAD);
			}
			
			_pool.Queue(this.ThreadTask,this);
		}
		
		private void ThreadTask( object ignore )
		{
			_timer = NewRelicPlugin.CreateTimer();

			ITcpClient client = null;
			try
			{
				client = GetClient();
				//client.ReadTimeout = kReadTimeout*1000;
				
				// 1. connect
				if (Connect(client))
				{
					// 2, send request
					if (WriteRequest(client))
					{
						ReadResponse(client);
					}
				}
				else
				{
					_connectionFailures++;
				}
			}
			catch ( System.Exception e )
			{
				_connectionFailures++;

				var failure = Net.NetworkFailure.None;
				if (client != null)
				{
					failure = client.Error;
				}

				// read/write failure
				if (failure == NetworkFailure.None && e is System.IO.IOException)
				{
					failure = NetworkFailure.TimedOut;
				}

				Error(e.Message, failure);
			}
			
			if (client!= null)
			{
				ReturnClient(client);
			}

			_timer.Stop();

			
			_done = true;
		}
		
		// ELB connection timeout is 60 but we are going to be conservative here
		const int kConnectionTimeout = 45;
		
		private ITcpClient GetClient()
		{
			lock (_connections)	
			{
				List<ITcpClient> clients = null;
				if ( _connections.TryGetValue(connectionKey,out clients) )
				{
					// remove bad clients
					//Debug.Log("removing clients " + connectionKey);
					clients.RemoveAll( delegate(ITcpClient client ) 
					{
						if ( client.Connected == false || Time.Since(client.LastTime) >= kConnectionTimeout  )
						{
							//Debug.Log("removing client");
							client.Close();
							return true;
						}						
						return false;
					});
					
					if ( clients.Count > 0 )
					{
						var client = clients[clients.Count-1];
						clients.RemoveAt(clients.Count-1);
						//Debug.Log("reusing client " + Time.Since(client.LastTime));
						return client;
					}
				}
			}
			//Debug.Log("creating new client " + connectionKey); 
			return TcpClientFactory.Create( _uri.Scheme == "https" ); 
		}
		
		private void ReturnClient( ITcpClient client )
		{
			try
			{
				if ( statusCode == 200 && client.Connected && GetResponseHeader("Connection") != "close" && string.IsNullOrEmpty(_error) )
				{
					lock(_connections)
					{
						List<ITcpClient> clients = null;
						if ( !_connections.TryGetValue(connectionKey,out clients) )
						{
							clients = new List<ITcpClient>();
							_connections[connectionKey] = clients;
						}
						clients.Add(client);
					}
				}
				else	
				{
					// dispose 
					client.Close();
				}
			}
			catch 
			{
				
			}
		}
		
		private bool Connect( ITcpClient client )
		{
			if ( client.Connected )
			{
				return true;
			}
			
			// do the connect
			EB.Debug.Log("Connecting to " + _uri.Host + " on port " + _uri.Port );
			if (!client.Connect( _uri.Host, _uri.Port, 5*1000)) 
			{
				client.Close();  
				Error("Connect failed", client.Error);
				return false;
			}
			
			// nothing should ever take longer than x seconds
			client.ReadTimeout = kReadTimeout * 1000;
			client.WriteTimeout = 10 * 1000;
			
			_connectionFailures = 0;
			return true;
		}
		
		
		private void Error(string fmt, Net.NetworkFailure failure, params object[] args)
		{
			if (failure != NetworkFailure.None)
			{
				_failure = failure;
			}
			else
			{
				_failure = Net.NetworkFailure.Unknown;
			}

			_randomSeed = 0; // reset the seed so that we pick a new server/ip
			if ( args.Length > 0 )
			{
				_error = string.Format(fmt, args);
			}
			else
			{
				_error = fmt;
			}
			EB.Debug.LogError("http error: (" + url + "): " + _error + " " + _failure );
			//Debug.LogError(_error);
		}
		
		private void AddData( byte[] bytes )
		{
			if ( bytes != null && bytes.Length > 0 )
			{
				// for what ever reason, this add range causes a bad trampoline to be added which screws up everything
				foreach( var b in bytes )
				{
					_data.Add(b);
				}
			}
		}
		
#if USE_GZIP		
		private byte[] Decompress( byte[] src )
		{
			var input = new MemoryStream( src );
			var inflater = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(input);
			var output= new MemoryStream( src.Length * 5 );
			var buffer = new byte[4096];
			while (true)
			{
				int read = inflater.Read(buffer, 0, buffer.Length);
				if ( read == 0 )
				{
					break;
				}
				output.Write(buffer, 0, read);
			}
			return output.ToArray();
		}
#endif
		
		private bool WriteRequest( ITcpClient stream )
		{
			if ( !WriteHeaders(stream) )
			{
				return false;
			}
			
			// see if we need to write a body or not
			if ( _body != null && _body.Length > 0 )
			{
				return WriteRequestBody( stream );
			}
			
			_requestSent = System.DateTime.Now;
			
			return true;
		}
		
		private bool WriteHeaders( ITcpClient stream )
		{
			// add the host header
			_requestHeaders["Host"] = _uri.Host;
			
			// support gzip
#if USE_GZIP		
			_requestHeaders["Accept-Encoding"] = "gzip";
#endif
			// this user agent
			_requestHeaders["User-Agent"] = "Sparx/1.0";
			
			var sb = new System.Text.StringBuilder();
			sb.AppendFormat("{0} {1} HTTP/1.1\r\n", _method, _uri.PathAndQuery );
			foreach( var header in _requestHeaders )
			{
				sb.AppendFormat("{0}:{1}\r\n", header.Key, header.Value);
			}
			
			sb.Append("\r\n");
			
			var buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());		
			stream.Write(buffer, 0, buffer.Length); 
			return true;
		}
		
		private bool WriteRequestBody( ITcpClient stream )
		{
			stream.Write( _body, 0, _body.Length ); 
			return true;
		}
		
		private bool ReadResponse( ITcpClient stream )
		{
			// read the status
			var status = ReadLine(stream);
			if ( !ParseStatus(status) )
			{
				Error("Failed to parse status header: " + status, Net.NetworkFailure.BadServerResponse);
				return false;
			}
			
			// read the headers
			while (true)
			{
				var line = ReadLine(stream);
				//Debug.Log("line: " + line + " " + line.Length);
				if ( line.Length == 0 )
				{
					break;
				}
				
				if ( !ParseHeader(line) )
				{
					Error("Failed to parse header: " + line, Net.NetworkFailure.BadServerResponse);
					return false;
				}
			}

			_headersReceived = System.DateTime.Now;
			
			var result = false;
			
			// check to see if this is chunked or not
			if ( GetResponseHeader("Transfer-Encoding") == "chunked" )
			{
				result = ReadBodyChunked(stream);
			}
			else
			{
				result = ReadBody(stream);
			}
			
			//Debug.Log("Got " + _data.Count + " bytes : " + Encoding.ToHexString(_data.ToArray(),1) );	
			_bytesRecieved = _data.Count;
			
#if USE_GZIP			
			if ( result && GetResponseHeader("Content-Encoding") == "gzip" )
			{
				var src = _data.ToArray();
				_data = new List<byte>(src.Length*5);
				foreach( var b in Decompress(src) )
				{
					_data.Add(b);
				}
			}
#endif

			if (OnComplete != null)
			{
				OnComplete(this);
			}

			return result;
		}
		
		private bool ReadBody( ITcpClient stream )
		{
			// see if there's a content length header
			var contentLength = GetResponseHeader("Content-Length");
			if ( string.IsNullOrEmpty(contentLength) )
			{
				// check for connect close, then read to end of the stream.
				if ( GetResponseHeader("Connection") == "close" )
				{
					var data = new List<byte>();
					var buffer = new byte[1024];
					while(true)
					{
						// read until its closed
						try 
						{
							int read = stream.Read(buffer,0,buffer.Length);
							if (read > 0 )
							{
								for (int i = 0; i < read; ++i ) 
								{
									data.Add( buffer[i] ); 	
								}
							}
						}
						catch
						{
							break;
						}
					}
				}
				else
				{
					Error("Missing Content-Length header!", Net.NetworkFailure.BadServerResponse);
					return false;
				}
			}
			
			int count = 0;
			if ( !int.TryParse(contentLength, out count ) )
			{
				Error("Failed to read body, content length header is missing", Net.NetworkFailure.BadServerResponse);
				return false;
			}
			
			//Debug.Log("Content-Length: " + count);
			if ( count == 0 )
			{
				EB.Debug.LogWarning("Zero length response for url " + url);
				return true;
			}
			
			var bufferSize = 64*1024;
			while ( count > 0 )
			{
				var toRead = Mathf.Min(count, bufferSize);
				AddData(Read(stream, toRead));
				count -= toRead;
			}
			
			return true;
		}
		
		private bool ReadBodyChunked(ITcpClient stream)
		{
			while ( true )
			{
				var status = ReadChunk(stream);
				if ( status < 0 )
				{
					return false;
				}
				else if ( status == 0 )
				{
					break;
				}
			}
			return true; 
		}
		
		private int ReadChunk( ITcpClient stream ) 
		{
			// read to the end of the line
			var line = ReadLine(stream);
			int count = 0;
			
			try
			{
				count = int.Parse(line, System.Globalization.NumberStyles.HexNumber );
			}
			catch 
			{
				Error("Failed to decoded chunked response, line:" + line, Net.NetworkFailure.BadServerResponse);
				return -1;
			}
			
			if ( count == 0 )
			{
				// read trailers
				ReadLine(stream);
				return 0;
			}
			
			//Debug.Log("Reading: " + count);
			var buffer = Read(stream, count); 
			AddData(buffer);
			ReadLine(stream); // should have a CRLF after the buffer
			return buffer.Length;
		}
		
		static System.TimeSpan _span = System.TimeSpan.FromSeconds(kReadTimeout);		
		
		private byte[] Read( ITcpClient stream, int count )
		{
			var buffer = new byte[count];
			var read   = 0;
			var timeout = System.DateTime.Now + _span;
			while ( read < count )
			{
				var r = stream.Read( buffer, read, count-read); 
				if ( r < 0 )
				{
					throw new System.IO.IOException("Read Failed");
				}
				else if ( r == 0 )
				{
					if ( System.DateTime.Now > timeout )
					{
						throw new System.IO.IOException("Read Timed out");
					}
					System.Threading.Thread.Sleep( System.TimeSpan.FromMilliseconds(1) ); 
				}
				else
				{
					timeout = System.DateTime.Now + _span;
					read += r;
				}
				
			}
			return buffer;
		}
		
		private string ReadLine( ITcpClient s )
		{
			var data = new List<byte>(64);
			while ( true )
			{
				var buffer = Read(s, 1);
				byte b = buffer[0];
				if ( b == 10 ) // LF
				{
					break;
				}
				else if ( b != 13 ) // CR
				{
					data.Add(b);
				}
			}
			return Encoding.GetString(data.ToArray());
		}
		
		public string GetResponseHeader( string key )
		{
			string value;
			if ( _responseHeaders.TryGetValue(key.ToLower(), out value) )
			{
				return value;
			}
			return string.Empty;
		}
		
		private bool ParseHeader( string header )
		{
			//Debug.Log("Header: " + header);
			var split = header.IndexOf(':');
			if ( split < 0 )
			{
				EB.Debug.LogError("Failed to parse header: " + header);
				return false;
			}
			
			var key = header.Substring(0, split).Trim().ToLower();
			var value = header.Substring(split+1).Trim();
			_responseHeaders[key] = value;		
			
			return true;
		}
		
		private bool ParseStatus( string status )
		{
			//Debug.Log("Status: " + status);
			var parts = status.Split(' ');
			if ( parts.Length < 3 )
			{
				return false;
			}
			
			int code = 0;
			if ( !int.TryParse(parts[1],out code) )
			{
				return false;
			}
			statusCode = code;
			statusText = parts[2];
			for (int i = 3; i < parts.Length; ++i)
			{
				statusText += " " + parts[3];
			}
			
			return true;
		}

	}
	
}
#endif

