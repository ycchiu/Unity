using UnityEngine;
using UnityEditor;
using System.Collections;

public class EBFontSwapper : ScriptableWizard 
{
	public UIFont find;
	public UIFont replace;
	
	[MenuItem ("EBG/Swap Fonts")]
    static void CreateWizard () 
	{
        ScriptableWizard.DisplayWizard<EBFontSwapper>("Swap Fonts", "Replace");
    }
	
	bool _replaced = false;
	
    void OnWizardCreate() 
	{
		var prefabs = GeneralUtils.GetFilesRecursive("Assets/Resources", "*.prefab");	
		foreach(var path in prefabs )
		{
			Debug.Log("Path: " + path);
			var obj = AssetDatabase.LoadMainAssetAtPath(path);
			if ( obj is GameObject )
			{
				var go = (GameObject)obj;
				_replaced = false;
				ReplaceRecursive(go);
				if (_replaced)
				{
					EditorUtility.SetDirty(obj);
				}
			}
		}
		EditorApplication.SaveAssets();
	}
	
	void ReplaceRecursive( GameObject go )
	{
		if ( go == null)
		{
			return;
		}
		
		var old = go.GetComponent<UILabel>();
		if (old)
		{
			Replace(old);
		}
		
		foreach( Transform tx in go.transform )
		{
			ReplaceRecursive(tx.gameObject);
		}
	}
	
	void Replace( UILabel old )
	{
		if ( old.bitmapFont == find )
		{
			old.bitmapFont = replace;
			_replaced = true;
		}
		
		
	}
	
}
