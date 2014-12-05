using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	// a network implementation using ENet
	public class NetworkENet : Network
	{
		private enum PacketMask
		{
			None 		= 0x00,		// no flags
			Broadcast 	= 0x80,		// is a broadcast packet
			Player 		= 0x40,		// contains a player id
		}
		
		enum DisconnectReason
		{
			Normal = 1,
			Now = 2,
		}
		
		private ENet.Host		_host;
		private ENet.Peer		_peer;
		private Buffer			_sendBuffer;
		
		private Tracker			_out;
		private Tracker 		_in;
		private System.DateTime _dT;
		
		private Hmac			_hmac;
		
		static bool _initialized;
		
		public NetworkENet()
		{
			if (!_initialized)
			{
				_initialized = true;
				ENet.Plugin.Initialize();
			}
						
			_sendBuffer = new Buffer(64*1024);
			_dT = System.DateTime.Now;
		}
		
		#region implemented abstract members of EB.Sparx.Network
		
		public override void Dispose ()
		{	
			if ( _peer != null)
			{
				_peer.Dispose();
				_peer = null;
			}
			
			if ( _host != null )
			{
				_host.Dispose();
				_host = null;
			}
			
			base.Dispose ();
		}
		
		public override void Update ()
		{
			if (_host != null )
			{
				
				
				ENet.Event ev = default(ENet.Event);
				
				while(true)
				{
					int count = ENet.Plugin.SerivceHost(_host, out ev, 0 );
					if ( count > 0 )
					{
						switch(ev.type)
						{
						case ENet.EventType.EVENT_TYPE_CONNECT:
							{
								Debug.Log("Got connect!");
								DispatchConnect();
							}
							break;
						case ENet.EventType.EVENT_TYPE_DISCONNECT:
							{
								Debug.Log("Got disconnect!" + ev.channelID + " " + ev.data );
								DispatchDisconnect( string.Empty );
							}
							break;
						case ENet.EventType.EVENT_TYPE_RECEIVE:
							{
								var packet = ev.Packet;
								var buffer = new Buffer( packet.Data, false );
							
								_in.Add(buffer.Capacity);
							
								if (_hmac == null || buffer.Verify(_hmac)) 
								{
									byte control = buffer.ReadByte();
									uint playerId= HostId;
									if ( (control&(int)PacketMask.Player) != 0 )
									{
										playerId = buffer.ReadUInt32LE();
									}
									
									var remaining = buffer.Remaining();
									
									var p = new Packet( (Channel)ev.channelID, remaining );
									DispatchReceive( playerId, p );
								}
								else
								{
									Debug.LogError("Failed to verify packet!!!!!");
								}
							
								packet.Dispose();
							}
							break;
						default:
							break;
						}
					}	
					else if ( count < 0 )
					{
						// error occured
						DispatchError( Localizer.GetString("ID_SPARX_ERROR_LOST_CONNECT") );
						break;
					}
					else
					{
						break;
					}
				}
			}
			
			var now = System.DateTime.Now;
			var dT = (float)(now - _dT).TotalSeconds;
			_in.Update(dT);
			_out.Update(dT);
			_dT = now;
		}
		
		public override void Connect (string url, uint connId)
		{
			// convert connId into sec key
			Debug.Log("Connecting to " + url);
			var uri = new EB.Uri(url);
			
			var local = new ENet.Address();
			
			_host = ENet.Plugin.CreateHost( local, 1, 4, 0, 0 );
			
			var query = EB.QueryString.Parse(uri.Query);
			
			if (EB.Dot.Integer("compressed", query, 0) == 1)
			{
				Debug.Log("Using compression");
				ENet.Plugin.Compress(_host);
			}
			
			var baseKey = Dot.String("signed", query, null);
			if ( !string.IsNullOrEmpty(baseKey) )
			{
				// generate the signing key
				List<byte> key = new List<byte>();
				key.AddRange( Hub.Instance.Config.ApiKey.Value );
				key.AddRange( Encoding.GetBytes( connId.ToString()) );
				_hmac = Hmac.MD5(key.ToArray());				
				Debug.Log("Using signed packets");
			}
			
			var remote = new ENet.Address();
			ENet.Plugin.Lookup( uri.Host, out remote );
			remote.port = (ushort)uri.Port;
			_peer = ENet.Plugin.Connect(_host, remote, 4, connId ); 
		}

		public override void Disconnect (bool now)
		{
			if (_peer != null)
			{
				if (now)
				{
					Debug.Log("Disconnect NOW!");
					ENet.Plugin.PeerDisconnectNow(_peer, (int)DisconnectReason.Now);
					DispatchDisconnect(string.Empty);
				}
				else
				{
					ENet.Plugin.PeerDisconnect(_peer, (int)DisconnectReason.Normal);
				}
			}
		}

		public override void SendTo(uint playerId, Packet packet)
		{
			_sendBuffer.Reset();
						
			byte control = 0;
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
			
			// sign the packet
			if (_hmac != null)
			{
				//Debug.Log("signing packet");
				_sendBuffer.Sign(_hmac);
			}
			
			var segment = _sendBuffer.ToArraySegment(false);
			var flags = ENet.PacketFlag.PACKET_FLAG_NONE;
			
			if ( packet.Channel != Channel.Game_UnReliable )
			{
				flags = ENet.PacketFlag.PACKET_FLAG_RELIABLE;
			}
			
			_out.Add(segment.Count);
	
			///Debug.Log("sending " + segment.Count);
			var p = ENet.Plugin.CreatePacket(segment.Array, segment.Count, flags ); 
			ENet.Plugin.Send(_peer, (int)packet.Channel, p );
			
			p.Dispose();
		}

		public override void Broadcast (Packet packet)
		{
			_sendBuffer.Reset();
			
			byte control = (byte)(PacketMask.Broadcast);
			_sendBuffer.WriteByte( control );
			_sendBuffer.WriteBuffer( packet.Data );
			
			// sign the packet
			if (_hmac != null)
			{
				_sendBuffer.Sign(_hmac);
			}
			
			var segment = _sendBuffer.ToArraySegment(false);
			var flags = ENet.PacketFlag.PACKET_FLAG_NONE;
			
			if ( packet.Channel != Channel.Game_UnReliable )
			{
				flags = ENet.PacketFlag.PACKET_FLAG_RELIABLE;
			}
	
			_out.Add(segment.Count);
			
			var p = ENet.Plugin.CreatePacket(segment.Array, segment.Count, flags ); 
			ENet.Plugin.Send(_peer, (int)packet.Channel, p );
			
			p.Dispose();
		}
		#endregion
		

		#region implemented abstract members of EB.Sparx.Network
		public override NetworkStats Stats {
			get {
				var stats = new NetworkStats();
				stats.bandwidthIn = (int)_in.rate;
				stats.bandwidthOut = (int)_out.rate;
				return stats;
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

