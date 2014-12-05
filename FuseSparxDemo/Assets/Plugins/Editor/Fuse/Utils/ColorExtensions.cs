using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ExtensionMethods 
{
	public static class ColorExtensions
	{
		 private static readonly Dictionary<string, Color> colorNameMapping = typeof( Color ).GetProperties( BindingFlags.Public | BindingFlags.Static)
	                     																		.Where( prop => prop.PropertyType == typeof( Color ) )
	                     																		.ToDictionary( prop => prop.Name.ToLower(), prop => (Color) prop.GetValue( null, null ) );
	
		public static Color FromName( string name )
	    {
	    	Color color = Color.black;
	    	
	    	if( colorNameMapping.TryGetValue( name.ToLower(), out color ) == false )
	    	{
	    		color = Color.black;
	    	}
	    	
	    	return color;
	    }
	}
}
