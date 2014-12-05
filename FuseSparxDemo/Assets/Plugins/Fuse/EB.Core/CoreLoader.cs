using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB
{
	public static class Loader
	{
	
		public static int 		BundleCacheBlankHeaderSize = 1024;	
		
		#if UNITY_ANDROID
		public static string[] SDCardPaths = { "/mnt/sdcard", "/storage/emulated/legacy", "/storage/sdcard0", "/storage/emulated/0", "/sdcard" };
		#endif
		
		private static string _devDataPath = null;
		public static string DevDataPath {
			get 
			{
				if (_devDataPath == null)
				{
					#if UNITY_EDITOR
					_devDataPath = EB.Loader.DataPath.Replace("file://","");
					#else
					#if UNITY_ANDROID
					string persistentDataPath = Application.persistentDataPath;
					if (System.IO.Directory.Exists(persistentDataPath))
					{
						_devDataPath = persistentDataPath;
					}
					else
					{
						// sometimes Unity gives us a path that doesn't actually exist so we need to check if it works w/ one of the standard paths
						int index = persistentDataPath.ToLower().IndexOf("/Android/");
						string appSpecific = persistentDataPath.Substring(index);
						
						foreach(string testBasePath in SDCardPaths)
						{
							string testPath = testBasePath + appSpecific;
							if (System.IO.Directory.Exists(testPath))
							{
								_devDataPath = testPath;
								break;
							}
						}
						if (_devDataPath == null)
						{
							Debug.LogError("Couldn't find existing SDCARD path for development build asset bundles!");
						}
						
					}
					if (_devDataPath != null)
					{
						Debug.Log("**** Using DevDataPath: "+_devDataPath);
					}
					#elif UNITY_IPHONE
					_devDataPath = Application.persistentDataPath;
					#else
					_devDataPath = "";
					#endif
					#endif
				}
				return _devDataPath;
			}
		}
		
	
		public class CachedLoadHandler
		{
			
			private System.Uri	_devURI;
			private string 		_assetPath;
			private string 		_origURI;
			private string 		_relPath;
			public WWW 			_www = null;
			private AssetBundle _assetBundle = null;
			private bool		_useCache;
			private int 		_hash;
			private long		_reqTimeStamp;
			private bool 		_suppressErrors = false;
			
			public string text
			{
				get 
				{
					if (_www != null && string.IsNullOrEmpty(_www.error))
					{
						return _www.text;
					}
					return string.Empty;
				}
			}
			
			public AssetBundle assetBundle
			{
				get 
				{
					if (_assetBundle != null) 
					{
						Debug.Log("returning _assetBundle");
						return _assetBundle;
						
					} else if (_www != null && _www.assetBundle != null) 
					{
						Debug.Log("returning _www.assetBundle");	
						return _www.assetBundle;				
					}
					return null;
				}
			}
			
			public WWW GetWWW() 
			{ 
				WWW result = null;
				if (_www != null && _www.isDone) 
				{
					result = _www; 
				}
				return result; 
			}
			
			private static System.Uri GetDevelopmentAssetURI(string path)
			{
				string devDataPath = Path.Combine(DevDataPath, path);
				Debug.Log("GetDevelopmentAssetURI - devDataPath "+devDataPath+" path "+path);
				return new System.Uri(devDataPath);
			}
			
			public CachedLoadHandler(string filePath, bool useCache, int hash = 0, long reqTimeStamp = 0, bool suppressErrors = false)
			{
				if (filePath.IndexOf("://") != -1)
				{
					_origURI = filePath;
				}
				else
				{
					_origURI = "file://"+filePath;
				}
				string uriString ="://";
				
				int uriIndex = filePath.ToLower().IndexOf(uriString);
				int skipIndex = (uriIndex == -1) ? 0 : uriIndex + uriString.Length;
				_assetPath = filePath.Substring(skipIndex);
				_assetPath = Path.GetFullPath(_assetPath); // normalize (remove .. etc)
				Debug.Log("filepath:"+filePath+" _origURI:"+_origURI+" _assetPath"+_assetPath);
				
				uriIndex = DataPath.IndexOf(uriString);
				skipIndex = (uriIndex == -1) ? 0 : uriIndex + uriString.Length;
				_relPath = _assetPath.Substring(DataPath.Length - skipIndex);
				Debug.Log("relPath: "+_relPath);
				
				_devURI = GetDevelopmentAssetURI(_relPath);
				_useCache = useCache;
				_reqTimeStamp = reqTimeStamp;
				_suppressErrors = suppressErrors;
				_hash = hash;
				Debug.Log(string.Format("cachedLoadHandler: assetpath: {0}, devURI: {1}", _assetPath, _devURI));
			}
			
			public IEnumerator LoadAssetBundle(string path)
			{
				Debug.Log("LoadAssetBundle: "+path);
				#if UNITY_ANDROID
				if( path.StartsWith("jar:") )
				{
					Debug.Log( "Trying to load from Jar: " + path );
					path = path.Replace("jar:file://", "");
					string[] parts = path.Split( '!' );
					if( parts.Length == 2 )
					{
						string jarPath = parts[ 0 ];
						string filePath = parts[ 1 ];
						if( filePath.StartsWith("/") == true )
						{
							filePath = filePath.Substring( 1 );
						}
						string outputPath = System.IO.Path.Combine( Application.persistentDataPath, System.IO.Path.GetFileName( filePath ) );
						
						bool success = false;
						yield return EB.Coroutines.Run( JarExtractor.SyncLoadFromJar( jarPath, filePath, outputPath, _hash, delegate(bool s) { success = s; } ) );
						
						if( success == true )
						{
							_assetBundle = AssetBundle.CreateFromFile(outputPath);
						}
					}
				} 
				else
				#endif
				{
					path = System.IO.Path.GetFullPath(path.Replace("file://", "")); // normalize the path
					Debug.Log("Testing if can load bundle w/ AssetBundle.CreateFromFile at: " + path);
					_assetBundle = AssetBundle.CreateFromFile(path);
				}

				Debug.Log("LoadAssetBundle.CreateFromFile result: "+(_assetBundle == null ? "FAILED":"SUCCESSFUL"));
				yield break;
			}
			
			public static void CacheWWWAsset(WWW www, string relPath, bool sentByBundleServer = false)
			{
				string devDataPath = GetDevelopmentAssetURI(relPath).LocalPath;
				string writeDir = Path.GetDirectoryName(devDataPath);
				
				Debug.Log (string.Format("CacheWWAsset devDataPath: {0}, writeDir {1}", devDataPath, writeDir));
				
				if (!Directory.Exists(writeDir))
				{
					Directory.CreateDirectory(writeDir);
				}
				
				FileStream fs = File.OpenWrite(devDataPath);
				using (BinaryWriter bw = new BinaryWriter(fs))
				{
					int offset = 0;
					// we need to do this offset bullshit because unity blocks you from accessing WWW.bytes if it detects it's an assetbundle
					if (relPath.ToLower().EndsWith("assetbundle") && sentByBundleServer)
					{
						offset = EB.Loader.BundleCacheBlankHeaderSize;
					}
					if (www.bytes.Length > offset)
					{
						bw.Write(www.bytes, offset, www.bytes.Length - offset);
					}
					else
					{
						Debug.LogError("Error using bundle cache for file: "+relPath);
					}
				}
			}
			
			public Coroutine Load()
			{
				return EB.Coroutines.Run(DoLoad());
			}
			
			private IEnumerator DoLoad()
			{
				bool loaded = false;
				
				Debug.Log("CachedLoadHandler DoLoad()");

				string devDataPath = _devURI.LocalPath;
				
				string platformDir = EB.Loader.Folder;
				
				bool isAssetBundle = _assetPath.ToLower().EndsWith("assetbundle");
				
				Debug.Log (string.Format("devDataPath {0}, platformDir {1}, _assetPath: {2}", devDataPath, platformDir, _assetPath));
				
				bool devPathExists = File.Exists(devDataPath);
				long existingWriteTime = -1;
				if (devPathExists)
				{
#if !UNITY_WEBPLAYER
					existingWriteTime = File.GetLastWriteTime(devDataPath).Ticks;
#endif
				}
				
				if (!EB.Assets.BundleCacheServerEnabled)
				{
					#if !UNITY_EDITOR
					if (devPathExists && UnityEngine.Debug.isDebugBuild)	// delete previous bundle server transferred data
					{
						Debug.Log("Bundle Server not enabled! Deleting cached file @"+devDataPath);
						File.Delete(devDataPath);
					}
					#endif
					if (isAssetBundle)
					{
						Debug.Log("Attempting to load assetbundle @"+_origURI);
						yield return EB.Coroutines.Run(LoadAssetBundle(_origURI));
						loaded = (_assetBundle != null);
					}
					if (!loaded) 
					{
						Debug.Log("Attempting to load from: "+_origURI);
						_www = new WWW(_origURI);
						yield return _www;
						if (!string.IsNullOrEmpty(_www.error) && !_suppressErrors)
						{
							EB.Debug.LogError("Failed to load "+_origURI+" error: "+_www.error);
						}
					}
					yield break;	// done. end coroutine
				}
				
				string bundleCacheURI = EB.Assets.BundleCacheServerAddress +"/"+ Path.Combine(platformDir, _relPath);
				
				Debug.Log(string.Format("bundleCacheURI: {0}, bundlecacheserveraddress {1}, platformdir {2}, _relPath {3}",bundleCacheURI, EB.Assets.BundleCacheServerAddress, platformDir, _relPath));
				
				if (!_useCache || !devPathExists || existingWriteTime < _reqTimeStamp)
				{
					
					if (!_useCache && devPathExists)
					{
						File.Delete(devDataPath);
					}
					
					loaded = false;
					if (isAssetBundle)
					{
						yield return LoadAssetBundle(devDataPath);
						if (_assetBundle != null)
						{
							yield break;
						}
					}				
					
					int retryCount = 3;
					while ( true )
					{
						_www = new WWW(bundleCacheURI);
						yield return _www;
						if (!string.IsNullOrEmpty(_www.error))
						{
							EB.Debug.LogWarning("Failed to load "+bundleCacheURI+" error: "+_www.error);
							retryCount--;
							if (retryCount <= 0)
							{
								break;
							}
						}
						else					
						{
							Debug.Log("Saving Asset to cache path: "+devDataPath);
							
							CacheWWWAsset(_www, _relPath, true);
							
							break;
						}
					}
				}
				loaded = false;
				if (isAssetBundle)
				{
					Debug.LogError("Loading assetbundle from disk: "+devDataPath);
					yield return EB.Coroutines.Run(LoadAssetBundle(devDataPath));
					loaded = (_assetBundle != null);
				}
				if (!loaded)
				{
					Debug.Log("CachedLoadHandler Loading w/ _www devURI:"+_devURI);
					_www = new WWW(_devURI.ToString());
					yield return _www;
				}
			}
		};
		
	
		public static string Folder
		{
			get
			{
#if UNITY_IPHONE
				return "ios/";
#elif UNITY_WEBPLAYER
				return "web/";
#elif UNITY_FLASH
				return "flash/";
#elif UNITY_ANDROID
				return "android/";
#else
				return "data/";			
#endif
			}
		}
		
		public static bool CanUseCache = false;

        private static string _dataPath = string.Empty;
		private static Dictionary<string,string> _packs = new Dictionary<string,string>();
		
		public static string DataPath
		{
			get
			{
				if ( string.IsNullOrEmpty(_dataPath) )
				{
	                if (Application.isWebPlayer)
						_dataPath = Application.dataPath + "/" + Folder;
					else if (Application.platform == RuntimePlatform.Android)
					{
						_dataPath = "jar:file://" + Application.dataPath + "!/assets/";
					}
					else if (Application.platform == RuntimePlatform.IPhonePlayer)
						_dataPath = "file://" + Path.GetFullPath(Application.dataPath + "/" + Folder);
					else if (Application.platform == RuntimePlatform.OSXPlayer)
						_dataPath = "file://" + Path.GetFullPath(Application.dataPath + "/../../" + Folder);
					else
					{
						_dataPath = "file://" + Path.GetFullPath(Application.dataPath + "/../" + Folder);
					}
	            		}
				return _dataPath;
			}
		}

		public static void OverridePath( string dp )
		{
			_dataPath = dp;
		}
		
		public static void OverridePath( string pack, string path )
		{
			_packs[pack] = path;
		}
		
        public static string GetBaseURL( string pack )
        {
			var path = string.Empty;
			if (_packs.TryGetValue(pack, out path))
			{
				return path;
			}
            return DataPath + pack + "/";
        }		
		
		public static WWW Load( string pack, string path )
		{
//#if UNITY_ANDROID
//			var cacheUrl = System.IO.Path.Combine(EB.Loader.DevDataPath, path);
//			if (System.IO.File.Exists(cacheUrl))
//			{
//				EB.Debug.Log("Found " + cacheUrl + " from cache"); 
//				return new WWW("file://"+cacheUrl);
//			}
//#endif
								
			var url = GetBundlePath(pack, path);
			if (CanUseCache)
			{
				EB.Debug.Log("Loading " + url + " from cache"); 
				return WWW.LoadFromCacheOrDownload(url,1);
			}
			else
			{
				EB.Debug.Log("Loading " + url);
				return new WWW(url);
			}
		}
		
		public static string GetBundlePath( string bundleId )
		{
			string folder = string.Empty;
			string file = bundleId;
			
			int slash = bundleId.LastIndexOf('/');
			if ( slash >= 0 )
			{
				folder = bundleId.Substring(0,slash+1);
				file = bundleId.Substring(slash+1);
			}
			
			string path = string.Format("{0}{1}.assetbundle", folder, file);
			return path;
		}
		
		public static WWW LoadBundle( string pack, string bundleId )
		{	
			var path = GetBundlePath(bundleId);
			return Load(pack, path);
		}
		
		public static string GetSceneBundleName(string sceneName)
		{
			string path = string.Format("scene_{0}.assetbundle", sceneName);
			return path;
		}
		
		public static string GetBundlePath(string pack, string path)
		{
			var url = Path.Combine(GetBaseURL(pack),path);
			return url;
		}
		
		public static WWW LoadScene( string pack, string name )
		{
			string path = GetSceneBundleName(name);
			return Load(pack, path);
		}
		
	}
}

