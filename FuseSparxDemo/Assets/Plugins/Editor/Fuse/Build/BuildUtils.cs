using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;


[InitializeOnLoad]
public class AutorunInterruptedBuildCleanup
{
    static AutorunInterruptedBuildCleanup()
    {
        EditorApplication.update += RunOnce;
		EditorApplication.playmodeStateChanged += PlaymodeCallback;
	}
	
	static void RunOnce()
    {
		//Debug.Log("RunOnce EditorApplication.isCompiling "+(EditorApplication.isCompiling ? "YES" : "NO"));
    	if (!EditorApplication.isCompiling) 
    	{
			Debug.Log("Autorun: Restoring Resources/Bundles if necessary...");
			EditorApplication.update -= RunOnce;
			
			// In case Unity was shut down / crashed during a build we want to return the Bundles back to Resources/Bundles
			BuildUtils.UnHideBundlesDirs();
			
		}
	}
	
	static void PlaymodeCallback()
    {
//    	Debug.LogError("Playmode Callback");
//		Debug.LogError("isPlayingOrWillChangePlaymode "+ (EditorApplication.isPlayingOrWillChangePlaymode ? "TRUE":"FALSE" ) );
//		Debug.LogError("isPlaying "+(EditorApplication.isPlaying ? "TRUE":"FALSE" ));
//		Debug.LogError("isCompiling "+(EditorApplication.isCompiling ? "TRUE":"FALSE" ));
//		Debug.LogError("isUpdating "+(EditorApplication.isUpdating ? "TRUE":"FALSE" ));
//		Debug.LogError("isPaused "+(EditorApplication.isPaused ? "TRUE":"FALSE" ));
		if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && BuildSettings.DevBundleMode == EB.Assets.DevBundleType.NoBundles)
    	{
			BuildUtils.BuildEditorDataOnlyQuick();
    	}
    }
}

public static class BuildUtils 
{
	private static List<string> _bundlesResources = null;
	
	public static string BundleDir = "Assets/Bundles/";
	public static string TempBundleDir = "TempBundles/";
	public static string FailedMoveDir = "FailedMoveBundles/";
	public static string ResourcesBundleDir = "Assets/Resources/Bundles/";
	
	public static string SkipFileName = "bundle_dir_only.txt";
	
	private class CaseInsensitivePredicate
	{
		private string _find;
		
		public CaseInsensitivePredicate(string find)
		{
			_find = find.ToLower();
		}
		
		public bool Predicate(string compare)
		{
			return (compare.ToLower() == _find);
		}
		
	}
		
	public static List<string> UnmovedBundleDirectories
	{
		get
		{
			List<string> skipFolders = new List<string>();
			
			string skipMovePath = System.IO.Path.Combine(BuildSettings.BundlerConfigFolder, SkipFileName);
			if (System.IO.File.Exists(skipMovePath))
			{
				var skipJSON = System.IO.File.ReadAllText(skipMovePath);
				ArrayList skipping = (ArrayList)EB.JSON.Parse(skipJSON);
				if (skipping != null)
				{
					var sArray = skipping.ToArray(typeof(string));
					skipFolders = new List<string>((string[])sArray);
				}
			}
			return skipFolders;
		}
	}
	
	private static List<string> BundlesResources
	{
		get 
		{
			//if (_bundlesResources == null || _bundlesResources.Count == 0)
			{
				_bundlesResources = new List<string>();
				
				List<string> skipFolders = UnmovedBundleDirectories;
								
				string[] files =  Directory.GetFiles(BuildSettings.BundlerConfigFolder, "*.txt", SearchOption.AllDirectories);
				foreach( string file in files )
				{
					if (file.ToLower().EndsWith(SkipFileName))
					{
						continue;
					}
				
					DateTime modified = new DateTime();
					ArrayList bundles = null; 
					try 
					{ 
						bundles = Bundler.LoadBundles(file, out modified);
					} 
					catch (System.Exception e)
					{
						Debug.LogError("LoadBundles parse failed on file: "+ file +" exception: "+e);
					}

					if (bundles != null)
					{
						foreach( Hashtable bundle in bundles )
						{
							string folder = (string)bundle["folder"];
							if (!string.IsNullOrEmpty(folder))
							{
								string bundleFolder = folder;
								
								string 	bundleDir = ResourcesBundleDir; //TODO: do some sort of checking to see which one we're building from
								int 	bundleDirLength = bundleDir.Length;
								
								if (bundleFolder.StartsWith(BuildUtils.ResourcesBundleDir))
								{
									bundleDir = BuildUtils.ResourcesBundleDir;
								}
								else if (bundleFolder.StartsWith(BuildUtils.BundleDir))
								{
									bundleDir = BuildUtils.BundleDir;
								}
								else
								{
									Debug.LogWarning("Bundle folder is using unknown bundle directory! skipping! Bundle Info: "+ EB.JSON.Stringify(bundle) );
									continue;
								}							
								
								bundleDirLength = bundleDir.Length;
								
								int dirIndex = folder.IndexOf(System.IO.Path.DirectorySeparatorChar, bundleDirLength);
								if (dirIndex < 0)
								{
									dirIndex = folder.IndexOf(System.IO.Path.AltDirectorySeparatorChar, bundleDirLength);
								}
								
								if (dirIndex > -1)
								{
									bundleFolder = folder.Substring(bundleDirLength, dirIndex - bundleDirLength);
								}
								else
								{
									bundleFolder = folder.Substring(bundleDirLength);
								}
								
								CaseInsensitivePredicate pred = new CaseInsensitivePredicate(bundleFolder);
								if (_bundlesResources.Find(pred.Predicate) == null && skipFolders.Find(pred.Predicate) == null)
								{
									_bundlesResources.Add(bundleFolder);
								}
							}
						}
					}
				}
			}
			
			return _bundlesResources;
		}
	}
	
	public static void ForceDeleteDirectory(string path)
	{
		if (Directory.Exists(path))
		{
		    var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };
		    
		    FileSystemInfo[] infos = directory.GetFileSystemInfos("*");
		
		    foreach (var info in infos)
		    {
		    	if ((info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
		    	{
		        	info.Attributes = (info.Attributes & ~FileAttributes.ReadOnly);
		        }

		        if ((info.Attributes & FileAttributes.Directory) != 0)
		        {
		        	ForceDeleteDirectory(info.FullName);
		        }
		    }
		    
			try 
			{
		    	directory.Delete(true);
		    } 
		    catch (System.Exception e)
		    {
		    	Debug.LogError("Failed to delete Directory: "+directory.FullName+" Exception: "+e.ToString());
		    }
		}
	}
	
	private static void MoveDirRecursive(string sourceDir, string destDir, string failedDest)
	{
		if (!Directory.Exists(sourceDir))
		{
			Debug.LogError("sourcedir doesn't exist, cannot move: "+sourceDir);
		}
		else
		{
			DirectoryInfo di = new DirectoryInfo(sourceDir);
			string dirName = di.Name;
			
			{	// scoping so the variables created here aren't used after as they're not fixed up if we change destDir
				DirectoryInfo destDirInfo = new DirectoryInfo(destDir);
				string destDirName = destDirInfo.Name;
				if (destDirName.ToLower() != dirName.ToLower())
				{
					destDir = Path.Combine(destDir, dirName);
				}
				DirectoryInfo failedDirInfo = new DirectoryInfo(failedDest);
				string failedDirName = failedDirInfo.Name;
				if (failedDirName.ToLower() != dirName.ToLower())
				{
					failedDest = Path.Combine(failedDest, dirName);
				}
			}
			string path = sourceDir;
			string destDirFullPath =  destDir;
			if (!Directory.Exists(destDir))
			{
				Directory.Move(sourceDir, destDir);
			}
			else
			{
				// destination exists... lets recursively try the dirs inside (sometimes the dest directories are created empty where they were before)
				string[] entries = Directory.GetFileSystemEntries(path);
				foreach (string entry in entries)
				{
					if (Directory.Exists(entry))
					{
						try 
						{
							MoveDirRecursive(entry, destDir, failedDest);
						} 
						catch (System.Exception e)
						{
							Debug.LogError("Error moving: " + entry + " -> " + destDir + ": " + e.Message);
						}							
					}
					else // if (File.Exists(entry))
					{
						try 
						{
							string fileName = Path.GetFileName(entry);
							string destFileFullPath = Path.Combine(destDirFullPath, fileName);
							Debug.Log ("move check : "+destFileFullPath);
							if (!File.Exists(destFileFullPath))
							{
								File.Move(entry, destFileFullPath);
							}
							else
							{
								
								string failedDestFullPath = Path.Combine(failedDest,fileName);
								if (!Directory.Exists(failedDest))
								{
									Directory.CreateDirectory(failedDest);
								}
								Debug.LogWarning("File Exists, saving file ("+entry+") to failed backup: "+failedDestFullPath);
								File.Move(entry, failedDestFullPath);
							}
						} 
						catch (System.Exception e)
						{
							Debug.LogError("Error moving: "+entry+" -> "+destDirFullPath+" exception: "+e.ToString());
						}
					}
				}
				Directory.Delete(path);
				File.Delete(path+".meta");
			}			
		}
	}
	
	private static void MoveBundleDirs(string sourceDir, string destDir)
	{
		List<string> bundleDirs = BundlesResources;
		
		string failedMoveDir = Path.Combine(FailedMoveDir, "" + DateTime.Now.Ticks);
		foreach (string bundleDir in bundleDirs)
		{
			string path = sourceDir + bundleDir;
			string dest = destDir + bundleDir;
			string pathMeta = path + ".meta";			
			string destMeta = dest + ".meta";
			
			//string failedDest = Path.Combine(failedMoveDir, bundleDir);
			try 
			{
				if (System.IO.Directory.Exists(path))
				{
					if (!Directory.Exists(destDir))
					{
						Directory.CreateDirectory(destDir);
					}
					DirectoryInfo pathDirInfo = new DirectoryInfo(path);
					string dirName = pathDirInfo.Name;
					DirectoryInfo destDirInfo = new DirectoryInfo(dest);
					string destDirName = destDirInfo.Name;
					
					string destDirFullPath =  (dirName != destDirName) ? Path.Combine(dest, dirName) : dest;
					if (dirName != destDirName)
					{
						Debug.LogError ("CONSOLE LOG  dirName != destDirName");
					}

					if (Directory.Exists(destDirFullPath))
					{
						MoveDirRecursive(path, destDirFullPath, Path.Combine(failedMoveDir,dirName));
					}
					else
					{
						System.IO.Directory.Move(path, dest);
					}

					//Debug.Log(String.Format("MoveResourceBundlesToBundleDir moved {0} -> {1}", path, dest));
				}

				if (System.IO.File.Exists(pathMeta))
				{
					try
					{
						if (File.Exists(destMeta))
						{
							File.Delete(destMeta);
						}
						System.IO.File.Move(pathMeta, destMeta);
					}
					catch (System.Exception e)
					{
						Debug.Log("Unable to move meta file "+pathMeta+" -> "+destMeta+" exception: "+e.ToString());
					}
					//Debug.Log(String.Format("MoveResourceBundlesToBundleDir moved {0} -> {1}", path, dest));
				}				
			}
			catch (System.Exception e)
			{
				Debug.LogError(String.Format("Exception during MoveResourceBundlesToBundleDir moving {0} -> {1}: {2} ", path, dest, e.ToString()));
			}
		}	
	}
	
	
	[MenuItem("Helpers/Hide Bundles Dir")]
	public static void HideBundlesDirs()
	{
		Debug.Log("HideBundlesDirs");
		MoveBundleDirs(ResourcesBundleDir, TempBundleDir);
	}
	
	[MenuItem("Helpers/Unhide Bundles Dir")]
	public static void UnHideBundlesDirs()
	{
		Debug.Log("UnHideBundlesDirs");
		MoveBundleDirs(TempBundleDir, ResourcesBundleDir);
	}
	
	[MenuItem("Build/Utils/Build External Bundles Only")]
	public static void BuildEditorDataOnly()
	{
		// make sure we are in the right state for bundling.
		//UnHideBundlesDirs();
		Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.None | Bundler.BundleOptions.Extended | Bundler.BundleOptions.Uncompressed | Bundler.BundleOptions.ExternalOnly );
	}

	public static void BuildEditorDataOnlyQuick()
	{
		// Making the assumption that if packs.txt exists then cubemaps have been built because they are built for every type of bundle step
		Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.None | Bundler.BundleOptions.Extended | Bundler.BundleOptions.Uncompressed | Bundler.BundleOptions.ExternalOnly | Bundler.BundleOptions.BuildNotExistsOnly);
	}
	
	// Just quickly checking that we've bundled at least once. not checking in depth (at least for now)
	public static void BundlesBuiltSanityCheck()
	{
		// make sure we are in the right state for bundling.
		//UnHideBundlesDirs();
		Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.None | Bundler.BundleOptions.Extended | Bundler.BundleOptions.Uncompressed | Bundler.BundleOptions.BuildNotExistsOnly );
	}	
	
	public static void BuildBundles(Bundler.BundleOptions bundleOptions = (Bundler.BundleOptions.None | Bundler.BundleOptions.Extended | Bundler.BundleOptions.Uncompressed))
	{
		// make sure we are in the right state for bundling.
		//UnHideBundlesDirs();
		Bundler.BuildAll( BuildSettings.BundlerConfigFolder, bundleOptions); 
	}
	
	[MenuItem("Build/Utils/Clean Local Data")]
	public static void CleanData()
	{
		Debug.Log("Removing previous bundles: "+BuildSettings.BuildFolder);
		BuildUtils.ForceDeleteDirectory(BuildSettings.BuildFolder);
	}	
	
	[MenuItem("Build/Utils/Bundle Force")]
	public static void BuildBundlesForceBase()
	{
		Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.Force ); 
	}
}
