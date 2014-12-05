using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class ResourceUtils
{
	static public List<string>		globalLocatorList 		= new List<string>();
	static public List<string>		globalSceneList 		= new List<string>();
	static public List<string>		globalQuestList 		= new List<string>();
	
	public static List<string> GetLocatorList()
	{
		return globalLocatorList;
	}

	public static List<string> GetSceneList()
	{
		return globalSceneList;
	}
	
	public static List<string> GetQuestList()
	{
		return globalQuestList;
	}

	public static void Initialize()
	{
		UpdateQuests();
		UpdateLocatorsAndScenes();		
	}
	
	static void UpdateQuests()
	{
		globalQuestList.Clear();
		
		List<string> prefabs = GeneralUtils.GetFilesWildcardRecursive(Application.dataPath, "Prefab_Sequences","*.prefab");
		
		for (int i=0; i<prefabs.Count;i++)
		{
			string displayName = Path.GetFileNameWithoutExtension(prefabs[i]);			
			string questId = GetQuestId(displayName);

			if (globalQuestList.Contains(questId)==false)
			{
				globalQuestList.Add(questId);
			}
		}
	}
	
	/******************************************************************************************/
	public static string GetQuestId(string sequence)
	{
		string[] tokens = sequence.Split('_');
		
		string questId="";
		// Find the quest id
		foreach( string s in tokens )
		{
			if (s.LastIndexOf("quest") != -1)
			{
				questId = s;
				break;
			}
		}
		
		return questId;		
	}
	
	/*****************************************************************************************/
	public static void UpdateLocatorsAndScenes()
	{
		List<string> prefabs = GeneralUtils.GetFilesRecursive(Application.dataPath+"/Resources/Locators/","*.prefab");
		
		globalLocatorList.Clear();
		globalSceneList.Clear();
		
		// Open the dialog, display tree..
		for (int i=0; i<prefabs.Count;i++)
		{
			int first = prefabs[i].IndexOf("Locators/");
			
			if (first != -1)
			{
				int startOffset = first+9;
				
				string sourceFile = prefabs[i].Substring(startOffset, prefabs[i].Length-startOffset);
				
				// Directory
				string pathName = Path.GetDirectoryName(sourceFile);			
				
				
				string finalName = pathName+"/"+Path.GetFileNameWithoutExtension(prefabs[i]);
				
				if (string.IsNullOrEmpty(pathName)==false)
				{
					if (globalSceneList.Contains(pathName)==false)
					{
						globalSceneList.Add(pathName);
					}
					
					if (globalLocatorList.Contains(finalName)==false)
					{
						globalLocatorList.Add(finalName);
					}
				}				
			}
		}		
	}	
	
	
	
	/***********************************************************************************/
	public static string[] GetLocatorsFromScene(string sceneName)
	{
		List<string> locatorList = new List<string>();
			
		List<string> sourceLocators = ResourceUtils.GetLocatorList(); 
		int numLocators = sourceLocators.Count;
			
		for (int i=0; i<numLocators; i++)
		{
			string loc = sourceLocators[i];
					
			if (loc.Contains(sceneName))
			{
				int offset = loc.IndexOf('/');
				offset++;
				string finalLoc = loc.Substring(offset, loc.Length-offset);					
				// Strip out the shit upto / and add in the locator
				locatorList.Add(finalLoc);
			}
		}
		
		locatorList.Sort(delegate(string s1, string s2)
	    {
   	       return s1.CompareTo(s2);
       	});
		return locatorList.ToArray();
	}

	public static int GetIndexFromLocator( string loc )
	{
		for (int i=0; i<globalLocatorList.Count; i++)
		{
			if (globalLocatorList[i]==loc)
				return i;
		}
			
		return -1;
	}
	
	public static int GetIndexFromLocator( string loc, string[] locators )
	{
		for (int i=0; i<locators.Length; i++)
		{
			if (locators[i]==loc)
				return i;
		}
			
		return -1;
	}

	public static string GetLocatorFromIndex( int index )
	{
		if (index>=0 && index<globalLocatorList.Count)
		{
			return globalLocatorList[index];
		}
		
		return string.Empty;
	}
	
	public static string GetLocatorFromIndex( int index, string[] locators )
	{
		if (index>=0 && index<locators.Length)
		{
			return locators[index];
		}
		
		return string.Empty;
	}
	
	/***********************************************************************************/
	public static int GetIndexFromScene( string scene )
	{
		for (int i=0; i<globalSceneList.Count; i++)
		{
			if (globalSceneList[i]==scene)
				return i;
		}
			
		return -1;
	}
		
	/***********************************************************************************/
	public static string GetSceneFromIndex( int index )
	{
		if (index>=0 && index<globalSceneList.Count)
		{
			return globalSceneList[index];
		}
		
		return string.Empty;
	}
	
	/***********************************************************************************/
	public static int GetIndexFromQuest( string quest )
	{
		for (int i=0; i<globalQuestList.Count; i++)
		{
			if (globalQuestList[i]==quest)
			{
				return i;
			}
		}
			
		return -1;
	}
	
	/***********************************************************************************/
	public static string GetQuestFromIndex( int index )
	{
		if (index>=0 && index<globalQuestList.Count)
		{
			return globalQuestList[index];
		}
		
		return null;
	}

}
