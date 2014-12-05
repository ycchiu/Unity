using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using EB.Sequence.Serialization;

namespace EB.Sequence.Editor
{
	public partial class Intellisense
	{
		/************************************************************************/
		public static object[] GetValues( Node node, Property property )
		{
			string function = string.Format("GetValues_{0}_{1}", node.runtimeTypeName, property.name );
			
			System.Type thisType = typeof(Intellisense);
			
			var attribute = node.GetPropertyAttribute(property.name);
			if ( attribute != null && !string.IsNullOrEmpty(attribute.Hint) )
			{
				function = "GetValues_" + attribute.Hint;
			}
			
			MethodInfo method = thisType.GetMethod(function);
			if ( method != null )
			{
				return (object[])method.Invoke( null, new object[]{node,property} );
			}
			
			return null;
		}
				
		/************************************************************************/
		public static object[] GetValues_Locator( Node node, Property property )
		{
			var scene = node.FindByHint("Scene");
			string sceneName = scene != null ? scene.stringValue : string.Empty;			

			return ResourceUtils.GetLocatorsFromScene(sceneName);			
		}
		
		/************************************************************************/
		public static object[] GetValues_Scene( Node node, Property property )
		{
			return ResourceUtils.GetSceneList().ToArray();
		}

		/************************************************************************/
		public static object[] GetValues_Quest( Node node, Property property )
		{
			return ResourceUtils.GetQuestList().ToArray();
		}

	}
}