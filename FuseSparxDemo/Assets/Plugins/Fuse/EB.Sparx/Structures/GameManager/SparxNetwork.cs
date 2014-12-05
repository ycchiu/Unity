using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public enum Channel
	{
		Manager_Reliable	= 0,
		Manager_Unreliable	= 1,
		Game_Reliable		= 2,
		Game_UnReliable		= 3,
	}
	
	public struct NetworkStats
	{
		public int bandwidthOut;
		public int bandwidthIn;
		public int totalDataOut;
		public int totalDataIn;
	}
	
	public struct Tracker
	{
		public int counter;
		public int total;
		public float interval;
		public float rate;
		
		public void Add( int v )
		{
			counter += v;
		}
		
		public void Update(float dT)
		{
			interval += dT;
			if (interval > 1.0f)
			{
				rate = counter / interval;
				counter = 0;
				interval = 0;
			}
		}
	}
	
	public class Packet
	{
		public Channel Channel 	{ get;set; }
		public Buffer	Data 	{ get;set; }
		public string 	Utf8Data{ get { return Encoding.GetString(Data);} }
		
		public Packet( Channel channel, Buffer buffer )
		{
			Channel = channel;
			Data = buffer;
		}
		
		public Packet( Channel channel, byte[] data ) :
			this( channel, new Buffer(data, true) )
		{
		}
		
		public Packet( Channel channel, string message ) :
			this( channel, Encoding.GetBytes(message) )
		{
		}
	}
	
	public abstract class Network : System.IDisposable
	{
		public abstract NetworkStats Stats {get;}
		public abstract bool IsLocal {get;}
		
		// the host id
		public const uint HostId = 0;
		public const uint BroadcastId = uint.MaxValue;
		public const uint NpcId = 10;
		public const uint NpcIdLast = 20;
		
		public abstract void Connect( string url, uint connId );
		public abstract void Disconnect(bool now);
		
		public abstract void SendTo( uint playerId, Packet packet );
		public abstract void Broadcast( Packet packet );
		
		public abstract void Update();
		
		public virtual void Dispose(){
			OnConnect = null;
			OnDisconnect = null;
			OnError = null;
			OnReceive = null;
		}
		
		public event EB.Action 					OnConnect;
		public event EB.Action<string>			OnDisconnect;
		public event EB.Action<string>			OnError;
		public event EB.Action<uint,Packet>		OnReceive;		
		
		protected void DispatchConnect() 							{ if (OnConnect!=null) OnConnect(); }
		protected void DispatchDisconnect(string reason) 			{ if (OnDisconnect!=null) OnDisconnect(reason); }
		protected void DispatchError(string error) 					{ if (OnError != null) OnError(error); }
		protected void DispatchReceive(uint playerId,Packet packet) { if (OnReceive != null) OnReceive(playerId,packet); } 
	}
	
	public static class NetworkFactory 
	{
		static Dictionary<string,System.Type> _types = new Dictionary<string, System.Type>();
		
		public static void Register( string scheme, System.Type type ) 
		{
			_types.Add(scheme, type);
		}
		
		public static Network Create( string url )
		{
			var uri = new EB.Uri(url);
			System.Type type = null;
			if (_types.TryGetValue(uri.Scheme, out type))
			{
				return (Network)System.Activator.CreateInstance(type);
			}
			throw new System.Exception("Uknown network type: " +  uri.Scheme);
		}
		
		public static void Unregister(string scheme)
		{
			_types.Remove(scheme);
		}
	}
}
