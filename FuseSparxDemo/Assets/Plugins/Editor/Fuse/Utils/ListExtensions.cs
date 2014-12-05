using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ExtensionMethods 
{
	public static class ListExtensions
	{
	    public static bool AddUnique<T>(this List<T> self, T value)
	    {
	    	bool added = false;
	    	if( self.Contains( value ) == false )
	    	{
	    		self.Add( value );
	    		added = true;
	    	}
	    	return added;
	    }
	    
	    public static void Toggle<T>(this List<T> self, T value)
	    {
	    	if( self.Contains( value ) == true )
	    	{
	    		self.Remove( value );
	    	}
	    	else
	    	{
	    		self.Add( value );
	    	}
	    }
	}
}
