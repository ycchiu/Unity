using UnityEngine;
using System.Collections;
using System.Reflection;

namespace EB.Replication
{	
	public enum NetworkMigrationSynchronization
	{
		Off,		// no migration,
		Destroy, 	// destroy on migration
		Host,		// migrate to the host
	}
	
	[AddComponentMenu("Replication/View")]
	public class View : MonoBehaviour
	{
		public Component 							observed;
		public NetworkStateSynchronization 			stateSynchronization = NetworkStateSynchronization.Off;
		public NetworkMigrationSynchronization 		migrationSynchronization = NetworkMigrationSynchronization.Off;
		public float 								updateInterval = 0.0f;	// 0 for whenever the replication manager updates, and > 0 for custom
		
		public ViewId 		viewId 			{get;private set;}
		public uint			instantiatorId 	{get;private set;}
		
		// just a place to store the last hash of the serialze view for delta compression
		public int 			lastHash		{get;set;}
		
		public bool			isMine
		{
			get
			{
				return Manager.IsMine(viewId);
			}
		}
		
		public uint			ownerId
		{
			get
			{
				return viewId.p;
			}
		}
		
		public Sparx.Player	ownerPlayer
		{
			get
			{
				return Manager.GetPlayer(viewId.p);
			}
		}
		
		public Sparx.Player	instantiatorPlayer
		{
			get
			{
				return Manager.GetPlayer(instantiatorId);
			}
		}
		
		
		private float _lastUpdate  = 0.0f;
		
		public void AllocateId()
		{
			// TODO: Temporarily wrap this in a try-catch block to prevent the game from crashing when a Replication View is used outside a Sparx game. --NWF
			try
			{
				viewId = Manager.Register(this);
			}
			catch (System.Exception)
			{
				Debug.LogWarning("Could not register replication view. Do you have a Sparx game?");
			}

			instantiatorId = viewId.p;
			
			//Debug.Log("Replicated View: "  + viewId + " " + name);
		}
		
		void OnDestroy()
		{
			Manager.Unregister(this);
		}
		
		enum TransformFlags
		{
			P_X = 1 << 0,
			P_Y = 1 << 1,
			P_Z = 1 << 2,
			R_X	= 1 << 3,
			R_Y = 1 << 4,
			R_Z	= 1 << 5,
		}
		
		public void TransferOwnership() 
		{
			Manager.Unregister(this);
			viewId = Manager.Register(this);
			
			// notify
			if ( observed != null )
			{
				observed.SendMessage("OnTransferedOwnership", SendMessageOptions.DontRequireReceiver);
			}
		}
		
		public void RPC( string name, RPCMode mode, params object[] args ) 
		{
			// call a rpc on this object
			if ( observed != null )
			{
				var type = observed.GetType();
				if ( type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) != null )
				{
					Manager.ViewRPC( name, viewId, mode, args ); 		
				}
				else
				{
					EB.Debug.LogWarning("skipping rpc ("+name+") on type  " + observed.GetType() );
				}
			}
		}
		
		public void Serialize( BitStream bs )
		{
			if ( observed == null )
			{
				throw new System.Exception("View Is in a invalid state " + viewId );
			}
			
			if ( bs.isWriting )
			{
				if (updateInterval > 0 && (Time.realtimeSinceStartup - _lastUpdate) < updateInterval ) 
				{
					return;
				}
				_lastUpdate = Time.realtimeSinceStartup;
			}
			
			if ( observed is Transform )
			{
				var transform   = (Transform)observed;
				
				if ( bs.isWriting )
				{
					var position= transform.localPosition;
					var rotation= transform.localRotation.eulerAngles;
					
					int index = bs.Reserve();
					int flags = 0;
					if ( position.x != 0.0f )
					{
						flags |= (int)TransformFlags.P_X;
						bs.Serialize( ref position.x );
					}
					
					if ( position.y != 0.0f )
					{
						flags |= (int)TransformFlags.P_Y;
						bs.Serialize( ref position.y );
					}
					
					if ( position.z != 0.0f )
					{
						flags |= (int)TransformFlags.P_Z;
						bs.Serialize( ref position.z );
					}
					
					if ( rotation.x != 0.0f )
					{
						flags |= (int)TransformFlags.R_X;
						bs.Serialize( ref rotation.x );
					}
					
					if ( rotation.y != 0.0f )
					{
						flags |= (int)TransformFlags.R_Y;
						bs.Serialize( ref rotation.y );
					}
					
					if ( rotation.z != 0.0f )
					{
						flags |= (int)TransformFlags.R_Z;
						bs.Serialize( ref rotation.z );
					}
					
					bs.Poke( index, (byte)flags ); 
					
				}
				else
				{
					byte flags = 0;
					bs.Serialize( ref flags );
					
					var position = Vector3.zero;
					var rotation = Vector3.zero;
					
					if ( (flags & (int)TransformFlags.P_X) != 0 ) bs.Serialize(ref position.x);
					if ( (flags & (int)TransformFlags.P_Y) != 0 ) bs.Serialize(ref position.y);
					if ( (flags & (int)TransformFlags.P_Z) != 0 ) bs.Serialize(ref position.z);
					
					if ( (flags & (int)TransformFlags.R_X) != 0 ) bs.Serialize(ref rotation.x);
					if ( (flags & (int)TransformFlags.R_Y) != 0 ) bs.Serialize(ref rotation.y);
					if ( (flags & (int)TransformFlags.R_Z) != 0 ) bs.Serialize(ref rotation.z);
					
					transform.localPosition = position;
					transform.localRotation = Quaternion.Euler(rotation);
				}
			}
			else if ( observed is MonoBehaviour )
			{
				var behaviour = (MonoBehaviour)observed;
				behaviour.GetType().InvokeMember("OnSerializeView", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, behaviour, new object[]{ bs } );
			}
			else if( observed is Rigidbody )
			{
				var pos = transform.position;
				var rot = transform.rotation;
				var vel = rigidbody.velocity;
				var angularVel = rigidbody.angularVelocity;
				
				bs.Serialize(ref pos.x);
				bs.Serialize(ref pos.y);
				bs.Serialize(ref pos.z);
				bs.Serialize(ref vel.x);
				bs.Serialize(ref vel.y);
				bs.Serialize(ref vel.z);
				
				if ((rigidbody.constraints & RigidbodyConstraints.FreezeRotationX) != 0)
				{
					bs.Serialize(ref rot.x);
					bs.Serialize(ref angularVel.x);
				}
					
				if ((rigidbody.constraints &  RigidbodyConstraints.FreezeRotationY) != 0)
				{
					bs.Serialize(ref rot.y);
					bs.Serialize(ref angularVel.y);
				}
					
				if ((rigidbody.constraints &  RigidbodyConstraints.FreezeRotationZ) != 0)
				{
					bs.Serialize(ref rot.z);
					bs.Serialize(ref angularVel.z);
				}
				
				if (bs.isReading)
				{
					transform.position = pos;
					transform.rotation = rot;
					rigidbody.velocity = vel;
					rigidbody.angularVelocity = angularVel;
				}
			}
			else
			{
				throw new System.Exception("Unsupported observed type: " + observed.GetType() ) ;
			}
			
		}
	}
}

// just to keep unity happy
public sealed class ReplicationView : EB.Replication.View {
	
}