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
		readonly static float SearchMinWidth = 800.0f;
		
		private List<string>			bookmarkList;
		
		struct 	NodeInfo
		{
			public string displayName;
			public System.Type nodeType;
		};
		
		int 			nodeSelectionIndex;
	    string 			textSearchString = string.Empty;
		
		List<NodeInfo>			nodeList;
		
		void CreateSearch()
		{
			bookmarkList = new List<string>();
			
			nodeList = new List<NodeInfo>();
			
			
			NodeInfo nInfo = new NodeInfo(); 
			nInfo.displayName = "ALL NODES";
			nInfo.nodeType = null;
			
			nodeList.Add(nInfo);
			
			var nodetypes = EB.Sequence.Utils.GetAllTypes();
			foreach( var n in nodetypes )
			{
				var mi = n.GetCustomAttributes(typeof(MenuItemAttribute),false )[0] as MenuItemAttribute;
				if( mi != null )
				{
					NodeInfo info = new NodeInfo();
					info.displayName = mi.Path;
					info.nodeType = (System.Type)(n);
					nodeList.Add(info);
				}
			}
			
			
			if (Selection.activeObject != null && Selection.activeObject.name != null)
			{
				UpdateSequenceHistory( Selection.activeObject.name);			
			}
			
			RefreshSequenceFilenames();
		}
	
		void UpdateSearch( Event e, Rect editorRect, Rect searchRect )
		{
			GUILayout.BeginHorizontal();
				
				GUILayout.BeginVertical();
					GUILayout.Space(21);
					if ( GUILayout.Button("Search"))
					{
						//SR Test
						SearchAllSequences();
					}
			
					GUILayout.Space(20);			
			
					GUILayout.BeginHorizontal();
						string[] displayList = nodeList.ConvertAll(m => m.displayName).ToArray();
						nodeSelectionIndex = EditorGUILayout.Popup("Node Type", nodeSelectionIndex, displayList, GUILayout.MaxWidth(350) );	
					 	if (GUILayout.Button("Reset",GUILayout.MaxWidth(80)))
						{
							nodeSelectionIndex=0;
						}
					GUILayout.EndHorizontal();
			
				
					GUILayout.BeginHorizontal();
						textSearchString = EditorGUILayout.TextField("Search Term", textSearchString);	// String
					 	if (GUILayout.Button("Reset",GUILayout.MaxWidth(80)))
						{
							textSearchString="";
						}
					GUILayout.EndHorizontal();				
				GUILayout.EndVertical();
			
				GUILayout.BeginVertical();
					GUILayout.Space(21);
					if ( GUILayout.Button("Show Bookmarks"))
					{
						ShowBookmarks();
					}
			
					GUILayout.Space(21);
					if ( GUILayout.Button("Add Bookmark"))
					{
						if (Target != null)
						{
							AddBookmark(Target.name);
						}
					}
	
					GUILayout.Space(21);
					if ( GUILayout.Button("Remove Bookmark"))
					{
						if (Target != null)
						{
							RemoveBookmark(Target.name);
						}
					}
				GUILayout.EndVertical();
			
				GUILayout.Space(20);
			
				GUILayout.BeginVertical();
					GUILayout.Label("Sequence History");
					foreach( string s in sequenceHistory )
					{
						if (GUILayout.Button(s, GUILayout.MaxWidth(500)))
						{
							ConfirmSave();
							GotoSequence(s);
						}
					}
				GUILayout.EndVertical();
				
			GUILayout.EndHorizontal();
		}
		
		void ShowBookmarks()
		{		
			selectionTarget = null;
			bookmarkList.Sort(delegate(string s1, string s2)
		    {
	           return s1.CompareTo(s2);
	        });
						
			UpdateSelectionWindow(bookmarkList);
		}
		
		void ClearBookMarks()
		{
			bookmarkList.Clear();
		}
		
		void AddBookmark(string displayName)
		{
			if (displayName.Length>0)
			{
				string fileName = GetSequenceFilename(displayName);
	
				if (fileName.Length>0)
				{
					if (bookmarkList.Contains(fileName)==false)
					{
						bookmarkList.Add(fileName);			
						EditorUtility.DisplayDialog("Bookmark added!", displayName, "OK"); 		
					}
				}
			}
		}
		
		void RemoveBookmark(string displayName)
		{
			if (displayName.Length>0)
			{
				string fileName = GetSequenceFilename(displayName);
	
				if (fileName.Length>0)
				{
					if (bookmarkList.Remove(fileName))
					{
						EditorUtility.DisplayDialog("Bookmark removed!", displayName, "OK"); 	
					}
				}
			}
		}
		
		void SearchAllSequences()
		{
			if (nodeSelectionIndex==0 && textSearchString.Length==0)
			{
	            UnityEngine.Debug.Log("Unable to search all nodes with no search parameters");
				return;
			}
					
			selectionDepth=0;
			selectionItem="";
			
			List<string> prefabs = GeneralUtils.GetFilesWildcardRecursive(Application.dataPath, "Prefab_Sequences","*.prefab");
	
			List<string> finalList = new List<string>();
			finalList.Sort(delegate(string s1, string s2)
		    {
	           return s1.CompareTo(s2);
	        });
	
			foreach( string s in prefabs)
			{
				GameObject go = LoadPrefab(s);
				
				if (go != null)
				{
					var seq = go.GetComponent<EB.Sequence.Component>();				
					if (seq != null )
					{					
						for (int i=0; i<seq.Nodes.Count; i++)
						{
							EB.Sequence.Serialization.Node n = seq.Nodes[i];
			
							bool bValidNode = false;
							
							if (nodeSelectionIndex!=0 && n.RuntimeType == nodeList[nodeSelectionIndex].nodeType)
							{							
								bValidNode = true;
							}
							else
							{
								if (nodeSelectionIndex==0)
								{	
									bValidNode=true;
								}
							}
						
							if (bValidNode)
							{
								if (textSearchString.Length>0)
								{
									// Property search! SR Need to enhave for other 
									foreach (EB.Sequence.Serialization.Property p in n.properties)
									{
										if (p.stringValue.IndexOf(textSearchString)>=0)
										{
											if (finalList.Contains(s)==false)
											{
												finalList.Add(s);	
												break;
											}
										}
										
										// Check to see if an int?
										try
										{
											int v = System.Convert.ToInt32(textSearchString);
											
											if (p.intValue==v)
											{
												if (finalList.Contains(s)==false)
												{
													finalList.Add(s);	
													break;
												}
											}
										}
										catch //(System.Exception e)
										{
										}
									}
								}
								else
								{
									if (finalList.Contains(s)==false)
									{
										finalList.Add(s);	
									}
								}
							}
						}						
						
					}
					else				
					{
	                    UnityEngine.Debug.Log("No sequence found!");
					}
				}
			}
	
			UpdateSelectionWindow(finalList);
		}
		
		void RefreshSequenceFilenames()
		{
			sequenceFilenameList = GeneralUtils.GetFilesWildcardRecursive(Application.dataPath,"Prefab_Sequences","*.prefab");
		}
		
		string GetSequenceFilename(string displayName)
		{
			for (int i=0; i<sequenceFilenameList.Count; i++)
			{
				string prefabDisplayName = Path.GetFileNameWithoutExtension(sequenceFilenameList[i]);
				
				if (displayName == prefabDisplayName)
				{
					return sequenceFilenameList[i];
				}
			}
			return string.Empty;	
		}
		
		void GotoSequence(string name )
		{
			string file = GeneralUtils.FindPrefab(Path.GetFileNameWithoutExtension(name), "Prefab_Sequences");		
			GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(file, typeof(GameObject));
			
			if (obj != null )
			{
				Selection.activeObject = obj;			
				UpdateSequenceHistory( Selection.activeObject.name );			
			}
			else
			{
	            UnityEngine.Debug.Log("CANNOT FIND SEQUENCE " + file);
			}
		}
		
		void UpdateSequenceHistory( string sequenceName )
		{
			int count=0;
			foreach(string s in sequenceHistory)
			{
				if (s == sequenceName)
				{
					sequenceHistory.Remove(s);
					break;
				}
			    count++;
			}
			
			if (sequenceHistory.Count>=7)
			{
				sequenceHistory.RemoveLast();
			}
				
			sequenceHistory.AddFirst(sequenceName);					
		}
		
		void UpdateSelectionWindow(List<string> prefabs)
		{
			this.IsSelectingASequence = true;
					
			mPrefabList.Clear();
			mPrefabQuestList.Clear();
					
			foreach( string s in prefabs)
			{
				mPrefabList.Add(s);
							
				string displayName = Path.GetFileNameWithoutExtension(s);			
				string questId = ResourceUtils.GetQuestId(displayName);
				
				if (mPrefabQuestList.Contains(questId)==false)
				{				
					mPrefabQuestList.Add(questId);
				}
			}
		}
	}
}
