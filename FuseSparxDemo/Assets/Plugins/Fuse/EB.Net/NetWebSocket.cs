//#define WS_DEBUG
using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace EB.Net
{
	public enum WebSocketCloseStatus
	{
		NormalClosure = 1000,
		EndpointUnavailable = 1001,
		ProtocolError = 1002,
		InvalidMessageType = 1003,
		Empty = 0,
		InvalidPayloadData = 1007,
		PolicyViolation = 1008,
		MessageTooBig = 1004,
		MandatoryExtension = 1010,
		InternalServerError,
	}
	
	public enum WebSocketState
	{
		None,
		Connecting,
		Open,
		CloseSent,
		CloseReceived,
	}
	
	public enum WebSocketOpCode
	{
		Continuation= 0x0,
		Text		= 0x1,
		Binary		= 0x2,
		Close	 	= 0x8,
		Ping		= 0x9,
		Pong		= 0xA,
		
		Control	 	= 0x8, // control mask
	}
		
	public class WebSocket : IDisposable
	{
		public WebSocketState State 						{ get { return _impl!=null? _impl._state : WebSocketState.None; } }
		public int PingTimeout								{ get; set; } // no ping and lost conneciton
		public int ActivityTimeout							{ get; set; } // if set, then we will close the connection if nothing is being sent.
		public int Ping										{ get { return _impl!=null? _impl._ping : 0; } }
		
		// events
		public event EB.Action OnConnect;
		public event EB.Action<string> OnError;
		public event EB.Action<string> OnMessage;
		public event EB.Action<byte[]> OnData;
		
		public class WebSocketFrame
		{
			public bool fin;
			public bool rsv1;
			public bool rsv2;
			public bool rsv3;
			public int opcode;
			public int 	mask;
			public byte[] payload;
		}
		
		// data
		protected class Impl
		{
			public WebSocket _parent;
			public ITcpClient _client;
			public Uri _uri;
			public bool _running;
			public Thread _thread;
			public System.Random _random = new System.Random();
			public WebSocketState _state;
			public string _protocol;
			public int _ping;
			
			public DateTime _lastPing;
			public DateTime _lastRead;
			public DateTime _lastActivity;
			
			public string _key;
			public string _accept;
			public Dictionary<string,string> _responseHeaders 	= new Dictionary<string, string>();
			public Queue<WebSocketFrame> _writeQueue 			= new Queue<WebSocketFrame>();	
			
			public Impl(WebSocket parent)
			{
				_parent = parent;
			}
			
			public void Connect( Uri uri, string protocol, byte[] key )  
			{
				GenerateKeyAndAccept(key);
				
				_uri = uri;
				_protocol = protocol;
				
				_state = WebSocketState.Connecting;
			
				_running = true;			
				_thread = new Thread(this.DoConnectSafe, 256*1024);
				_thread.Start();
			}
			
			public void QueueFrame( WebSocketFrame frame ) 
			{
				lock(_writeQueue)
				{
					_writeQueue.Enqueue(frame);
				}
			}
			
			void Error( string err )
			{
				if (IsActive())
				{
					_parent.Error(err);
				}
			}
			
			bool IsActive()
			{
				return _parent._impl == this;
			}
			
			void OnConnect()
			{
				if(IsActive())
				{
					if (_parent.OnConnect!=null)
					{
						_parent.OnConnect();
					}
				}
			}
			
			void OnMessage(string message)
			{
				if(IsActive())
				{
					if (_parent.OnMessage!=null)
					{
						_parent.OnMessage(message);
					}
				}
			}
			
			void OnData(byte[] message)
			{
				if(IsActive())
				{
					if (_parent.OnData!=null)
					{
						_parent.OnData(message);
					}
				}
			}
			
			void DoConnectSafe( object state )
			{
				try 
				{
					Thread.CurrentThread.Name = "websocket";
				}catch {}
				
				try 
				{
					if ( DoConnect() )
					{
						if (SendUpgradeRequest())
						{
							MainLoop();
						}
						
					}
				}
				catch (System.Threading.ThreadAbortException)
				{
					// dont' care
				}
				catch (System.Exception e)
				{
					Error ("[NetWebSocket] exception " + e.ToString());
				}
				
				_state = WebSocketState.None;
				
				EB.Debug.Log("[NetWebSocket] Disconnecting from socket " + _uri);

				// always try to send the close event
				try  {
					SendClose();
				}
				catch {}

				try
				{
					if (_client != null)
					{
						_client.Close();
					}	
				}
				catch
				{
					
				}			
				_client = null;
				
				if (_parent != null)
				{
					_parent.DidClose(this);
				}
			}
			
			bool DoConnect()
			{
				_client = TcpClientFactory.Create(_uri.Scheme == "wss");
				
				EB.Debug.Log("[NetWebSocket] connect " + _uri.Host + " " + _uri.Port );
				
				if (!_client.Connect(_uri.Host, _uri.Port, 5*1000))
				{
					_client.Close();  
					_client = null;
					_state = WebSocketState.None;
					Error("failed to connect");
					return false;	
				}
				_client.ReadTimeout = _parent.PingTimeout>0 ? _parent.PingTimeout : 5*1000;
				_client.WriteTimeout = 5 * 1000;
									
				if (!_running)
				{
					return false;
				}
				return true;	
			}
			
			void AddRange( List<byte> dst, byte[] src)
			{
				foreach( var b in src )
				{
					dst.Add(b);
				}
			}
			
			void GenerateKeyAndAccept(byte[] key)
			{
				if (key == null)
				{
					key = Crypto.RandomBytes(64);
				}
				
				_key = System.Convert.ToBase64String(key);
				
				// generate the accept			
				List<byte> data = new List<byte>();
				AddRange( data, Encoding.GetBytes( _key) );
				AddRange( data, Encoding.GetBytes( "258EAFA5-E914-47DA-95CA-C5AB0DC85B11") );
				
				var hash = EB.Digest.Sha1().Hash(data.ToArray());
				_accept = Convert.ToBase64String(hash);
			}
			
			private string ReadLine( ITcpClient s )
			{
				var data = new List<byte>(64);
				var buffer = new byte[1];
				while ( true )
				{
					int r = s.Read(buffer,0,1);
					if (r == 0)
					{
						Thread.Sleep(10);
						continue;
					}
					else if (r < 0 )
					{
						throw new System.IO.IOException("Read Failed");
					}
									
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
			
			bool ParseHeader( string header )
			{
				//Debug.Log("Header: " + header);
				var split = header.IndexOf(':');
				if ( split < 0 )
				{
					EB.Debug.LogError("[NetWebSocket] Failed to parse header: " + header);
					return false;
				}
				
				var key = header.Substring(0, split).Trim().ToLower();
				var value = header.Substring(split+1).Trim();
				_responseHeaders[key] = value;		
				
				//Debug.Log("Header " + key + " " + value);
				
				return true;
			}
			
			string GetResponseHeader( string key )
			{
				string value;
				if ( _responseHeaders.TryGetValue(key.ToLower(), out value) )
				{
					return value;
				}
				return string.Empty;
			}
			
			byte[] Read( int count )
			{
				var buffer = new byte[count];
				int read = 0;
				
				while ( read < count )
				{
					if (!_running)
					{
						throw new System.Exception("Read aborted");
					}
					
					var n = _client.Read(buffer, read, count-read);
					if ( n < 0 )
					{
						throw new System.Exception("Read Failed");
					}
					read += n;
					if (n == 0)
					{
						Thread.Sleep(10); 
					}
				}
				return buffer;
			}
			
			Int16 ReadInt16()
			{
				var buffer = Read(2);
				Array.Reverse(buffer);
				return BitConverter.ToInt16(buffer,0);
			}
			
			Int32 ReadInt32()
			{
				var buffer = Read(4);
				Array.Reverse(buffer);
				return BitConverter.ToInt32(buffer,0);
			}
			
			Int64 ReadInt64()
			{
				var buffer = Read(8);
				Array.Reverse(buffer);
				return BitConverter.ToInt64(buffer,0);
			}
			
			byte[] WriteInt16( Int16 value )
			{
				var buffer = BitConverter.GetBytes( value );
				Array.Reverse(buffer);
				return buffer;
			}
			
			byte[] WriteInt32( Int32 value )
			{
				var buffer = BitConverter.GetBytes( value );
				Array.Reverse(buffer);
				return buffer;
			}
			
			byte[] WriteInt64( Int64 value )
			{
				var buffer = BitConverter.GetBytes( value );
				Array.Reverse(buffer);
				return buffer;
			}
			
			WebSocketFrame ReadFrame()
			{
				WebSocketFrame frame = new WebSocketFrame();
				
				var header = Read(2);
				
				var first = header[0];
				var second = header[1];
				
				frame.fin     = (first  & 0x80) != 0;
	            frame.rsv1    = (first  & 0x40) != 0;
	            frame.rsv2    = (first  & 0x20) != 0;;
	            frame.rsv3    = (first  & 0x10) != 0;;
				frame.opcode  = (first & 0x0F);
				
	            var mask    = (second & 0x80) != 0;;
				
				
				var length = (second & 0x7F);
				
				if ( (frame.opcode & (int)WebSocketOpCode.Control) != 0 )
				{
					// make sure size < 125
					if ( length > 125 ) {
						throw new Exception("Protocol error, control frame not allow to have payload > 125");
					}
				}
				
				// check the lenght
				if (length == 126)
				{
					// 16-bit length				
					length = ReadInt16();
				}
				else if (length == 127)
				{
					// 64-bit length, but we don't support it length >2^31
					length = (int)ReadInt64();
				}
				
				if (mask)
				{
					// mask
					frame.mask = ReadInt32();
				}
				
				frame.payload = Read(length);
				
				return frame;	
			}
			
			
			void WriteFrame( WebSocketFrame frame )
			{
				List<byte> buffer = new List<byte>();
				
				// send fin and opcode
				byte tmp = (byte)frame.opcode;
				if (frame.fin) {
					tmp |= 0x80;
				}
				buffer.Add(tmp);
				
				var len = frame.payload != null ? frame.payload.Length : 0;
				if ( len <= 125 )
				{
					buffer.Add( (byte)(0x80 | len) ); 
					
				}
				else if ( len < Int16.MaxValue )
				{
					buffer.Add( 0x80 | 126 );
					AddRange( buffer, WriteInt16((Int16)len) );  
				}
				else
				{
					buffer.Add( 0x80 | 127 );
					AddRange( buffer, WriteInt64(len) );
				}
				
				// generate a mask
				frame.mask = _random.Next();
				
				var maskBytes = WriteInt32(frame.mask);
				AddRange( buffer, maskBytes );
				
				if (frame.payload != null)
				{
					for (int i =0; i < frame.payload.Length; ++i )
					{
						buffer.Add( (byte)(frame.payload[i] ^ maskBytes[i%4]) );
					}
				}
				
				if (_client != null)
				{
					_client.Write(buffer.ToArray(), 0, buffer.Count);
				}
				
	#if WS_DEBUG			
				EB.Debug.Log("Frame: " + Encoding.ToHexString(buffer.ToArray()));
	#endif
			}
			
			
			bool SendUpgradeRequest()
			{
				var host = _uri.HostAndPort;
				var origin = "http://"+host;
				
				string request = "GET " + _uri.PathAndQuery + " HTTP/1.1\r\n" +
						"Upgrade: WebSocket\r\n" +
	                    "Connection: Upgrade\r\n" +
	                    "Host: " + host + "\r\n" +
	                    "Origin: " + origin + "\r\n" +
						"Sec-WebSocket-Key: " + _key + "\r\n" +
						"Sec-WebSocket-Protocol: " + _protocol + "\r\n" +
						"Sec-WebSocket-Version: 13\r\n" + 
	                    "\r\n";
				
				var bytes = Encoding.GetBytes(request);
				_client.Write(bytes, 0, bytes.Length);
	
	            string header = ReadLine(_client);
	            if (header != "HTTP/1.1 101 Switching Protocols")
	                throw new IOException("Invalid handshake response: " + header);
				
				// read the headers
				while (true)
				{
					if (!_running)
					{
						return false;
					}
					
					var line = ReadLine(_client);
					//Debug.Log("line: " + line + " " + line.Length);
					if ( line.Length == 0 )
					{
						break;
					}
					
					if ( !ParseHeader(line) )
					{
						EB.Debug.LogError("[NetWebSocket] Failed to parse header: " + line);
						break;
					}
				}
				
				if ( GetResponseHeader("connection").ToLower() != "upgrade" )
				{
					throw new IOException("Unknow connection header: " + GetResponseHeader("connection") );
				}
				
				if ( GetResponseHeader("upgrade").ToLower()  != "websocket" )
				{
					throw new IOException("Unknow upgrade header: " + GetResponseHeader("upgrade") );
				}
				
				// check key
				if ( GetResponseHeader("Sec-WebSocket-Accept") != _accept )
				{
					throw new IOException("Unknow connection accpet key " + GetResponseHeader("Sec-WebSocket-Accept") );
				}
				
				// connected!
				_state = WebSocketState.Open;
							
				OnConnect();
				
				return true;
		    }	
			
			int Since( DateTime dt )
			{
				return (int)( (DateTime.Now-dt).TotalMilliseconds );
			}
			
			bool  NeedPing()
			{
				if (_parent.PingTimeout == 0)
				{
					return false;
				}
				
				var ms = Since(_lastPing);
				var ht = _parent.PingTimeout / 2;
				if ( ms > ht) 
				{
					return true;
				}
				return false;
			}
			
			void  CheckTimeout()
			{
				if ( _parent.PingTimeout > 0 ) 
				{
					if ( Since(_lastRead) > _parent.PingTimeout )
					{
						throw new IOException("connection timed out");
					}
				}
				
				if ( _parent.ActivityTimeout > 0 )
				{
					if ( Since(_lastActivity) > _parent.ActivityTimeout )
					{
						EB.Debug.Log("[NetWebSocket] Closing idle connection");
						_running = false;
					}
				}
			}
			
			void SendPing()
			{
	#if WS_DEBUG				
				EB.Debug.Log("sending ping");
	#endif
				_lastPing = DateTime.Now;
				var frame = new WebSocketFrame();
				frame.fin = true;
				frame.opcode = (int)WebSocketOpCode.Ping;
				WriteFrame(frame);
			}
			
			void SendClose()
			{
				var frame = new WebSocketFrame();
				frame.fin = true;
				frame.opcode = (int)WebSocketOpCode.Close;
				WriteFrame(frame);
			}

			void  MainLoop()
			{
				_lastPing 		= DateTime.Now;
				_lastActivity 	= DateTime.Now;
				_lastRead 		= DateTime.Now;
				
				while(_running)
				{
	#if WS_DEBUG				
					//EB.Debug.Log("main loop");
	#endif				
					WebSocketFrame frame = null;
					lock(_writeQueue)
					{
						if (_writeQueue.Count>0)		
						{
							frame = _writeQueue.Dequeue();
						}
					}
					
					if (!_client.Connected)
					{
	#if WS_DEBUG				
						EB.Debug.Log("lost connection");
	#endif						
						Error("[NetWebSocket] Lost connection");
						return;
					}
					else if (frame != null)
					{
						_lastActivity = DateTime.Now;
						WriteFrame(frame);
					}
					else if (_client.DataAvailable)
					{
						frame = ReadFrame();
						switch(frame.opcode)
						{
						case (int)WebSocketOpCode.Text:
							{
								_lastActivity= DateTime.Now;
								OnMessage(Encoding.GetString(frame.payload));
							}
							break;
						case (int)WebSocketOpCode.Binary:
							{
								_lastActivity= DateTime.Now;
								OnData(frame.payload);
							}
							break;
						case (int)WebSocketOpCode.Ping:
							{
	#if WS_DEBUG		
								EB.Debug.Log("Got ping!");
	#endif
								var reply = new WebSocketFrame();
								reply.fin = true;
								reply.opcode = (int)WebSocketOpCode.Pong;
								WriteFrame(reply);
							}
							break;
						case (int)WebSocketOpCode.Pong:
							{
								_ping  = Since(_lastPing);
	#if WS_DEBUG							
								EB.Debug.Log("Got Pong : " + _ping);
	#endif
							}
							break;
						case (int)WebSocketOpCode.Close:
							{
								EB.Debug.Log("Got close!");
								Error("[NetWebSocket] Connection Closed by Server");
								return;
							}
						}
						
						_lastPing = DateTime.Now;
						_lastRead = DateTime.Now;
					}
					else if ( NeedPing() )
					{
						SendPing();
					}
					else
					{
						CheckTimeout();
						Thread.Sleep(5);
					}
				}
			}			
		}
		
		Impl _impl;
		
		
				
		public WebSocket()	
		{
			PingTimeout = 10 * 1000; // 
		}
		
		void DisposeImpl()
		{
			if (_impl!=null)
			{
				//EB.Debug.Log("[NetWebSocket] Disposing web socket: " + _impl._uri.ToString());
				_impl._running=false;
				_impl = null;
			}
		}
		
		void QueueFrame( WebSocketFrame frame )
		{
			if (_impl!=null)
			{
				_impl.QueueFrame(frame);
			}
			else
			{
				Error("[NetWebSocket] sending without connecting");
			}
		}
		
		public virtual void Dispose ()
		{	
			OnError = null;
			OnMessage = null;
			OnData = null;
			
			DisposeImpl();
		}

		public virtual void Reset()
		{
			DisposeImpl();
			_impl = new Impl(this);
		}
		
		public void ConnectAsync( Uri uri, string protocol, byte[] key )
		{
			Reset();
			_impl.Connect(uri,protocol,key);
		}
		
		public void SendUTF8( string message )
		{
#if WS_DEBUG	
			EB.Debug.Log("Send: " + message);
#endif
			var frame = new WebSocketFrame();
			frame.fin = true;
			frame.opcode = (int)WebSocketOpCode.Text;
			frame.payload = Encoding.GetBytes(message);
			QueueFrame(frame);
		}
		
		public void SendBinary( byte[] binary )
		{
#if WS_DEBUG	
			EB.Debug.Log("SendBinary: " + binary.Length);
#endif			
			//Debug.Log("Send: " + message);
			var frame = new WebSocketFrame();
			frame.fin = true;
			frame.opcode = (int)WebSocketOpCode.Binary;
			frame.payload = binary;
			QueueFrame(frame);
		}
		
		public void SendBinary( ArraySegment<byte> segment )
		{
#if WS_DEBUG	
			EB.Debug.Log("SendBinary: " + segment.Count);
#endif			
			//Debug.Log("Send: " + message);
			var frame = new WebSocketFrame();
			frame.fin = true;
			frame.opcode = (int)WebSocketOpCode.Binary;
			frame.payload = new byte[segment.Count];
			System.Array.Copy( segment.Array, segment.Offset, frame.payload, 0, segment.Count ); 
			QueueFrame(frame);
		}	
		
		protected virtual void Error(string err)
		{
			EB.Debug.LogError("[NetWebSocket] WebSocketError: " + err);
		
			DisposeImpl();
			
			if (OnError!=null)
			{
				OnError(err);
			}
		}
		
		public void SendBinary( Buffer buffer )
		{
			SendBinary( buffer.ToArraySegment(false) );
		}
		
		protected virtual void DidClose( Impl impl )
		{
		}

		
	}
}
