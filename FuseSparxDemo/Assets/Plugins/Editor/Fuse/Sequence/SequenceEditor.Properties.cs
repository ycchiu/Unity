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
		readonly static float PropertiesMinWidth = 600.0f;
		
		Vector2 PropertiesScrollPos = Vector2.zero;
		
		//This is required to deal with the Layout/Paint events as if the value changes in between we get an exception
		int LayoutNumSelected = 0;
		EB.Sequence.Serialization.Node LayoutSelectedNode = null;
		EB.Sequence.Serialization.Node LastLayoutSelectedNode = null;
	
		void UpdateProperties( Event e, Rect editorRect, Rect propertyRect )
		{
			GUILayout.BeginVertical();
			{
				GUI.backgroundColor = ( this.Dirty == true ) ? Color.red : Color.white;
				{
					if( GUILayout.Button("Save") == true )
					{
						CommitPrefab();
					}
				}
				GUI.backgroundColor = Color.white;
				
				bool clearFocus = false;	
				
				//We are only allowed to update the CacheNumSelected after the repaint is complete
				if( e.type == EventType.Layout )
				{
					this.LayoutNumSelected = this.SelectedNodes.Count;
					this.LayoutSelectedNode = ( this.LayoutNumSelected > 0 ) ? this.SelectedNodes[ 0 ] : null;
					if( this.LastLayoutSelectedNode != this.LayoutSelectedNode )
					{
						clearFocus = true;
					} 
					this.LastLayoutSelectedNode = this.LayoutSelectedNode;
				}
				
				if( clearFocus == true )
				{
					GUI.FocusControl( "" );
				}
			
				switch( this.LayoutNumSelected )
				{
					case 0:
					{
						this.PropertiesScrollPos = Vector2.zero;
						break;
					}
					case 1:
					{
						this.PropertiesScrollPos = GUILayout.BeginScrollView( this.PropertiesScrollPos, GUIStyle.none );
						{
							var info = GetNodeInfo( this.LayoutSelectedNode );
							
							GUILayout.BeginVertical();
							{
								GUILayout.BeginHorizontal();
								
								EditorGUILayout.LabelField( "Type", this.LayoutSelectedNode.runtimeTypeName );
							
								GUILayout.EndHorizontal();
								EditorGUILayout.LabelField( "Id", this.LayoutSelectedNode.id.ToString() );  
								this.LayoutSelectedNode.comment =  EditorGUILayout.TextField( "Comment", this.LayoutSelectedNode.comment );  
								
								foreach( var prop in this.LayoutSelectedNode.properties )
								{
									GUILayout.BeginHorizontal();
							        var attribute = info.Owner.GetPropertyAttribute(prop.name);
					
									info.dirty |= doProperty(this.LayoutSelectedNode, prop, attribute);
													
									if (attribute != null)
									{
										/*
										if (attribute.Hint=="InventoryQuestCategory")
										{
											GUILayout.BeginHorizontal();
										
											if (GUILayout.Button( "Select" , GUILayout.MaxWidth(50))) 
										    {
												FindInventoryQuestCategories(prop);
											}
											GUILayout.EndHorizontal();
										}
										else
										if (attribute.Hint=="Inventory")
										{
											GUILayout.BeginHorizontal();
										
											if (GUILayout.Button( "Select" , GUILayout.MaxWidth(50))) 
										    {
												FindInventory(prop);
											}
											GUILayout.EndHorizontal();
										}
										else
										*/
										if (attribute.Hint=="Sequence")	
										{
											GUILayout.BeginHorizontal();
												if (GUILayout.Button("Select", GUILayout.MaxWidth(50)))
												{
													FindSequence(prop);
												}
											
												if (prop.stringValue.Length>0)
												{
													if (GUILayout.Button( "Goto" , GUILayout.MaxWidth(50))) 
												    {
														ConfirmSave();
														GotoSequence(prop.stringValue);
													}
												}
											GUILayout.EndHorizontal();
										}
										else
										if (attribute.Hint=="Quest")
										{
											GUILayout.BeginHorizontal();
										
											if (prop.stringValue.Length>0)
											{
												if (GUILayout.Button( "Goto" , GUILayout.MaxWidth(50))) 
											    {
													ConfirmSave();
													GotoQuest(prop.stringValue);
												}
											}
											GUILayout.EndHorizontal();
										}					
									}					
					
									GUILayout.EndHorizontal();
								}
								
								foreach(var array in this.LayoutSelectedNode.propertyArrays)
						        {
						            int count = array.items.Count;
									
									GUILayout.BeginHorizontal();
									{
										GUILayout.Label(array.name);
										GUILayout.FlexibleSpace();
										
										if ( GUILayout.Button("+", GUILayout.Width(20) ) )
										{
											info.dirty = info.dirty || array.Resize(count + 1);
										}
										
										if ( GUILayout.Button("-", GUILayout.Width(20) ) && count > 0 )
										{
											info.dirty = info.dirty || array.Resize(count - 1);
										}
									}
									GUILayout.EndHorizontal();
									
									for (int i=0; i<array.items.Count; i++)
						            {
										var property = array.items[i];
					
										GUILayout.BeginHorizontal();
										
										string hint = "";
										
										var attribute = info.Owner.GetPropertyAttribute(array.name);
										
										if (attribute != null)
										{
											hint = attribute.Hint;
										}
					
										info.dirty = info.dirty || doProperty(this.LayoutSelectedNode, property, attribute);
										
										/*
										if (hint=="Inventory")
										{						
											if (GUILayout.Button( "Select" , GUILayout.MaxWidth(50))) 
								    		{
												FindInventory(property);
											}
										}
										else */
										if (hint=="Quest")
										{				
											GUILayout.BeginHorizontal();
											if (GUILayout.Button( "Select" , GUILayout.MaxWidth(50))) 
								    		{
												FindQuest(property);
											}
											if (GUILayout.Button( "X" , GUILayout.MaxWidth(50))) 
								    		{
												array.items.RemoveAt(i);
												break;
											}
											GUILayout.EndHorizontal ();
					
										}
										GUILayout.EndHorizontal();
						            }
						        }	
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndScrollView();
						break;
					}
					default:
					{
						this.PropertiesScrollPos = Vector2.zero;
						this.PropertiesScrollPos = GUILayout.BeginScrollView( this.PropertiesScrollPos, GUIStyle.none );
						{
							GUILayout.BeginVertical();
							{
								GUILayout.Label( "Multiple Nodes Selected. Cannot generate a property grid." );
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndScrollView();
						break;
					}
				}
			}
			GUILayout.EndVertical();
		}
		
		 bool doProperty(EB.Sequence.Serialization.Node node, EB.Sequence.Serialization.Property property, EB.Sequence.PropertyAttribute propAttribute)
	    {
	        string propName = Utils.NiceName(property.name);
	        switch (property.type)
	        {
	            case EB.Sequence.Serialization.PropertyType.Int:
	                {
	
	                    if ( propAttribute != null && propAttribute.MapTo != null )
	                    {
	                        var old = property.intValue;
	                        var enumValue = EditorGUILayout.EnumPopup(propName, (System.Enum)System.Enum.ToObject(propAttribute.MapTo, old));
	                        property.intValue = System.Convert.ToInt32(enumValue);
	                        return property.intValue != old;
	                    }
	                    else
	                    {
	                        var old = property.intValue;
	                        property.intValue = EditorGUILayout.IntField(propName, property.intValue);
	                        return property.intValue != old;
	                    }
	                }
	
	         	   case EB.Sequence.Serialization.PropertyType.String:
	                {
					
		               // test for dropdown
	                    string old = property.stringValue;
	                    object[] values = EB.Sequence.Editor.Intellisense.GetValues(node, property);
	                    if (values != null)
	                    {
	                        string[] strings = (string[])values;
	                        
	                        int current = System.Array.IndexOf(strings, property.stringValue);
	                        current = EditorGUILayout.Popup(propName, current, strings);
	                        if (current >= 0 && current < strings.Length)
	                        {
	                            property.stringValue =  strings[current];
	                        }
	                    }
	                    else
	                    {
							if ( propAttribute != null && propAttribute.Hint == "LocId")
							{
								if (old.StartsWith("ID_")==false)
								{
									var db = LocalizationUtils.GetLocDb("sequence");
									var id = db.NextId("SEQUENCE");
									LocalizationUtils.LocTextField(propName, id, "sequence", GUILayout.MinWidth(500) );
									property.stringValue = id;
									return true;
								}
								else
								{
									return LocalizationUtils.LocTextField(propName, property.stringValue, "sequence", GUILayout.MinWidth(500) );	
								}
							}
							else if (propAttribute != null && propAttribute.NonEditable==true)		
							{
	                        	EditorGUILayout.LabelField(propName, property.stringValue ?? string.Empty, GUILayout.MinWidth(500));
							}
							else
							{
	                        	property.stringValue = EditorGUILayout.TextField(propName, property.stringValue ?? string.Empty, GUILayout.MinWidth(500));
							}
	                    }
	
	                    return property.stringValue != old;
	                }
	            case EB.Sequence.Serialization.PropertyType.Float:
	                {
	                    var old = property.floatValue;
	                    property.floatValue = EditorGUILayout.FloatField(propName, property.floatValue);
	                    return property.floatValue != old; 
	                }
	            case EB.Sequence.Serialization.PropertyType.GameObject:
	                {
	                    var old = property.gameObjectValue;
	                    property.gameObjectValue = (GameObject)EditorGUILayout.ObjectField(propName, property.gameObjectValue, typeof(GameObject), false);
	                    return property.gameObjectValue != old;
	                }
	            case EB.Sequence.Serialization.PropertyType.Boolean:
	                {
	                    var old = (bool)property.Value;
	                    property.Value = EditorGUILayout.Toggle(propName, (bool)property.Value);
	                    return old != (bool)property.Value;
					}
				case  EB.Sequence.Serialization.PropertyType.Color:
					{
						var old = property.colorValue;
						property.colorValue = EditorGUILayout.ColorField(propName, old);
						return old != property.colorValue;
					}
				case  EB.Sequence.Serialization.PropertyType.Vector2:
					{
						var old = property.vector2Value;
						property.vector2Value = EditorGUILayout.Vector2Field(propName, old);
						return old != property.vector2Value;
					}	
				case  EB.Sequence.Serialization.PropertyType.Vector3:
					{
						var old = property.vector3Value;
						property.vector3Value = EditorGUILayout.Vector3Field(propName, old);
						return old != property.vector3Value;
					}	
				case  EB.Sequence.Serialization.PropertyType.Vector4:
					{
						var old = property.vector4Value;
						property.vector4Value = EditorGUILayout.Vector4Field(propName, old);
						return old != property.vector4Value;
					}
	        }
	        return false;
	    }
		
		void FindQuest(EB.Sequence.Serialization.Property prop)
		{
			selectionTarget = prop;	
			mMenu.ClearTree();
			
			List<string> objects = ResourceUtils.globalQuestList;
	
				
			objects.Sort(delegate(string s1, string s2)
		    {
	           return s1.CompareTo(s2);
	        });
			
			foreach(string s in objects)
			{
				mMenu.AddItem(s,null);
			}				
			mbDisplaySelectWindow=true;
		}
		
		void GotoQuest(string name)
		{
			selectionDepth=1;
			string displayName = Path.GetFileNameWithoutExtension(name);
			selectionItem = ResourceUtils.GetQuestId(displayName);
			FindSequence(null);	
		}
		
		void FindSequence(EB.Sequence.Serialization.Property prop)
		{
			selectionTarget = prop;
			List<string> prefabs = GeneralUtils.GetFilesWildcardRecursive(Application.dataPath,"Prefab_Sequences","*.prefab");
			
			prefabs.Sort(delegate(string s1, string s2)
		    {
	           return s1.CompareTo(s2);
	        });
						
			UpdateSelectionWindow(prefabs);
		}
	}
}
