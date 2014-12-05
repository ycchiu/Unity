using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class VersionControllUtils 
{
	public enum AssetStatus
	{
		BadState,
		Calculating,
		ClientOnly,
		Ignored,
		NewLocalVersion,
		NewVersionAvailable,
		RestoredFromTrash,
		Same,
		ServerOnly,
		Unchanged,
		Conflict
	}
	
	public class AssetServer
	{
		private System.Type _type;
		private object _instance;		
		public AssetServer()
		{
			var assembly = System.Reflection.Assembly.GetAssembly(typeof(AssetDatabase));
			_type = assembly.GetType("UnityEditor.AssetServer");
			_instance = System.Activator.CreateInstance(_type);
		}
		
		public AssetStatus GetStatusGUID(string guid)
		{
			return InvokeEnum<AssetStatus>("GetStatusGUID", guid);
		}
		
		private T InvokeEnum<T>(string name, params object[] arguments) 
		{
			return (T)System.Enum.Parse( typeof(T), Invoke(name,arguments).ToString() );
		}
		
		private object Invoke(string name, params object[] arguments)
		{
			var method = _type.GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			return method.Invoke(_instance, arguments);
		}
	}
	
	
	public static string[] GetFilesOpenForEdit()
	{
		var assetServer = new AssetServer();
		
		var files = System.IO.Directory.GetFiles("Assets/", "*", System.IO.SearchOption.AllDirectories);
		var open = new List<string>();
		
		foreach ( var file in files )
		{
			if (file.EndsWith(".meta") )
			{
				continue;
			}
			
			var guid = AssetDatabase.AssetPathToGUID(file);
			if ( string.IsNullOrEmpty(guid) )
			{
				continue;
			}
			
			var status = assetServer.GetStatusGUID(guid);
			switch(status)
			{
			case AssetStatus.NewLocalVersion:
			case AssetStatus.ClientOnly:
			case AssetStatus.RestoredFromTrash:
				open.Add(file);
				break;
			}
		}
		
		return open.ToArray();
	}
	
	[MenuItem("EBG/Version Control/Save Work")]
	public static void Save()
	{
		var files = GetFilesOpenForEdit();
		var now = System.DateTime.Now;
		var filename = string.Format("work-{0}-{1}-{2}-{3}-{4}-{5}.unitypackage", now.Month, now.Day, now.Year, now.Hour, now.Minute, now.Second);
		AssetDatabase.ExportPackage(files, filename, ExportPackageOptions.Interactive );
	}


	// ***************************************************************************
	//	moko: added a function to either check out a file or force it to be writeable (or both)
	//		assuming file is under project root folder (ie input should be either full path or as "Assets/...")
	//
	[System.Flags]
	public enum CheckOutType
	{
		None = 0,
		ForceWriteable =1,
		CheckOut =2
	}

	public static System.IO.FileInfo CheckOutFile(string fname, CheckOutType type =CheckOutType.CheckOut|CheckOutType.ForceWriteable)
	{
		var fullpath = fname;
		if (!System.IO.File.Exists(fname))
		{
			fullpath = Application.dataPath + "/../"+ fname;
		}
	
		var fInfo = new System.IO.FileInfo(fullpath);
		if (fInfo != null)
		{
			if (fInfo.Exists)
			{
				if ((UnityEditor.VersionControl.Provider.isActive) && ((type & CheckOutType.CheckOut) == CheckOutType.CheckOut))
				{
					System.Uri uri1 = new System.Uri(fInfo.FullName);
					System.Uri uri2 = new System.Uri(Application.dataPath);
					var path = uri2.MakeRelativeUri(uri1).ToString();
					UnityEditor.VersionControl.Provider.Checkout(path, UnityEditor.VersionControl.CheckoutMode.Both);
					//EB.Debug.Log("Checking out asset: " + path);
				}

				if (fInfo.IsReadOnly && (type & CheckOutType.ForceWriteable) == CheckOutType.ForceWriteable)
				{
					fInfo.Attributes ^= System.IO.FileAttributes.ReadOnly;
					//EB.Debug.Log("Force readonly file to be writable: " + fname);
				}				
			}
		}
		return fInfo;
	}

	// ***************************************************************************
	//
}
