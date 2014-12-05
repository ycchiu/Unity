//#define ENABLE_DELTA_BUNDLES  // Experimental: If defined, larger bundles will create delta bundles of JUST the changed files which can speed up bundling time 
// (but the original bundle still needs to be loadable - ie: no stale dependencies)

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

public static class Bundler 
{		
	public enum BundleOptions
	{
		None 			= 0,
		Force			= 1 << 0,
		SkipScenes 		= 1 << 1,
		Extended		= 1 << 2,
		SkipBase		= 1 << 3,
		SkipOgg			= 1 << 4,
		DeltaOnly 		= 1 << 5,
		Uncompressed 	= 1 << 6,
		ExternalOnly		= 1 << 7,
		BuildNotExistsOnly = 1 << 8,
	}
	
	private class DeltaBundleInfo
	{
		public string bundleNameBase;
		public string singleBundlePath;
		public string path;
		public string relPath;
		public Object[] objList;
	}	
	
	// asset toc
	private static Hashtable _bundles = new Hashtable();
	private static Hashtable _scenes;
	private static Hashtable _folders;
	
	private static Hashtable _deltaBundleContents = new Hashtable();
	
	private static Dictionary<string,System.DateTime> _modifiedTimes = new Dictionary<string, System.DateTime>();
	private static string _outFolder = string.Empty;
	private static System.DateTime _configTime;
	private static BuildAssetBundleOptions _bundleOptions = BuildAssetBundleOptions.CollectDependencies; // | BuildAssetBundleOptions.DeterministicAssetBundle;
	
	public static string OutFolder
	{
		get {
			return _outFolder;
		}
		set {
			_outFolder = value;
		}
	}
	
	private static string AssetName( string path )
	{
		return EB.Assets.AssetName(path);
	}
	
	private static string PathName( string path )
	{
		return path.ToLower().Replace('\\','/');
	}
	
	public static ArrayList LoadBundles( string filename, out System.DateTime modifiedTime )
	{
		if (filename.ToLower().EndsWith(BuildUtils.SkipFileName))
		{
			modifiedTime = new System.DateTime();
			return null;
		}
		string text = File.ReadAllText( filename );
		FileInfo info = new FileInfo(filename);
		modifiedTime = info.LastWriteTime;
		
		var parsed = (ArrayList)EB.JSON.Parse(text);
		if (parsed == null)
		{
			Debug.LogError("Bundle " + filename + " parsed as null");
		}
		return parsed;
	}
	
	private static void LoadModifiedTimes()
	{
		_modifiedTimes.Clear();
		
		foreach( string file in Directory.GetFiles("Library/metadata", "*", SearchOption.AllDirectories) ) 
		{
			string guid = Path.GetFileNameWithoutExtension(file).ToLower();
			FileInfo info = new FileInfo(file);
			_modifiedTimes[guid] = info.LastWriteTime;
		}
	}
	
	public static List<string> GetBundleConfigs( string directory = BuildSettings.BundlerConfigFolder, BundleOptions options = (Bundler.BundleOptions.None | Bundler.BundleOptions.Extended))
	{	
		if ( (options & BundleOptions.Force) != 0 )
		{
			GeneralUtils.DeleteDirectory(BuildSettings.BuildFolder, true);
		}
		
		List<string> bundles = new List<string>();
		
		var baseFiles = new List<string>( Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly) );
		
		var recursive = (options & BundleOptions.Extended) != 0;
		var files =  Directory.GetFiles(directory, "*.txt", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
		foreach( string file in files ) 
		{
			if ( baseFiles.Contains(file) && (options&BundleOptions.SkipBase) != 0 )
			{
				continue;
			}
			if (file.ToLower().EndsWith(BuildUtils.SkipFileName))
			{
				continue;
			}
			bundles.Add(file);
		}
		
		return bundles;
	}
	
	public static string[] BuildAll( string directory, BundleOptions options )
	{
		System.DateTime buildAllStart = System.DateTime.Now;
		_deltaBundleContents = new Hashtable();
		
		List<string> files = GetBundleConfigs(directory, options);
		
		List<string> packs = new List<string>();
		
		string tocPath = Path.Combine( BuildSettings.BuildFolder, "packs.txt" );
		
		string buildConfigDestPath = Path.Combine(BuildSettings.BuildFolder, Path.GetFileName(BuildSettings.BuildConfigPath));
		if (File.Exists(buildConfigDestPath)) 
		{
			File.Delete(buildConfigDestPath);
		}
		
		if (!Directory.Exists(BuildSettings.BuildFolder))
		{
			Directory.CreateDirectory(BuildSettings.BuildFolder);
		}		
		File.Copy(BuildSettings.BuildConfigPath, buildConfigDestPath);
		
		//for now we're just going to make the quick assumption that if packs.txt exists they have built before -- it's written last so 99% of the time this should be true.
		if (EB.Flag.IsSet(options, BundleOptions.BuildNotExistsOnly))
		{
			if (File.Exists(tocPath))
			{
				return new string[] {};
			}
			else
			{
				EB.Flag.Unset(ref options, BundleOptions.BuildNotExistsOnly);
			}
		}
		
		
		foreach( string file in files ) 
		{
			System.DateTime start = System.DateTime.Now;
			BuildBundles( file, options ); 
			System.DateTime end = System.DateTime.Now;
			Debug.Log("Time to create bundle: "+file+" : "+(end-start).TotalSeconds+" seconds");
			packs.Add(Path.GetFileNameWithoutExtension(file));
		}
		
		// write the pack file
		string toc = EB.JSON.Stringify( packs.ToArray() );
		Debug.Log("Writing Packs to " + tocPath );
		
		byte[] bytes = EB.Encoding.GetBytes(toc);
		if (bytes.Length > 0)    // moko: make sure we have something to write into pack.txt file
		{
			File.WriteAllBytes(tocPath, bytes);
		}		
		
		if (_deltaBundleContents.Count > 0)
		{
			// write the delta bundles toc file
			toc = EB.JSON.Stringify( _deltaBundleContents );
			tocPath = Path.Combine( BuildSettings.BuildFolder, "delta_bundles.txt" );
			Debug.Log("Writing BuildCacheInfo to:" + tocPath );
			File.WriteAllBytes(tocPath, EB.Encoding.GetBytes(toc ));
		}
		
		Debug.Log("Build All Bundles Total Time: "+(System.DateTime.Now - buildAllStart).ToString());
		return packs.ToArray();
	}
	
	public static string BuildOBB()
	{
		PlatformSwitch.AssertPlatform (BuildTarget.Android);
		var obbFile = Path.Combine(BuildSettings.BuildFolder, "../android.obb");
		if (File.Exists(obbFile))
		{
			File.Delete(obbFile);
		}
		
		var files = new List<string>(Directory.GetFiles(BuildSettings.BuildFolder,"*", SearchOption.AllDirectories));
		files.RemoveAll(delegate(string obj) {
			return obj.EndsWith(".DS_Store") || obj.EndsWith(".meta");
		});
		files.Sort();
		
		using( var zip = ZipFile.Create( obbFile ) )
		{
			zip.BeginUpdate();
			foreach( var filename in files )
			{
				var entryName = filename.Substring(BuildSettings.BuildFolder.Length+1);
				zip.Add(filename, entryName);
			}
			zip.CommitUpdate();
		}
		return obbFile;
	}
	
	public static void BuildBundles( string filename, BundleOptions options )		
	{
		
		bool buildUncompressed = BuildSettings.IsDevelopmentBuild;
		#if UNITY_IPHONE || UNITY_ANDROID
		buildUncompressed = true;
		#endif
		if (buildUncompressed)
		{
			// un uncompressed asset bundles on these platforms 
			_bundleOptions |= BuildAssetBundleOptions.UncompressedAssetBundle;
		}
		
		EditorUtility.UnloadUnusedAssets();
		
		EditorApplication.SaveAssets();
		
		LoadModifiedTimes();
		
		var shortName = Path.GetFileNameWithoutExtension(filename);
		_outFolder = Path.Combine(BuildSettings.BuildFolder, shortName);
		Directory.CreateDirectory(_outFolder);
		
		_bundles = new Hashtable();
		_scenes = new Hashtable();
		_folders = new Hashtable();
		
		var bundles = LoadBundles( filename, out _configTime );
		if (bundles != null)
		{
			foreach( Hashtable bundle in bundles )
			{
				BuildBundle(bundle,string.Empty,options);
			}
			WriteTOC(_outFolder);			
		}
	}
	
	public static void WriteTOC(string directory, string tocFileName = "toc.txt", string bundleName = null)
	{
		string tocPath = Path.Combine( directory, tocFileName );
		
		Hashtable data 		= new Hashtable();
		
		bool buildUncompressed = EB.Flag.IsSet(_bundleOptions, BuildAssetBundleOptions.UncompressedAssetBundle);
		if (buildUncompressed)
		{
			data["uncompressed"] = true;
		}
		
		data["version"] 	= PlayerSettings.bundleVersion;
		if (string.IsNullOrEmpty(bundleName))
		{
			data["bundles"] 	= _bundles;
		}
		else
		{
			Hashtable singleBundle = new Hashtable();
			singleBundle[bundleName] = _bundles[bundleName];
			data["bundles"] 	= singleBundle;
		}
		data["scenes"] 		= _scenes;
		data["folders"]		= _folders;
		
		// write the toc
		string toc = EB.JSON.Stringify( data );
		
		EB.Debug.Log("Writing TOC to " + tocPath );
		File.WriteAllBytes(tocPath, EB.Encoding.GetBytes(toc ));
		
	}
	
	private static bool BuildBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		BuildPipeline.PushAssetDependencies();
		EditorUtility.UnloadUnusedAssets();
		
		string type = EB.Dot.String("type", bundle, "standard");
		
		if ((options & BundleOptions.ExternalOnly) != 0)
		{
			string folder 	= EB.Dot.String("folder",bundle, string.Empty);
			List<string> externalBundles = BuildUtils.UnmovedBundleDirectories;
			bool isExternal = false;
			foreach( string externalBundleFolder in externalBundles )
			{
				if (folder.ToLower().StartsWith(Path.Combine("assets/bundles",externalBundleFolder.ToLower())))
				{
					isExternal = true;
					break;
				}
			}
			if (!isExternal)
			{
				Debug.Log("BundleOptions.ExternalOnly - skipping folder: "+folder);
				return false;
			}
			Debug.Log("BundleOptions.ExternalOnly - building folder: "+folder);
		}
		
		bool result = false;
		
		switch(type)
		{
		case "standard":
		{
			result = BuildStandardBundle(bundle,parent, options);
		}
			break;
		case "single":
		{
			result = BuildSingleBundle(bundle,parent, options);
		}
			break;	
		case "folder":
		{
			result = BuildFolderBundle(bundle,parent, options);
		}
			break;
		case "copy":
		{
			result = BuildCopyBundle(bundle,parent, options);
		}
			break;
		case "ogg":
		{
			result = BuildOggBundle(bundle,parent, options);
		}
			break;
		case "scenes":
		{
			if ( (options & BundleOptions.SkipScenes) == 0 )
			{
				result = BuildScenesBundle(bundle,parent,options);
			}	
		}
			break;
		default:
			EB.Debug.LogError("unknown bundle type: " + type);
			break;
		}
		
		parent = EB.Dot.String("id",bundle,string.Empty);
		
		// if we built, make sure to force children updates
		if ( result )
		{
			options |= Bundler.BundleOptions.Force;
		}
		
		// build the dependants
		foreach( Hashtable child in EB.Dot.Array("deps", bundle, new ArrayList()) ) 
		{
			BuildBundle( child, parent, options );	
		}
		
		BuildPipeline.PopAssetDependencies();
		
		return result;
	}
	
	private static bool BuildScenesBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		string folder 		= EB.Dot.String("folder", bundle, string.Empty);
		string filter 		= EB.Dot.String("filter",bundle,  string.Empty);
		string id 		= EB.Dot.String("id",bundle,  string.Empty);
		bool recursive 		= EB.Dot.Bool("recursive",bundle,  true );
		
		bool result 		= false;
		
		string[] buildScenes = BuildSettings.GetScenesFromEditorSettings();
		var tmp = new List<string>(buildScenes);
		var buildSceneNames = EB.ArrayUtils.Map<string,string>(tmp, Path.GetFileNameWithoutExtension);
		
		if (!System.IO.Directory.Exists(folder))
		{
			Debug.LogWarning("BuildScenesBundle - Folder does not exist : "+folder);
			return result;
		}
		
		string[] items = Directory.GetFiles(folder, "*.unity", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		
		List<string> devLevels = new List<string>((string[])BuildSettings.DevelopmentLevelsBuild.ToArray(typeof(string)));
		devLevels = EB.ArrayUtils.Map<string, string>(devLevels, 
		                                              delegate(string path) 
		                                              {
			string sceneName = Path.GetFileNameWithoutExtension(path).ToLower();
			foreach (string testName in BuildSettings.PlayerBuiltInScenes)
			{
				if (sceneName.Contains(testName))
				{
					return null;
				}
			}
			return sceneName;
		});
		
		
		
		var filtered = new List<string>();
		var arrayList= new ArrayList();
		foreach ( string item in items )
		{
			
			Debug.Log("item: "+item+" "+ (BuildSettings.IsDevelopmentBuild ? "YES" : "NO") );
			if ( !string.IsNullOrEmpty(filter) )
			{
				if ( item.Contains(filter) == false  )
				{
					EB.Debug.Log("Skipping scene because its in the build settings " + item);
					continue;
				}
			}			
			
			string sceneName = Path.GetFileNameWithoutExtension(item);
			string sceneNameLower = sceneName.ToLower();
			
			if (BuildSettings.IsDevelopmentBuild &&  (BuildSettings.DevBundleMode == EB.Assets.DevBundleType.BundleServer) 
			    && (BuildSettings.Target == BuildTarget.Android || BuildSettings.Target == BuildTarget.iPhone))	// build everything except the basic transition scenes as separate bundles for faster turn around when authoring/debugging.
			{
				if (!devLevels.Contains(sceneNameLower))
				{
					continue;
				}
			}
			else if (buildSceneNames.Contains(sceneName))
			{
				Debug.Log("Skipping scene because its in the build settings " + item);
				continue;
			}
			
			filtered.Add(item);
			arrayList.Add(sceneName);
		}
		
		if ( EB.Dot.Bool("single_bundle", bundle, false) )
		{
			
			var bundle_name = string.Format("scene_{0}.assetbundle", id);
			var bundle_path = Path.Combine( _outFolder, bundle_name );
			
			var deps = AssetDatabase.GetDependencies(items);
			if ( (options & BundleOptions.Force) != 0 || CheckDependencies(bundle_path, deps)  )
			{
				// build
				EB.Debug.Log("Building " +items.Length + " scenes in one");
				BuildPipeline.PushAssetDependencies();
				BuildPipeline.BuildStreamedSceneAssetBundle( filtered.ToArray(), bundle_path, BuildSettings.Target );	        
				BuildPipeline.PopAssetDependencies();
				result = true;
			}
			long ts = File.GetLastWriteTime(bundle_path).Ticks;
			Hashtable sceneTSLookup = new Hashtable();
			foreach( string scene in arrayList)
			{
				sceneTSLookup[scene] = ts;
			}
			_scenes[id] = sceneTSLookup;
		}
		else
		{
			int itemsCount = filtered.Count;
			for ( int i = 0; i < itemsCount; i++)
			{
				string shortname = (string)arrayList[i];
				string item = filtered[i];
				
				var bundle_name = string.Format("scene_{0}.assetbundle", shortname);
				var bundle_path = Path.Combine( _outFolder, bundle_name );
				
				var deps = AssetDatabase.GetDependencies( new string[] {item} );
				
				// check dependancies			
				if ((options & BundleOptions.Force) != 0 || CheckDependencies(bundle_path, deps) )
				{
					// build
					EB.Debug.Log("Building scene " + shortname + " " +bundle_path);
					BuildPipeline.PushAssetDependencies();
					
					string error = null;
					error = BuildPipeline.BuildStreamedSceneAssetBundle( new string[]{item}, bundle_path, BuildSettings.Target );
					
					if (!string.IsNullOrEmpty(error))
					{
						Debug.LogError("Error during BuildPipeline.BuildStreamedSceneAssetBundle "+item+" error: "+error);
					} else if (!File.Exists(bundle_path)) 
					{
						Debug.LogError("No Error returned, but no bundle created: "+bundle_path);
					}
					BuildPipeline.PopAssetDependencies();
					EditorUtility.UnloadUnusedAssets();
					
					result = true;
				}
				
				long ts = File.GetLastWriteTime(bundle_path).Ticks;
				
				Hashtable sceneTSLookup = new Hashtable();
				sceneTSLookup[shortname] = ts;
				_scenes[shortname] = sceneTSLookup;				
				
			}
			
		}
		
		
		return result;
	}
	
	private static void CheckNPOT( string path )
	{
		var asset = AssetDatabase.LoadMainAssetAtPath(path);
		if ( asset is Texture2D )
		{
			var ti = (TextureImporter)TextureImporter.GetAtPath(path);
			if (ti.npotScale != TextureImporterNPOTScale.None)
			{
				ti.npotScale = TextureImporterNPOTScale.None;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				asset = AssetDatabase.LoadMainAssetAtPath(path);
			}
			
			var text2D = (Texture2D)asset;
			if ( !Mathf.IsPowerOfTwo(text2D.width) || !Mathf.IsPowerOfTwo(text2D.height) )
			{
				Selection.activeObject = asset;
				throw new System.Exception("Texture at path " + path + " is NPOT!"); 
			}
		}
	}
	
	private static bool BuildCopyBundle( Hashtable bundle, string parent, BundleOptions options ) 
	{
		string id 			= EB.Dot.String("id",bundle,  string.Empty );
		string folder 		= EB.Dot.String("folder", bundle, string.Empty);
		string pathRoot 	= EB.Dot.String("relpath", bundle, folder);
		string filter 		= EB.Dot.String("filter",bundle,  string.Empty);
		bool recursive 		= EB.Dot.Bool("recursive",bundle,  true );
		
		string targetFolder = Path.Combine( _outFolder, id );
		if (!Directory.Exists(targetFolder))
		{
			Directory.CreateDirectory(targetFolder);
		}
		
		// Interrogate ALL found objects in the source path and all sub-directories
		List<string> paths = new List<string>();
		
		string[] items = {};
		if (Directory.Exists(folder))
		{
			items = Directory.GetFiles(folder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}
		else
		{
			Debug.LogWarning("BuildCopyBundle folder does not exist: "+folder);
		}
		
		foreach ( string item in items )
		{
			// never copy anything but 
			if ( !(item.EndsWith(".png" ) || item.EndsWith(".jpg") || item.EndsWith(".ogg") || item.EndsWith(".mp3") || item.EndsWith(".mp4") ) )
			{
				continue;
			}
			
			if ( !string.IsNullOrEmpty(filter) )
			{
				if ( item.Contains(filter) == false )
				{
					continue; // skip asset
				}
			}
			
			CheckNPOT(item);
			
			string path = item.Substring( pathRoot.Length + 1 );
			paths.Add( Path.Combine(id,path).Replace("\\","/") );
			
			string dest = Path.Combine(targetFolder, path);
			string dir = Path.GetDirectoryName(dest);
			
			
			if (File.Exists(dest) && File.GetLastWriteTime(dest) > File.GetLastWriteTime(item) )
			{
				// skip this file it's already been handled.
				continue;
			}
			
			Directory.CreateDirectory(dir);
			
			if (File.Exists(dest))
			{
				if(Application.platform == RuntimePlatform.WindowsEditor)
					CommandLineUtils.RunWindowsSyncCommand("attrib.exe", "-R " + dest);
				else if(Application.platform == RuntimePlatform.OSXEditor)
					CommandLineUtils.Run("chmod", "777 " + dest);
			}
			
			File.Copy( item, dest, true ); 
		}
		
		_folders.Add(id, paths.ToArray());
		
		return false;
	}
	
	private static bool BuildFolderBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		string id = EB.Dot.String("id",bundle, string.Empty );
		string folder = EB.Dot.String("folder",bundle, string.Empty);
		string subfolder = EB.Dot.String("subfolder",bundle, string.Empty);
		
		// build all the sub directories
		string subFolderPath = Path.Combine(folder,subfolder);
		
		bool result = false;
		
		// 1. build the root bundle
		{
			BuildPipeline.PushAssetDependencies();
			
			Hashtable rootBundle = new Hashtable();
			rootBundle["id"] = id;
			rootBundle["folder"] = subFolderPath;
			rootBundle["relpath"] = folder;
			rootBundle["recursive"] = false;
			result |= BuildStandardBundle(rootBundle,parent,options);
			
			BuildPipeline.PopAssetDependencies();
		}
		// 2. build the subfolders
		DirectoryInfo dir = new DirectoryInfo(subFolderPath);
		foreach( DirectoryInfo subDir in dir.GetDirectories() )
		{
			BuildPipeline.PushAssetDependencies();
			
			Hashtable subBundle = new Hashtable();
			subBundle["id"] = id + "_" + subDir.Name.ToLower();
			subBundle["folder"] = Path.Combine(subFolderPath,subDir.Name);
			subBundle["relpath"] = folder;
			subBundle["recursive"] = true;
			result |= BuildStandardBundle(subBundle,parent,options);
			
			BuildPipeline.PopAssetDependencies();
		}
		
		return result;
	}
	
	// returns true if need to build
	private static bool CheckDependencies( string bundlePath, Object[] objectList ) 
	{
		if ( !File.Exists(bundlePath) )
		{
			EB.Debug.Log(bundlePath + " does not exist, need to build" );
			return true;
		}
		
		FileInfo info = new FileInfo(bundlePath);
		if ( info.LastWriteTime < _configTime )
		{
			EB.Debug.Log(bundlePath + " config file changed, requires build");
			return true;
		}
		
		foreach ( Object obj in objectList )
		{
			var deps = EditorUtility.CollectDependencies( new Object[1]{obj} );
			foreach( var dep in deps )
			{
				var assetPath = AssetDatabase.GetAssetPath(dep);
				if ( assetPath.Length > 0 && !(dep is MonoScript) ) // ignore code changes
				{
					string guid = AssetDatabase.AssetPathToGUID(assetPath);
					
					System.DateTime modified;
					
					if ( !_modifiedTimes.TryGetValue(guid, out modified) )
					{
						EB.Debug.LogError("Asset: " + AssetDatabase.GetAssetPath(obj) + " depends on " + assetPath + " which is an invalid asset" );
						continue;
					}
					
					System.DateTime modifiedFile = File.GetLastWriteTime(assetPath);
					System.DateTime modifiedMeta = modifiedFile;
					string metaFile = assetPath+".meta";
					if (File.Exists(metaFile))
					{
						modifiedMeta = File.GetLastWriteTime(metaFile);
					}
					
					System.DateTime modifiedUse = (modifiedMeta > modifiedFile) ? modifiedMeta : modifiedFile;
					
					if ( modifiedUse > info.LastWriteTime )
					{
						Debug.Log(assetPath + " is newer than " + bundlePath + ", need to build" );	
						
						return true;
					}
				}
			}
		}
		return false;
	}
	
	private static bool CheckDependencies( string bundlePath, string[] depPaths ) 
	{
		if ( !File.Exists(bundlePath) )
		{
			EB.Debug.Log(bundlePath + " does not exist, need to build" );
			return true;
		}
		
		FileInfo info = new FileInfo(bundlePath);
		if ( info.LastWriteTime < _configTime )
		{
			EB.Debug.Log(bundlePath + " config file changed, requires build");
			return true;
		}
		
		foreach( var assetPath in depPaths )
		{
			if ( assetPath.Length > 0 && !assetPath.EndsWith(".cs") ) // ignore code changes
			{
				string guid = AssetDatabase.AssetPathToGUID(assetPath);
				
				System.DateTime modified;
				if ( !_modifiedTimes.TryGetValue(guid, out modified) )
				{
					EB.Debug.LogError("Failed to get modified time for asset: " + assetPath);
					continue;
				}
				
				if ( modified > info.LastWriteTime )
				{
					EB.Debug.Log(assetPath + " is newer than " + bundlePath + ", need to build" );	
					return true;
				}
			}
		}
		return false;
	}
	
	private static bool BuildStandardBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		string id 		= EB.Dot.String( "id", bundle, string.Empty );
		string folder 	= EB.Dot.String("folder", bundle, string.Empty);
		string pathRoot = EB.Dot.String("relpath", bundle, null);
		if (pathRoot == null)
		{
			pathRoot = folder;
			if (pathRoot.ToLower().StartsWith("assets/resources/bundles"))
			{
				pathRoot = "Assets/Resources";
			}
			else if (pathRoot.ToLower().StartsWith("assets/bundles"))
			{
				pathRoot = "Assets";
			}
		}
		
		string filter 	= EB.Dot.String("filter", bundle, string.Empty);
		string prefix	= EB.Dot.String("prefix", bundle, string.Empty);
		bool recursive 	= EB.Dot.Bool("recursive", bundle, true );
		bool shortNames = EB.Dot.Bool("shortnames", bundle, false );
		bool gatherComplete = EB.Dot.Bool("gather_complete", bundle, false);
		bool shaders		= EB.Dot.Bool ("shaders", bundle, false);
		
		return BuildStandardBundle(id, folder, pathRoot, filter, prefix, recursive, shortNames, gatherComplete, shaders, parent, options);
	}
	
	public static bool BuildStandardBundle( string id, string folder, string pathRoot, string filter, string prefix, bool recursive, bool shortNames, bool gatherComplete, bool shaders, string parent, BundleOptions options )
	{
		string targetFolder = _outFolder;
		if (!Directory.Exists(targetFolder))
		{
			try 
			{
				Directory.CreateDirectory(targetFolder);
			}
			catch (System.Exception e)
			{
				Debug.Log(e.ToString());
				throw;
			}
		}
		
		List<Object> objectList = new List<Object>();
		List<string> nameList = new List<string>();
		List<string> pathList = new List<string>();
		
		List<DeltaBundleInfo> deltaBundleList = new List<DeltaBundleInfo>();
		
		string[] items = {};
		
		if (System.IO.File.Exists(folder))
		{
			items = new string[] { folder };
		}
		else if (System.IO.Directory.Exists(folder))
		{
			// Interrogate ALL found objects in the source path and all sub-directories
			items = Directory.GetFiles(folder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}
		
		string path = Path.Combine( _outFolder, string.Format("{0}.assetbundle", id) );	//e.g. "/Users/User/perforce/ff7/main/Unity/android/download/audio.assetbundle"
		
		if (!BuildSettings.IsDevelopmentBuild)
		{
			// disable delta builds for non development builds.
			options &= ~BundleOptions.DeltaOnly;
		}
		
		#if ENABLE_DELTA_BUNDLES	// if enabled will auto-switch to delta bundles 
		if (BuildSettings.IsDevelopmentBuild && (options & BundleOptions.Force) == 0)
		{
			if ((options & BundleOptions.DeltaOnly) == 0 && File.Exists(path))
			{
				// Not forcing & the original bundle exists, we'll switch to just building Delta bundles.
				Debug.Log("Bundle already exists, switching to create Delta Bundles.");
				options |= BundleOptions.DeltaOnly;
			}
		}
		#endif
		
		BuildAssetBundleOptions buildAssetBundleOptions = _bundleOptions | ( (options & BundleOptions.Uncompressed) != 0 ? BuildAssetBundleOptions.UncompressedAssetBundle : 0);
		buildAssetBundleOptions |= (gatherComplete ? BuildAssetBundleOptions.CompleteAssets : 0);
		
		EB.Debug.Log("Target - " + BuildSettings.Target + ", AssetBundleOptions - " + buildAssetBundleOptions);
		
		int itemsCount = items.Length;
		
		long lastModified = 0;
		if (File.Exists(path))
		{
			lastModified = File.GetLastWriteTime(path).Ticks;
			if (itemsCount < 30 && EB.Flag.IsSet(options, BundleOptions.DeltaOnly))
			{
				// only use delta bundles for large bundles with many files.
				options = EB.Flag.Unset(options, BundleOptions.DeltaOnly);
			}
		}
		else
		{
			if (EB.Flag.IsSet(options,BundleOptions.DeltaOnly))
			{
				Debug.LogError("Can't generate delta bundles, main bundle ("+path+") does not exist. Generating main bundle instead.");
				options = EB.Flag.Unset(options, BundleOptions.DeltaOnly);
			}
		}
		
		int bundleRelPathLength = folder.ToLower().IndexOf("bundles/");		// e.g. folder: "Assets/Resources/Bundles/audio"
		if (bundleRelPathLength < 0)
		{
			bundleRelPathLength = 0;
		}
		
		// Let's see what we've got
		foreach (string s in items)
		{
			if (s.ToLower().EndsWith(".meta") || (!string.IsNullOrEmpty(filter) && s.Contains(filter) == false))
			{
				continue; // skip asset
			}
			
			UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(s);
			
			if (obj != null && obj.GetType() == typeof(UnityEngine.Object) )
			{
				var tmp = AssetDatabase.LoadAllAssetsAtPath(s);
				obj = null;
				for ( int i = 0; i < tmp.Length; ++i )
				{
					if (tmp[i] != null && tmp[i].GetType() != typeof(UnityEngine.Object) && !string.IsNullOrEmpty(tmp[i].name) && s.Contains(tmp[i].name))
					{
						//EB.Debug.Log ("Loading "+s+" option type: "+(tmp[i] != null ? tmp[i].GetType().ToString() : "NULL OBJ" ));
					}
				}
				
				for ( int i = 0; i < tmp.Length; ++i )
				{
					if (tmp[i] != null && tmp[i].GetType() != typeof(UnityEngine.Object) && !string.IsNullOrEmpty(tmp[i].name) && s.Contains(tmp[i].name))
					{
						//EB.Debug.Log ("Loaded "+s+" type: "+(tmp[i].GetType() != null ? tmp[i].GetType().ToString() : "NULL OBJ" ));
						obj = tmp[i];
						break;
					}
				}
				
				if (obj == null)
				{
					EB.Debug.Log("No asset loaded for " + s);
				}
			}
			else if (obj != null)
			{
				//EB.Debug.Log ("Loaded "+s+" type: "+obj.GetType().ToString());
			}
			
			
			if ( obj == null || (obj is MonoScript)  )
			{
				continue;
			}
			
			string p = s.Substring( pathRoot.Length + 1 );
			int ext = p.LastIndexOf('.');
			if ( ext > 0 )
			{
				p = p.Substring(0,ext);
			}
			p = prefix + p.ToLower();
			
			if ( shortNames )
			{
				p = Path.GetFileNameWithoutExtension(p);
			}
			
			nameList.Add( AssetName(p) );
			pathList.Add( PathName(p) );
			objectList.Add(obj);
			
			if (EB.Flag.IsSet(options, BundleOptions.DeltaOnly))
			{
				string fileBundleRelPath = s.Substring(bundleRelPathLength);	// "Assets/Resources/Bundles/audio/engineSound/OM_V10 Lvl 1.txt" -> "audio/engineSound/OM_V10 Lvl 1.txt"
				string bundleNameBase = Path.GetFileNameWithoutExtension(fileBundleRelPath.ToLower().Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_').Replace(' ', '_')); // "audio_enginesound_om_v10_lvl_1"
				string bundleFileName = bundleNameBase+".assetBundle";
				
				string singleBundlePath = Path.Combine(BuildSettings.BuildFolder, bundleFileName);
				
				string checkPath = path;
				
				Object[] singleObj = { obj };
				
				if (File.Exists(singleBundlePath))
				{
					long lastBundleTS = File.GetLastWriteTime(singleBundlePath).Ticks;
					if (lastBundleTS > lastModified )
					{
						checkPath = singleBundlePath;
					}
					_deltaBundleContents[bundleNameBase] = lastBundleTS;
				}
				
				if (CheckDependencies(checkPath, singleObj))
				{
					DeltaBundleInfo deltaBundleInfo = new DeltaBundleInfo();
					deltaBundleInfo.bundleNameBase = bundleNameBase;
					deltaBundleInfo.path = path;
					deltaBundleInfo.relPath = p;
					deltaBundleInfo.singleBundlePath = singleBundlePath;
					deltaBundleInfo.objList = singleObj;
					
					deltaBundleList.Add(deltaBundleInfo);
				}
				int deltaBundleListCount = deltaBundleList.Count;
				if (deltaBundleListCount > itemsCount/3 || deltaBundleListCount > 20)	// if 1/3 of this bundle has changed, just rebuild the bundle.
				{
					Debug.LogWarning("Too many changes to assetBundle, will just rebuild the whole thing.");
					// okay we have too many changes, let's just build the full bundle.
					// force build the bundle, no checking dependencies we already know there's changes
					options = EB.Flag.Unset(options, BundleOptions.DeltaOnly);
				}
			}
			
		}
		
		if (EB.Flag.IsSet(options, BundleOptions.DeltaOnly))	// could have been delta bundles only but then reset above to build a normal bundle.
		{
			foreach( DeltaBundleInfo dbi in deltaBundleList )
			{
				string deltaPath = dbi.path;
				string p = dbi.relPath;
				string singleBundlePath = dbi.singleBundlePath;
				string bundleNameBase = dbi.bundleNameBase;
				
				BuildPipeline.BuildAssetBundleExplicitAssetNames(dbi.objList, new string[] { AssetName(p) }, singleBundlePath, buildAssetBundleOptions, BuildSettings.Target);
				
				string tocFileName = Path.Combine(BuildSettings.BuildFolder, bundleNameBase+"_toc.txt");
				
				// build the toc
				Hashtable deltatoc = new Hashtable();
				FileInfo deltafi = new FileInfo( deltaPath );
				deltatoc["paths"] = new string[] { PathName(p) };
				deltatoc["parent"]= "";
				deltatoc["size" ] = deltafi.Length;
				deltatoc["ts"] = deltafi.LastWriteTime.Ticks;
				
				#if UNITY_ANDROID
				deltatoc["hash"] = CalculateFileHash(deltafi);
				#endif
				
				_bundles[bundleNameBase] = deltatoc;
				Bundler.WriteTOC(BuildSettings.BuildFolder, tocFileName, bundleNameBase);
				_bundles.Remove(bundleNameBase);
				
				_deltaBundleContents[bundleNameBase] = File.GetLastWriteTime(singleBundlePath).Ticks;
			}
			
		}
		else
		{		
			if ( objectList.Count == 0 )
			{
				Debug.Log("Skipping empty bundle, id: "+id);
				return false;
			}
			
			if ( (options & BundleOptions.Force) != 0 || CheckDependencies(path, objectList.ToArray()) )
			{
				// build
				bool result = BuildPipeline.BuildAssetBundleExplicitAssetNames(objectList.ToArray(), nameList.ToArray(), path, buildAssetBundleOptions, BuildSettings.Target);
				EB.Debug.Log("BuildPipeline.BuildAssetBundleExplicitAssetNames : BundleID: "+id+" : "+(result ? "SUCCESSFUL " : "FAILED"));
				
			}
		}
		
		
		// build the toc
		Hashtable toc = new Hashtable();
		FileInfo fi = new FileInfo( path );
		toc["paths"] = pathList.ToArray();	
		toc["parent"]= parent;
		toc["size" ] = fi.Length;
		toc["ts"] = fi.LastWriteTime.Ticks;
		
		#if UNITY_ANDROID
		toc["hash"] = CalculateFileHash(fi);
		#endif
		
		_bundles[id] = toc;	
		
		return true;
	}
	
	private static int CalculateFileHash( FileInfo info )
	{
		var stream = info.OpenRead();
		byte [] buffer = new byte[stream.Length];
		stream.Read (buffer, 0, (int)stream.Length);
		stream.Close ();
		return EB.Hash.FNV32(buffer, (int)EB.Hash.HASH_INIT_32);
	}
	
	private static bool BuildSingleBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		string id = EB.Dot.String("id", bundle, string.Empty);
		string folder = EB.Dot.String("folder", bundle, string.Empty);
		string prefix = EB.Dot.String("prefix", bundle, string.Empty);
		string filter = EB.Dot.String("filter", bundle, string.Empty);
		
		string targetFolder = Path.Combine( _outFolder, id);
		Directory.CreateDirectory(targetFolder);
		
		string[] files = Directory.GetFiles(folder, "*.prefab", SearchOption.AllDirectories );
		System.Array.Sort(files);
		
		bool result = false;
		
		foreach (string file in files)
		{
			if ( !string.IsNullOrEmpty(filter) )
			{
				if (!file.Contains(filter))
				{
					continue; // skip asset
				}
			}
			
			Object obj = AssetDatabase.LoadMainAssetAtPath(file);
			if (obj != null && obj is GameObject)
			{
				string name = obj.name.ToLower().Replace(' ', '_');
				string path = Path.Combine(targetFolder, string.Format("{0}.assetbundle", name));
				
				var objects	= new Object[] { obj };
				var names = new string[] { AssetName(prefix + obj.name) };
				var paths = new string[] { PathName(prefix + obj.name) };
				
				if ((options & BundleOptions.Force) != 0 || CheckDependencies(path,objects))
				{
					BuildPipeline.PushAssetDependencies();
					BuildPipeline.BuildAssetBundleExplicitAssetNames(objects, names, path, _bundleOptions, BuildSettings.Target);
					BuildPipeline.PopAssetDependencies();
					result = true;
				}
				
				// add to the toc
				Hashtable toc = new Hashtable();
				toc["paths"] = paths;	
				toc["parent"]= parent;
				toc["ts"] = File.GetLastWriteTime(file).Ticks;
				_bundles[id + "/"  + name] = toc;
			}
		}	
		
		return result;
	}
	
	private static bool BuildOggBundle( Hashtable bundle, string parent, BundleOptions options )
	{
		if ( (options & BundleOptions.SkipOgg) != 0 )
		{
			EB.Debug.Log("Skipping Ogg");
			return true;
		}
		
		string id 		= EB.Dot.String("id", bundle, string.Empty );
		string folder 	= EB.Dot.String("folder", bundle, string.Empty);
		
		string targetFolder = Path.Combine( _outFolder, id);
		Directory.CreateDirectory(targetFolder);
		
		if (Directory.Exists(folder))
		{
			return false;
		}
		
		string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories );
		System.Array.Sort(files);
		
		foreach( var file in files )
		{
			Object obj = AssetDatabase.LoadMainAssetAtPath(file);
			if ( obj != null)
			{
				string name = obj.name.ToLower().Replace(' ','_');
				string path = Path.Combine( targetFolder, string.Format("{0}.ogg", name) );
				
				if ( (options & BundleOptions.Force) != 0  || CheckDependencies(path,new Object[]{obj})  )
				{
					if ( !EditorUtility.ExtractOggFile(obj,path) )
					{
						EB.Debug.LogError("Failed to build ogg file: " + path);
					}
				}				
			}
		}
		
		return false;
	}
	
	
}
