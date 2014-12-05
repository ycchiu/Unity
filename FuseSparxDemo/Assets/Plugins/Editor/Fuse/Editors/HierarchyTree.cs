using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EB;
namespace EB
{
public class HierarchyTree {
	
	private Color 	highlightColor = Color.white;
	private Color   defaultColor = Color.white;
				
	public class TreeElement
	{
		public string 			 Label;
		public string  			 Tooltip;
		public List<TreeElement> mList;
		public bool  	 		 IsOpen;
		public bool  			 IsHeader;
		public object 			 context;
		public Color 			 color;
			
		public TreeElement()
		{
			IsOpen=false;
			IsHeader=false;
			mList = new List<TreeElement>();
		}		
	};
		
	private int 	yOffset = 15;		// Line spacing
	private int 	xIndent = 20;		// Indent when tree is opended up
	List<TreeElement>   mTree;			// Container for tree info

	// Delegates
	public delegate void OnSelectDelegate( TreeElement item );	
	OnSelectDelegate	selectCallback;
		
	public HierarchyTree()
	{			
		highlightColor = Color.yellow;
		defaultColor = Color.white;
			
		mTree = new List<TreeElement>();
	}
	
	public void SetSelectionCallback( OnSelectDelegate callback )
	{
		selectCallback = callback;
	}
		
	public void AddItem(string item, object context, string tooltip)
	{
		// Parse the string
		string[] tokens = item.Split(new char[] {'/'});

		int NumTokens = tokens.Length;
			
		// If the tokens
		TreeElement currItem = null;
		for (int i=0; i<NumTokens; i++)
		{	
			TreeElement element = null;
					
			if(i==0)
			{
				element = FindItem(mTree, tokens[i],false);
			}
			else
			{
				element = FindItem(currItem.mList,tokens[i],false);
			}
			
			if (element!=null)
			{
				currItem = element;
				continue;
			}
	
			if (i<NumTokens-1 && NumTokens>1)
			{
				currItem = AddHeader( currItem, tokens[i],tooltip);
			}
			else
			{
				currItem = AddItem( currItem, tokens[i],context,tooltip);
			}			
		}
				
	}
		
	public int GetCount()
	{
		return GetCount(mTree);
	}
		
	public float GetHeight()
	{
		return yOffset+(GetCount()*yOffset);
	}
		
	public int GetCount( List<TreeElement> treeitems)
	{
		int count = 0;
		int length = treeitems.Count;
			
		count+=length;
		for (int i=0; i<treeitems.Count;i++)
		{
			if (treeitems[i].IsOpen)
			{
				count+=	GetCount(treeitems[i].mList);			
			}
		}

		return count;
	}
		
	public TreeElement AddItem( TreeElement parent, string label, object context, string tooltip)
	{		
		// Parse the list and check for duplicates!
		TreeElement pItem = new TreeElement();
		pItem.Label = label;
		pItem.Tooltip = tooltip;
		pItem.IsHeader = false;
		pItem.IsOpen   = true;
		pItem.context  = context;
		pItem.color  = defaultColor;
			
		if (parent!=null)
		{
			parent.mList.Add(pItem);
		}
		else
		{
			mTree.Add(pItem);
		}
						
		return pItem;
	}
		
	public void SortList()
	{
		mTree.Sort(CompareTreeItems);
		foreach(TreeElement treeElement in mTree)
		{
			treeElement.mList.Sort(CompareTreeItems);
		}		
	}
		
	public TreeElement AddHeader( TreeElement parent, string label, string tooltip )
	{
		TreeElement pItem = new TreeElement();
		pItem.Label = label;
		pItem.Tooltip = tooltip;
		pItem.IsHeader = true;
		pItem.IsOpen   = false;
		pItem.color  = defaultColor;
			
		if (parent!=null)
		{
			parent.mList.Add(pItem);
		}
		else
		{
			mTree.Add(pItem);
		}
		return pItem;
	}
	
	public void DrawTree(Event e, int x, int y)
	{
		DrawTree( e, mTree, x, y );
	}
	
	public void SetItemColor( string label, Color col)
	{
		TreeElement ele = FindItem(label);
			
		if (ele!=null)
		{
			ele.color = col;			
		}
	}
		
	private TreeElement FindItem( List<TreeElement> treeitems, string itemToFind, bool useDepth)
	{		
		int length = treeitems.Count;

		for (int i=0; i<length; i++)
		{		
			if (itemToFind==treeitems[i].Label)
			{			
				return treeitems[i];
			}
				
			if (useDepth)
			{
				if (treeitems[i].mList.Count>0)
				{
					return FindItem(treeitems[i].mList, itemToFind, true);
				}
			}
		}						
			
		return null;
	}

	public TreeElement FindItem( string item )
	{
		// Parse the string
		string[] tokens = item.Split(new char[] {'/'});

		int NumTokens = tokens.Length;
			
		// Find root element
		// If the tokens
		TreeElement currItem = FindItem(mTree,tokens[0],false);
			
		for (int i=1; i<NumTokens; i++)
		{
			currItem = FindItem(currItem.mList, tokens[i], true);
				
			if (currItem==null)
			{
				Debug.Log("HierarchyTree:FindItem - Unable to find "+item);
				break;
			}
		}
		
		return currItem;
	}
		
	public void RemoveItem( string item ) 
	{	
		TreeElement elementToRemove = FindItem(item);
			
		if (elementToRemove!=null)
		{
			DeleteItem(mTree,elementToRemove);
		}
		else
		{
			Debug.Log("Hierarchy Tree:RemoveItem Unable to find "+item);
		}
	}
	
	public void ClearTree()
	{
		DeleteChildren(mTree);
	}

	private void DeleteChildren( List<TreeElement> treeitems )	
	{
		int length = treeitems.Count;

		for (int i=0; i<length; i++)
		{
			// Force delete all children!
			if (treeitems[i].mList.Count>0)
			{
				DeleteChildren(treeitems[i].mList);
			}				
		}
			
		treeitems.Clear();

	}

	private void DeleteItem( List<TreeElement> treeitems, TreeElement itemToRemove )
	{
		int length = treeitems.Count;

		for (int i=0; i<length; i++)
		{
			if (treeitems[i].mList.Count>0)
			{
				DeleteItem(treeitems[i].mList, itemToRemove);
			}

			if (itemToRemove==treeitems[i])
			{
				// Force delete all children!
				if (treeitems[i].mList.Count>0)
				{
					DeleteChildren(treeitems[i].mList);
				}
			}	
				
		}
	}
		
	private int CompareTreeItems(TreeElement item1, TreeElement item2)
    {
		return string.CompareOrdinal(item1.Label, item2.Label);
    }
	
	private int DrawTree( Event e, List<TreeElement> treeitems, int x,int y)
	{	
		int length = treeitems.Count;

		if (length>0)
			x+=xIndent;
		
		for (int i=0; i<length; i++)
		{
			y+=yOffset;
			
			if (treeitems[i].mList.Count>0)
			{
				if (treeitems[i].IsHeader==true)
				{
					GUI.color = treeitems[i].color;					
					treeitems[i].IsOpen = EditorGUI.Foldout(new Rect(x,y,200,15), treeitems[i].IsOpen, treeitems[i].Label);
					GUI.color = Color.white;
						
					if (treeitems[i].IsOpen)
					{
						y = DrawTree( e, treeitems[i].mList, x, y );
					}
				}
			}				
			else
			{	
					
				Color original = GUI.color;
				Color col = treeitems[i].color; 

				bool bOverLabel = false;
				if (e.mousePosition.x>=x && e.mousePosition.x<x+150)
				{
					if (e.mousePosition.y>=y && e.mousePosition.y<y+yOffset)
					{
						bOverLabel = true;
						col = highlightColor;
					}
				}

					
				GUI.color = col;

				if (e.type == EventType.MouseDown)
				{
					if (bOverLabel)
					{		
						if (selectCallback!=null)
						{
							selectCallback(treeitems[i]);
						}
					}
				}
					
				GUIContent c = new GUIContent();
				c.text = treeitems[i].Label;
				c.tooltip = treeitems[i].Tooltip;
				GUI.Label(new Rect(x,y,300,20), c);		
				GUI.color = original;
			}
		}
		
		return y;
	}
}
}
