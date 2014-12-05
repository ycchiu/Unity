using System.Collections.Generic;
using System.Reflection;
using EB.Sequence.Serialization;
using EB.Sequence;
using UnityEngine;

namespace EB.Sequence.Editor
{
	public struct LinkInfo
	{
	    public string name;
	    public string editor;
	
	    public LinkInfo( string n, string ed )
	    {
	        name = n;
	        editor = ed;
	    }
	
	    public LinkInfo(string n)
	    {
	        name = n;
	        editor = EB.Sequence.Utils.NiceName(n);
	    }
	
	    public LinkInfo(string n, System.Type type)
	    {
	        name = n;
	        editor = EB.Sequence.Utils.EditorName(n, type);
	    }
	}
	
	public static class SequenceUtils
	{
	    // Add menu named "Create Quest Object" to the main menu
	    [UnityEditor.MenuItem("EBG/Sequencer/Create Sequence")]
	    static void CreateSequenceEntity () 
		{
			GameObject obj = new GameObject( "sequence", typeof(global::Sequence) );
			obj.tag = "Sequence";
	    }
		
		static void FixupProperty( EB.Sequence.Serialization.Property prop )
		{
			if ( prop != null )
			{
				var db = LocalizationUtils.GetLocDb("sequence");				
				var value = prop.stringValue ?? string.Empty;
				if (value.Length == 0)
				{
					return;
				}
				
				if ( !value.StartsWith("ID_") )
				{
					var id = db.NextId("SEQUENCE");
					db.Add(id, value);
					prop.stringValue = id;
				}
			}
		}
		
		[UnityEditor.MenuItem("EBG/Sequencer/Fixup loc ids")]
		static void FixupLocalizations()
		{
			foreach( var sequence in GeneralUtils.FindAllObjectsOfType<global::Sequence>("Assets") )
			{
				foreach( var node in sequence.Nodes )
				{
					foreach ( var prop in node.properties )
					{
						var attribute = node.GetPropertyAttribute(prop.name);
						if (attribute == null)
						{
							EB.Debug.LogWarning("Missing attribute on property " + prop.name + " " + node.runtimeTypeName );
							continue;
						}
						
						if ( attribute.Hint == "LocId")
						{
							FixupProperty(prop);
							UnityEditor.EditorUtility.SetDirty(sequence);
						}
					}
				}
			}
			
			LocalizationUtils.SaveDbs();
			
		}
	
	    private delegate bool ValidateAttributeDelegate(object obj);
	
	    private static bool All(object obj) 
	    {
	        return true;
	    }
	
	    private static LinkInfo[] GetFieldAttributes<T>(System.Type type, ValidateAttributeDelegate cb) where T : System.Attribute
	    {
	        List<LinkInfo> items = new List<LinkInfo>();
	
			if( type != null )
	    	{
		        foreach (var field in type.GetFields())
		        {
		            T[] attributes = (T[])field.GetCustomAttributes(typeof(T), true);
		            if (attributes.Length > 0)
		            {
		                if ( cb(attributes[0]))
		                    items.Add( new LinkInfo(field.Name,type) );
		            }
		        }
		    }
	        return items.ToArray();
	    }
	
	    private static LinkInfo[] GetMethodAttributes<T>(System.Type type, ValidateAttributeDelegate cb) where T : System.Attribute
	    {
	    	List<LinkInfo> items = new List<LinkInfo>();
	    	if( type != null )
	    	{
		        foreach (var method in type.GetMethods())
		        {
		            T[] attributes = (T[])method.GetCustomAttributes(typeof(T), true);
		            if (attributes.Length > 0)
		            {
		                if (cb(attributes[0]))
		                    items.Add(new LinkInfo(method.Name, type));
		            }
		        }
		    }
	        return items.ToArray();
	    }
	
	    private static bool VariableAll(object obj)
	    {
	        VariableAttribute attr = (VariableAttribute)obj;
	        return attr.Show;
	    }
	
	    private static bool VariableIn(object obj)
	    {
	        VariableAttribute attr = (VariableAttribute)obj;
	        return attr.Show && attr.Direction == Direction.In;
	    }
	
	    private static bool VariableOut(object obj)
	    {
	        VariableAttribute attr = (VariableAttribute)obj;
	        return attr.Show && attr.Direction != Direction.In;
	    }
	
	    public static LinkInfo[] GetVariableLinks(Node node)
	    {
	        List<LinkInfo> items = new List<LinkInfo>();
	        items.AddRange( GetVariableInLinks(node)  );
	        items.AddRange( GetVariableOutLinks(node) );
	        return items.ToArray();
	    }
	
	    public static LinkInfo[] GetVariableInLinks(Node node)
	    {
	        var type = node.RuntimeType;
	        List<LinkInfo> items = new List<LinkInfo>();
	
	        items.AddRange(GetFieldAttributes<VariableAttribute>(type, VariableIn));
	        items.AddRange(CustomEditor.GetCustomLinks(node, "VariableIn"));
	
	        return items.ToArray();
	    }
	
	    public static LinkInfo[] GetVariableOutLinks(Node node)
	    {
	        var type = node.RuntimeType;
	        List<LinkInfo> items = new List<LinkInfo>();
	
	        items.AddRange(GetFieldAttributes<VariableAttribute>(type, VariableOut));
	        items.AddRange(CustomEditor.GetCustomLinks(node, "VariableOut"));
	
	        return items.ToArray();
	    }
	
	    private static bool TriggerAll(object obj)
	    {
	        TriggerAttribute attr = (TriggerAttribute)obj;
	        return attr.Show;
	    }
	
	    public static LinkInfo[] GetOutputLinks(Node node)
	    {
	        var type = node.RuntimeType;
	        List<LinkInfo> items = new List<LinkInfo>();
	        items.AddRange(GetFieldAttributes<EB.Sequence.TriggerAttribute>(type, TriggerAll));
	
	        if (EB.Sequence.Utils.GetNodeType(type) == NodeType.Variable)
	        {
	            items.Insert(0, new LinkInfo(EB.Sequence.Runtime.Variable.ValueLinkName));
	        }
	
	        items.AddRange(CustomEditor.GetCustomLinks(node, "Output"));
	
	        return items.ToArray();
	    }
	
	    public static LinkInfo[] GetInputLinks(Node node)
	    {
	        var type = node.RuntimeType;
	
	        List<LinkInfo> items = new List<LinkInfo>();
	        items.AddRange(GetMethodAttributes<EB.Sequence.EntryAttribute>(type,All));
	        items.AddRange(CustomEditor.GetCustomLinks(node, "Input"));
	
	        return items.ToArray();
	    }
	}

}

