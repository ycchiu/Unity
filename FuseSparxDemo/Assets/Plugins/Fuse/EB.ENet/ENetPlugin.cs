#if UNITY_IPHONE && !UNITY_EDITOR
#define STATIC_LINKAGE
#endif

#define USE_ENET

using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;

namespace EB.ENet
{
#if USE_ENET	
	public enum HostType
	{
	   HOST_ANY       = 0,             /**< specifies the default server host */
	   HOST_BROADCAST = -1,   			/**< specifies a subnet-wide broadcast */
	   PORT_ANY       = 0              /**< specifies that a port should be automatically chosen */
	};
	
	public enum SocketType
	{
	   SOCKET_TYPE_STREAM   = 1,
	   SOCKET_TYPE_DATAGRAM = 2
	};
	
	public  enum SocketWait
	{
	   SOCKET_WAIT_NONE    = 0,
	   SOCKET_WAIT_SEND    = (1 << 0),
	   SOCKET_WAIT_RECEIVE = (1 << 1)
	};
	
	public enum SocketOption
	{
	   SOCKOPT_NONBLOCK  = 1,
	   SOCKOPT_BROADCAST = 2,
	   SOCKOPT_RCVBUF    = 3,
	   SOCKOPT_SNDBUF    = 4,
	   SOCKOPT_REUSEADDR = 5,
	   SOCKOPT_RCVTIMEO  = 6,
	   SOCKOPT_SNDTIMEO  = 7
	};
	
	public enum PacketFlag
	{
	   PACKET_FLAG_NONE = 0,
	   /** packet must be received by the target peer and resend attempts should be
	     * made until the packet is delivered */
	   PACKET_FLAG_RELIABLE    = (1 << 0),
	   /** packet will not be sequenced with other packets
	     * not supported for reliable packets
	     */
	   PACKET_FLAG_UNSEQUENCED = (1 << 1),
	   /** packet will not allocate data, and user must supply it instead */
	   PACKET_FLAG_NO_ALLOCATE = (1 << 2),
	   /** packet will be fragmented using unreliable (instead of reliable) sends
	     * if it exceeds the MTU */
	   PACKET_FLAG_UNRELIABLE_FRAGMENT = (1 << 3)
	};
	
	public enum EventType
	{
	   /** no event occurred within the specified time limit */
	   EVENT_TYPE_NONE       = 0,  
	
	   /** a connection request initiated by enet_host_connect has completed.  
	     * The peer field contains the peer which successfully connected. 
	     */
	   EVENT_TYPE_CONNECT    = 1,  
	
	   /** a peer has disconnected.  This event is generated on a successful 
	     * completion of a disconnect initiated by enet_pper_disconnect, if 
	     * a peer has timed out, or if a connection request intialized by 
	     * enet_host_connect has timed out.  The peer field contains the peer 
	     * which disconnected. The data field contains user supplied data 
	     * describing the disconnection, or 0, if none is available.
	     */
	   EVENT_TYPE_DISCONNECT = 2,  
	
	   /** a packet has been received from a peer.  The peer field specifies the
	     * peer which sent the packet.  The channelID field specifies the channel
	     * number upon which the packet was received.  The packet field contains
	     * the packet that was received; this packet must be destroyed with
	     * enet_packet_destroy after use.
	     */
	   EVENT_TYPE_RECEIVE    = 3
	};
	
	[StructLayout(LayoutKind.Sequential)]
	public struct Address
	{
	   public uint 		host;
	   public ushort 	port;
	};
	
	[StructLayout(LayoutKind.Sequential)]
	public struct Event 
	{
	   public EventType         	type;      /**< type of the event */
	   public IntPtr           		peer;      /**< peer that generated a connect, disconnect or receive event */
	   public byte           		channelID; /**< channel on the peer that generated the event, if appropriate */
	   public UInt32          		data;      /**< data associated with the event, if appropriate */
	   public IntPtr         		packet;    /**< packet associated with the event, if appropriate */
		
	   public Packet			Packet	{ get { if (packet!=IntPtr.Zero) return new Packet(packet); return null; } }
	};
	
	public class Host : IDisposable
	{
		public IntPtr Handle { get; private set;}
		
		public int Socket { get { return Marshal.ReadInt32(Handle,0); } }
		public uint BindIp { get { return (uint)Marshal.ReadInt32(Handle,4); } }
		public ushort BindPort { get { return (ushort)Marshal.ReadInt16(Handle,8); } }
		
		public uint IncomingBandwidth { get { return (uint)Marshal.ReadInt32(Handle,12); } }
		public uint OutgoingBandwidth { get { return (uint)Marshal.ReadInt32(Handle,16); } }
		public uint BandwidthThrottleEpoch { get { return (uint)Marshal.ReadInt32(Handle,20); } }
		public uint Mtu { get { return (uint)Marshal.ReadInt32(Handle,24); } }
		public uint RandomSeed { get { return (uint)Marshal.ReadInt32(Handle,28); } }
		
		public Host( IntPtr handle )
		{
			Handle = handle;
		}
		
		public void Clear()
		{
			Handle = IntPtr.Zero;
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			if ( Handle != IntPtr.Zero )
			{
				Plugin.HostDestroy(this);
				Handle = IntPtr.Zero;
			}
		}
		#endregion
	}
	
	public class Peer : IDisposable
	{
		public IntPtr Handle { get; private set;}
		
		public uint PeerIp { get { return (uint)Marshal.ReadInt32(Handle,24); } }
		public ushort PeerPort { get { return (ushort)Marshal.ReadInt16(Handle,28); } }

		public Peer( IntPtr handle )
		{
			Handle = handle;
			
			Debug.Log("New Peer: " + handle.ToInt32() );
			
			Debug.Log("PeerIp: " + new System.Net.IPAddress(PeerIp) );
			Debug.Log("PeerPort: " + PeerPort );
		}
		
		public void Clear()
		{
			Handle = IntPtr.Zero;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if ( Handle != IntPtr.Zero )
			{
				Plugin.PeerDisconnectNow(this, 0);
				Handle = IntPtr.Zero;
			}
		}
		#endregion
	}
	
	public class Packet : IDisposable
	{
		public IntPtr Handle { get; private set;}
		
		public int RefCount { get { return Marshal.ReadInt32(Handle,0); } }
		public uint Flags { get { return (uint)Marshal.ReadInt32(Handle,4); } }
		public byte[] Data
		{
			get
			{
				// get the ptr
				IntPtr ptr  = Marshal.ReadIntPtr(Handle,8);
				int length 	= Marshal.ReadInt32(Handle, 12);
				byte[] data = new byte[length];
				Marshal.Copy(ptr, data, 0, length); 
				return data;
			}
		}
		
		bool isSent = false;
		
		public Packet( IntPtr handle )
		{
			Handle = handle;
		}
		
		public void Dispose ()
		{
			// destroy the packet
			if (!isSent && Handle != IntPtr.Zero)
			{
				Plugin.DestroyPacket(this);
			}
		}
		
		public void Reset()
		{
			Handle = IntPtr.Zero;
			isSent = false;
		}
		
		public void MarkSent()
		{
			isSent = true;
		}
	}
	
	public static class Plugin
	{
	#if STATIC_LINKAGE	
		const string DLL_NAME = "__Internal";
	#else
		const string DLL_NAME = "enetlib";
	#endif	
	
		[DllImport(DLL_NAME)]
		static extern int enet_initialize();
		
		[DllImport(DLL_NAME)]
		static extern void enet_deinitialize(); 
		
		[DllImport(DLL_NAME)]
		static extern uint enet_time_get(); 
		
		[DllImport(DLL_NAME)]
		static extern IntPtr enet_host_create( [MarshalAs(UnmanagedType.LPStruct)] Address address, uint peerCount, uint channelLimit, uint incomingBandwidth, uint outgoingBandwidth);
		
		[DllImport(DLL_NAME)]
		static extern int enet_host_service( IntPtr host, IntPtr netevent, uint timeout );
		
		[DllImport(DLL_NAME)]
		static extern int enet_address_set_host( IntPtr address, [MarshalAs(UnmanagedType.LPStr)] string hostname );
		
		[DllImport(DLL_NAME)]
		static extern IntPtr enet_host_connect( IntPtr host, [MarshalAs(UnmanagedType.LPStruct)] Address address, uint channelCount, uint data	);	
		
		[DllImport(DLL_NAME)]
		static extern int enet_host_compress_with_range_coder( IntPtr host);
		
		[DllImport(DLL_NAME)]
		static extern IntPtr enet_packet_create ( byte[] data, int dataLength, uint flags);
		
		[DllImport(DLL_NAME)]
		static extern void enet_packet_destroy (IntPtr packet);
		
		[DllImport(DLL_NAME)]
		static extern void enet_peer_disconnect_now (IntPtr peer, uint data);
		
		[DllImport(DLL_NAME)]
		static extern void enet_peer_disconnect(IntPtr peer, uint data);
		
		[DllImport(DLL_NAME)]
		static extern int	enet_peer_send (IntPtr peer, byte channel, IntPtr packet);
		
		[DllImport(DLL_NAME)]
		static extern void enet_host_destroy(IntPtr host);
		
		public static int Initialize()
		{
			return enet_initialize();
		}
		
		public static void Shutdown()
		{
			enet_deinitialize();
		}
		
		public static uint GetTime()
		{
			return enet_time_get();
		}
		
		private static IntPtr ToPtr<T>( T obj )
		{
			var ptr = Marshal.AllocHGlobal( Marshal.SizeOf(typeof(T)) );
			Marshal.StructureToPtr(obj, ptr, false);
			return ptr;
		}
		
		private static void Free( IntPtr ptr )
		{
			if ( ptr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(ptr);
			}
		}
		
		public static int Lookup( string hostname, out Address addr ) 
		{
			var tmp 	= Marshal.AllocHGlobal( Marshal.SizeOf(typeof(Address)) );
			var result	= enet_address_set_host(tmp, hostname );
			addr = (Address)Marshal.PtrToStructure(tmp, typeof(Address));
			Marshal.FreeHGlobal(tmp);
			Debug.Log("Lookup: " + hostname + " = " + new System.Net.IPAddress( (long)addr.host).ToString() );
			return result;
		}
		
		public static Host CreateHost( Address addr, uint peerCount, uint channelLimit, uint incomingBandwidth, uint outgoingBandwidth )  
		{
			var handle = enet_host_create( addr, peerCount, channelLimit, incomingBandwidth, outgoingBandwidth );
			if ( handle != IntPtr.Zero )
			{
				return new Host(handle);
			}
			return null;
		}
		
		public static int  Compress( Host host )
		{
			return enet_host_compress_with_range_coder(host.Handle);
		}
		
		public static Peer Connect( Host host, Address addr, uint channelCount, uint data ) 
		{
			var handle = enet_host_connect( host.Handle, addr, channelCount, data);
			if ( handle != IntPtr.Zero )
			{
				return new Peer(handle);
			}
			return null;
		}
		
		public static int SerivceHost( Host host, out Event netEvent, uint timeout )
		{
			netEvent = default(Event);
			
			if (host.Handle == IntPtr.Zero)
			{
				return 0;
			}
			var tmp 	= Marshal.AllocHGlobal( Marshal.SizeOf(typeof(Event)) );
			var result 	= enet_host_service( host.Handle, tmp, timeout );
			
			Event tmpEvn = (Event)Marshal.PtrToStructure(tmp, typeof(Event));  
			Marshal.FreeHGlobal(tmp);
			
			netEvent = tmpEvn;
			
			return result;			
		}
		
		public static Packet CreatePacket( byte[] data, int length, PacketFlag flags ) 
		{
			var result = enet_packet_create( data, length, (uint)flags);
			if ( result != IntPtr.Zero)
			{
				return new Packet(result);
			}
			return null;
		}
		
		public static void DestroyPacket( Packet packet )
		{
			if (packet != null)
			{
				var handle = packet.Handle;
				packet.Reset();
				
				if ( handle != IntPtr.Zero )
				{
					//Debug.Log("Destroying packet " + handle );
					enet_packet_destroy(handle);
				}
			}
		}
		
		public static int Send( Peer peer, int channel, Packet packet)
		{
			if ( peer == null || peer.Handle == IntPtr.Zero)
			{
				return -1;
			}
			
			packet.MarkSent();
			return enet_peer_send(peer.Handle, (byte)channel, packet.Handle);
		}
		
		public static void PeerDisconnectNow( Peer peer, uint data )
		{
			if ( peer == null || peer.Handle == IntPtr.Zero)
			{
				return;
			}
			
			enet_peer_disconnect_now(peer.Handle, data);
			peer.Clear();
		}
		
		public static void PeerDisconnect( Peer peer, uint data )
		{
			if (peer == null || peer.Handle == IntPtr.Zero)
			{
				return;
			}
			
			enet_peer_disconnect(peer.Handle, data);
			peer.Clear();
		}
		
		public static void HostDestroy( Host host )
		{
			if (host == null || host.Handle == IntPtr.Zero)
			{
				return;
			}
			
			enet_host_destroy(host.Handle);
			host.Clear();
		}

	}
#endif	
}