using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using EB.Sequence.Serialization;

namespace EB.Sequence.Editor
{
	public partial class CustomEditor
	{
		public static LinkInfo[] GetCustomLinks_SequenceAction_Director_VariableIn(Node node)
	    {
	        List<LinkInfo> items = new List<LinkInfo>();
			
			var go = node.GetProperty("DirectorData").gameObjectValue;
			var director = go != null ? go.GetComponent<EB.Director.Component>() : null;
			if ( director != null )
			{
				foreach( var group in director.Groups )
				{
					if ( EB.Director.Utils.HasGroupInput(group.type) )
					{
						LinkInfo info = new LinkInfo( string.Format("Groups[{0}]", items.Count), group.name );
						items.Add(info);
					}
				}
			}
			
			return items.ToArray();
	    }
		
		public static LinkInfo[] GetCustomLinks_SequenceAction_Director_Output(Node node)
	    {
	        List<LinkInfo> items = new List<LinkInfo>();
			
			var go = node.GetProperty("DirectorData").gameObjectValue;
			var director = go != null ? go.GetComponent<EB.Director.Component>() : null;
			if ( director != null )
			{
				var events = director.GetEvents();			
				foreach( var key in events )
				{
					LinkInfo info = new LinkInfo( string.Format("Events[{0}]", items.Count), key );
					items.Add(info);
				}
			}
			
			return items.ToArray();
	    }
		
	}
}


