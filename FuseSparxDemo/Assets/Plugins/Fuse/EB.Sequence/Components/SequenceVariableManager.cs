using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EB.Sequence.Runtime;

namespace EB.Sequence
{
	public class VariableManager : MonoBehaviour
	{
		public static VariableManager Instance {get;private set; }
		
		private Hashtable _values = new Hashtable();
		private Hashtable _listeners = new Hashtable();
		
		public void AddNode( Node node, string name )
		{
			ArrayList list = (ArrayList) _listeners[name];
			if ( list == null )
			{
				list = new ArrayList();
				_listeners[name] = list;
			}
			list.Remove(node);
			list.Add(node);
		}
		
		public void RmvNode( Node node, string name )
		{
			ArrayList list = (ArrayList) _listeners[name];
			if ( list != null )
			{
				list.Remove(node);
			}
		}
		
		public void Awake()
		{
			Instance = this;			
		}
		
		public double GetNumber( string name, double defaultV ) 
		{
			object value = GetVariable(name);
			if ( value != null )
			{
				double tmp;
				if ( double.TryParse(value.ToString(), out tmp ) )
				{
					return tmp;
				}
			}
			SetVariable(name, defaultV);
			return defaultV;
		}
		
		public string GetString( string name, string defaultV )
		{
			object value = GetVariable( name  );
			if ( value != null )
			{
				return value.ToString();
			}
			SetVariable(name, defaultV);
			return defaultV;
		}
		
		public virtual object GetVariable( string name )
		{
			return _values[name];
		} 
		
		public void RemoveVariable( string name )
		{
			_values.Remove(name);
		}
		
		public void SetVariable( string name, object value )
		{
			_values[name] = value;
			
			// trigger value changed listeners
			ArrayList list = (ArrayList) _listeners[name];
			if ( list != null )
			{
				foreach( Node node in list )
				{
					node.GlobalValueChanged();
				}
			}	
		}
		
		public void Save (Hashtable data)
		{			
			foreach( DictionaryEntry entry in _values )
			{
				data[entry.Key] = entry.Value;
			}
		}
	
		public void Load (Hashtable data)
		{
			_values = new Hashtable(data);
		}
	}	
}

// for unity
public class SequenceVariableManager : EB.Sequence.VariableManager
{
	
}


