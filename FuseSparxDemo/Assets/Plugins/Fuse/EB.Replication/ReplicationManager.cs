//#define USE_FARSEER
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if USE_FARSEER
using Microsoft.Xna.Framework;
#endif

namespace EB.Replication
{
	public interface ISerializable
	{
		void Serialize(BitStream bs);
	}
	
	public class Manager
	{		
		class AllocationInfo
		{
			uint _p;
			uint _n;
			uint _m;
			bool _s;
			
			public AllocationInfo( uint p, uint n, uint m, bool s)
			{
				_p = p;
				_n = n;
				_m = n+m;
				_s = s;
			} 
			
			public ViewId Allocate()
			{
				if (_n == _m)
				{
					throw new System.Exception("Ran out of view ids: " + ToString());
				}
				var id = new ViewId();
				id.p = _p;
				id.n = _n;
				id.s = _s;
				
				++_n;
				
				//Debug.Log("Allocate " + id);
				
				return id;
			}
			
			public void ClearSceneFlag()
			{
				_s = false;
			}
			
			public override string ToString ()
			{
				return string.Format ("[AllocationInfo: p{0}, n{1}, m{2}, s{3}]", _p, _n, _m, _s);
			}
		}
		
		interface IRpcCaller
		{
			void Invoke(object[] args);
		}
		
		class DelegateRpc : IRpcCaller
		{
			System.Delegate _cb;
			
			public DelegateRpc( System.Delegate cb )
			{
				_cb = cb;
			}
			
			public void Invoke( object[] args )
			{
				if (_cb != null)	
				{
					try
					{
						_cb.DynamicInvoke(args);
					}
					catch(System.Exception e)
					{
						EB.Debug.LogError("Exception calling delegate RPC: " + _cb.ToString() + " " + e.ToString());
					}
				}
			}
		}
		
		class MethodRpc : IRpcCaller
		{
			object _t;
			string _m;
			
			public MethodRpc( object target, string method )
			{
				_t = target;
				_m = method;
			}
			
			public void Invoke( object[] args )
			{
				if (_t != null)	
				{
					try 
					{
						_t.GetType().InvokeMember( _m, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, _t, args );  
					}
					catch (System.Exception ex ){
						EB.Debug.LogError("Failed to call " + _m + " " + ex);
					}
				}
			}
		} 
		
		public delegate Object InstantiateMethodDelegate( Object prefab, Vector3 position, Quaternion rotation );
		public static InstantiateMethodDelegate InstantiateMethod = null;
		
		public static int UpdateRate {get;set;}
		
		private static Dictionary<ViewId,View> _viewMap = new Dictionary<ViewId, View>();
		private static Dictionary<string,IRpcCaller> _rpc = new Dictionary<string,IRpcCaller>();
		private static Sparx.Game _game = null;
		
		private static EB.Collections.Stack<AllocationInfo> _allocations = new EB.Collections.Stack<AllocationInfo>();
		
		private static uint _localP;
		private static uint _localN;
		private static List<uint> _myIds = new List<uint>();
		
		private static byte _viewCmd 	= 0;
		private static byte _rpcCmd 	= 1;
		private static byte _viewRpcCmd = 2;
		private static System.DateTime _lastUpdate;
		private static Dictionary<uint,List<Buffer>> _reliable 		= new Dictionary<uint,List<Buffer>>();
		private static Dictionary<uint,List<Buffer>> _unreliable 	= new Dictionary<uint,List<Buffer>>();
		private static Dictionary<int,Object> _objMap				= new Dictionary<int, Object>();
		private static Buffer _buffer								= new Buffer( 256 * 1024 ); // more than enough
		private static Buffer _tmp									= new Buffer( 16 * 1024 );
		private static bool _needsFlush								= false;
		private static bool _pauseViewUpdates						= false;
		
		private static Dictionary<byte,System.Type>	_serializablesIn	= new Dictionary<byte, System.Type>();
		private static Dictionary<System.Type,byte>	_serializablesOut	= new Dictionary<System.Type, byte>();
		
		private static byte _serializeNext							= (byte)VarArgsType.Serializable;
		
		public static bool IsHost { get { return _game != null ? _game.IsHost : true; } }
		public static uint LocalPlayerId { get { return _localP; } }
		public static Sparx.Player LocalPlayer { get { return _game != null ? _game.LocalPlayer : null; } }
		public static int  LocalPlayerIndex { get { return _game != null ? _game.LocalPlayer.Index : 0; } }
		public static bool IsConnected { get { return _game != null; } }
		public static List<Sparx.Player> Players
		{
			get
			{
				List<Sparx.Player> players = null;
				if( _game != null )
				{
					players = _game.Players;
				}
				else
				{
					players = new List<Sparx.Player>();
					Players.Add( new Sparx.Player( new Sparx.Id(), 0 ) );
				}
				return players;
			}
		}
		public static bool IsLocalGame { get { return _game == null || _game.Network.IsLocal; } }
		
		public static bool IsLoadingLevel {get; private set;}
		
		// for load level
		private static Hashtable _doneLoading = new Hashtable();
		
		// constants
		const string LoadLevelRPC 				= "0";
		const string LoadLevelCompleteRPC 		= "1";
		const string InstantiateRPC 			= "2";
		const string TransferSceneOwnershipRPC 	= "4";
		const string TransferOwnershipRPC 		= "5";
		const string DestroyRPC 				= "6";
		const string BroadcastRPC				= "7";
		const uint	 MaxNetworkViewsPerObject 	= 10;
		const uint 	 MaxNetworkViewsPerScene  	= 2000;
		const int	 MaxPacketSize = 512; 
		
		enum VarArgsType
		{
			Null,		//0
			Boolean,	// 1
			Byte,		// 2
			Short,		// 3
			UShort,		// 4
			Int,		// 5
			UInt,		// 6
			Long,		// 7
			ULong,		// 8
			Single,		// 9
			Double,		// 10
			Object,		// 11
			String,		// 12
			Vector2,	// 13
			Vector3,	// 14
			Vector4,	// 15
			Quaternion,	// 16
			ViewId,		// 17
#if USE_FARSEER
			FVector2,	// 18
#endif
			
			Serializable, // 19
		}
		
		static Manager()
		{
			UpdateRate = 15;
			RegisterRPC(LoadLevelRPC, (Action<uint,uint,string,bool,bool>)_LoadLevelRPC);
			RegisterRPC(LoadLevelCompleteRPC, (Action<uint>)_LoadLevelCompleteRPC );
			RegisterRPC(InstantiateRPC, (Action<uint,uint,Object,Vector3,Quaternion>)_InstantiateRPC );
			RegisterRPC(TransferSceneOwnershipRPC, (Action<uint,uint>)_TransferSceneOwnershipRPC);
			RegisterRPC(TransferOwnershipRPC, (Action<ViewId,uint,uint>)_TransferOwnershipRPC);
			RegisterRPC(DestroyRPC, (Action<uint,uint>)_DestroyRPC);
			RegisterRPC(BroadcastRPC, (Action<string>)_BroadcastMessageRPC);
		}
		
		public static View GetView( ViewId id )
		{
			View v;
			_viewMap.TryGetValue(id, out v);
			return v;
		}
		
		public static GameObject GetObject( ViewId id )
		{
			var v = GetView(id);
			return v ? v.gameObject : null;
		}
		
		public static void PauseViewUpdates( bool pause )
		{
			_pauseViewUpdates = pause;
		}
		
		public static void SetGame( Sparx.Game game )
		{
			_game   = game;
			_localP = game.LocalPlayer.PlayerId;
			_localN = 1;
			_lastUpdate = System.DateTime.Now;
			
			_myIds.Clear();
			_myIds.Add(_localP);
			
			// AI ids
			for ( var i = Sparx.Network.NpcId; i <= Sparx.Network.NpcIdLast; ++i)
			{
				_myIds.Add(i);	
			}
			
			_buffer.Reset();
		}
		
		public static bool IsMine( ViewId id )
		{
			return _myIds.Contains(id.p) || (id == default(ViewId) );
		}

		public static Sparx.Player GetPlayer(uint p)
		{
			if ( _game != null )
			{
				return _game.GetPlayer( p );
			}
			return null;
		}
		
		public static void ClearGame()
		{
			_game = null;
		}
		
		public static void RegisterSerializable( System.Type type )
		{
			_serializablesIn[_serializeNext] = type;
			_serializablesOut[type] = _serializeNext;
			_serializeNext++;
		}
				
		public static ViewId Register( View view )
		{
			var viewId = AllocateViewId();
			_viewMap[viewId] = view;
			return viewId;
		}
		
		public static void Unregister( View view ) 
		{
			_viewMap.Remove( view.viewId ); 
		}
		
		public static void RegisterRPC( string name, System.Delegate function )
		{
			_rpc[name] = new DelegateRpc(function);
		}
		
		public static void RegisterPrefab( Object obj )
		{
		//	EB.Debug.Log("Register prefab {0}->{1}", obj.name, Hash.StringHash(obj.name) );
			_objMap[Hash.StringHash(obj.name)] = obj;
		}
		
		public static void RegisterResource( string path )
		{
			RegisterPrefab( Assets.Load(path) ); 
		}
		
		public static void RegisterResourceFolder( string folder )
		{
			foreach( var obj in Assets.LoadAll(folder,typeof(Object)) )
			{
				RegisterPrefab(obj);
			}
		}
		
		public static void RegisterRPCs( object obj )
		{
			var type = obj.GetType();
			var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			foreach( var method in methods )
			{
				if ( method.GetCustomAttributes( typeof(UnityEngine.RPC), true ).Length > 0 )  
				{
					_rpc[method.Name] = new MethodRpc(obj, method.Name);
				}
			}
		}
		
		public static void TransferSceneOwnership()
		{
			// take control of the scene objects
			var nextId = _localN;
			_localN += MaxNetworkViewsPerScene;
			
			RPC(TransferSceneOwnershipRPC, RPCMode.All, _localP, nextId );
		}
		
		// MEGA HACK FOR SP
		public static ViewId TransferOwnershipTo (View view, uint owner)
		{
			// never allow this in a only game!
			if ( !IsLocalGame )	
			{
				EB.Debug.LogError("Cant call this silly function in a online game!");
				return view.viewId;
			}
			
			var newId = view.viewId;
			
			_viewMap.Remove(newId);
			newId.p = owner;
			_viewMap.Add( newId, view);
			
			return newId;
		}
		
		public static void TransferOwnership( ViewId viewId ) 
		{
			var nextId 	= _localN;
			_localN 	+= 1;	
			RPC(TransferOwnershipRPC, RPCMode.AllBuffered, viewId, _localP, nextId);
		}
		
		static void _DestroyRPC( uint p, uint n )
		{
			var viewId = new ViewId();
			viewId.n = n;
			viewId.p = p;
			View view;
			if (_viewMap.TryGetValue(viewId, out view) )
			{
				UnityEngine.Object.Destroy(view.gameObject);
			}
			else
			{
				EB.Debug.LogError("Failed to destroy object " + viewId );
			}
		}
		
		static void _TransferOwnershipRPC( ViewId viewId, uint newOwner, uint nextId ) 
		{
			View view;
			if (!_viewMap.TryGetValue(viewId, out view))
			{
				EB.Debug.LogError("Transfer ownership failed, can't find object for view id " + viewId );
				return;
			}
			
			Push(newOwner,nextId,1,viewId.s);
			view.TransferOwnership();
			Pop();
		}
		
		static void _TransferSceneOwnershipRPC( uint newOwner, uint nextId ) 
		{
			var items = new List<View>();
			foreach( KeyValuePair<ViewId,View> kvp in _viewMap)
			{
				if (kvp.Key.s)
				{
					items.Add(kvp.Value);
				}
			}
			
			// sort by viewId
			items.Sort(delegate(View a, View b){
				return a.viewId.CompareTo(b.viewId);
			});
			
			Push(newOwner, nextId, MaxNetworkViewsPerScene, true);
			foreach( var view in items )
			{
				//Debug.Log("Transfering ownership of " + view.name);
				view.TransferOwnership();
			}
			Pop();
		}
				
		public static void Update()
		{
			if (_game != null)
			{
				var updateT = 1.0f / UpdateRate;
				var dT = (System.DateTime.Now-_lastUpdate).TotalSeconds;
				
				if ( dT < updateT )
				{
					if (_needsFlush)
					{
						Flush();
					}
					return;
				}
				
				if (!IsLocalGame && !_pauseViewUpdates)
				{
					SerializeViews();
				}
				
				_lastUpdate = System.DateTime.Now;
				
				Flush();
			}
		}
		
		public static void OnPlayerLeft( Sparx.Game game, Sparx.Player player )
		{
			if ( game != _game )
			{
				return;
			}
			
			var playerId = player.PlayerId;
			var isHost	 = IsHost; // are we the next host?
			
			// migrate objects
			var transfer 	= new List<View>(16);
			var destroy 	= new List<View>(16);
			
			foreach( KeyValuePair<ViewId, View> kvp in _viewMap )
			{
				if ( kvp.Key.p == playerId )
				{
					switch(kvp.Value.migrationSynchronization)	
					{
					case NetworkMigrationSynchronization.Host:
						{
							if ( isHost )
							{
								transfer.Add(kvp.Value);
							}
						}
						break;
					case NetworkMigrationSynchronization.Destroy:
						{
							destroy.Add(kvp.Value);
						}
						break;
					default:
						break;
					}
				}
			}
			
			// destroy
			foreach( var view in destroy )
			{
				EB.Debug.Log("Migration: Destroying " + view.name );
				Object.Destroy(view.gameObject);
			}
			
			// migrate
			foreach( var view in transfer )
			{
				EB.Debug.Log("Migration: Transfering " + view.name );
				TransferOwnership( view.viewId );
			}
			
			Flush();
		}
		
		public static void Receive( uint fromId, Buffer buffer )
		{
			//EB.Debug.Log("Receive: fromId: {0} buffer: {1}", fromId, Encoding.ToHexString(data,1));  
			try 
			{
				var bs = new BitStream( buffer, false );
				while( bs.DataAvailable )
				{
					byte cmd = 0;
					bs.Serialize(ref cmd);
					if ( cmd == _rpcCmd ) 
					{
						ReadRPC( fromId, bs );
					}
					else if ( cmd == _viewCmd )
					{
						ReadViewUpdate( fromId, bs );
					}
					else if ( cmd == _viewRpcCmd )
					{
						ReadViewRPC( fromId, bs );
					}
					else
					{
						throw new System.ArgumentException("Unknow command " + cmd );
					}
					
					// VERY IMPORTANT!
					// We have to reset the boolean flag here because they weren't serialized 
					// with bool packing between the cmd's.
					bs.ResetBoolFlag();
				}
			}
			catch (System.Exception ex)
			{
				var buff = buffer.ToArraySegment(true);
				EB.Debug.LogError("Invalid packet: " + Encoding.ToHexString(buff.Array, buff.Offset, buff.Count, 1));
				throw ex; // rethrow
			}

		}
		
		static void Flush()
		{
			Flush( _reliable, Sparx.Channel.Game_Reliable );
			Flush( _unreliable, Sparx.Channel.Game_UnReliable );
			//Flush( _unreliable, Sparx.Channel.Game_Reliable );
			_buffer.Reset();
			_needsFlush = false;
		}
		
		static void Send( uint playerId, Sparx.Channel channel, Buffer buffer ) 
		{
			//Debug.Log("Sending: to {0} buffer: {1}", (int)playerId, buffer );
			if ( playerId == Sparx.Network.BroadcastId )
			{
				_game.Broadcast( channel, buffer );
			}
			else
			{
				_game.SendTo( playerId, channel, buffer ); 
			}
		}
		
		static void Flush( Dictionary<uint,List<Buffer>> dic, Sparx.Channel channel ) 
		{			
			// try to fit as many updates as we can in a single packet
			foreach( KeyValuePair<uint,List<Buffer>> kvp in dic ) 
			{
				var playerId = kvp.Key;
				var data = kvp.Value;
			
				_tmp.Reset();
				
				foreach( var chunk in data )
				{	
					// check to see if we should packetize this
					if ( chunk.Length > MaxPacketSize )
					{
						// don't packetize
						Send( playerId, channel, chunk );
						continue;
					}
					
					// see if we need to flush
					if ( (_tmp.Length + chunk.Length) > MaxPacketSize )
					{
						Send( playerId, channel, _tmp );
						_tmp.Reset();
					}
					
					_tmp.WriteBuffer(chunk);
				}
				
				if ( _tmp.Length > 0 )
				{
					Send( playerId, channel, _tmp );
					_tmp.Reset();
				}
				
				data.Clear();
			}			
		}
		
		static void SerializeViews()
		{
			// go through the views and serialize them	
			foreach( KeyValuePair<ViewId,View> kvp in _viewMap ) 
			{
				var viewId = kvp.Key;
				var view  = kvp.Value;
				if ( viewId.p == _localP )
				{
					if ( view.enabled && view.gameObject.activeSelf && view.observed != null && view.stateSynchronization != NetworkStateSynchronization.Off)
					{
						_tmp.Reset();
						
						var bs = new BitStream(_buffer,true);
						var bs2= new BitStream(_tmp, true);
						
						bs.Serialize( ref _viewCmd );
						
						// write out the n (we don't need the whole view update (as they know who sent the data) )
						bs.SerializeUInt24( ref viewId.n ); 
						
						// serialize on a different adapter so we can fast-forward the stream if the object isn't there on the other side
						view.Serialize(bs2);
						
						var buf = bs2.Slice();
						
						if ( buf.Length > 0 )
						{
							bs.Serialize( ref buf );
																		
							if ( view.stateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed )
							{
								// see if we need to send this update
								var digest = buf.Digest();
								if ( digest != view.lastHash )
								{
									view.lastHash = digest;
									GetPacketList( Sparx.Network.BroadcastId, _reliable ).Add( bs.Slice() ); 
								}
								else
								{
									//Debug.Log("Digest is the same, skipping view update on " + view.name);
								}
							}
							else
							{
								GetPacketList( Sparx.Network.BroadcastId, _unreliable ).Add( bs.Slice() ); 
							}
						}
					}
				}
			}
		}
		
	
		static List<Buffer> GetPacketList( uint playerId, Dictionary<uint,List<Buffer>> dic ) 
		{
			List<Buffer> list;
			if ( !dic.TryGetValue( playerId, out list) )
			{
				list = new List<Buffer>();
				dic.Add(playerId, list);
			}
			return list;
		}
		
		static public void RPC( string name, RPCMode mode, params object[] args )
		{			
			var nextId = _localN;
			_localN += MaxNetworkViewsPerObject;
		
			if ( IsLocalGame )
			{
				if ( mode != RPCMode.Others && mode != RPCMode.OthersBuffered )
				{					
					CallRPC( _localP, nextId, name, args);
				}
				return;
			}
			
			switch(mode)
			{
			case RPCMode.All:
			case RPCMode.AllBuffered:
				{
					var res = WriteRPC( Sparx.Network.BroadcastId, nextId, name,args);
					Receive(_localP, res.Clone() );
				}
				break;
			case RPCMode.Others:
			case RPCMode.OthersBuffered:
				{
					WriteRPC( Sparx.Network.BroadcastId, nextId, name, args);
				}
				break;
			case RPCMode.Server:
				{
					if ( _game != null )
					{
						var hostPlayer = _game.HostPlayer;
						if ( hostPlayer.PlayerId == _localP )
						{
							CallRPC(_localP, nextId, name, args); 
						}
						else
						{
							WriteRPC( hostPlayer.PlayerId, nextId, name, args);	
						}
					}
				}
				break;
			}
			
			// if we aren't buffered, then flush
			if ( mode != RPCMode.AllBuffered && mode != RPCMode.OthersBuffered )
			{
				_needsFlush = true;
			}
			
		}
		
		static public void ViewRPC( string name, ViewId id, RPCMode mode, params object[] args )
		{
			var nextId = _localN;
			_localN += MaxNetworkViewsPerObject;
			
			if (IsLocalGame)
			{
				if ( mode != RPCMode.Others && mode != RPCMode.OthersBuffered )
				{	
					CallViewRPC(id.p,nextId,name,id,args);
				}
				return;
			}
			
			switch(mode)
			{
			case RPCMode.All:
			case RPCMode.AllBuffered:
				{
					var res = WriteViewRPC( Sparx.Network.BroadcastId, nextId, id, name,args);
					Receive( _localP, res.Clone() );
				}
				break;
			case RPCMode.Others:
			case RPCMode.OthersBuffered:
				{
					WriteViewRPC( Sparx.Network.BroadcastId, nextId, id, name,args);
				}
				break;
			case RPCMode.Server:
				{
					if ( _game != null )
					{
						var hostPlayer = _game.HostPlayer;
						if ( hostPlayer.PlayerId == _localP )
						{
							CallViewRPC(_localP, nextId, name, id, args); 
						}
						else
						{
							WriteViewRPC( hostPlayer.PlayerId, nextId, id, name, args);	
						}
					}
				}
				break;
			}
			
			// if we aren't buffered, then flush
			if ( mode != RPCMode.AllBuffered && mode != RPCMode.OthersBuffered )
			{
				_needsFlush = true;
			}
		}
		
        public static GameObject GetObjectFromViewId( ViewId viewId )
        {
            View view = null;
            _viewMap.TryGetValue( viewId, out view );
            return ( view != null ) ? view.gameObject : null;
		}
        
		static void ReadViewUpdate( uint fromId, BitStream bs ) 
		{
			var viewId = new ViewId();
			viewId.p = fromId;
			bs.SerializeUInt24( ref viewId.n );
			
			byte[] bytes = null;
			bs.Serialize( ref bytes );
			
			View view;
			if ( _viewMap.TryGetValue( viewId, out view ) )
			{
				var bs2 = new BitStream(bytes);
				try
				{
					view.Serialize(bs2); 
				}
				catch (System.Exception e)
				{
					EB.Debug.LogError("Exception while deserializing view {0}, buffer {1}, {2}, ex: {3}", viewId, bytes.Length, Encoding.ToHexString(bytes,1), e );
				}
			}
			else
			{
				// this can happen during transfer of ownership
				//EB.Debug.LogWarning("Failed to deserialize view update for id: " + viewId );
			}
		}
		
		static void ReadRPC( uint fromId, BitStream bs )
		{
			string name = string.Empty;
			bs.Serialize( ref name);
			
			uint nextId = 0;
			bs.Serialize( ref nextId );
			
			object[] args = null;
			SerializeVarAgs( bs, ref args );
			
			CallRPC(fromId, nextId, name, args ); 
		}
		
		static void ReadViewRPC( uint fromId, BitStream bs )
		{
			string name = string.Empty;
			bs.Serialize( ref name);
			
			ViewId viewId = new ViewId();
			bs.Serialize( ref viewId );
			
			uint nextId = 0;
			bs.Serialize( ref nextId );
			
			object[] args = null;
			SerializeVarAgs( bs, ref args );
			
			CallViewRPC( fromId, nextId, name, viewId, args ); 
		}
		
		static Buffer WriteViewRPC( uint playerId, uint nextId, ViewId id, string name, object[] args ) 
		{	
			var bs = new BitStream(_buffer,true);
			bs.Serialize( ref _viewRpcCmd );
			bs.Serialize( ref name );
			bs.Serialize( ref id );
			bs.Serialize( ref nextId );
			
			SerializeVarAgs( bs, ref args ); 
			
			var slice = bs.Slice();
			GetPacketList( playerId, _reliable ).Add( slice ); 
			return slice;
		}
		
		static Buffer WriteRPC( uint playerId, uint nextId, string name, object[] args ) 
		{
			var bs = new BitStream(_buffer,true);
			bs.Serialize( ref _rpcCmd );
			bs.Serialize( ref name );
			bs.Serialize( ref nextId );
			
			SerializeVarAgs( bs, ref args ); 
			
			var slice = bs.Slice();
			GetPacketList( playerId, _reliable ).Add( slice ); 
			return slice;
		}
		
		static void CallRPC( uint ownerId, uint nextId, string name, object[] args )
		{
			Push( ownerId, nextId, MaxNetworkViewsPerObject, false);

			IRpcCaller rpc = null;
			if (_rpc.TryGetValue(name, out rpc) )
			{
				rpc.Invoke( args ); 
			}
			else
			{
				EB.Debug.LogError("Failed to call RPC : " + name);
			}
			
			Pop();

		}
		
		static void CallViewRPC( uint ownerId, uint nextId, string name, ViewId Id, object[] args )
		{
			View view;
			if (!_viewMap.TryGetValue( Id, out view))
			{
				EB.Debug.LogError("Failed to call RPC : " + name + " on view " + Id);
				return;
			}

			Push( ownerId, nextId, MaxNetworkViewsPerObject, false);
			
			var observered = view.observed;
			if ( observered != null )
			{
				try 
				{
					observered.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, observered, args);
                }
				catch (System.MissingMethodException)
				{
					EB.Debug.LogWarning("Missing method " + name + " on " + observered.name); 
				}
				catch(System.Exception e)
				{
					EB.Debug.LogError("Exception: on " + observered.name + " " + e.ToString());
				}
			}	
			
			Pop();
		}
		
		public static void SerializeVarAgs( BitStream bs, ref object[] args )
		{
			byte type = 0;
			if ( bs.isReading )
			{
				byte size = 0; 
				bs.Serialize(ref size);
				//EB.Debug.Log("size: " + size);
				args = new object[size];
				for ( int i = 0; i < size; ++i )
				{
					bs.Serialize(ref type);
					switch(type)
					{
					case (byte)VarArgsType.Null:
						{
							args[i] = null;
						}
						break;
					case (byte)VarArgsType.Boolean:
						{
							bool v = false;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.Byte:
						{
							byte v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;
					case (byte)VarArgsType.Short:
						{
							short v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.UShort:
						{
							ushort v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.Int:
						{
							int v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;		
					case (byte)VarArgsType.UInt:
						{
							uint v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;
					case (byte)VarArgsType.Long:
						{
							long v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;		
					case (byte)VarArgsType.ULong:
						{
							ulong v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;
					case (byte)VarArgsType.Single:
						{
							float v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.Double:
						{
							double v = 0;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.String:
						{
							string v = string.Empty;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.Object:
						{
							int v = 0;
							bs.Serialize( ref v );
							args[i] = GetObjectFromHashId(v);
						}
						break;
					case (byte)VarArgsType.Vector2:
						{
							Vector2 v = Vector2.zero;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;		
					case (byte)VarArgsType.Vector3:
						{
							Vector3 v = Vector3.zero;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;
					case (byte)VarArgsType.Vector4:
						{
							Vector4 v = Vector4.zero;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
					case (byte)VarArgsType.Quaternion:
						{
							Quaternion v = Quaternion.identity;
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;
					case (byte)VarArgsType.ViewId:
						{
							ViewId v = new ViewId();
							bs.Serialize(ref v);
							args[i] = v;
						}
						break;	
#if USE_FARSEER
					case (byte)VarArgsType.FVector2:
						{
							FVector2 v = default(FVector2);							
							bs.Serialize(ref v.X);
							bs.Serialize(ref v.Y);
							args[i] = v;
						}
						break;
#endif
					default:
						System.Type t;
						if (_serializablesIn.TryGetValue(type, out t))
						{
							var v = (ISerializable)System.Activator.CreateInstance(t);
							v.Serialize(bs);
							args[i] = v;
						}
						else
						{
							throw new System.ArgumentException("Unknown var arg type: " + type);	
						}
						break;
						
						
					}
				}
			}
			else
			{
				if ( args.Length > byte.MaxValue )
				{
					throw new System.ArgumentException("Too many args!");
				}
				
				byte size = (byte)args.Length;
				bs.Serialize( ref size);
				for ( int i = 0; i < size; ++i )
				{
					var arg = args[i];
					if ( arg == null )
					{
						type = (byte)VarArgsType.Null;
						bs.Serialize( ref type );
					}
					else if ( arg is bool )
					{
						var v = (bool)arg;
						type = (byte)VarArgsType.Boolean;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is byte )
					{
						var v = (byte)arg;
						type = (byte)VarArgsType.Byte;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is ushort )
					{
						var v = (ushort)arg;
						type = (byte)VarArgsType.UShort;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is short )
					{
						var v = (short)arg;
						type = (byte)VarArgsType.Short;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is int )
					{
						var v = (int)arg;
						type = (byte)VarArgsType.Int;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is uint )
					{
						var v = (uint)arg;
						type = (byte)VarArgsType.UInt;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is long )
					{
						var v = (long)arg;
						type = (byte)VarArgsType.Long;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is ulong )
					{
						var v = (ulong)arg;
						type = (byte)VarArgsType.ULong;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is float )
					{
						var v = (float)arg;
						type = (byte)VarArgsType.Single;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is double )
					{
						var v = (double)arg;
						type = (byte)VarArgsType.Double;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is string )
					{
						var v = (string)arg;
						type = (byte)VarArgsType.String;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is Object )
					{
						var v = Hash.StringHash( ((Object)arg).name );
						if (GetObjectFromHashId(v) == null)
						{
							EB.Debug.LogError("Cant serialize object " + ((Object)arg).name + " Because is hasnt been register" );
						}
						
						type = (byte)VarArgsType.Object;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is Vector2 )
					{
						var v = (Vector2)arg;
						type = (byte)VarArgsType.Vector2;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is Vector3 )
					{
						var v = (Vector3)arg;
						type = (byte)VarArgsType.Vector3;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is Vector4 )
					{
						var v = (Vector4)arg;
						type = (byte)VarArgsType.Vector4;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is Quaternion )
					{
						var v = (Quaternion)arg;
						type = (byte)VarArgsType.Quaternion;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
					else if ( arg is ViewId )
					{
						var v = (ViewId)arg;
						type  = (byte)VarArgsType.ViewId;
						bs.Serialize( ref type );
						bs.Serialize( ref v);
					}
#if USE_FARSEER
					else if ( arg is FVector2 )
					{
						var v = (FVector2)arg;
						type = (byte)VarArgsType.FVector2;
						bs.Serialize( ref type );
						bs.Serialize( ref v.X);
						bs.Serialize( ref v.Y);
					}
#endif				
					else if ( arg is ISerializable )
					{
						var v = (ISerializable)arg;
						if (!_serializablesOut.TryGetValue(v.GetType(), out type))
						{
							throw new System.ArgumentException("Unregister serializable type " + v.GetType() );
						}
						bs.Serialize( ref type );
						v.Serialize(bs);
					}
					else
					{
						throw new System.ArgumentException("Unknown var arg type: " + arg.GetType() );
					}
				}
			}
		}
		
		static Object GetObjectFromHashId( int hashId )
		{
			Object obj;
			if (_objMap.TryGetValue(hashId, out obj))
			{
				return obj;
			}
			EB.Debug.LogError("Cant find object for hash " + hashId);
			return null;
		}
		
		public static void LoadLevel( string name, bool additive = false, bool force = false )
		{
			var nextId = _localN;
			_localN   += MaxNetworkViewsPerScene;
			RPC(LoadLevelRPC,RPCMode.All,_localP,nextId,name,additive,force);
		}
		
		public static void Destroy( GameObject go )
		{
			if ( go != null )
			{
				var view = go.GetComponent<View>();
				if ( view)
				{
					RPC(DestroyRPC, RPCMode.All, view.viewId.p, view.viewId.n );
				}
				else
				{
					EB.Debug.LogError("Can't call destroy on a object that doesn't have a network view");
				}
			}
		}
		
		public static Object Instantiate( Object prefab, Vector3 position, Quaternion rotation )
		{
			var nextId = _localN;
			_localN   += MaxNetworkViewsPerObject;
			
			if (GetObjectFromHashId( Hash.StringHash(prefab.name) )==null)  
			{
				EB.Debug.LogError("Instantiate: Prefab not registered " + prefab.name);
				return null;
			}
			
			RPC(InstantiateRPC,RPCMode.Others,_localP, nextId, prefab, position, rotation);
			return _Instantiate(_localP, nextId, prefab, position, rotation); 
		}
		
		public static Object InstantiateAs( uint connId, Object prefab, Vector3 position, Quaternion rotation )
		{
			var nextId = _localN;
			_localN   += MaxNetworkViewsPerObject;
			
			RPC(InstantiateRPC,RPCMode.Others,connId, nextId, prefab, position, rotation);
			return _Instantiate(connId, nextId, prefab, position, rotation); 
		}
		
		static ViewId AllocateViewId()
		{
		//	Debug.LogWarning("AllocateViewId " + _allocations.Count);
			if (_allocations.Count == 0)
			{
				throw new System.Exception("Failed to allocate view Id!");
			}
			
			return _allocations.Peek().Allocate();
		}
		
		static void Push( uint p, uint n, uint m, bool s)
		{
			_allocations.Push( new AllocationInfo(p,n,m,s) ); 
		//	Debug.Log("Push: " + _allocations.Peek().ToString() );
		}
		
		static void Pop()
		{ 
			//Debug.Log("Pop:" + _allocations.Peek().ToString() );
			_allocations.Pop();
		}
		
		static Object _Instantiate( uint ownerId, uint nextId, Object prefab, Vector3 position, Quaternion rotation)
		{
		//	EB.Debug.Log("_Instantiate {0}, {1}, {2}", ownerId, nextId, prefab ); 
			Push(ownerId, nextId, MaxNetworkViewsPerObject, false);
			
			Object r = null;
			if (prefab != null)
			{
				// If a specific InstantiateMethod is provided, use it. Otherwise, fall back to Object.Instantiate
				if( InstantiateMethod != null )
				{
					r = InstantiateMethod( prefab, position, rotation );
				}
				else
				{
					r = Object.Instantiate( prefab, position, rotation );
				}

				// Unity doesn't guarantee the right order for awakes to be called
				if (!IsLocalGame)
				{
					if (r is ReplicationView)
					{
						ReplicationView view = r as ReplicationView;
						view.AllocateId();
						view.gameObject.SendMessage("OnViewIdAllocated", view, SendMessageOptions.DontRequireReceiver);
					}
					else if (r is GameObject)
					{
						ReplicationView[] views = (r as GameObject).GetComponentsInChildren<ReplicationView>();
						for (int i = 0; i < views.Length; i++)
						{
							views[i].AllocateId();
							views[i].gameObject.SendMessage("OnViewIdAllocated", views[i], SendMessageOptions.DontRequireReceiver);
						}
					}
				}
			}
			else
			{
				EB.Debug.LogError("Network instantiate prefab is null! ");
			}
			Pop();
			return r; 
		}
		
		static void _InstantiateRPC( uint ownerId, uint nextId, Object prefab, Vector3 position, Quaternion rotation)
		{
			_Instantiate(ownerId,nextId,prefab,position,rotation);
		}
			
		static void _LoadLevelRPC( uint ownerId, uint nextId, string name, bool additive, bool force )
		{
			Coroutines.Run(_LoadLevelCoroutine(ownerId,nextId,name,additive,force));
		}
		
		static IEnumerator _LoadLevelCoroutine( uint ownerId, uint nextId, string name, bool additive, bool force )
		{
			//EB.Debug.Log("_LoadLevelRPC {0}, {1}, {2}", ownerId, nextId, name ); 
			
			IsLoadingLevel = true;
			
			// pause any view updates
			_pauseViewUpdates = true;
			
			yield return 1;
			
			Push(ownerId, nextId, MaxNetworkViewsPerScene, true);
			
			if( additive == true )
			{
				yield return Assets.LoadLevelAdditive( name );
			}
			else if( force == true )
			{
				yield return Assets.ForceLoadLevel( name );
			}
			else
			{
				yield return Assets.LoadLevel( name );
			}
		
			// shitty but need to be sure all the awakes are called
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();

			// Unity doesn't guarantee the right order for awakes to be called
			if (!IsLocalGame)
			{
				ReplicationView[] views = Object.FindObjectsOfType(typeof(ReplicationView)) as ReplicationView[];
				for (int i = 0; i < views.Length; i++)
				{
					views[i].AllocateId();
					views[i].gameObject.SendMessage("OnViewIdAllocated", views[i], SendMessageOptions.DontRequireReceiver);
				}
			}
			
			// need to clear the scene flags now
			_allocations.Peek().ClearSceneFlag();
		
			// tell the everyone that we are done loading
			RPC(LoadLevelCompleteRPC,RPCMode.All, _localP ); 
			
			// wait for everyone to be loaded
			while (true)
			{
				bool allLoaded = true;
				if ( _game != null && !_game.Network.IsLocal )
				{
					foreach( var player in _game.Players )
					{
						if ( _doneLoading.ContainsKey(player.PlayerId) == false )
						{
							allLoaded = false;
						}
					}
				}
				if ( _game == null )
				{
					// game's gone, we probably disconnected?
					Debug.LogWarning("Game is null, aborting wait for level load");
					break;
				}
				if ( allLoaded )
				{
					break;
				}
				
				yield return 1;
			}
			
			yield return new WaitForFixedUpdate();
			
			_doneLoading.Clear();
			
			// tell everyone to go
			if (IsHost || _game == null)
			{
				RPC(BroadcastRPC, RPCMode.All, "OnNetworkLoadedLevel");
			}
			
			Pop();
						
			IsLoadingLevel = false;
			
			_pauseViewUpdates = false;
		}
		
		static void _BroadcastMessageRPC( string name )
		{
			foreach (GameObject go in Object.FindObjectsOfType(typeof(GameObject)))
			{
				go.SendMessage(name,SendMessageOptions.DontRequireReceiver);	
			}
			
		}
		
		static  void _LoadLevelCompleteRPC( uint playerId )
		{
			//EB.Debug.Log("Player " + playerId + " is done loading" );
			_doneLoading[playerId] = true;			
		}
		
	}
}

// global shortcuts
public class Replication : EB.Replication.Manager
{
}

