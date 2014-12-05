//#define USE_P4CONNECT
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class iOSUtils
{
	private static string XCodeApp 
	{
		get
		{
			return EnvironmentUtils.Get("XCodeApp", "Xcode.app");	
		}
	}
	
	private static bool UseTerminalForXCode 
	{
		get
		{
			bool useTerminalForXCode = false;
			if( EnvironmentUtils.Get("UseTerminalForXCode", "false") == "true" )
			{
				useTerminalForXCode = true;
			}
		
			return 	useTerminalForXCode;
		}
	}
	
	public const string kProfilePath = "Assets/Editor/Profiles";

	public static string GetProvisioningProfilePath(string profilename)
	{		
		// copy provisioning profiles
		return string.Format("{0}/{1}/{2}.mobileprovision", Directory.GetCurrentDirectory(), kProfilePath, profilename );
	}
	
	public static void CopyProvisioningProfile( string profilename )
	{
		var path = GetProvisioningProfilePath(profilename);	
		try 
		{
			var dir = string.Format("/Users/{0}/Library/MobileDevice/Provisioning Profiles", System.Environment.UserName);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir); 
			}
			File.Copy( path, string.Format("{0}/{1}.mobileprovision", dir, profilename), true );	
		}
		catch (System.Exception ex)
		{
			EB.Debug.LogWarning("IOSUtils > CopyProvisioningProfile Warning: " + ex.ToString());
		}
	}
	
	public static void CopyProvisioningProfiles()
	{
		Debug.Log("CopyProvisioningProfiles");

		var dir = string.Format("/Users/{0}/Library/MobileDevice/Provisioning Profiles", System.Environment.UserName);
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir); 
		}

		foreach( var file in Directory.GetFiles(kProfilePath, "*.mobileprovision") )
		{
			EB.Debug.Log("File: " + file);
			try 
			{
				var targetFile = string.Format("{0}/{1}.mobileprovision", dir, Path.GetFileNameWithoutExtension(file));	// moko: make sure the target location is writeable
				VersionControllUtils.CheckOutFile(targetFile, VersionControllUtils.CheckOutType.ForceWriteable);
				File.Copy( file, targetFile, true );	
			}
			catch (System.Exception ex)
			{
				EB.Debug.LogWarning("IOSUtils > CopyProvisioningProfile Warning: " + ex.ToString());
			}
		}
	}
	
	public static string GetBuildPath()
	{
		return Path.Combine( BuildSettings.BaseBuildFolder, "iPad" );
	}
	
	public static string GetProductName()
	{
		var parts = PlayerSettings.bundleIdentifier.Split('.');
		return parts[parts.Length-1];
	}
	
	public static void InstallMobileProvisions()
	{
		 CommandLineUtils.Run("cp", "-Rf Assets/Editor/Profiles/* ~/Library/MobileDevice/Provisioning\\ Profiles/");
	}
	
	public static void BuildiOSPlayer( string profile, bool debug, BuildOptions options)
	{
		BuildSettings.Options = options;
		var path = GetBuildPath();
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		
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
		
		// copy profiles
		CopyProvisioningProfile(profile);
									
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
				Debug.Log("Moving Resources/Bundles out of Assets/ to build iOS Player ");
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
			Debug.Log("***** BuildPipeline.BuildPlayer");
			BuildPipeline.BuildPlayer( buildScenes, path, BuildTarget.iPhone, options );	
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
	}
	
	public static void CompileiOSPlayer( string profilename, string cert, string ipaFile, bool debug )
	{
	
		//InstallMobileProvisions();
		
		var profilepath = GetProvisioningProfilePath(profilename);
		var dir =  GetBuildPath();
		
		FileUtil.DeleteFileOrDirectory(ipaFile);
		
		var configuration = debug ? "Debug" : "Release";
		string buildOutput = CommandLineUtils.Run( string.Format("/Applications/{0}/Contents/Developer/usr/bin/xcodebuild", XCodeApp),
								string.Format("-project {0}/Unity-iPhone.xcodeproj -configuration {1} -arch armv7 -target \"Unity-iPhone\" clean build", dir, configuration),
								Directory.GetCurrentDirectory(),
								UseTerminalForXCode );
								
		Debug.Log("Build Output: "+buildOutput);
				
		// copy the profile
		string cmdLine = string.Format("-sdk iphoneos PackageApplication -v {1}/iPad/build/{0}.app -o {3} --sign {2} --embed {4}", GetProductName(), BuildSettings.BaseBuildFolder, cert, ipaFile, profilepath );
		string packageOutput = CommandLineUtils.Run(string.Format("/Applications/{0}/Contents/Developer/usr/bin/xcrun",XCodeApp), cmdLine, dir, UseTerminalForXCode );
		Debug.Log("Package Output: "+packageOutput);
		
		if (!File.Exists(ipaFile))
		{
			throw new System.Exception("CompileiOSPlayer Failed: BuildOutput: "+buildOutput);
		}
		
		// create the dsym file file
		// /usr/bin/zip -r /Unity/rivets/1.zip /Unity/rivets/iPad/build/rivets.app.dSYM/
		var zipName = Path.GetFileNameWithoutExtension(ipaFile);
		var zipDir  = Path.GetDirectoryName(ipaFile);
		var zipFile = Path.Combine(zipDir, zipName);
		
		CommandLineUtils.Run("/usr/bin/zip", string.Format("-r {0}.dSYM.zip {2}/iPad/build/{1}.app.dSYM", zipFile, GetProductName(), BuildSettings.BaseBuildFolder ) );
	}
	
#if UNITY_IPHONE
	
	[MenuItem("Build/Build Bundle Data Only (No Code or Resources)", false, 1)]
	public static void BuildDataOnlyiOS()
	{
		BuildSettings.Options = (BuildOptions.Development | BuildOptions.UncompressedAssetBundle);
		BuildUtils.BuildBundles();
	}
	
	[MenuItem("Build/Build Code (and Resources) and Run", false, 2)]
	public static void BuildCodeAndRun()
	{
		if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
		{
			BuildUtils.BundlesBuiltSanityCheck();
		}
		BuildOptions options = BuildSettings.Options | BuildOptions.Development | BuildOptions.UncompressedAssetBundle | BuildOptions.AutoRunPlayer | BuildOptions.SymlinkLibraries;
		BuildiOSPlayer("0C0BD4ED-97A6-4088-9253-B2AAC48A27EF", true, options);
	}
	
	[MenuItem("Build/Build and Run", false, 3)]
	public static void BuildAndRun()
	{
		if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
		{
			BuildDataOnlyiOS();
		}
		else
		{
			BuildUtils.BuildEditorDataOnly();
		}
		BuildCodeAndRun();
	}
	
#endif
	
}
