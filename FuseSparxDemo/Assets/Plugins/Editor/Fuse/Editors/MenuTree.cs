using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EB;
namespace EB
{
public class MenuTree {
					
	public class MenuElement
	{
		public string 			 Label;
		public List<MenuElement> mList = new List<MenuElement>();
		public object 			 context;			
	};
		
	List<MenuElement>   mMenu = new List<MenuElement>();			// Container for tree info

	// Delegates
	
	/*
	public delegate void OnSelectDelegate( MenuElement item );	
	OnSelectDelegate	selectCallback;
			
	public void SetSelectionCallback( OnSelectDelegate callback )
	{
		selectCallback = callback;
	}
	*/
		
	/***************************************************************************/
	public void AddItem(string item, object context)
	{
		// Parse the string
		string[] tokens = item.Split(new char[] {'/'});

		int NumTokens = tokens.Length;
			
		// If the tokens
		MenuElement currItem = null;
		for (int i=0; i<NumTokens; i++)
		{	
			MenuElement element = null;
					
			if(i==0)
			{
				element = FindItem(mMenu, tokens[i],false);
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
	
			currItem = AddItem( currItem, tokens[i],context);
		}
				
	}
		
	/***************************************************************************/
	public int GetCount()
	{
		return GetCount(mMenu);
	}
				
	/***************************************************************************/
	public int GetCount( List<MenuElement> treeitems)
	{
		int count = 0;
		int length = treeitems.Count;
			
		count+=length;
		for (int i=0; i<treeitems.Count;i++)
		{
			count+=	GetCount(treeitems[i].mList);			
		}

		return count;
	}
		
	/***************************************************************************/
	public MenuElement AddItem( MenuElement parent, string label, object context)
	{		
		// Parse the list and check for duplicates!
		MenuElement pItem = new MenuElement();
		pItem.Label = label;
		pItem.context  = context;
			
		if (parent!=null)
		{
			parent.mList.Add(pItem);
		}
		else
		{
			mMenu.Add(pItem);
		}
						
		return pItem;
	}
	
			
	/***************************************************************************/
	private MenuElement FindItem( List<MenuElement> treeitems, string itemToFind, bool useDepth)
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
		
	/***************************************************************************/
	public List<MenuElement> GetChildItems( string item )
	{			
		if (string.IsNullOrEmpty(item))
		{
			return mMenu;
		}
		else
		{
			MenuElement list = FindItem(item);
			if (list != null)
			{
				return list.mList;
			}
		}
			
			
		return null;
	}
		
	/***************************************************************************/
	public List<string> GetChildItemNames( string item )
	{			
		List<string> s = new List<string>();
			
		if (string.IsNullOrEmpty(item))
		{
			s = GetItemNames(mMenu);
		}
		else
		{
			MenuElement list = FindItem(item);
				
			if (list != null)
			{
				s = GetItemNames(list.mList);
			}				
		}
			
		return s;			
	}
		
	/***************************************************************************/
	List<string> GetItemNames( List<MenuElement> items)
	{
		List<string> s = new List<string>();
			
		foreach(MenuElement element in items)
		{
			s.Add(element.Label);
		}
			
		return s;
	}
	

	/***************************************************************************/
	public MenuElement FindItem( string item )
	{
		// Parse the string
		string[] tokens = item.Split(new char[] {'/'});

		int NumTokens = tokens.Length;
			
		// Find root element
		// If the tokens
		MenuElement currItem = FindItem(mMenu,tokens[0],false);
			
		for (int i=1; i<NumTokens; i++)
		{
			currItem = FindItem(currItem.mList, tokens[i],false);
				
			if (currItem==null)
			{
				Debug.Log("MenuTree:FindItem - Unable to find "+item);
				break;
			}
		}
		
		return currItem;
	}
		
	/***************************************************************************/
	public void RemoveItem( string item ) 
	{	
		MenuElement elementToRemove = FindItem(item);
			
		if (elementToRemove!=null)
		{
			DeleteItem(mMenu,elementToRemove);
		}
		else
		{
			Debug.Log("MenuTree:RemoveItem Unable to find "+item);
		}
	}
	
	/***************************************************************************/
	public void ClearTree()
	{
		DeleteChildren(mMenu);
	}

	/***************************************************************************/
	private void DeleteChildren( List<MenuElement> treeitems )	
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

	/***************************************************************************/
	private void DeleteItem( List<MenuElement> treeitems, MenuElement itemToRemove )
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
	
	/***************************************************************************/
	// SR Old legacy tree draw from hierarchy tree - TODO
	/*
	private int DrawTree( List<TreeElement> treeitems, int x,int y)
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
						y = DrawTree(treeitems[i].mList,x,y);
					}
				}
			}				
			else
			{	
					
				Color original = GUI.color;
				Color col = treeitems[i].color; 
				
				Event e = Event.current;

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
	*/
}
}
