//#define FUSE_DEBUG

using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace EB.Sequence.Runtime
{
	public struct LinkFieldInfo
	{
		public string field;
		public int index;
	};
	
	public class Exception : System.Exception
	{
		public Node Node {get;set;}
		
		public Exception( Node node, string message ) : base(message)
		{
			this.Node = node;
		}
	}

	public class Node : System.IDisposable
	{
        public static string kAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public Component Parent {get;set;}
		public int Id { get;set; }
	
		public virtual void Init()
		{
			
		}
		
		public virtual void Dispose ()
		{
			
		}
	
		public virtual void OnInputChanged()
		{
		}
		
		public virtual void GlobalValueChanged()
		{
			
		}
		
		public virtual void Serialize( EB.BitStream bs )
		{
			
		}
	}
	
	public class Trigger
	{
		public event EB.Action trigger;
		
		public bool IsValid()
		{
			if (trigger != null )
				return true;
			
			return false;
		}
		
		public void Invoke()
		{
			if ( trigger != null )
			{
				trigger();
			}
		}
	}
	
	public class Variable : Node
	{
		public bool IsNull { get { return this.Id == 0; } }
		
		public virtual object Value { get {return null;} set {} }	
		
		public static Variable Null = new Variable();
		public const string ValueLinkName = "Value";
		
		public T GetValue<T>() 
		{
			var v = Value;
			if ( v != null && (v is T) )
			{
				return (T)v;
			}
			return default(T);
		}
		
		public override void GlobalValueChanged()
		{
			ValueChanged();	
		}
		
		public void ValueChanged()
		{
			Parent.VariableChanged(this);
		}
	}
	
	public abstract class Operation : Node 
	{
		// todo: add active set to parent when activated
		public bool IsActive {get;set;}

		[EB.Sequence.Property]
		public string PrintMessage = "";

		public virtual bool Activate()
		{
			if ( !IsActive )
			{
				DebugPrint ("OnActivate: ");
#if FUSE_DEBUG
				UnityEngine.EB.Debug.Log("Activating Node Id:"+this.Id+" ("+this.GetType().Name+") Sequence:"+ this.Parent.name);
#endif
				IsActive = true;
				Parent.AddToUpdate(this);
				return true;
			}
			return false;
		}
		
		public virtual bool Update()
		{ 
			return false; // return false when done
		}

		public virtual void DebugPrint(string prefix = "")
		{
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(PrintMessage))
			{
				string output = string.Format("Sequence Node (ID{0}) Output ({1}).", Id, prefix + PrintMessage);
				EB.Debug.Log (output);
			}
#endif
		}
	}
	
	public class EventList : ArrayList
	{
		private bool _dirty = false;
		
		class EventCompare : System.Collections.IComparer
		{
			public int Compare (object e1, object e2)
			{
				return ((Event)e2).Priority - ((Event)e1).Priority;
			}
		}
		
		private static EventCompare _compare = new EventCompare();	
		
		public void Add( Event e )
		{
			_dirty = true;
			base.Add(e);
		}
		
		public void Remove( Event e )
		{
			base.Remove(e);
		}
		
		public void Check()
		{
			if ( _dirty )
			{
				_dirty = false;
				base.Sort(_compare);
			}
		}
	}
	
	public abstract class Event : Operation
	{
		private static Hashtable _active = new Hashtable();
		
		[EB.Sequence.Property]
		public int Priority = 0;

		[EB.Sequence.Property]
		public bool AllowPropagation = true;

		[EB.Sequence.Trigger]
		public Trigger Out = new Trigger();
		
		public virtual bool CheckActivate( UnityEngine.GameObject instigator, object target )
		{
			return true;
		}
		
		public override bool Update ()
		{
#if FUSE_DEBUG
			UnityEngine.EB.Debug.Log("Invoking Event: " + GetType().Name );
#endif
			Out.Invoke();
			return false;
		}
		
		public override void Init ()
		{
			base.Init ();
			
			EventList list = (EventList)_active[GetType()];
			if(list == null)
			{
				list = new EventList();
				_active[GetType()] = list;
			}
			
			list.Add(this);
			//_active.Add(this);
		}
		
		public override void Dispose ()
		{
			EventList list = (EventList)_active[GetType()];
			list.Remove(this);
			
			base.Dispose();
		}
		
		public static void Activate( UnityEngine.GameObject instigator, object target, System.Type eventType )  
		{
			EventList list = (EventList)_active[eventType];
			if ( list == null )
			{
				return;
			}
			
			list.Check(); 
			
			foreach( Event ev in list)
			{
#if FUSE_DEBUG
			    UnityEngine.EB.Debug.Log(eventType.ToString() + " " + ev.Priority );
#endif
				
				if ( ev.CheckActivate(instigator, target) )
				{
					ev.Activate();
					
					if ( !ev.AllowPropagation )
					{
						return;
					}
				}
			}
		}
	}
	
	public abstract class Action : Operation
	{
	}
	
	public abstract class Condition : Operation
	{	
		[EB.Sequence.Property]
		public bool AutoCheck = false;
		
		public override void OnInputChanged()
		{
			// active us on input changed
			if ( AutoCheck )
			{
				this.Activate();
			}
		}
		
		public override void Init ()
		{
			base.Init ();
			if ( AutoCheck )
			{
				this.Activate();
			}
		}
		
		[Entry]
		public void Check()
		{
			this.Activate();
		}
	}
}
