using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EB.Perforce;

[InitializeOnLoad]
[ExecuteInEditMode]
public static class BuildSettings 
{	
	// use lower case only names here. if listed here in build settings, these will not be built into bundles but always packaged with the player for development builds
	public static string[] PlayerBuiltInScenes = new string[] { "0_selector", "1_boot", "2_main" };	

	#region Class Functions
	public enum BuildConfig
	{
		Debug,		// moko: this is the default
		Release,
		Profile,
		Submission
	};

	public static BuildTarget Target		{ get { return EditorUserBuildSettings.activeBuildTarget; } set { EditorUserBuildSettings.SwitchActiveBuildTarget(value); }  }
	public static string BuildFolder 		{ get { return GetBuildFolder(Target); } }
	public static string PlatformFolder 	{ get { return GetPlatformFolder(Target); } }
	public static BuildOptions Options 		{ get { return _buildOptions; } set { _buildOptions = value; SaveSettings(); } }
	public static BuildConfig Config		{ get; set; }
	
	public static ArrayList DevelopmentLevelsBuild = new ArrayList();
	
	public static string BundleServerPort = "1235";
	public static string BundleServerAddress = "http://127.0.0.1:"+BundleServerPort;
		
	public static EB.Assets.DevBundleType DevBundleMode = EB.Assets.DevBundleType.NoBundles;
	
	public static bool IsDevelopmentBuild
	{ 
		get 
		{ 
			return (BuildSettings.Options & BuildOptions.Development) != 0; 
		}
		
		set
		{
			if (value)
			{
				_buildOptions |= BuildOptions.Development;
			}
			else
			{
				_buildOptions &= ~BuildOptions.Development;
			}
		}
	}
	private static BuildOptions _buildOptions = BuildOptions.None | BuildOptions.Development;	// defaulting to development build.
	
	private static string _buildConfigPath = null;
	public static string BuildConfigPath
	{
		get 
		{
			if (_buildConfigPath == null)
			{
				_buildConfigPath = Path.Combine(BaseBuildFolder, "BuildConfig.txt");
			}
			return _buildConfigPath;
		}
	}	
	
	public static string BaseBuildFolder
	{
		get
		{
#if UNITY_IPHONE
			// ios doesn't support different build folders with the current project file system
			return System.IO.Directory.GetCurrentDirectory();
#else
			return EnvironmentUtils.Get("BUILD_FOLDER", System.IO.Directory.GetCurrentDirectory() );
#endif
		}
	}
	
	public static string GetBuildVersion( string cl )
	{
		var current = PlayerSettings.bundleVersion;
		var parts = new List<string>(current.Split('.'));
		if (parts.Count > 0)
		{
			parts[parts.Count-1] = cl;
		}
		return EB.ArrayUtils.Join(parts, '.');
	}
	
	public static string GetPlatformFolder( BuildTarget target )
	{
		var folder = "out";
		
		switch(target)
		{
		case BuildTarget.WebPlayer:
		case BuildTarget.WebPlayerStreamed:
			folder = "web"; break;
		case BuildTarget.StandaloneOSXIntel:
		case BuildTarget.StandaloneWindows:
		case BuildTarget.StandaloneWindows64:
			folder = "data"; break;
		case BuildTarget.iPhone:
			folder = "ios"; break;
		case BuildTarget.Android:
			folder = "android"; break;
		}
		return folder;
	}
	
	public static string GetBuildFolder( BuildTarget target )
	{
		return System.IO.Path.Combine(BaseBuildFolder, GetPlatformFolder(target));
	}
	
	public static string[] GetScenesFromEditorSettings()
	{
		List<string> scenes= new List<string>();
		
		foreach( var scene in EditorBuildSettings.scenes )
		{
			if ( scene.enabled && System.IO.File.Exists(scene.path) && !scenes.Contains(scene.path) )
			{
				scenes.Add( scene.path );
			}
		}
		
		return scenes.ToArray();
	}

	public static string GetBuildDesc()
	{
		var cl = EnvironmentUtils.Get("BUILD_CL", EB.Time.ToPosixTime(System.DateTime.Now).ToString());
		var date = System.DateTime.Now.ToString("dd/MM/yy HH:mm");
		var projectName = EnvironmentUtils.Get("BUILD_PROJECT", "");
		var targetName = EnvironmentUtils.Get("BUILD_TARGET", "");
		return string.Format("[{0}-{1}] {2} {3} CL: {4}\n", projectName, targetName, BuildSettings.Target, date, cl);
	}
	public static string GetBuildSubject(bool isDebug)
	{
		var cl = EnvironmentUtils.Get("BUILD_CL", EB.Time.ToPosixTime(System.DateTime.Now).ToString());
		var projectName = EnvironmentUtils.Get("BUILD_PROJECT", "");
		var targetName = EnvironmentUtils.Get("BUILD_TARGET", "");
		return string.Format("[{2}][{0}-{1}] {3} Build: {4}@{6} {5}", projectName, targetName, BuildSettings.ProjectName, BuildSettings.Target, PlayerSettings.bundleVersion, (isDebug ? "DEBUG" : "Submission"), cl);
	}
	
	// moko: added a 'toString' method to get the buildsetting details. a bit abused the name 'toString' ;-P
	public static new string ToString()	
	{
		System.Type buildType = typeof(BuildSettings);
		string result = string.Format("Class: {0} [{1}]", buildType.Name, buildType.AssemblyQualifiedName);
		var flags = System.Reflection.BindingFlags.NonPublic
			|System.Reflection.BindingFlags.Public
			|System.Reflection.BindingFlags.Static
			|System.Reflection.BindingFlags.Instance;			
		System.Reflection.PropertyInfo[] props = buildType.GetProperties(flags);
		foreach (var p in props)
		{
			result += string.Format("\nProperty: {0}.{1} [{2}] = {3}", buildType.Name, p.Name, p.PropertyType.ToString(), p.GetValue(null, null));
		}	
		System.Reflection.FieldInfo[] fields = buildType.GetFields(flags);
		foreach (var f in fields)
		{
			result += string.Format("\nField: {0}.{1} [{2}] = {3}", buildType.Name, f.Name, f.FieldType.ToString(), f.GetValue(null));
		}	
		return result;
	}
	
	public static string GenerateBundleVersion(string cl)
	{
		var version = "1.0." + cl;
		try {
			var parts = BuildSettings.Version.Split('.');
			version = parts[0] + "." + parts[1] + "." + cl;
		}
		catch {
		}
		return version;
	}
	
	static BuildSettings()
	{
		UpdateFromSettings();
	}

	public static void SaveSettings()
	{
        try
        {               
            if (File.Exists(BuildConfigPath) && ((File.GetAttributes(BuildConfigPath) & FileAttributes.ReadOnly) != 0))
            {
                EB.Perforce.Perforce.P4Checkout(BuildConfigPath);
            }
        }
        catch (System.Exception ex)
        {
            EB.Debug.LogError("BuildSetting can not save file: " + BuildConfigPath);
        }

		Hashtable bundleServerConfig = new Hashtable();
		bundleServerConfig["BundleServerAddress"] = BuildSettings.BundleServerAddress;
		bundleServerConfig["BundleMode"] = (int)BuildSettings.DevBundleMode;
		bundleServerConfig["BuildOptions"] = (int)BuildSettings.Options;
		bundleServerConfig["DevLevelsBuild"] = BuildSettings.DevelopmentLevelsBuild;

		try
		{
			var path = BuildConfigPath;
			var dir = System.IO.Path.GetDirectoryName(path);
			if (!System.IO.Directory.Exists(dir))
			{
				System.IO.Directory.CreateDirectory(dir);
			}
			
			File.WriteAllText(BuildConfigPath, EB.JSON.Stringify(bundleServerConfig));
		}
		catch (System.Exception ex)
		{
			Debug.LogError("Couldn't save BuildConfig settings: " + ex.ToString());
		}
	}
	
	public static void UpdateFromSettings()
	{
		Hashtable bundleServerConfig = null;
		if (File.Exists(BuildConfigPath))
		{
			bundleServerConfig = (Hashtable)EB.JSON.Parse(File.ReadAllText(BuildConfigPath));
		}
		else
		{
			bundleServerConfig = new Hashtable();
		}
		BuildSettings.BundleServerAddress = EB.Dot.String("BundleServerAddress", bundleServerConfig, "http://127.0.0.1:"+BuildSettings.BundleServerPort);
		BuildSettings.DevBundleMode = (EB.Assets.DevBundleType)EB.Dot.Integer("BundleMode", bundleServerConfig, (int)EB.Assets.DevBundleType.NoBundles);
		BuildSettings.Options = (BuildOptions)EB.Dot.Integer("BuildOptions", bundleServerConfig, (int)BuildSettings.Options);
		BuildSettings.DevelopmentLevelsBuild = EB.Dot.Array("DevLevelsBuild", bundleServerConfig, null);
		if (BuildSettings.DevelopmentLevelsBuild == null || BuildSettings.DevelopmentLevelsBuild.Count == 0)
		{
			BuildSettings.DevelopmentLevelsBuild = new ArrayList(GetScenesFromEditorSettings());
		}
	}
	#endregion
	
	#region Project Specific Settings
	public const string SceneFolder = "Assets/Scenes";
	public const string SceneExt 	= "unity";	
	public const string ScenePrefix = "";
	
	public const string ProjectName = "Booble";
	
	public static string Version
	{
		get 
		{
			switch (BuildSettings.Target)
			{
				case BuildTarget.iPhone:
					return "0.0.1";
				case BuildTarget.Android:
					return "0.0.1";
				case BuildTarget.WebPlayer:
					return "0.0.1";
			}
			return "0.0.0";		// moko: default version for unrecognized platform
		}
	}
	
	public static string BundleIdentifier 
	{
		get 
		{
			switch (BuildSettings.Target)
			{
			case BuildTarget.iPhone:
                return "com.kabam.fusesandbox";
			case BuildTarget.Android:
                return "com.kabam.fusesandbox";
			}
            return "com.kabam.fusesandbox";		// moko: default bundle name for unrecognized platform
		}
	}
	
	// iOS deployment settings
    public const string ProfileDev   = "Fuse_Sandbox_Dev";
    public const string ProfileAdHoc = "Fuse_Sandbox_Adhoc";
	public const string ProfileQA	 = "Sculpin_QA_Kabam_F6";
	public const string ProfileSubmission = "BOOBLE_AppStore";
	
    public const string CertDev      = "'iPhone Developer: Jeff Howell (P8AL3PZS8A)'";
    public const string CertDist     = "'iPhone Distribution: Kabam Inc. (43VP5W2U27)'";
    public const string CertQA       = "'iPhone Distribution: Sculpin QA'";
    public const string CertSubmission = "'iPhone Distribution: Kabam Inc.'";
	
	// bundles
	public const string BundlerConfigFolder 			= "Assets/Config";
	public const Bundler.BundleOptions BundlerOptions 	= Bundler.BundleOptions.Force | Bundler.BundleOptions.SkipOgg | Bundler.BundleOptions.Extended;	 	// moko: added extended bundle to the web player build
	
	//	email
	public const string SuccessEmail = "ebgcoretech@watercooler-inc.com";
	public const string SuccessTestFlightEmail = "EBG";
	#endregion
}
