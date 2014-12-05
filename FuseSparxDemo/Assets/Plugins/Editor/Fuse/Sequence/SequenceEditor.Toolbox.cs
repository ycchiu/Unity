using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace EB.Sequence.Editor
{
	public partial class SequenceEditor : PrefabEditor<EB.Sequence.Component>
	{
		readonly static float ToolBoxMinWidth = 200.0f;
		
		HierarchyTree ToolboxTree = null;
		Vector2 ToolboxScrollPos;
		
		void CreateToolbox()
		{	
			this.ToolboxTree = new HierarchyTree();
			this.ToolboxTree.SetSelectionCallback( OnToolboxItemSelected );
			
			var nodetypes = EB.Sequence.Utils.GetAllTypes();
			foreach( var n in nodetypes )
			{
				var mi = n.GetCustomAttributes(typeof(MenuItemAttribute),false )[0] as MenuItemAttribute;
				this.ToolboxTree.AddItem(mi.Path,n, string.Empty );
			}
			
			this.ToolboxTree.SortList();
			
			this.ToolboxTree.SetItemColor("Events", this.EventColor);
			this.ToolboxTree.SetItemColor("Actions", this.ActionColor);
			this.ToolboxTree.SetItemColor("Variables", this.VariableColor);
			this.ToolboxTree.SetItemColor("Conditions", this.ConditionColor);
		}
	
		void UpdateToolbox( Event e, Rect editorRect, Rect toolboxRect )
		{
			//Determine how big we need to make the overall scrollbar window
			Rect toolboxScrollRect = toolboxRect;
			float requiredHeight = this.ToolboxTree.GetHeight();
			if( requiredHeight > toolboxScrollRect.height )
			{
				toolboxScrollRect.height = requiredHeight;
			}
					
			this.ToolboxScrollPos = GUI.BeginScrollView( toolboxRect, this.ToolboxScrollPos, toolboxScrollRect, true, true );		
				this.ToolboxTree.DrawTree( e, 0, 0 );
			GUI.EndScrollView();
		}
		
		public void OnToolboxItemSelected( EB.HierarchyTree.TreeElement item)
		{
			if (Target!=null)
			{
				StartCreationPlacement( item.context as System.Type );
			}
			else
			{
				UnityEngine.Debug.Log("NO SEQUENCE SELECTED!");
			}
		}
	}
}
