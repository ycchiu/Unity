//#define FUSE_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EB.Sequence
{
	public class Component : MonoBehaviour
	{
		// serialization
		[UnityEngine.HideInInspector]
		public int NextId = 1;
		
		//[UnityEngine.HideInInspector]
		public List<Serialization.Node> Nodes = new List<Serialization.Node>();
		
		//[UnityEngine.HideInInspector]
		public List<Serialization.Link> Links = new List<Serialization.Link>();
		
		//[UnityEngine.HideInInspector]
		public List<Serialization.Group> Groups = new List<Serialization.Group>();
		
		public bool _dontDestroyOnLoad = true;
	
		public Serialization.Node FindById( int id )
		{
			foreach( var obj in Nodes )
			{
				if ( obj.id == id )
				{
					return obj;
				}
			}
			return null;
		}
		
		public void Clear()
		{
			Nodes.Clear();
			Links.Clear();
			NextId = 1;
		}
		
		public Serialization.Node AddNode( System.Type type, bool addToNodes = true )
		{
			var node = Utils.CreateSerializationNode(type, NextId++);
			if( addToNodes == true )
			{
				Nodes.Add(node);
			}
			return node;
		}
		
		public Serialization.Node AddNode( Serialization.Node node )
		{
			node.id = NextId++;
			Nodes.Add(node);
			return node;
		}
		
		private class LinkFinder
		{
			private int _id; 
			
			public LinkFinder(int id)
			{
				_id = id;	
			}
			
			public bool Find( Serialization.Link link )
			{
				return link.inId == _id || link.outId == _id;
			}
		}
		
		public void RemoveNode( int nodeId )
		{
			Serialization.Node node = FindById(nodeId);
			if ( node != null )
			{
				Nodes.Remove(node);
				
				// remove all links
				Links.RemoveAll( new LinkFinder(nodeId).Find );
			}
		}
		
		public void OnDestroy()
		{
#if FUSE_DEBUG
			EB.Debug.Log("SEQUENCE "+name+" DESTROYED");

			if ( _allNodes.Count>0)
			{
				EB.Debug.LogWarning("OnDestroy SEQUENCE - KILLING AN ACTIVE SEQUENCE - NEED TO FIX: " + name);
			}
#endif			
			EndSequence();
		}
		
		public Serialization.Link AddLink( EB.Sequence.Serialization.Node outNode, string outName, EB.Sequence.Serialization.Node inNode, string inName )
		{
			if ( Utils.ValidateLink(outNode, outName, inNode, inName ) == Utils.ValidateLinkResult.Ok )
			{
				return AddLink( outNode.id, outName, inNode.id, inName );	
			}
			return null;
		}
		
		private Serialization.Link AddLink( int outId, string outName, int inId, string inName )
		{
			// Is there already a link here?
			if (FindLink(outId,outName,inId,inName)!=null)
				return null;
			
			var link = new Serialization.Link();
			link.outId = outId;
			link.outName = outName;
			link.inId = inId;
			link.inName = inName;
			Links.Add(link);
			return link;
		}
		
		public Serialization.Link FindLink( int outId, string outName, int inId, string inName )
		{
			foreach ( var link in Links )
			{
				if ( link.outId == outId && link.inId == inId && link.outName == outName && link.inName == inName )
				{
					return link;
				}
			}
			
			return null;
		}
		
		public void RmvLink( Serialization.Link link )
		{
			Links.Remove(link);
		}
		
		// runtime
		protected List<Runtime.Operation> _activeOperations = new List<Runtime.Operation>();
		protected Dictionary<int,Runtime.Node>	_allNodes = new Dictionary<int,Runtime.Node>();
				
		private static string GoName( GameObject go )
		{
			return go != null ? go.name : "null";
		}
		
		public static void Activate( GameObject instigator, object target, System.Type eventType )
		{			
			Runtime.Event.Activate(instigator,target,eventType);	
		}
		
		
		public void AddToUpdate( Runtime.Operation node )
		{
			if ( _activeOperations.Contains(node) == false )
			{
				_activeOperations.Add(node);
			}
		}
		
		public void VariableChanged( Runtime.Variable variable )
		{
#if FUSE_DEBUG
			EB.Debug.Log("VariableChanged:" + variable.Id );
#endif
			foreach( var link in this.Links ) 
			{
				if ( link.outId == variable.Id )
				{
					var node = GetNode(link.inId);
					if ( node != null )
					{
						node.OnInputChanged();
					}
				}
			}
		}
		
		public Runtime.Node GetNode( int id )
		{
			Runtime.Node node;
			if ( _allNodes.TryGetValue( id, out node ) )  
			{
				return node;
			}
			return null;
		}
		
		private List<Runtime.Operation> _removeList = new List<Runtime.Operation>();
		
		protected void Update()
		{
			if ( _activeOperations.Count > 0 )
			{
				for ( int i = 0; i < _activeOperations.Count; ++i )
				{
					bool result = false;
					
					Runtime.Operation operation = _activeOperations[i];
					
					//EB.Debug.Log("Updating Node: " + operation.Id );
				
#if CATCH_EXCEPTIONS					
					try
					{
						//EB.Debug.Log("Updating Node: " + operation.Id );
						result = operation.Update();
					}
					catch ( System.Reflection.TargetInvocationException e )
					{
						EB.Debug.LogError( e.InnerException.ToString() );
					}
					catch ( Runtime.Exception e )
					{
						EB.Debug.LogError( e.ToString() ); 
					}
					catch (System.Exception e )
					{
						EB.Debug.LogError( e.ToString() );
					}
#else
					result = operation.Update();
#endif
					
					if (result == false)
					{
						_removeList.Add(operation);
					}
				}
				
				// defer removals here, otherwise we could get into a infinite loop (and we have)
				if ( _removeList.Count > 0 )
				{
					foreach( Runtime.Operation op in _removeList )
					{
						op.IsActive = false;
						_activeOperations.Remove(op);
					}
					_removeList.Clear();
				}
				
			}
			
			if ( _kill )
			{
				EndSequence();
				DestroyMe();
			}
			
		}
		
		private bool _kill = false;
		
		protected virtual void DestroyMe()
		{
			Destroy(gameObject);
		}
		
		public void KillSequence()
		{
            if (_kill == false)
            {
                _kill = true;
                Activate(gameObject, gameObject, typeof(SequenceEvent_SequenceKilled));
            }
		}
		
		private static int _numNodes = 0;
		
		protected void AllocateNodes()
		{
			if ( _allNodes.Count>0)
			{
				return;
			}	
			
			// rebuild all the nodes
			foreach( Serialization.Node node in Nodes )
			{
				_allNodes[node.id] = Utils.CreateRuntimeNode(node,this);
			}
			_numNodes += _allNodes.Count;
			
#if FUSE_DEBUG
			EB.Debug.Log("Sequence Nodes: " + _numNodes);
#endif			
		}
				
		public virtual void StartSequence()
		{
			if(_dontDestroyOnLoad)
			{
				DontDestroyOnLoad(gameObject);
			}
			
			AllocateNodes();	
			
			LinkNodes();
			
			ActivateVariables();
			
			Activate(gameObject, gameObject, typeof(SequenceEvent_SequenceStarted));
		}
		
		protected void ActivateVariables()
		{
			foreach( var node in _allNodes.Values ) 
			{
				if ( node is Runtime.Variable )
				{
					VariableChanged( (Runtime.Variable)node ); 
				}
			}
		}
		
		protected void LinkNodes()
		{
			// connect everything
			foreach( Serialization.Link link in Links )
			{
				// determine the link type
				Runtime.Node inNode = GetNode(link.inId);
				Runtime.Node outNode= GetNode(link.outId);
				
				if ( inNode == null )
				{
					EB.Debug.LogError("Invalid Link: inNode is null " + link.inId + " " + this.name );
					continue;
				}
				
				if ( outNode == null )
				{
					EB.Debug.LogError("Invalid Link: outNode is null " + link.outId + " " + this.name );
					continue;
				}
				
				var inFieldInfo = Utils.ParseField(link.inName);
				var outFieldInfo = Utils.ParseField(link.outName);
				
				if ( outNode is Runtime.Variable && (link.outName == Runtime.Variable.ValueLinkName || link.outName == string.Empty) )
				{
					// variable link
					FieldInfo field = inNode.GetType().GetField(inFieldInfo.field);
					if ( field != null )
					{
						if ( field.FieldType.IsArray )
						{
							// add to the array
							List<Runtime.Variable> items = new List<Runtime.Variable>();
							Runtime.Variable[] current = (Runtime.Variable[])field.GetValue(inNode);
							if ( current != null ) 
							{
								foreach( var c in current )
								{
									items.Add(c);
								}
							}
							
							if ( inFieldInfo.index == -1 )
							{
								items.Add( (Runtime.Variable)outNode);	
							}
							else
							{
								// resize
								while (items.Count <= inFieldInfo.index )
								{
									items.Add( Runtime.Variable.Null );
								}
								items[inFieldInfo.index] = (Runtime.Variable)outNode;
							}
							
							field.SetValue(inNode, items.ToArray() );
						}
						else
						{
							field.SetValue(inNode, outNode); 
						}
					}
					else
					{
						EB.Debug.LogWarning("Failed to find variable link: " + link.inName );
					}
				}
				else
				{
					// regular link
					FieldInfo field = outNode.GetType().GetField(outFieldInfo.field);
					if ( field != null )
					{
						Runtime.Trigger trigger = null;
						
						if ( field.FieldType.IsArray )
						{
							// add to the array
							List<Runtime.Trigger> items = new List<Runtime.Trigger>();
							Runtime.Trigger[] current = (Runtime.Trigger[])field.GetValue(outNode);
							if ( current != null )
							{
								foreach( var c in current )
								{
									items.Add(c);
								}
							}
							
							if ( outFieldInfo.index == -1 )
							{
								trigger = new Runtime.Trigger();
								items.Add(trigger);	
							}
							else
							{
								// resize
								while (items.Count <= outFieldInfo.index )
								{
									items.Add( new Runtime.Trigger() );
								}
								trigger = items[outFieldInfo.index];
							}
							
							field.SetValue(outNode, items.ToArray() );
						}
						else
						{
							trigger = (Runtime.Trigger)field.GetValue(outNode);
						}
						
						// use create delegate
						var mi = inNode.GetType().GetMethod(link.inName);
						if ( mi != null )
						{
							var action = System.Delegate.CreateDelegate( typeof(EB.Action), inNode, mi, false );
							if ( action != null )
							{
								trigger.trigger += (EB.Action)action;
							}
							else
							{
								EB.Debug.LogError("Failed to create delegate for function {0} on type {1}", link.inName, inNode.GetType());
							}
						}
						else
						{
							EB.Debug.LogError("Failed to get methodInfo for function {0} on type {1}", link.inName, inNode.GetType());
						}
						
					}
					else
					{
						EB.Debug.LogWarning("Failed to find link: " + link.outName );
					}
				}
				
			}			
		}
		
		public virtual void EndSequence()
		{
			_numNodes -= _allNodes.Count;

			// cleanup
			foreach( Runtime.Node node in _allNodes.Values )
			{
				node.Dispose();
			}
			_allNodes.Clear();
			_activeOperations.Clear();
		}
		
		
	}
}
