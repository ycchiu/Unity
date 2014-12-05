using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public partial class Builder
{
#if UNITY_ANDROID
	static bool _debug = true;
	static bool _useobb = false;
	
	[MenuItem("Builder/Build Debug APK")]
	public static void BuildAndroidEBG()
	{
		_debug = true;
		BuildSettings.Config = BuildSettings.BuildConfig.Debug;
		BuildSettings.DevBundleMode = EB.Assets.DevBundleType.StandardBundles;
		BuildSettings.SaveSettings();		
		BuildAPK(BuildSettings.SuccessEmail);
	}
	
	[MenuItem("Builder/Build Submission APK")]
	public static void BuildAndroidKabam()
	{
		_debug = false;
		BuildSettings.Config = BuildSettings.BuildConfig.Release;
		BuildSettings.DevBundleMode = EB.Assets.DevBundleType.StandardBundles;
		BuildSettings.SaveSettings();		
		BuildAPK(BuildSettings.SuccessEmail);
	}
	
	[MenuItem("Builder/Build OBB")]
	public static void BuildAndroidOBB()
	{
		_debug = true;
		_useobb = true;
		BuildSettings.Config = BuildSettings.BuildConfig.Debug;
		BuildSettings.DevBundleMode = EB.Assets.DevBundleType.StandardBundles;
		BuildSettings.SaveSettings();
		BuildOBB();
	}

	[MenuItem("Builder/Build Submission NoOBB APK")]
	public static void BuildAndroidKabamNoOBB()
	{
		_debug = false;
		_useobb = false;
		BuildAPK(BuildSettings.SuccessEmail);
	}
	
	public static string BuildOBB()
	{
		try
		{			
			var obbFile 		= Bundler.BuildOBB();
			var cl 		= EnvironmentUtils.Get("BUILD_CL", "0" );			
			var url 			= S3Utils.Put(obbFile, Path.Combine(cl,cl+".obb") );  
			return url;
		}
		catch( System.Exception ex )
		{
			EB.Debug.Log("BuildOBB Failed: exception: " + ex.ToString());
			throw ex;
		}		
	}
	
	public static void BuildAPK(string distributionList)
	{
		try
		{			
			var folder = BuildSettings.BaseBuildFolder;
			var platformFolder = BuildSettings.BuildFolder;
			
			// moko: changed to do a debug dump of all builder job info first
			var date 	= System.DateTime.Now.ToString("dd/MM/yy HH:mm");
			string header = "*******************************************************************************\n";
			header += "Building to " + folder + " @" + date;
			EB.Debug.Log(header);			
			EB.Debug.Log("Build Setting Parameters:\n" + BuildSettings.ToString());
			EB.Debug.Log("Envirnoment Setting Parameters:\n" + EnvironmentUtils.GetEnvirnomentDetails());
			
			// cleanup
            GeneralUtils.DeleteDirectory(platformFolder, true);   // mko: cleaning up build folder
            GeneralUtils.DeleteDirectory("Android", true);   // mko: cleaning up build folder
			Directory.CreateDirectory(folder);
			Directory.CreateDirectory(platformFolder);
			
			var version = "1.0";
			try {
				var parts= PlayerSettings.bundleVersion.Split('.');
				version = parts[0] + "." + parts[1];
			}
			catch {
			}
			
			// moko: prepend #define instead of replace in case there is some specific settings stored in playerSetting
			string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
			if (_debug)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "USE_DEBUG;" + defines);
			}
			else
			{
				if( _useobb == true )
				{
					PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "USE_OBB;" + defines);
				}
			}
						
			var cl 		= EnvironmentUtils.Get("BUILD_CL", "0" );
			var desc	= "Android " + date + " CL: " + cl + "\n";
			var notes	= (ArrayList)EB.JSON.Parse( EnvironmentUtils.Get("BUILD_NOTES", "[]") );
			
			PlayerSettings.bundleVersion = version + "." + cl;
			PlayerSettings.bundleIdentifier = BuildSettings.BundleIdentifier;
			PlayerSettings.use32BitDisplayBuffer = false;
			
			WriteVersionFile(version + "." + cl );
			
			EB.Debug.Log("Desc: " + desc );
			
			// disable scenes that are the tracks
			var settings = new List<EditorBuildSettingsScene>();
			foreach ( var setting in UnityEditor.EditorBuildSettings.scenes )
			{
				if (setting.path.Contains("tracks"))
				{
					setting.enabled = !_useobb;
				}
				
				if (_debug == false && setting.path.Contains("selector") )
				{
					setting.enabled = false;
				}
				
				settings.Add(setting);
			}
			EditorBuildSettings.scenes = settings.ToArray();
			
			// build bundles
			DisplayProgressBar("Building", "Building Bundles", 0.0f);
			Bundler.BundleOptions bundleOptions = Bundler.BundleOptions.Force | Bundler.BundleOptions.SkipOgg;
			if( _debug || ( _useobb == false ) )
			{
				bundleOptions |= Bundler.BundleOptions.Extended;
			}
			
			Bundler.BuildAll(BuildSettings.BundlerConfigFolder, bundleOptions );
			
			// build the player 
			var apkPath = Path.Combine(folder, cl + ".apk");
						
			PlayerSettings.Android.bundleVersionCode = int.Parse(cl);
			
			if (!_debug)
			{
				PlayerSettings.bundleVersion = BuildSettings.Version;
			}
			else
			{
				PlayerSettings.Android.bundleVersionCode = 1;	
			}
			
			EB.Debug.Log("Building to " + apkPath);
			
//			PlayerSettings.Android.keyaliasPass = "123456";		//NOTE: this is set already in AndroidUtils.BuildPlayer. 
//			PlayerSettings.Android.keystorePass = "123456"; 		
			
			AndroidUtils.BuildPlayer(apkPath, BuildOptions.None);
			
			if (!File.Exists(apkPath))
			{
				throw new System.Exception("Failed to build apk!");
			}
			
			// upload to s3
			string url = S3Utils.Put( apkPath, string.Format("{0}-{1}-{2}.apk", PlayerSettings.bundleIdentifier, PlayerSettings.bundleVersion, cl) ); 
			
			
			// send email
			var data = new Hashtable();
			data["cl"] = cl;
			data["title"] = desc;
			data["url"] = url;
			data["notes"] = notes;
			data["obb"] = "";
			
			if (!_debug && _useobb)
			{
				data["obb"] = BuildOBB();	
				UploadApkFiles(apkPath, WWWUtils.Environment.Prod);
			}
			
			Email( distributionList, BuildSettings.ProjectName + " - Android Build: " + PlayerSettings.bundleVersion + (_debug ? " DEBUG" : " SUBMISSION"), File.ReadAllText("Assets/Editor/EB.Core.Editor/Build/Email/androidbuild.txt"), data );  
		
			Done();
		}
		catch(System.Exception e)
		{
			EB.Debug.Log("Build Failed: exception: " + e.ToString());
			Failed(e);
		}
		
		ClearProgressBar();
	}
	
	[MenuItem("Builder/Protect APK (Testing)")]
	static void Test()
	{
		UploadApkFiles("android.apk", WWWUtils.Environment.Local);
	}
	
	
	static void UploadApkFiles( string apkPath, WWWUtils.Environment env )
	{
		try 
		{
			var tmpDir = "/tmp/apk";
			GeneralUtils.DeleteDirectory(tmpDir, true);   // mko: cleaning up build folder
			Directory.CreateDirectory(tmpDir);
			
			// unzip
			var res = CommandLineUtils.Run("/usr/bin/unzip", string.Format("-d {0} {1}", tmpDir, apkPath) );
			EB.Debug.Log(res);
				
			// generate the app.dll
			var files = new List<string>( Directory.GetFiles("/tmp/apk/assets/bin/Data/Managed","*.dll",SearchOption.TopDirectoryOnly) );
			files.Sort( System.StringComparer.OrdinalIgnoreCase );
			
			List<byte> bytes = new List<byte>();
			foreach (string filePath in files )
			{
				EB.Debug.Log("Adding file " + filePath);
				bytes.AddRange( File.ReadAllBytes(filePath ) ); 
			}
			
			EB.Debug.Log("MSIL size is " + EB.Localizer.FormatNumber(bytes.Count,true) );
			
			WWWForm form = new WWWForm();
			form.AddBinaryData("data", bytes.ToArray(), "data" );
			form.AddField("version", EB.Version.GetVersion() );
			form.AddField("platform", "android");
			form.AddField("sha1", EB.Encoding.ToBase64String( EB.Digest.Sha1().Hash(bytes.ToArray()) ));    
			
			var stoken = WWWUtils.AdminLogin(env);
			var postUrl = WWWUtils.GetAdminUrl(env) + "/protect/upload?stoken="+stoken;
			res = WWWUtils.Post(postUrl, form);
			EB.Debug.Log("version: " + res);
		}
		catch(System.Exception e)
		{
			EB.Debug.Log("Build Failed: exception: " + e.ToString());
			Failed(e);
		}
	}
#endif
	
}
