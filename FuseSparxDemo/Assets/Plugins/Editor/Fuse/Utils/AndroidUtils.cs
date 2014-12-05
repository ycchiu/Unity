//#define AUTO_ADB_SYNC_ANDROID
//#define USE_P4CONNECT
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EB.BundleServer;


public static class AndroidUtils
{

	public static string GetADBPath() 
	{
		string adbPath = "";
		if(Application.platform == RuntimePlatform.WindowsEditor)
			adbPath = "C:\\Program Files (x86)\\Android\\android-sdk\\platform-tools\\adb.exe";
		else if(Application.platform == RuntimePlatform.OSXEditor)
			adbPath = "/opt/android-sdk-macosx/platform-tools/adb";
		else
			throw new System.Exception("Unsupported Runtime Platform!");
		if (!File.Exists(adbPath))
		{
			Debug.LogError("Didn't fine adb path @ "+adbPath);
		}
		return adbPath;
	}

	public static void BuildPlayer( string apkPath, BuildOptions options )
	{
		BuildSettings.Options = options;
		PlayerSettings.Android.keyaliasPass = "123456";
		PlayerSettings.Android.keystorePass = "123456"; 
		
		Debug.Log("Building to " + apkPath);
		
		
		string[] originalScenes = BuildSettings.GetScenesFromEditorSettings();
		
		
		string[] buildScenes = originalScenes;
		
		
		
		
		
		if (BuildSettings.IsDevelopmentBuild)
		{		
			if (BuildSettings.DevBundleMode == EB.Assets.DevBundleType.BundleServer)	// only the basic levels are built in to the player, others are separate bundles for smaller packages while debugging
			{
				List<string> filteredScenes = new List<string>();
				foreach ( string scene in originalScenes )
				{
					string lowerScene = scene.ToLower();
					foreach (string playerBuilt in BuildSettings.PlayerBuiltInScenes)
					{
						if (lowerScene.Contains(playerBuilt))
						{
							filteredScenes.Add(scene);
							Debug.Log("Player Building scene: "+scene);
							break;
						}
					}
				}
				buildScenes = filteredScenes.ToArray();
			}			
		}
		else
		{
			List<string> tmp = new List<string>(originalScenes);
			//NOTE: including the selector for now.
			//			if (tmp[0].Contains("selector"))
			//			{
			//				Debug.Log("REMOVING SELECTOR SCENE");
			//				tmp.RemoveAt(0);
			//				
			//			}
			buildScenes = tmp.ToArray();
		}
		
		EB.Debug.Log("Building player with...");
		foreach (string s in buildScenes)
		{
			EB.Debug.Log("\t" + s);
		}
		
		bool movedBundles = false;
#if USE_P4CONNECT
		bool isP4ConnectEnabled = P4Connect.Config.PerforceEnabled;
#endif
		try {
			if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
			{
				Debug.Log("Moving Resources/Bundles out of Assets/ to build Player ");
				movedBundles = true;
#if USE_P4CONNECT				
				P4Connect.Config.PerforceEnabled = false;
#endif			
				BuildUtils.HideBundlesDirs();
				//EditorUtility.UnloadUnusedAssets();
				
				if (UnityEditor.VersionControl.Provider.isActive)
				{
					Debug.LogError("Warning: Running AssetDatabase.Refresh with Unity VersionControl active... could check out a lot of files.");
				}
				// only do this on the build server (no UNITY P4 version control set up) because it will check out every file we just moved for delete (works fine with the 3rd party plugin)
				Debug.Log("Refreshing Asset Database to remove references in ASsets/Resources/Bundles");
				AssetDatabase.Refresh();
				
			}
			Debug.Log("***** BuildPipeline.BuildPlayer" + apkPath);
			BuildOptions filteredBuildOptions = options;
			filteredBuildOptions = EB.Flag.Unset(filteredBuildOptions, BuildOptions.AutoRunPlayer); // filter out autorun because it will install & run before the postprocess has run.....
			BuildPipeline.BuildPlayer( buildScenes, apkPath, BuildTarget.Android, filteredBuildOptions );
			Debug.Log("***** BuildPipeline.BuildPlayer DONE");
		}
		finally
		{
			if (movedBundles)
			{
				Debug.Log("Moving Resources/Bundles back to Assets/ after building Player");
				BuildUtils.UnHideBundlesDirs();
#if USE_P4CONNECT					
				P4Connect.Config.PerforceEnabled = isP4ConnectEnabled;
#endif
				//EB.Perforce.Perforce.P4RevertUnchanged();
			}
		}
		
		if (!File.Exists(apkPath))
		{
			throw new System.Exception("Failed to build apk!");
		}		
		
	}
	
	public static bool HasDevicesConnected()
	{
		var res = CommandLineUtils.Run(GetADBPath(), "devices");
		var index = res.IndexOf("List of devices attached");
		if (index >= 0)
		{
			var endl = res.IndexOf('\n', index+1);
			if (endl >= 0)
			{
				var result = false;
				var devices = res.Substring(endl+1).Split('\n');
				foreach( var device in devices )
				{
					if (device.Contains("device") )
					{
						EB.Debug.Log("device: " + device.Split(' ')[0] );
						result = true;
					}
				}
				return result;
			}
		}
		return false;
	}
	
	public static void Uninstall( string bundleIdentifier )
	{
		var res = CommandLineUtils.Run(GetADBPath(), "uninstall " + bundleIdentifier );
		EB.Debug.Log(res);
	}
	
	public static void Install( string apk )
	{
		var res = CommandLineUtils.Run(GetADBPath(), "install " + apk );
		EB.Debug.Log(res);
	}

	public static void Reinstall( string apk )
	{
		var res = CommandLineUtils.Run(GetADBPath(), "install -r " + apk );
		EB.Debug.Log(res);
	}
			
#if UNITY_ANDROID

	public struct FileTimeEntry
	{
		public string 	filePath;
		public long		fileTime;
		public FileTimeEntry(string path, long time)
		{
			filePath = path;
			fileTime = time;
		}
	}

	public static void SyncFilesToSDCard(string localDir, string remoteSdcardDir)
	{
		System.Diagnostics.Debug.Assert(Directory.Exists(localDir));
		
		string adb = GetADBPath();
		
		Hashtable fileToTimestampHash = new Hashtable();
		
		if (!remoteSdcardDir.StartsWith("/"))
		{
			//remoteSdcardDir = remoteSdcardDir.Substring(1);
			remoteSdcardDir += "/";
		}
		if (remoteSdcardDir.EndsWith("/"))
		{
			remoteSdcardDir = remoteSdcardDir.Substring(0, remoteSdcardDir.Length - 1);
		}
		if (!localDir.EndsWith("/"))
		{
			//localDir = localDir.Substring(0, localDir.Length - 1);
			localDir += "/";
		}		
				
		bool foundSdCardDir = false;
		
		foreach (string sdcardPath in EB.Loader.SDCardPaths)
		{
			string remotePath = sdcardPath + remoteSdcardDir;
			int remotePathLength = remotePath.Length;
					
			string res = CommandLineUtils.Run(adb, "shell ls -laR " + sdcardPath + "/Android/data/" );
			
			string[] lines = res.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries );
			
			if (lines.Length == 1)
			{
				Debug.Log("adb listing failed: "+lines[0]);
				continue;
			}
			else
			{
				foundSdCardDir = true;
				
				res = CommandLineUtils.Run(adb, "shell mkdir -p " + remotePath);	// make sure the directory exists
				
				if (res != "")
				{
					Debug.LogError("Error creating remote dir: "+remotePath+" error: "+res);
				}
				
				
				res = CommandLineUtils.Run(adb, "shell ls -laR " + remotePath );
				
				lines = res.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries );
				
			
				string dirPath = null;
				foreach( string line in lines )
				{
					if (line.StartsWith("/"))
					{
						dirPath = line.Substring(remotePathLength);
						if (dirPath.EndsWith(":"))
						{
							dirPath = dirPath.Substring(0, dirPath.Length -1);
						}
						if (dirPath.StartsWith("/"))
						{
							dirPath = dirPath.Substring(1);
						}
						if (dirPath.Length > 0 && !dirPath.EndsWith("/"))
						{
							dirPath += "/";
						}
					}
					else
					{
						if (line.StartsWith("d"))
						{
							//skip directories
							continue;
						}
						string[] lineElems = line.Split(new char [] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
						string dateTimeString = string.Format("{0} {1}", lineElems[4], lineElems[5]);
						System.DateTime fileTime = System.DateTime.ParseExact(dateTimeString, "yyyy-MM-dd HH:mm", null);
						string fileName = lineElems[6];
						
						string relFilePath = dirPath+fileName;
						string fullFilePath = remotePath+"/"+dirPath+fileName;
						FileTimeEntry entry = new FileTimeEntry(fullFilePath, fileTime.Ticks);
						
						fileToTimestampHash.Add(relFilePath, entry);	// using add because a hash collision would mean bad things so we want to know about it
						
					}
				}

				
				string[] localFiles = Directory.GetFiles(localDir, "*", SearchOption.AllDirectories);
				
				int localDirLength = localDir.Length;
				
				foreach( string localFile in localFiles )
				{
					string localRelPath = localFile.Substring(localDirLength);
					
					long localTime = System.IO.File.GetLastWriteTime(localFile).Ticks;
					
					bool updateFile = true;
					if (fileToTimestampHash.ContainsKey(localRelPath))
					{
						FileTimeEntry entry = (FileTimeEntry)fileToTimestampHash[localRelPath];

						if (localTime <= entry.fileTime)
						{
							updateFile = false;
						}
						fileToTimestampHash.Remove(localRelPath);
					}
					if (updateFile)
					{
						string adbArgs = "push "+localDir+localRelPath+" "+remotePath+"/"+localRelPath;
						Debug.Log("Updating file : adb "+adbArgs);
						string result = CommandLineUtils.Run(adb, adbArgs);
						if (!string.IsNullOrEmpty(result))
						{
							Debug.LogError("FAILED: adb "+adbArgs+" : ERROR: "+result);
						}
					}
				}
				
				for(var iter = fileToTimestampHash.GetEnumerator(); iter.MoveNext();)
				{
					FileTimeEntry curr = (FileTimeEntry)iter.Value;
					string adbArgs = "shell rm -f "+curr.filePath;
					Debug.Log("Removing file : adb "+adbArgs);
					string result = CommandLineUtils.Run(adb, adbArgs);
					if (!string.IsNullOrEmpty(result))
					{
						Debug.LogError("FAILED: adb "+adbArgs+" : ERROR: "+result);
					}
				}
				
				break;
			}
		}
		if (!foundSdCardDir)
		{
			Debug.LogError("Couldn't find remote directory in /mnt/sdcard (or variants of that) "+remoteSdcardDir);
		}
	}
	
	
	[MenuItem("Helpers/Sync Android Files")]
	public static void SyncAndroidFiles()
	{
		System.DateTime startSync = System.DateTime.Now;
		SyncFilesToSDCard(BuildSettings.BuildFolder, "/Android/data/"+PlayerSettings.bundleIdentifier+"/files/");
		Debug.Log("Sync Files Time: "+(System.DateTime.Now - startSync).ToString()+" seconds.");
	}
	
	[MenuItem("Build/Build Bundle Data Only (No Code or Resources)", false, 1)]
	public static void BuildDataOnly()
	{
		BuildOptions options = (BuildOptions.Development | BuildOptions.UncompressedAssetBundle);
		BuildDataOnly(options);
		LaunchGameAndroid();
	}
	
	public static void BuildDataOnly(BuildOptions options)
	{
		//AndroidConvertor.IsBuilding = true;
		try 
		{
			BuildSettings.Options = options;
			BuildUtils.BuildBundles();		
		}
		finally
		{
			//AndroidConvertor.IsBuilding = false;
		}
#if AUTO_ADB_SYNC_ANDROID 		
		if (BuildSettings.DevBundleMode == EB.Assets.DevBundleType.BundleServer)
		{
			System.DateTime startSync = System.DateTime.Now;
			SyncFilesToSDCard(BuildSettings.BuildFolder, "/Android/data/"+PlayerSettings.bundleIdentifier+"/files/");
			Debug.Log("Sync Files Time: "+(System.DateTime.Now - startSync).ToString()+" seconds.");
		}
#endif
	}
			
	[MenuItem("Build/Build and Run", false, 3)]
	public static void BuildAndRun()
	{
		//AndroidConvertor.IsBuilding = true;
		try 
		{
			//open the bundle server window for configuration
			BundleServerWindow.BundleServerWidget();
			
			if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
			{
				BuildDataOnly(BuildOptions.Development | BuildOptions.UncompressedAssetBundle);
			}
			else
			{
				BuildUtils.BuildEditorDataOnly();
			}			
	
			BuildCodeAndRun();
		}
		finally
		{
			//AndroidConvertor.IsBuilding = false;
		}
	}
	
	[MenuItem("Build/Build Code (and Resources) and Run", false, 2)]
	public static void BuildCodeAndRun()
	{
		//AndroidConvertor.IsBuilding = true;
		try
		{
			//open the bundle server window for configuration
			if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
			{
				BuildUtils.BundlesBuiltSanityCheck();
			}
			else
			{
				BuildUtils.BuildEditorDataOnlyQuick();
			}
			BundleServerWindow.BundleServerWidget();
			BuildOptions options = BuildSettings.Options | BuildOptions.Development | BuildOptions.UncompressedAssetBundle;
			options = EB.Flag.Unset(options, BuildOptions.AutoRunPlayer);
			BuildPlayer( Directory.GetCurrentDirectory() +  "/android.apk", options);
			LaunchGameAndroid();
		}
		finally
		{
			//AndroidConvertor.IsBuilding = false;
		}
	}

	
	[MenuItem("Build/Utils/Uninstall")]
	public static void UnInstallApk()
	{
		Uninstall( PlayerSettings.bundleIdentifier );
	}

	[MenuItem("Build/Utils/Install")]
	public static void InstallApk()
	{
		Uninstall( PlayerSettings.bundleIdentifier );
		Install( Directory.GetCurrentDirectory() +  "/android.apk" );
	}

	[MenuItem("Build/Utils/Launch Android Game")]
	public static void LaunchGameAndroid()
	{	
		Debug.Log("LaunchGameAndroid");
		
		CommandLineUtils.Run(GetADBPath(), "shell am force-stop "+PlayerSettings.bundleIdentifier);
		string res = CommandLineUtils.Run(GetADBPath(), "shell am start "+PlayerSettings.bundleIdentifier+"/com.unity3d.player.UnityPlayerNativeActivity");
		Debug.Log(res);
	}
#endif	
}
