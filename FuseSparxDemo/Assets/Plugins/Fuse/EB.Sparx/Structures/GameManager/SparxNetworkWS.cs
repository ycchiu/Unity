using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	// a network implementation using web sockets
	public class NetworkWS : Network
	{
		private enum PacketMask
		{
			None 		= 0x00,		// no flags
			Broadcast 	= 0x80,		// is a broadcast packet
			Player 		= 0x40,		// contains a player id
			Channel 	= 0x0F,		// channel mask
		}

		private Net.WebSocket 	_socket;
		private Deferred 		_deferred;
		private Buffer			_sendBuffer;

		public NetworkWS()
		{
			_deferred = new Deferred(4);
			_sendBuffer = new Buffer(64*1024);
		}

		#region implemented abstract members of EB.Sparx.Network

		public override void Dispose ()
		{
			_deferred.Dispose();
			if (_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}
			base.Dispose ();
		}

		public override void Update ()
		{
			// dispatch calls on
			_deferred.Dispatch();
		}

		public override void Connect (string url, uint connId)
		{
			// convert connId into sec key
			EB.Debug.Log("Connecting to " + url);
			var key = System.BitConverter.GetBytes(connId);
			_socket = new Net.WebSocket();
			_socket.OnData += OnSocketReceive;
			_socket.OnConnect += OnSocketConnect;
			_socket.OnError += OnSocketError;
			_socket.PingTimeout = 5*1000;
			_socket.ConnectAsync( new EB.Uri(url), "io.sparx.game", key );
		}

		public override void Disconnect (bool now)
		{
			if (_socket!=null)
			{
				_socket.Dispose();
				_socket = null;
			}
			DispatchDisconnect(string.Empty);
		}

		public override void SendTo(uint playerId, Packet packet)
		{
			_sendBuffer.Reset();

			byte control = (byte)( (int)PacketMask.Channel & (int)packet.Channel );
			if ( playerId != HostId )
			{
				control = (byte)( control | (int)PacketMask.Player );
				_sendBuffer.WriteByte( control );
				_sendBuffer.WriteUInt32LE( playerId );
			}
			else
			{
				_sendBuffer.WriteByte( control );
			}
			_sendBuffer.WriteBuffer( packet.Data );

			if (_socket!=null)
			{
				_socket.SendBinary( _sendBuffer );
			}
		}

		public override void Broadcast (Packet packet)
		{
			_sendBuffer.Reset();

			byte control = (byte)( (int)PacketMask.Channel & (int)packet.Channel );
			control 	 = (byte)( control | (int)PacketMask.Broadcast );

			_sendBuffer.WriteByte( control );
			_sendBuffer.WriteBuffer( packet.Data );

			if (_socket!=null)
			{
				_socket.SendBinary( _sendBuffer );
			}
		}
		#endregion

		void OnSocketError(string error)
		{
			EB.Debug.LogError("Socket error!: " + error);
			_deferred.Defer( (EB.Action<string>)DispatchError, "ID_SPARX_ERROR_LOST_CONNECT" );
		}

		void OnSocketConnect()
		{
			EB.Debug.Log("NetworkWS: Connected");
			_deferred.Defer( (EB.Action)this.DispatchConnect);
		}

		void OnSocketReceive( byte[] data )
		{
			try
			{
				var buffer 	 = new Buffer(data, false);

				byte control = buffer.ReadByte();
				byte channel = (byte)(control & (int)PacketMask.Channel);
				uint playerId= HostId;
				if ( (control&(int)PacketMask.Player) != 0 )
				{
					playerId = buffer.ReadUInt32LE();
				}

				var remaining = buffer.Remaining();

				var packet = new Packet( (Channel)channel, remaining );
				_deferred.Defer( (EB.Action<uint,Packet>)this.DispatchReceive, playerId, packet );
			}
			catch (System.Exception e )
			{
				_deferred.Defer( (EB.Action<string>)DispatchError, "error occured decoding packet : " + e.ToString() );
			}
		}

		#region implemented abstract members of EB.Sparx.Network
		public override NetworkStats Stats {
			get {
				return default(NetworkStats);
			}
		}

		public override bool IsLocal {
			get {
				return false;
			}
		}
		#endregion
	}
}

