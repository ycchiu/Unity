//#define EMULATE_DOWNLOAD_SPEED
//#define UNLOAD_SINGLE_ASSET_BUNDLES
//#define ENABLE_LOADING_DEBUG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB
{

	public static class Assets  
	{
		
#region DevBundling
		public enum DevBundleType
		{
			Unset = -1,
			BundleServer = 0,			
			NoBundles = 1,					
			StandardBundles = 2
		}
	
		private static string 	_bundleCacheServerAddress = null;
		
		public static bool 		BundleCacheServerEnabled 
		{
			get 
			{
				return (CurrentBundleMode == DevBundleType.BundleServer && !string.IsNullOrEmpty(BundleCacheServerAddress));
			}
		}
		
#if true
		private static void ReadBuildConfig()
		{
			// Bit of a hack to use DoReadBuildConfig w/ WWW to extract the BuildConfig.txt from the apk/package... WWW doesn't seem to work correctly this way w/ IOS though
			IEnumerator iter = DoReadBuildConfig();
			do{} while(iter.MoveNext());
		}
		

		private static IEnumerator DoReadBuildConfig()
		{
			if (!UnityEngine.Debug.isDebugBuild)
			{
				_currentBundleMode = DevBundleType.NoBundles;
				_bundleCacheServerAddress = "";
			}
			else
			{
				string path = Path.Combine(Loader.DataPath, "BuildConfig.txt");
#if UNITY_EDITOR
				if (UnityEditor.EditorApplication.isPlaying)
				{
					path = Path.Combine(Loader.DataPath, "../BuildConfig.txt");
				}
#endif
			
				EB.Debug.Log("Loading buildconfig from "+path);
				
#if UNITY_IOS
				// Using www didn't seem to work for IOS when we call it w/ the MoveNext() method in ReadBuildConfig
				string urlMatcher = "://";
				int urlIndex = path.IndexOf(urlMatcher);
				if (urlIndex != -1)
				{
					path = path.Substring(urlIndex + urlMatcher.Length);
				}
				
				string text = null;
				if (File.Exists(path))
				{
					text = File.ReadAllText(path);	
				}
				else
				{
					EB.Debug.LogError("Didn't find BuildConfig.txt!");
				}
				yield return 1; // needed to satisfy IEnumerator method signature
#else
				
				WWW textAssetLoader = new WWW(path);
				
				int count = 0;
				while(!textAssetLoader.isDone && string.IsNullOrEmpty(textAssetLoader.error))
				{
					count++;
					if (count > 100) { EB.Debug.LogError("***** FAILED TO READ BuildConfig.txt *****");}
					yield return textAssetLoader;
				}
				
				string text = null;
				if (!string.IsNullOrEmpty(textAssetLoader.error)) 
				{
					EB.Debug.LogError("Error loading build config.txt");
				}
				else
				{
					text = textAssetLoader.text;
				}
#endif
				
				EB.Debug.Log("FINISHED Loading buildconfig from "+path);
									
				if (!string.IsNullOrEmpty(text))
				{
					Hashtable bcConfig = (Hashtable)EB.JSON.Parse(text);
					_currentBundleMode = (DevBundleType)EB.Dot.Integer("BundleMode", bcConfig, (int)DevBundleType.NoBundles);
					if (UnityEngine.Debug.isDebugBuild && (_currentBundleMode == DevBundleType.BundleServer)) 
					{
						_bundleCacheServerAddress = EB.Dot.String("BundleServerAddress", bcConfig, "");
					}
					else
					{
						_bundleCacheServerAddress = "";
					}
				}
				else
				{
					Debug.LogWarning("Wasn't able to read bundle mode. Defaulting to NoBundles, No BundleServerAddress");
					_currentBundleMode = DevBundleType.NoBundles;
					_bundleCacheServerAddress = "";
				}
#if UNITY_EDITOR
				if (_currentBundleMode == DevBundleType.BundleServer) 
				{
					_currentBundleMode = DevBundleType.StandardBundles;
				}
#endif				
				EB.Debug.Log("Current bundlemode: "+_currentBundleMode.ToString());
			}
		}
#else
		private static void ReadBuildConfig()
		{
			if (!UnityEngine.Debug.isDebugBuild)
			{
				_currentBundleMode = DevBundleType.StandardBundles;
				_bundleCacheServerAddress = "";
			}
			else
			{
				TextAsset textAsset = EB.Assets.Load<TextAsset>("BuildConfig");
				if (textAsset != null && textAsset.text != null)
				{
					Hashtable bcConfig = (Hashtable)EB.JSON.Parse(textAsset.text);
					_currentBundleMode = (DevBundleType)EB.Dot.Integer("BundleMode", bcConfig, (int)DevBundleType.NoBundles);
					if (UnityEngine.Debug.isDebugBuild && (_currentBundleMode == DevBundleType.BundleServer)) 
					{
						_bundleCacheServerAddress = EB.Dot.String("BundleServerAddress", bcConfig, "");
					}
					else
					{
						_bundleCacheServerAddress = "";
					}
				}
				else
				{
					Debug.LogWarning("Wasn't able to read bundle mode. Defaulting to NoBundles, No BundleServerAddress");
					_currentBundleMode = DevBundleType.NoBundles;
					_bundleCacheServerAddress = "";
				}
			}
		}
#endif
		
		
		private static DevBundleType _currentBundleMode = DevBundleType.Unset;
		public static DevBundleType CurrentBundleMode 
		{
			get 
			{
				if (_currentBundleMode == DevBundleType.Unset)
				{
					ReadBuildConfig();
				}
				return _currentBundleMode;
			}
		}
		
		public static string 	BundleCacheServerAddress
		{
			get 
			{
				if (_bundleCacheServerAddress == null)
				{			
					ReadBuildConfig();
				}
				return _bundleCacheServerAddress;
			}
		}
#endregion
		
		private class AssetPack
		{
			public string 	id;
			public string 	folder;
			public string 	version;
			public List<AssetBundleInfo> bundles;
			public Hashtable folders;
		}
		
		private class AssetBundleInfo
		{
			public AssetPack   pack;
			public AssetBundle bundle;
			public string id;
			public int retry;
			public bool uncompressed = false;
			
			public string[] paths;
			public string parent;
			public long size;
			
			public int asyncLoadingCount = 0;
			public bool unload = false;
			public int hash;
			
			public long ts = 0;
					
			public bool isLoaded { get { return bundle != null;} }
		};
		
		private class AssetSceneInfo
		{
			public AssetPack 	pack;
			public string 	 	id;
			public long 		ts = 0;
			
		}
		
		private static Dictionary<string,AssetBundleInfo>	_bundles = new Dictionary<string,AssetBundleInfo>();
		private static Dictionary<string,AssetBundleInfo> 	_pathToBundle = new Dictionary<string, AssetBundleInfo>();
		private static Dictionary<string,WWW>				_www = new Dictionary<string, WWW>();
		private static Dictionary<string,AssetPack>			_packs = new Dictionary<string, AssetPack>();
		private static Dictionary<string,AssetSceneInfo> 	_scenes = new Dictionary<string, AssetSceneInfo>();
		
		
		private static List<AssetBundleInfo>				_unloads = new List<AssetBundleInfo>();
		private static AsyncOperation						_cleanupTask = null;
		
		private static GameObject							_missingObject;
		private static Texture2D							_missingTexture;
		
		private static List<string> 						_dlcPacks = new List<string>();
		
		private static AsyncOperation						_loadLevelOperation = null;
		public static AsyncOperation						LoadLevelOperation
		{
			get
			{
				return _loadLevelOperation;
			}
		}
		
		// This callback lets the caller know when loading level is ready to activate
		public delegate void DelayReadyCallback();
		
		public static string ContentVersion
		{
			get
			{
				string version = "";
				List<string> packs = new List<string>();
				foreach( var p in  _packs.Keys )
				{
					packs.Add(p);
				}
				packs.Sort();
				foreach( string pack in packs )
				{
					var info = _packs[pack];
					version += string.Format("/{0}:{1}",info.id, info.version);
				}
				return version;
			}
		}
		
		public static string[] Scenes
		{
			get
			{
				var tmp = new string[_scenes.Count];
				_scenes.Keys.CopyTo(tmp, 0);
				System.Array.Sort(tmp);
				return tmp;
			}
		}
		
		public static string FindDiskAsset( string path )
		{
			var index = path.IndexOfAny(new char[]{'/','\\'});
			var folder = path;
			if ( index >= 0 )
			{
				folder = path.Substring(0,index);
			}
			
			foreach( var pack in _packs )
			{
				var paths = Dot.Array(folder, pack.Value.folders, null);
				if ( paths != null )
				{
					foreach( string p in paths )
					{
						if (p.StartsWith(path))
						{
							return Loader.GetBaseURL(pack.Key)+p;
						}
					}	
				}
			}
			EB.Debug.LogWarning("Failed to find asset for path " + path);
			return string.Empty;
		}
				
		public static string AssetName( string path )
		{
			path = path.Replace('\\', '/').ToLower();
			return Hash.StringHash(path).ToString();
		}
		
		public static void AddDlcPacks( string[] packs )
		{
			foreach( var pack in packs )
			{
				_dlcPacks.Add(pack);
			}
		}
		
		public static bool HasDLCPack(string packName)
		{
			return _dlcPacks.Contains(packName);
		}
		
		public static bool HasPack(string packName)
		{
			return _packs.ContainsKey(packName);
		}
		
		public static Coroutine LoadPacks(bool mount, Action<bool> cb)
		{
			return StartCoroutine(DoLoadPacks(mount,cb));
		}
		
		private static IEnumerator DoLoadPacks(bool mount, Action<bool> cb) 
		{
			var dataPath = Loader.DataPath;
			
			string text = string.Empty;

			Loader.CachedLoadHandler loader = new Loader.CachedLoadHandler(Path.Combine(dataPath,"packs.txt"), false);
			yield return loader.Load();
			text = loader.text;
			Debug.Log("loader text: "+text);
						
			var packs = (ArrayList)JSON.Parse(text);
			if ( packs == null )
			{
				EB.Debug.LogError("FATAL: Failed to decode packs toc!: " + text );
				yield break;
			}
			
			// load the dlc packs first!
			foreach( var pack in _dlcPacks)
			{
				yield return LoadPack(pack.ToString());
			}
			
			// load standard packs
			foreach( var pack in packs )
			{
				yield return LoadPack(pack.ToString());
			}
			
			if (CurrentBundleMode == DevBundleType.NoBundles)
			{
				EB.Debug.Log("Skipping DoLoadPacks -- CurrentBundleMode is NoBundles");
				cb(true);
				yield break;
			}

			if (mount) 
			{
#if UNITY_ANDROID	
				if (Assets.CurrentBundleMode == Assets.DevBundleType.StandardBundles)
				{
					Dictionary<string,int> hashes = new Dictionary<string, int>();
	
					foreach( var pack in _packs.Values )
					{
						foreach( var bundle in pack.bundles)
						{
							var id = System.IO.Path.GetFileNameWithoutExtension(Loader.GetBundlePath(bundle.id));
							hashes[id] = bundle.hash;
						}
					}
	
	//				Debug.LogError("Bundle Hashes {0}", hashes);
							      
					if (dataPath.StartsWith("jar:"))
					{
						var done = false;
						var success = false;
						var filename = dataPath.Split('!')[0].Replace("jar:file://","");
						JarExtractor.ExtractAssetBundles( filename, hashes, delegate(bool s) {
							done = true;
							success = s;
						}); 
	
						while(!done) {
							System.Threading.Thread.Sleep( System.TimeSpan.FromMilliseconds(50) ); 
							yield return 1;
						}
	
						if (!success) {
							cb(false);
							yield break;
						}
					}
				}
#endif

				foreach( var pack in _packs.Values )
				{
					yield return StartCoroutine(DoLoadBundles(pack.bundles));
				}
			}

			cb(true);
			yield break;
		}
		
		public static Coroutine LoadPack(string name, bool mount = false)
		{
			return StartCoroutine(DoLoadPack(name, mount));
		}
		
		private static IEnumerator DoLoadPack(string name, bool mount, bool isDeltaBundle = false, long reqTimestamp = 0)
		{
			AssetPack pack = null;
			if (_packs.TryGetValue(name, out pack))
			{
				EB.Debug.Log("pack already loaded skipping " + name);
				yield break;
			}
			
			pack = new AssetPack();
			pack.id = name;
			
			// todo: find latest content pack folder
			pack.folder = Loader.GetBaseURL(name);
			
			var tocPath = pack.folder+"toc.txt";
			if (isDeltaBundle)
			{
				tocPath = pack.folder+pack.id+"_toc.txt";
			}

			tocPath = tocPath.Replace ("\\", "/");

			Loader.CachedLoadHandler loader = new Loader.CachedLoadHandler(tocPath, false);
			yield return loader.Load();
			string text = loader.text;

			Hashtable toc = (Hashtable)JSON.Parse( text );
			if ( toc == null )
			{
#if !UNITY_EDITOR
				EB.Debug.LogError("FATAL: Failed to decode asset toc!: " + text );
#endif				
				yield break;
			}

			_packs[name] = pack;
			
			// load folder
			pack.folders = EB.Dot.Object("folders", toc, new Hashtable() );
			
			if (CurrentBundleMode == DevBundleType.NoBundles)
			{
				yield break;
			}
			
			var scenes = EB.Dot.Object("scenes", toc, new Hashtable() );
			foreach( DictionaryEntry entry in scenes )
			{
				var id = entry.Key.ToString();
				Hashtable sceneTSLookup = (Hashtable)entry.Value;
				foreach( DictionaryEntry sceneEntry in sceneTSLookup )
				{
					Debug.Log("scenes entry: "+sceneEntry.Key.ToString()+":"+sceneEntry.Value.ToString());
					string scene = (string)entry.Key;
					long ts = (long)((double)sceneEntry.Value);

					var sceneInfo = new AssetSceneInfo();
					sceneInfo.pack = pack;
					sceneInfo.id = id;
					sceneInfo.ts = ts;
					_scenes.Add(scene, sceneInfo);
				}
			}
			
			Hashtable bundles = EB.Dot.Object("bundles", toc, new Hashtable() );
			pack.version = EB.Dot.String("version", toc, string.Empty);
			pack.bundles = new List<AssetBundleInfo>();
			
			bool uncompressed = EB.Dot.Bool("uncompressed", toc, false);
			
			ArrayList defaultPaths = new ArrayList();
			
			foreach( DictionaryEntry entry in bundles )
			{
				Hashtable bundle = null;
				try {
					bundle = (Hashtable)entry.Value;
				} catch (System.Exception e) {
					Debug.Log("exception "+e);
				}
				if (bundle != null)
				{
					AssetBundleInfo info = new AssetBundleInfo();
					
					info.pack = pack;
					info.id = entry.Key.ToString();
					info.parent = Dot.String( "parent",bundle, string.Empty);
					info.paths = (string[])Dot.Array("paths",bundle,defaultPaths).ToArray(typeof(string));
					info.size = Dot.Long( "size", bundle, 0 );
					info.hash = Dot.Integer("hash", bundle, 0);
					info.ts = (long)EB.Dot.Double("ts", bundle, 0);
					info.uncompressed = uncompressed;
					
					Debug.Log("BUNDLE TS: "+info.ts);

					_bundles[info.id] = info;	//NOTE: overriding existing
					
					// setup the lookup table
					foreach( string path in info.paths )
					{
						EB.Debug.Log("_pathToBundle: "+path);
						_pathToBundle[path] = info;

					}
					
					pack.bundles.Add(info);
					
				}
			}
			
			if (mount)
			{
				yield return StartCoroutine( DoLoadBundles(pack.bundles, isDeltaBundle, reqTimestamp) );
			}
			
			EB.Debug.Log("Loaded " + _bundles.Count + " bundles");
			EB.Debug.Log("Loaded " + _pathToBundle.Count + " assets");
			EB.Debug.Log(name + " Version: " + pack.version );
		}
		
		public static Coroutine LoadDeltaBundles()
		{
		
			return StartCoroutine(DoLoadDeltaBundles());
		}
		
		private static IEnumerator DoLoadDeltaBundles()
		{		
			if (UnityEngine.Debug.isDebugBuild && CurrentBundleMode != DevBundleType.NoBundles)
			{
				string deltaBundlesFilename = EB.Loader.DataPath +"/delta_bundles.txt";
	
				Loader.CachedLoadHandler loader = new Loader.CachedLoadHandler(deltaBundlesFilename, false, 0, 0, true);
				yield return loader.Load();
				string text = loader.text;
	
				if (!string.IsNullOrEmpty(text))
				{
					Hashtable bundles = (Hashtable)EB.JSON.Parse(text);
					foreach(DictionaryEntry bundle in bundles )
					{
						string bundleFileName = (string)bundle.Key;
						double sourceTime = (double)bundle.Value;
						
						yield return StartCoroutine(DoLoadPack(bundleFileName, true, true, (long)sourceTime));
	
					}
				}
			}
		}	
		
		public static AsyncOperation ProcessDeferedUnloads(bool runCleanup = true)
		{
			if ( _unloads.Count == 0 )
			{
				return _cleanupTask;
			}
			
			bool cleanup = false;
			for ( int i = 0; i < _unloads.Count; )
			{
				AssetBundleInfo info = _unloads[i];
				if ( info.isLoaded == false || info.unload == false )
				{
					_unloads.RemoveAt(i);
				}
				else if ( info.asyncLoadingCount == 0 )
				{
					EB.Debug.Log("***** UNLOADING BUNDLE: " + info.id);
					info.bundle.Unload(false);
					Object.DestroyImmediate(info.bundle,true);
					info.bundle = null;
					_unloads.RemoveAt(i);
					cleanup =true;
				}
				else
				{
					EB.Debug.LogWarning("Waiting for bundle to stop loading: " + info.id + " count:" + info.asyncLoadingCount );
					++i;
				}
			}
			
			if ( cleanup && runCleanup )
			{
				UnloadUnusedAssets();
			}
			return _cleanupTask;
		}
		
		public static AsyncOperation FlushAllAndUnload()
		{
			CoreAssets.Instance.Loaded.Clear();
			return UnloadUnusedAssets();
		}
		
		public static AsyncOperation UnloadUnusedAssets()
		{
			if ( _cleanupTask == null || _cleanupTask.isDone )
			{
				EB.Debug.Log("Running cleanup task");
				_cleanupTask = Resources.UnloadUnusedAssets();
			}
			return _cleanupTask;
		}

		// Debugging method to expose loaded assets.
		public static List<EB.Collections.Tuple<string, int>> GetUseCounts()
		{
			var results = new List<EB.Collections.Tuple<string, int>>();

			foreach (CoreAssets.Info assetInfo in CoreAssets.Instance.Loaded)
			{
				results.Add(new EB.Collections.Tuple<string, int>(assetInfo.path, assetInfo.refCount));
			}

			return results;
		}
			
		private static AssetBundleInfo GetBundleInfo(string id)
		{
			AssetBundleInfo info;
			if ( _bundles.TryGetValue(id.ToLower(), out info ) )
			{
				return info;
			}
			//EB.Debug.LogError("GETBUNDLEINFO FAILED ON {0}", id);
			return null;
		}
		
		private static Coroutine StartCoroutine(IEnumerator co)
		{
			return Coroutines.Run(co);
		}
		
		private static void GatherBundleLoadList( AssetBundleInfo info, List<AssetBundleInfo> loadList )
		{
			if ( info.isLoaded == false )
			{
				// load parent if needed
				if ( string.IsNullOrEmpty(info.parent) == false )
				{
					AssetBundleInfo parent = GetBundleInfo(info.parent);
					GatherBundleLoadList(parent, loadList);
				}
				
				// add the load
				if ( loadList.Contains(info) == false )
				{
					EB.Debug.Log("Need to load: " + info.id);
					loadList.Add(info);
				}
				
			//	PrintBundleLoadList(loadList);
			}
			else if ( info.unload == true )
			{
				EB.Debug.LogError("********************** WARNING: skipping unload of " + info.id + " because it was requested to load again" );
				// make sure not to unload
				info.unload = false;
			}
		}
		
		private static void PrintBundleLoadList(List<AssetBundleInfo> loadList)
		{
			EB.Debug.Log("Dumping bundle load list...");
			foreach(AssetBundleInfo i in loadList)
			{
				EB.Debug.Log("bundleInfo : {0}, {1}", i.id, i.isLoaded);
			}
		}
		
		private static void PrintBundleList()
		{
			EB.Debug.LogError("DUMPING BUNDLE LIST...{0}", _bundles.Count);
			foreach(KeyValuePair<string, AssetBundleInfo> i in _bundles)
			{
				EB.Debug.Log("bundle : {0}", i.Key);
			}
		}
			 	
		private static IEnumerator DoLoadBundle( string id )
		{		
			AssetBundleInfo info = GetBundleInfo(id);
			if ( info == null )
			{
				EB.Debug.LogError("ERROR: Bundle " + id + " does not exist!" );
				yield break;
			}
			info.unload = false;
			
			if ( info.isLoaded )
			{
				yield break;
			}
			
			List<AssetBundleInfo> loadList = new List<AssetBundleInfo>();
			GatherBundleLoadList( info, loadList);
			
			yield return StartCoroutine( DoLoadBundles(loadList) ); 
		}
		
		// lock down # of simultaneous streams
		const int kMaxWWW = 2;
		
		private static bool IsWWWLoading( AssetBundleInfo info)
		{
			return _www.ContainsKey(info.id);
		}
		private static WWW GetWWW( AssetBundleInfo info )
		{
			WWW www = null;
			if ( _www.TryGetValue(info.id, out www) )
			{
				if ( www.isDone )
				{
					if ( www.error != null )
					{
						// there was a error
						EB.Debug.LogError("WWW error: " + www.error );
						
						// don't use the cache anymore
	                    info.retry++;
						www.Dispose();
						www = null;
	
	                    if ( info.retry == 5 )
	                    {
	                        // give up
	                        FatalLoadError(info);
	                    }
					}
				}
			}
			
			if ( www == null )
			{
				if ( _www.Count >= kMaxWWW )
				{
					return null;
				}
				
				www = Loader.LoadBundle(info.pack.id, info.id);
				_www[info.id] = www;
			}
			
			return www;
		}
	
	    private static bool _fatalLoadError = false;
	
	    private static void FatalLoadError(AssetBundleInfo info)
	    {
	        EB.Debug.LogError("Something went VERY WRONG loading bundle: " + info.id);
	        _fatalLoadError = true;	
	    }
		
		private static IEnumerator DoLoadBundles( List<AssetBundleInfo> loadList, bool isDeltaBundle = false, long reqTimestamp = 0) 
		{
			bool done = false;
			WaitForFixedUpdate wait = new WaitForFixedUpdate();
			do
			{
				done = true;
				foreach( AssetBundleInfo info in loadList )
				{
					info.unload = false; // make sure we don't try to unload this
					
					if ( !info.isLoaded )
					{
						if (!isDeltaBundle)
						{
							//HACK: temp allow skipping of failed resources
	                    	done = false;
	                    }
	
	                    // see if we can start this load
	                    if ( !string.IsNullOrEmpty(info.parent) && !IsBundleLoaded(info.parent) )
	                    {
	                        continue;
	                    }
	                    
						if (info.uncompressed && !IsWWWLoading(info)) // try and load off disk, if it's not already loading via WWW
						{
							var baseURL = "";
							baseURL = Loader.GetBaseURL(info.pack.id);
							if ( isDeltaBundle )
							{
								baseURL = baseURL+ "../";
							}
							
							Debug.Log("DoLoadBundles baseURL: "+baseURL);
							var bundlePath = Loader.GetBundlePath(info.id);
							var path = baseURL + bundlePath;
							
							Loader.CachedLoadHandler loader = new Loader.CachedLoadHandler(path, true, info.hash, reqTimestamp);
							yield return loader.Load();
							info.bundle = loader.assetBundle;
																					
							if (!info.isLoaded)
							{
								if (isDeltaBundle)
								{
									Debug.LogWarning("Failed to load bundle: {0}", bundlePath);
									continue;
								}
								else
								{
									FatalLoadError(info);
									break;
								}
							}
							else
							{
								info.bundle.name = info.id;
								Object.DontDestroyOnLoad(info.bundle);
								continue;
							}
						}

						if (info.isLoaded)
							continue;
							
						WWW www = GetWWW(info);
	                    if (www != null && www.isDone)
	                    {
	             	        Debug.LogError("++++ LOADED ASSETBUNDLE WITH GetWWW");
							if (!info.isLoaded)
							{
								info.bundle = www.assetBundle;
								EB.Loader.CachedLoadHandler.CacheWWWAsset(www, EB.Loader.GetBundlePath(info.id));
							}
	                        
							
	#if EMULATE_DOWNLOAD_SPEED && UNITY_EDITOR
							var time = (float)www.size / (256.0f * 1024.0f);
							yield return new WaitForSeconds(time);
	#endif
							
							www.Dispose();
	                        _www.Remove(info.id);
												
	                        if (!info.isLoaded)
	                        {
	                            FatalLoadError(info);
	                            break;
	                        }
							else
							{
								info.bundle.name = info.id;
								Object.DontDestroyOnLoad(info.bundle);
							}
							
							EB.Debug.Log("***** BUNDLE LOADED : {0}", info.id);
	                    }
					}
				}
				yield return new WaitForFixedUpdate();
			}
	        while (!done && !_fatalLoadError);
	
	        // game is hosed at this point
	        while (_fatalLoadError)
	        {
	            yield return new WaitForFixedUpdate();
	        }
	
		}
		
		public static Coroutine WaitForIdle()
		{
			return StartCoroutine( DoWaitForIdle() ); 
		}
		
		private static IEnumerator DoWaitForIdle()
		{
			while(_www.Count > 0 )
			{
				yield return new WaitForFixedUpdate();
			}
		}
		
		public static bool IsBundleLoaded( string id )
		{
			AssetBundleInfo info = GetBundleInfo(id);
			return info != null && info.isLoaded;
		}
		
		public static Coroutine LoadBundle( string id )
		{
			//PrintBundleList();
			return StartCoroutine( DoLoadBundle(id) ); 
		}
		
		public static void UnloadBundle( string id )
		{
			AssetBundleInfo info = GetBundleInfo(id);
			UnloadBundle(info);
		}
			
		private static void UnloadBundle( AssetBundleInfo info )
		{
			if ( info != null && info.isLoaded )
			{
				info.unload = true;
				_unloads.Remove(info);
				_unloads.Add(info);
			}
			else 
			{
				//EB.Debug.LogError("UNLOAD BUNDLE DIDN'T UNLOAD BECAUSE (INFO!=NULL) {0}", info != null);
			}
		}
	
	    public static Coroutine LoadBundleFromResourcePath(string path)
	    {
	        AssetBundleInfo info = null;
	        string path_lower = path.ToLower();
	        if (_pathToBundle.TryGetValue(path_lower, out info))
	        {
	            return LoadBundle(info.id);
	        }
	        return null;
	    }
		
		public static void UnloadBundleFromResourcePath( string path )
		{
			AssetBundleInfo info = null;
			string path_lower = path.ToLower();
			if ( _pathToBundle.TryGetValue(path_lower, out info) )  
			{
				UnloadBundle(info);
			}
		}
		
		public static Coroutine ForceLoadLevel( string sceneName, DelayReadyCallback fnOnReady = null )
		{
			return StartCoroutine(_LoadScene(sceneName,false, true, fnOnReady));
		}
				
		public static Coroutine LoadLevel( string sceneName, DelayReadyCallback fnOnReady = null)
		{
			return StartCoroutine(_LoadScene(sceneName,false, false, fnOnReady));
		}
		
		public static Coroutine ForceLoadLevelAdditive( string sceneName )
		{
			return StartCoroutine(_LoadScene(sceneName,true, true));
		}
		
		public static Coroutine LoadLevelAdditive( string sceneName )
		{
			return StartCoroutine(_LoadScene(sceneName,true, false));
		}
		
		
		public static string CurrentScene { get; private set; }
		public static bool IsLoadingScene {get; private set;}
		
		static IEnumerator _LoadScene( string sceneName, bool additive, bool forceLoad, DelayReadyCallback fnOnReady = null )
		{
			EB.TestFlight.Plugin.Checkpoint("Loading scene : " + sceneName);
			
			while(IsLoadingScene)
			{
				yield return 1;
			}
		
			
			if ( CurrentScene == sceneName && !forceLoad )
			{
				yield break;
			}
			
			IsLoadingScene = true;
			
			// check to see if this is in a bundle or not
			AssetBundle bundle = null;
			AssetSceneInfo sceneInfo = null;
			if ( _scenes.TryGetValue(sceneName, out sceneInfo) )
			{
			
				Loader.CachedLoadHandler loader = new Loader.CachedLoadHandler(Loader.GetBundlePath(sceneInfo.pack.id, Loader.GetSceneBundleName(sceneInfo.id)), true, 0, sceneInfo.ts);
				yield return loader.Load();
				bundle = loader.assetBundle;
			}
									
			if (additive)
			{
				_loadLevelOperation = Application.LoadLevelAdditiveAsync( sceneName );
				while ( !_loadLevelOperation.isDone )
				{
					yield return _loadLevelOperation;
				}
			}
			else
			{
				_loadLevelOperation = Application.LoadLevelAsync( sceneName );

				/* GA : ADDED BEGIN */
				// Assumption: if the callback parameter is specified, we are to delay the activation of the new scene
				if (fnOnReady != null)
				{
					_loadLevelOperation.allowSceneActivation = false;
					while ( _loadLevelOperation.progress < 0.9f)	// Yep, this is the way to do it - GA
					{
						yield return 0;
					}
					fnOnReady();
				}
				/* GA: ADDED END */
				else
				{
				while ( !_loadLevelOperation.isDone )
				{
					yield return _loadLevelOperation;
				}
			}
			}
			
			CurrentScene = sceneName;
			
			if (bundle != null)
			{
				bundle.Unload(false);
				Object.DestroyImmediate(bundle,true);
				
				FixupEditorMaterials();
			}
			
			if ( RequiresUnloadAssets() )
			{
				yield return UnloadUnusedAssets();
			}
			
			IsLoadingScene = false;
		}

		public static void FixupEditorMaterials(GameObject obj)
		{
#if UNITY_EDITOR
			if (obj != null)
			{
				foreach( Material material in EB.Util.GatherMaterials(obj,string.Empty) )
				{
					var shader = material.shader;
					if (shader != null)
					{
						var newShader = Shader.Find(shader.name);
						if ( newShader != null)
						{
							material.shader = newShader;
						}
					}
				}
			}
			
#endif
		}
		
		public static void FixupEditorMaterials()
		{
#if UNITY_EDITOR
			foreach( Material material in Resources.FindObjectsOfTypeAll(typeof(Material)))
			{
				var shader = material.shader;
				if (shader != null)
				{
					var newShader = Shader.Find(shader.name);
					if ( newShader != null)
					{
						material.shader = newShader;
					}
				}
			}
#endif
		}
		
		public static bool RequiresUnloadAssets()
		{
#if UNITY_IPHONE 
			if (iPhone.generation == iPhoneGeneration.iPodTouch4Gen ||
				iPhone.generation == iPhoneGeneration.iPad1Gen ||
				iPhone.generation == iPhoneGeneration.iPad2Gen ||
				iPhone.generation == iPhoneGeneration.iPadMini1Gen ||
				iPhone.generation == iPhoneGeneration.iPhone4 ||
				iPhone.generation == iPhoneGeneration.iPhone4S
				)
			{
				return true;
			}
			return false;
#else
			return true;
#endif
			
		}
		
		private static Texture MissingTexture
		{
			get 
			{
				if ( _missingTexture == null )
				{
					_missingTexture = new Texture2D(2, 2);
					_missingTexture.wrapMode = TextureWrapMode.Repeat;
					_missingTexture.filterMode = FilterMode.Point;
					_missingTexture.SetPixel(0,0, Color.green);
					_missingTexture.SetPixel(1,1, Color.green);
					_missingTexture.SetPixel(1,0, Color.magenta);
					_missingTexture.SetPixel(0,1, Color.magenta);
					_missingTexture.Apply();
				}
				return _missingTexture;
			}
		}
		
		public static List<string> GetLoadedAssets()
		{
			List<string> items = new List<string>();
			
			for (int i=0; i<CoreAssets.Instance.Loaded.Count; i++)
			{
				items.Add(CoreAssets.Instance.Loaded[i].path);				
			}
			
			return items;
		}
			
		
		public static bool Unload( string path )
		{
			return CoreAssets.Instance.Unload( path.ToLower() );
		}
		
		public static bool Unload( Object asset )
		{
			return CoreAssets.Instance.Unload( asset );
		}
		
		private static void Track( string path, Object obj, AssetSource src )
		{
			if (obj != null)
			{
				CoreAssets.Instance.Add(path, obj, src);
			}
		}
		
		private static Object Find( string path )
		{
			return CoreAssets.Instance.Find(path);
		}
		
		private static GameObject MissingObject
		{
			get
			{
				if (_missingObject == null)
				{
					_missingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
					_missingObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
					_missingObject.renderer.material.shader = Shader.Find("Diffuse");
					_missingObject.renderer.material.mainTexture = MissingTexture;
					Object.Destroy(_missingObject.GetComponent<BoxCollider>());
				}
				return _missingObject;
			}
		}
			
		public static Object ReturnType( Object asset, System.Type type )
		{
			if ( asset == null ) return null;
			
			if ( type.IsInstanceOfType(asset) )
			{
				return asset;
			}
			else if ( asset is GameObject )
			{
				GameObject go = (GameObject)asset;
				if ( type.IsSubclassOf( typeof(Component) ) )	
				{
					return go.GetComponent(type);
				}
			}
			return null;
		}
		
		const bool DefaultSurrogate = false;
		
	    public static T Load<T>( string path) where T : Object
		{
			return Load(path, typeof(T), DefaultSurrogate) as T;
		}
		
		public static T Load<T>( string path, bool bReturnSurrogate) where T : Object
		{
			return Load(path, typeof(T), bReturnSurrogate) as T;
		}
	
	    public static Object Load(string path)
		{
			return Load<Object>(path, DefaultSurrogate);
		}
		
		public static Object Load(string path, bool bReturnSurrogate)
		{
			return Load<Object>(path, bReturnSurrogate);
		}
	
	    public static Object Load(string path, System.Type type)
	    {
	        return Load(path, type, DefaultSurrogate);
	    }
		
		public static Object Load(string path, System.Type type, bool bReturnSurrogate)
		{
#if ENABLE_LOADING_DEBUG
			Debug.Log ("Loading: "+path+" : "+type.ToString());	
#endif
			if (string.IsNullOrEmpty(path))
			{
				Debug.Log("EB.Assets.Load sent empty/null path!");			
				return null;
			}
			string path_lower = path.ToLower();
			var obj = Find(path_lower);
			if ( obj != null )
			{
#if ENABLE_LOADING_DEBUG
				Debug.Log ("Succesful Find.");	
#endif
				return ReturnType(obj, type);
			}
			
			AssetBundleInfo info = null;
			if ( _pathToBundle.TryGetValue(path_lower, out info) )
			{
				if ( info.isLoaded == false )
				{
					EB.Debug.LogError("ERROR: object at path " + path + " exists in bundle " + info.id + " but the bundle isn't loaded" );
					return null;
				}
				
				var assetName = AssetName(path_lower);
				var asset = info.bundle.Load( assetName, type );
				if ( asset != null )
				{
					Track(path_lower, asset, AssetSource.Bundle);
#if UNLOAD_SINGLE_ASSET_BUNDLES					
					// optimization
					// if this bundle only contains a single asset, then unload the bundle
					if (info.paths.Length ==1 )
					{
						UnloadBundle(info);
					}
#endif
#if ENABLE_LOADING_DEBUG
					Debug.Log ("Successful Find in Bundle.");
#endif
					return ReturnType(asset, type);
				}
				
				EB.Debug.LogError("Failed to load " + path_lower + " from bundle " + info.id );
				return null;
			}

#if DEBUG
			bool isPlaying = true;
#if UNITY_EDITOR
			isPlaying = UnityEditor.EditorApplication.isPlaying;
#endif
			if (isPlaying && EB.Assets.CurrentBundleMode != EB.Assets.DevBundleType.NoBundles && path_lower.StartsWith("bundles"))
			{
				Debug.LogError("Failed to find bundled file: "+path+" in a bundle. Make sure it's included in the bundled data specified in Assets/Config/...");
				obj = null;
			}
			else
			{
				obj = Resources.Load(path, type);
			}
#else
			obj = Resources.Load(path, type);
#endif

			if (obj == null)
			{
				if ( bReturnSurrogate)
				{
					obj = ReturnType(MissingObject, type);
				}
				
				if ( obj == null )
				{
					EB.Debug.LogError("Can't find: " + path);
				}			
			}
			else
			{
#if ENABLE_LOADING_DEBUG
				Debug.Log ("Successful Find in Resources.");
#endif
				Track( path_lower, obj, AssetSource.Resources );
			}
			
			return obj;
		}
		
		public static AssetBundle GetAssetBundle(string id)
		{
			var bundle = GetBundleInfo(id);
			if ( bundle != null && bundle.isLoaded )
			{
				return bundle.bundle;
			}
			
			EB.Debug.LogError("Failed to find bundle : " + bundle );
			return null;
		}
		
		public static Object[] LoadAllBundle( string id ) 
		{
			var bundle = GetBundleInfo(id);
			if ( bundle != null && bundle.isLoaded )
			{
				return bundle.bundle.LoadAll();
			}
			
			EB.Debug.LogError("Failed to load all bundle : " + bundle + " whose id is " + id );
			return new Object[]{};
		}
		
		public static Coroutine LoadBundleAssetsAndUnload( string id )
		{
			return StartCoroutine( DoLoadBundleAssetsAndUnload(id) ); 
		}
		
		static IEnumerator DoLoadBundleAssetsAndUnload( string id )
		{
			yield return LoadBundle(id);
			
			foreach( KeyValuePair<string,AssetBundleInfo> kvp in _pathToBundle )
			{
				if ( kvp.Value.id == id )
				{
					//yield return LoadAsync( kvp.Key, typeof(Object), null );
					Load( kvp.Key);
				}
			}
			
			UnloadBundle( id );
			
			yield return ProcessDeferedUnloads();
		}
		
		public static Object[] LoadAll( string path, System.Type type )
		{
#if ENABLE_LOADING_DEBUG
			EB.Debug.Log ("LOADALL: "+path+" type: "+type);
#endif
			List<Object> objects = new List<Object>();
			
			// load the resources first (so we can override things with bundles)
			foreach( var asset in Resources.LoadAll(path,type) )
			{
				// this sucks, don't want double load here
				var p = (path+"/"+asset.name).ToLower();
				var obj = Find(p);
				if ( obj != null )
				{
					objects.Add(obj);
				}
				else
				{
					Track( p, asset, AssetSource.Resources);
					objects.Add(asset);
				}
			}
			
			string path_lower = path.ToLower();
			foreach( KeyValuePair<string,AssetBundleInfo> kvp in _pathToBundle )
			{
				if ( kvp.Value.isLoaded && kvp.Key.StartsWith(path_lower) )
				{
					var asset = Load(kvp.Key, type, false);
					if ( asset != null )
					{		
						objects.Add(asset);
					}
				}
			}
			
#if ENABLE_LOADING_DEBUG
			EB.Debug.Log ("LOADALL FINISHED: "+path+" type: "+type);
#endif

			return objects.ToArray();
		}
		
		public static Coroutine LoadMultiAsync( string[] paths, System.Type type, EB.Action<Object[]> callback )
		{
			return StartCoroutine( DoLoadMultiAsync(paths,type,callback) ); 
		}
	
	    private static IEnumerator DoLoadMultiAsync(string[] paths, System.Type type, EB.Action<Object[]> callback, bool track = true)
		{
			List<AssetBundleInfo> loadList = new List<AssetBundleInfo>();
			foreach( string path in paths )
			{
				string path_lower = path.ToLower();
				AssetBundleInfo info = null;
				if ( _pathToBundle.TryGetValue(path_lower, out info) )
				{
					GatherBundleLoadList(info, loadList);
				}
			}
			
			if ( loadList.Count > 0 )
			{
				yield return StartCoroutine( DoLoadBundles(loadList) ); 			
			}
		
			Object[] objectList = new Object[paths.Length];
			for ( int i = 0; i < paths.Length; ++i )
			{
				yield return LoadAsync(paths[i], type, delegate(Object asset){
					objectList[i] = asset;
				}, track);
			}
			
			if ( callback != null)
			{
				callback(objectList);
			}
		}
		
		public static Coroutine LoadAsync( string path, System.Type type, EB.Action<Object> callback, bool track = true)
		{
			return StartCoroutine( DoLoadAsync(path,type,callback, track) ); 
		}
	
	    private static IEnumerator DoLoadAsync(string path, System.Type type, EB.Action<Object> callback, bool track = true)
		{
			Object asset = null;
			AssetBundleInfo info = null;
			
			string path_lower = path.ToLower();
			asset = Find(path_lower);
			if ( asset != null )
			{
				asset = ReturnType(asset, type);
			}
			else if ( _pathToBundle.TryGetValue(path_lower, out info) )
			{
				// inc the async loading count;
				info.asyncLoadingCount++;
				info.unload = false;
				
				if ( info.isLoaded == false )
				{
					// load the bundle
					EB.Debug.Log(path_lower + " requires bundle " + info.id + " to be loaded");
					yield return LoadBundle(info.id); 
				}
							

				var assetName = AssetName(path_lower);
				var async = info.bundle.LoadAsync( assetName, type);
				yield return async;
				
				if ( info.isLoaded )
				{
					asset = async.asset;
					if ( asset == null )
					{
						EB.Debug.LogError("Failed to load " + path_lower + " from bundle " + info.id );
					}
					else
					{
						if (track)
						{
							Track(path_lower, asset, AssetSource.Bundle);  
						}
#if UNLOAD_SINGLE_ASSET_BUNDLES
						// optimization
						// if this bundle only contains a single asset, then unload the bundle
						if (info.paths.Length ==1 )
						{
							UnloadBundle(info);
						}
#endif
					}
				}	
				
				// decrement loading count
				info.asyncLoadingCount--;
			}
			else
			{
#if DEBUG
				bool isPlaying = true;
#if UNITY_EDITOR
				isPlaying = UnityEditor.EditorApplication.isPlaying;
#endif
				if (isPlaying && EB.Assets.CurrentBundleMode != EB.Assets.DevBundleType.NoBundles && path_lower.StartsWith("bundles"))
				{
					EB.Debug.LogError("Failed to find bundled file: "+path+" in a bundle. Make sure it's included in the bundled data specified in Assets/Config/...");
					asset = null;
				}
				else
				{
					asset = Resources.Load(path, type);
				}
#else
				// try in resources
				asset = Resources.Load(path,type);
#endif			
				if ( asset != null )
				{
					Track( path_lower, asset, AssetSource.Resources); 
				}
			}
			
			if ( callback != null)
			{
				//EB.Debug.Log ("CALLING CB WITH ", asset);
				callback(asset);
			}
			
			yield break;
		}
		
		public static bool Exists( string path )
		{
			string path_lower = path.ToLower();
			AssetBundleInfo info = null;
			if ( _pathToBundle.TryGetValue(path_lower, out info) )
			{
				return true;
			}
			
		//	Debug.Log(string.Format("{0} wasn't found in bundle, so attempting to load from res", path));
			bool loadFromRes = Resources.Load(path) != null;
			return loadFromRes;
		}
		
		public static void PrintLoadedBundles()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("Bundles Loaded:");
			
			List<string> names = new List<string>();
			foreach( AssetBundleInfo bundle in _bundles.Values )
			{
				if ( bundle.isLoaded )
				{
					names.Add(bundle.id);
				}
			}
			names.Sort();
			
			foreach( var name in names )		    
			{
				sb.AppendFormat("\tbundle: {0}\n",name );
			}
			
			
			EB.Debug.Log(sb.ToString());
		}
		
	}

}

public enum AssetSource
{
	Resources,
	Bundle
}

class CoreAssets : MonoBehaviour
{
	[System.Serializable]
	public class Info
	{
		public string 		path;
		public Object 		asset;
		public int  		refCount;
		public AssetSource 	source;
	}
	
	public List<Info> Loaded = new List<Info>();
	
	static CoreAssets _this = null;
	public static CoreAssets Instance 
	{
		get
		{
			if (_this==null)
			{
				var go = new GameObject("_Assets");
				DontDestroyOnLoad(go);
				go.hideFlags = HideFlags.HideAndDontSave;
				_this = go.AddComponent<CoreAssets>();
			}
			return _this;
		}
	}
	
	int IndexOf( string path )
	{
		for( int i = 0; i < Loaded.Count; ++i)
		{
			if ( Loaded[i].path == path )
			{
				return i;
			}
		}
		return -1;
	}
	
	int IndexOf( Object asset )
	{
		for( int i = 0; i < Loaded.Count; ++i)
		{
			if ( Loaded[i].asset == asset )
			{
				return i;
			}
		}
		return -1;
	}
	
	public Object Find( string path )
	{
		var index = IndexOf(path);
		if ( index >=0 )
		{
			var info = Loaded[index];
			info.refCount++;
			return info.asset;
		}
		return null;
	}
	
	void OnDestroy()
	{
		Loaded.Clear();
	}
	
	public void Add( string path, Object asset, AssetSource source )
	{
		var index = IndexOf(path);
		if ( index == -1 )
		{
			index = Loaded.Count;	
			Loaded.Add( new Info(){ path = path, asset = asset, source = source, refCount = 0 } );
		}
		else
		{
			EB.Debug.LogError("Error: loaded asset more than once for path " + path);
		}
		Loaded[index].refCount++;
	}
	
	public bool Unload(Object asset)
	{
		var index = IndexOf(asset);
		if ( index >= 0 )
		{
			var info = Loaded[index];
			info.refCount--;
			if ( info.refCount == 0 )
			{
				Unload(index);
				return true;
			}
		}
		return false;
	}
	
	public bool Unload(string name)
	{
		var index = IndexOf(name);
		if ( index >= 0 )
		{
			var info = Loaded[index];
			info.refCount--;
			if ( info.refCount == 0 )
			{
				Unload(index);
				return true;
			}
		}
		return false;
	}
	
	void Unload( int index )
	{
		var info = Loaded[index];
		if ( info.asset != null )
		{
			//Debug.Log("Unloading asset " + info.path);
			switch(info.source)
			{
			case AssetSource.Resources:
				// this for some reason does not work on game objects..
				if ( !(info.asset is GameObject) )
				{
#if !UNITY_EDITOR					
					Resources.UnloadAsset(info.asset);
#endif
				}
				break;
			case AssetSource.Bundle:
				// this is shit because if you do this, you can never laod the asset again
				//Object.DestroyImmediate(info.asset, false);
				break;
			}
			info.asset = null;
		}
		
		// do a pop-back
		Loaded[index] = Loaded[Loaded.Count-1];
		Loaded.RemoveAt(Loaded.Count-1);
	}
	
}
