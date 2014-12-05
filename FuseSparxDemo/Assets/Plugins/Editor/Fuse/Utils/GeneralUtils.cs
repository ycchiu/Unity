using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class GeneralUtils
{
	static void MoveCurrentSelectionToCameraFocus()
	{		
		GameObject obj = Selection.activeGameObject;
		
		if (obj)
		{
			if (Camera.current)
			{
				Vector3 pos = Vector2.zero;
			 	Vector3 fwd = Camera.current.transform.TransformDirection(Vector3.forward);
			
	        	RaycastHit hit;
		        if (Physics.Raycast(Camera.current.transform.position, fwd, out hit))
			    {
					pos = hit.point;
		    	}
		    	else
		    	{
					fwd.Normalize();
					pos = Camera.current.transform.position + fwd;			
			   }		

				obj.transform.position = pos;				
			}
			else
			{
				EB.Debug.Log("GeneralUtils: MoveCurrentSelectionToCameraFocus - no camera selected");
			}
		}
		else
		{
			EB.Debug.Log("GeneralUtils: MoveCurrentSelectionToCameraFocus - no object selected");
		}
	}

	public static void FindCurrentHitPointFromCamera(out Vector3 pos)
	{
		pos = Vector3.zero;		
		if (Camera.current)
		{
		 	Vector3 fwd = Camera.current.transform.TransformDirection(Vector3.forward);
			
	        RaycastHit hit;
	        if (Physics.Raycast(Camera.current.transform.position, fwd, out hit))
		    {
				pos = hit.point;
		    }
		    else
		    {
				EB.Debug.Log("Found nothing to hittest against, stuffing object in front of camera");
				pos = Camera.current.transform.position + fwd;			
		    }		
		}
		else
		{
			EB.Debug.Log("FindCurrentHitPointFromCamera : Current camera not active!");
		}
	}
	
	public static Vector3 FindCurrentHitPointFromMouse()
	{
		//SR TODO
		Vector3 pos = Vector3.zero;		
	 	Vector3 fwd = Camera.current.transform.TransformDirection(Vector3.forward);
		
        RaycastHit hit;
        if (Physics.Raycast(Camera.current.transform.position, fwd, out hit))
	    {
			pos = hit.point;
	    }
	    else
	    {
			pos = Camera.current.transform.position + fwd;			
	   }
		
		return pos;
	}
	
	public static string FindPrefab( string name, string w ) 
	{
		name = name.ToLower();
		
		foreach( string file in GetFilesWildcardRecursive(Application.dataPath,w, "*.prefab") )
		{
			if ( Path.GetFileNameWithoutExtension(file).ToLower() == name )
			{
				return file;	
			}
		}
		return string.Empty;
	}
	
	public static List<string> GetFilesRootPath(string dir, string fileExt)
	{
		List<string> result = new List<string>();
		
		foreach( string file in Directory.GetFiles(dir, fileExt) )
		{
			string p = NormalizePath(file);
			int index = p.IndexOf("Assets/");
			if ( index > 0 )
			{
				p = p.Substring(index);
			}
			
			//Debug.Log("added: " + p );
			result.Add(p);
		}
		
		return result;
	}
	
	private const string prefabExt = ".prefab";
	public static T[] FindAllObjectsOfType<T>( string folder ) where T : UnityEngine.Component
	{
		List<T> objects = new List<T>();
		
		foreach( string f in Directory.GetFiles(folder,"*"+prefabExt, SearchOption.AllDirectories) ) 
		{
			Object obj = AssetDatabase.LoadAssetAtPath(f, typeof(GameObject) );
			if ( obj is GameObject )
			{
				var go = (GameObject)obj;
				T component = go.GetComponent<T>();
				if ( component != null )
				{
					objects.Add(component);
				}
			}
		}
		
		return objects.ToArray();
	}
	
	public static List<string> GetFilesWildcardRecursive(string b, string w, string fileExt)
    {
        // 1.
        // Store results in the file results list.
        List<string> result = new List<string>();

        // 2.
        // Store a stack of our directories.
        Stack<string> stack = new Stack<string>();

        // 3.
        // Add initial directory.
        stack.Push(b);

        // 4.
        // Continue while there are directories to process
        while (stack.Count > 0)
        {
            // A.
            // Get top directory
            string dir = stack.Pop();

            try
            {
                // B
                // Add all files at this directory to the result List.
				if ( dir.Contains(w) )
				{
					result.AddRange(GetFilesRootPath(dir, fileExt));
				}

                // C
                // Add all directories at this directory.
                foreach (string dn in Directory.GetDirectories(dir))
                {
                    stack.Push(dn);
                }
            }
            catch
            {
                EB.Debug.Log("GetFilesRecursive - Error opening directory");
                // Could not open the directory
            }
        }
        return result;
    }
	
	public static string NormalizePath( string path )
	{
		return path.Replace('\\','/');
	}
	
    public static List<string> GetFilesRecursive(string b, string fileExt)
    {
        // 1.
        // Store results in the file results list.
        List<string> result = new List<string>();

        // 2.
        // Store a stack of our directories.
        Stack<string> stack = new Stack<string>();

        // 3.
        // Add initial directory.
        stack.Push(b);

        // 4.
        // Continue while there are directories to process
        while (stack.Count > 0)
        {
            // A.
            // Get top directory
            string dir = stack.Pop();

            try
            {
                // B
                // Add all files at this directory to the result List.
                result.AddRange(GetFilesRootPath(dir, fileExt));

                // C
                // Add all directories at this directory.
                foreach (string dn in Directory.GetDirectories(dir))
                {
                    stack.Push( NormalizePath(dn) );
                }
            }
            catch
            {
                EB.Debug.Log("GetFilesRecursive - Error opening directory");
                // Could not open the directory
            }
        }
        return result;
    }
	
	[MenuItem("EBG/Helpers/Cleanup Texture")]
	public static void CleanupTexture()
	{
		Texture t = Selection.activeObject as Texture;
		if ( t == null )
		{
			return;
		}
		
		string assetPath = AssetDatabase.GetAssetPath(t);
		var texture		 = new Texture2D(1,1,TextureFormat.RGBA32, false);
		texture.LoadImage( File.ReadAllBytes(assetPath) );
		
		var pixels = texture.GetPixels();
		for ( int i =0; i < pixels.Length; ++i )
		{
			var c = pixels[i];
			if ( c.a <= 0.0001f )
			{
				c = new Color32(0,0,0,0);
			}
			pixels[i] = c;
		}
		texture.SetPixels(pixels);
		texture.Apply();
		
		File.WriteAllBytes( assetPath, texture.EncodeToPNG() );
		
		AssetDatabase.Refresh();
	}
	
	[MenuItem("EBG/Helpers/Make PoT")]
	public static void MakePoT()
	{
		foreach( Object obj in Selection.objects )
		{
			if ( obj is Texture2D )
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if ( path.ToLower().EndsWith(".png") == false )
				{
					continue;
				}
				
				var texture = (Texture2D)obj;
				
				TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
				if (ti.npotScale == TextureImporterNPOTScale.None && Mathf.IsPowerOfTwo(texture.width) && Mathf.IsPowerOfTwo(texture.height))
				{
					continue;
				}
				
				
				ti.npotScale = TextureImporterNPOTScale.ToNearest;
				ti.textureFormat  = TextureImporterFormat.ARGB32;
				ti.mipmapEnabled = false;
				ti.isReadable = true;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				texture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
				
				//
				var png = texture.EncodeToPNG();
				File.WriteAllBytes( path, png );
				
				ti.isReadable = false;
				ti.npotScale = TextureImporterNPOTScale.None;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				
			}
		}
		AssetDatabase.Refresh();
			
	}
  
    public static void DeleteDirectory(string path, bool recursive)
    {
        try 
        { 
            System.IO.Directory.Delete(path, recursive); 
        } 
        catch (System.Exception ex)
        {
            EB.Debug.LogWarning("Can not remove directory '" + path + "': " + ex.ToString());
        }
    }
  
}
