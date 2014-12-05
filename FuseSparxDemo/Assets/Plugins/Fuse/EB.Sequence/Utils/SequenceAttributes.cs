using System.Collections.Generic;

namespace EB.Sequence
{
	public class PropertyAttribute : System.Attribute
	{
		public string Description = string.Empty;

        public System.Type MapTo = null;
		public string Hint = string.Empty;
		public bool NonEditable = false;
	}
	
	public enum Direction
	{
		In, 
		Out,
		InOut,
	}
	
	public class VariableAttribute : System.Attribute
	{
		public System.Type ExpectedType = null;
		public Direction Direction = Direction.In;
        public bool Show = true;
	}
	
	public class TriggerAttribute : System.Attribute
	{
		public string EditorName = string.Empty;
        public bool Show = true;
	}
	
	public class EntryAttribute : System.Attribute
	{
	}
	
	public class MenuItemAttribute : System.Attribute
	{
		public string Path = string.Empty;
		public System.Type VariableType = null;
	}
}
