using UnityEngine;
using System.Collections;

public static class EnvironmentUtils 
{
	public static string Get( string name, string defaultValue ) 
	{
		try
			{
				var env = System.Environment.GetEnvironmentVariable(name);
				if ( string.IsNullOrEmpty(env) == false )
				{
					return env;
				}
			}
		catch
		{}
		
		return defaultValue;
	}
	
	// moko: added a method to get the env details on this machine
	public static string GetEnvirnomentDetails()	
	{
		System.Type envType = typeof(System.Environment);
		string result = string.Format("Class: {0} [{1}]", envType.Name, envType.AssemblyQualifiedName);
    	var flags = System.Reflection.BindingFlags.NonPublic
    		|System.Reflection.BindingFlags.Public
    		|System.Reflection.BindingFlags.Static
    		|System.Reflection.BindingFlags.Instance;    	    	
    		
    	System.Reflection.PropertyInfo[] props = envType.GetProperties(flags);
		foreach (var p in props)
		{
			result += string.Format("\nProperty: {0}.{1} [{2}] = {3}", envType.Name, p.Name, p.PropertyType.ToString(), p.GetValue(null, null));
		}

		foreach (DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
		{
			result += string.Format("EnvVars: {0} = {1}\n", entry.Key, entry.Value);
		}	
		string[] args = System.Environment.GetCommandLineArgs();
    	result += string.Format("CmdLine Args: {0}\n", string.Join(" ", args));
    	
    	foreach (System.Environment.SpecialFolder f in System.Enum.GetValues(typeof(System.Environment.SpecialFolder)))
    	{
    		result += string.Format("Folder: {0} = {1}\n", f.ToString(), System.Environment.GetFolderPath(f));
    	}

		return result;
	}
}
