using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using EB.Sequence.Serialization;

namespace EB.Sequence.Editor
{
	public partial class CustomEditor
	{
	    private static LinkInfo[] kNone = new LinkInfo[0];
	
	    public static LinkInfo[] GetCustomLinks(Node node, string linkType)
	    {
	        string function = string.Format("GetCustomLinks_{0}_{1}", node.runtimeTypeName, linkType);
	
	        System.Type thisType = typeof(CustomEditor);
	
	        MethodInfo method = thisType.GetMethod(function);
	        if (method != null)
	        {
	            return (LinkInfo[])method.Invoke(null, new object[] { node });
	        }
	
	        return kNone;
	    }

	
	    public static LinkInfo[] GetCustomLinks_SequenceCondition_BooleanEvaluation_VariableIn(Node node)
	    {
	        return MapToAlphabet(node, "Inputs", "Input");
	    }
		
		public static LinkInfo[] GetCustomLinks_SequenceCondition_Switch_Output(Node node)
	    {
	        return MapToPropertyArray(node, "TestValues", "Triggers", "== ", string.Empty);
	    }
		
	    public static LinkInfo[] GetCustomLinks_SequenceCondition_BooleanEvaluation_Output(Node node)
	    {
	        return MapToPropertyArray(node, "Evaluations", "Triggers", string.Empty, string.Empty);
	    }
	
	    public static LinkInfo[] GetCustomLinks_SequenceAction_Random_Output(Node node)
	    {
	        return MapToPropertyArray(node, "Probabilities", "Triggers", string.Empty, " %");
	    }
		
		
		private static LinkInfo[] MapToFormat(Node node, string countName, string dstName, string fmt)
	    {
	        int count = node.GetProperty(countName).intValue;
	
	        List<LinkInfo> items = new List<LinkInfo>();
	        for (int i = 0; i < count && i < EB.Sequence.Runtime.Node.kAlphabet.Length; ++i)
	        {
	            items.Add(new LinkInfo(string.Format("{0}[{1}]", dstName,i), string.Format(fmt,i) ) );
	        }
	
	        return items.ToArray();
	    }
	
	    private static LinkInfo[] MapToAlphabet(Node node, string countName, string dstName)
	    {
	        int count = node.GetProperty(countName).intValue;
	
	        List<LinkInfo> items = new List<LinkInfo>();
	        for (int i = 0; i < count && i < EB.Sequence.Runtime.Node.kAlphabet.Length; ++i)
	        {
	            items.Add(new LinkInfo(string.Format("{0}[{1}]", dstName,i), EB.Sequence.Runtime.Node.kAlphabet.Substring(i, 1)));
	        }
	
	        return items.ToArray();
	    }
	
	    private static LinkInfo[] MapToPropertyArray( Node node, string srcName, string dstName, string prefix, string postfix )
	    {
	        List<LinkInfo> items = new List<LinkInfo>();
	
	        PropertyArray array = node.GetPropertyArray(srcName);
	        for (int i = 0; i < array.items.Count; ++i )
	        {
	            string value = prefix + (array.items[i].Value ?? string.Empty).ToString() + postfix;
	            string id = string.Format("{0}[{1}]", dstName, i);
	            if (string.IsNullOrEmpty(value)) value = id;
	            items.Add(new LinkInfo(id, value));
	        }
	
	        return items.ToArray();
	    }
	}
}


