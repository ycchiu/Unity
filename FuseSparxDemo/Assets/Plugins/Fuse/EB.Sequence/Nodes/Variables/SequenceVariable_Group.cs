using EB.Sequence;
using EB.Sequence.Runtime;
using System.Collections.Generic;
using System.Collections;

[MenuItem(Path="Variables/Group", VariableType=typeof(UnityEngine.GameObject))]
public class SequenceVariable_Group : Variable
{
	private List<object> _items = new List<object>();
	
	public override object Value 
	{
		get 
		{
			return _items.ToArray();
		}
		set 
		{
			if ( value != null )
			{
				if ( value is IEnumerable )
				{
					IEnumerable enumerable = (IEnumerable)value;
					foreach( object obj in enumerable )
						_items.Add( obj );
				}
				else
				{
					_items.Add( value );
				}
			}
			else
			{
				_items.Clear();
			}
		}
	}
}
