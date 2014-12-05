using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ExtensionMethods 
{
	public static class EnumerableExtensions
	{
	    public static IEnumerable<T> ToEnumerable<T>(this T item)
	    {
	        yield return item;
	    }
	}
}