using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public partial class Builder
{
#if UNITY_IPHONE
	static string _profile 	= BuildSettings.ProfileAdHoc;
	static string _cert 	= BuildSettings.CertDist;
	static bool   _debug	= true;
	
	[MenuItem("Builder/Build iPad (TestFlight EBG)")]
	public static void BuildTestFlightEBG()
	{
		BuildSettings.DevBundleMode = EB.Assets.DevBundleType.StandardBundles;
		BuildSettings.SaveSettings();
        BuildTestFlight(BuildSettings.SuccessTestFlightEmail);
	}
	
	[MenuItem("Builder/Build iPad (TestFlight EBG+Kabam)")]
	public static void BuildTestFlightKabam()
	{
        BuildTestFlight(BuildSettings.SuccessTestFlightEmail);
	}
	

	[MenuItem("Builder/Build iPad (Submission)")]
	public static void BuildSubmission()
	{
		_profile	= BuildSettings.ProfileSubmission;
		_cert 		= BuildSettings.CertSubmission;
		_debug		= false;
		BuildIPA(BuildSettings.SuccessEmail);
	}
	
	public static void BuildIPA(string distributionList)
	{
		try
		{	
		
			BuildSettings.IsDevelopmentBuild = _debug;
			
			BuildUtils.CleanData();
						
			var folder = BuildSettings.BaseBuildFolder;
			var platformFolder = BuildSettings.BuildFolder;

			// moko: changed to do a debug dump of all builder job info first
			var date 	= System.DateTime.Now.ToString("dd/MM/yy HH:mm");
			string header = "*******************************************************************************\n";
			header += "Building to " + folder + " @" + date;
			EB.Debug.Log(header);
			EB.Debug.Log("Build Setting Parameters:\n" + BuildSettings.ToString());
			EB.Debug.Log("Envirnoment Setting Parameters:\n" + EnvironmentUtils.GetEnvirnomentDetails());
			
			EB.Editor.PostProcess.Signer = _cert.Trim('\'');
			
			// clean up old ipa files
			CommandLineUtils.Run("/bin/rm", "-f *.ipa");
			
			// cleanup
            GeneralUtils.DeleteDirectory(platformFolder, true);   // mko: cleaning up build folder
            GeneralUtils.DeleteDirectory("iPad", true);   // mko: cleaning up build folder
            
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(platformFolder);
            Directory.CreateDirectory("iPad");
			
			// always check for null.
			EditorUserBuildSettings.explicitNullChecks = true;
			
			var version = "1.0";
			try {
				var parts= PlayerSettings.bundleVersion.Split('.');
				version = parts[0] + "." + parts[1];
			}
			catch {
			}

			// moko: prepend #define instead of replace in case there is some specific settings stored in playerSetting
			string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iPhone);
			if (_debug)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iPhone, "USE_DEBUG;" + defines);
			}
									
			var cl 		= EnvironmentUtils.Get("BUILD_CL", "0" );
			var desc	= "iOS Univeral " + date + " CL: " + cl + "\n";
			var notes	= (ArrayList)EB.JSON.Parse( EnvironmentUtils.Get("BUILD_NOTES", "[]") );
			
			PlayerSettings.bundleVersion = version + "." + cl;
            PlayerSettings.bundleIdentifier = BuildSettings.BundleIdentifier;
			PlayerSettings.use32BitDisplayBuffer = true;
			WriteVersionFile(version + "." + cl );

			if (!_debug)
			{
				PlayerSettings.bundleVersion = BuildSettings.Version;
			}
			
			EB.Debug.Log("Desc: " + desc );
			
			// build bundles
			DisplayProgressBar("Building", "Building Bundles", 0.0f);
			
			BuildUtils.CleanData();
			if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
			{
				Bundler.BuildAll(BuildSettings.BundlerConfigFolder, BuildSettings.BundlerOptions | Bundler.BundleOptions.Extended );
			}
			else
			{
				Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.Extended |  Bundler.BundleOptions.ExternalOnly | Bundler.BundleOptions.Force );
			}
			
			// build the player
			DisplayProgressBar("Building", "Building Player", 0.0f);

			iOSUtils.BuildiOSPlayer(_profile, _debug, (_debug ? BuildOptions.Development : BuildOptions.None ));
						
			DisplayProgressBar("Building", "Building IPA", 0.0f);
			var ipaFile = Path.Combine( folder, cl+".ipa");
			FileUtil.DeleteFileOrDirectory(ipaFile);
			
			iOSUtils.CompileiOSPlayer(_profile, _cert, ipaFile, _debug);
			
			// upload to s3
			var ipaUrl = S3Utils.Put(ipaFile, string.Format("{0}-{1}-{2}.ipa", PlayerSettings.bundleIdentifier, PlayerSettings.bundleVersion, cl) );
			var dsymUrl = string.Empty;
			
			// upload symbols
			var zipName = Path.GetFileNameWithoutExtension(ipaFile) + ".dSYM.zip";
			var zipDir  = Path.GetDirectoryName(ipaFile);
			var zipFile = Path.Combine(zipDir, zipName);
			
			if ( File.Exists(zipFile) )
			{
				EB.Debug.Log("Adding symbols file");
				dsymUrl = S3Utils.Put(zipFile, Path.GetFileName(zipFile));
			}
			
			var size = new FileInfo(ipaFile).Length;
			
			var data = new Hashtable();
			data["cl"] = cl;
			data["size"] = size/(1024.0f*1024.0f);
			data["title"] = desc;
			data["ipaUrl"] = ipaUrl;
			data["dSymUrl"] = dsymUrl;
			data["notes"] = notes;
			Email(distributionList, BuildSettings.ProjectName + " - iOS Submission Build: " + version + "@" + cl, File.ReadAllText("Assets/Editor/EB.Core.Editor/Build/Email/submissionbuild.txt"), data);
			
			// build extended packs
			//BuildContentPacksWithOptions(true);
			Done();
		}
		catch(System.Exception e)
		{
			EB.Debug.Log("Build Failed: exception: " + e.ToString());
			Failed(e);
		}
		
		ClearProgressBar();
	}
	
	public static void BuildTestFlight(string distributionList)
	{
		try
		{			
		
			BuildSettings.IsDevelopmentBuild = _debug;
			
			BuildUtils.CleanData();
			
			var folder = BuildSettings.BaseBuildFolder;
			var platformFolder = BuildSettings.BuildFolder;

			// moko: changed to do a debug dump of all builder job info first
			var date 	= System.DateTime.Now.ToString("dd/MM/yy HH:mm");
			string header = "*******************************************************************************\n";
			header += "Building to " + folder + " @" + date;
			EB.Debug.Log(header);
			EB.Debug.Log("Build Setting Parameters:\n" + BuildSettings.ToString());
			EB.Debug.Log("Envirnoment Setting Parameters:\n" + EnvironmentUtils.GetEnvirnomentDetails());
			
			EB.Editor.PostProcess.Signer = _cert.Trim('\'');
			
			// clean up old ipa files
			CommandLineUtils.Run("/bin/rm", "-f *.ipa");
			CommandLineUtils.Run("/bin/rm", "-f *.dSYM.zip");
			
			// cleanup
            GeneralUtils.DeleteDirectory(platformFolder, true);   // mko: cleaning up build folder
            GeneralUtils.DeleteDirectory("iPad", true);   // mko: cleaning up build folder

			Directory.CreateDirectory(folder);
			Directory.CreateDirectory(platformFolder);
            Directory.CreateDirectory("iPad");
			
			// always check for null.
			EditorUserBuildSettings.explicitNullChecks = true;
			
			var version = "1.0";
			try {
				var parts= PlayerSettings.bundleVersion.Split('.');
				version = parts[0] + "." + parts[1];
			}
			catch {
			}
						
			var cl 		= EnvironmentUtils.Get("BUILD_CL", "0" );
			var desc	= "iOS Univeral " + date + " CL: " + cl + "\n";
			var notes	= (ArrayList)EB.JSON.Parse( EnvironmentUtils.Get("BUILD_NOTES", "[]") );
			
			// clean up the notes
			foreach( var note in notes )	
			{
				var user = EB.Dot.String("user", note, string.Empty);
				var task = EB.Dot.String("desc", note, string.Empty);
				var time = EB.Dot.Integer("date", note, 0);
								
				desc += string.Format("\n- ({0}@{1}): {2}", user, EB.Time.FromPosixTime(time), task);
			}
			
			PlayerSettings.bundleVersion = version + "." + cl;
			PlayerSettings.bundleIdentifier = BuildSettings.BundleIdentifier;
			PlayerSettings.use32BitDisplayBuffer = true;
			WriteVersionFile(PlayerSettings.bundleVersion);

			// Bill wanted this hardcoded for the ios7 submission - alim 9/10/13
			if (!_debug)
			{
				PlayerSettings.bundleVersion = BuildSettings.Version;
			}
			
			EB.Debug.Log("Desc: " + desc );
			
			// build bundles
			DisplayProgressBar("Building", "Building Bundles", 0.0f);
			
			BuildUtils.CleanData();
			if (BuildSettings.DevBundleMode != EB.Assets.DevBundleType.NoBundles)
			{
				Bundler.BuildAll(BuildSettings.BundlerConfigFolder, BuildSettings.BundlerOptions | Bundler.BundleOptions.Extended );
			}
			else
			{
				Bundler.BuildAll( BuildSettings.BundlerConfigFolder, Bundler.BundleOptions.Extended |  Bundler.BundleOptions.ExternalOnly | Bundler.BundleOptions.Force );
			}
			
			// build the player
			DisplayProgressBar("Building", "Building Player", 0.0f);
			iOSUtils.BuildiOSPlayer(_profile, _debug, (_debug ? BuildOptions.Development : BuildOptions.None ));
						
			DisplayProgressBar("Building", "Building IPA", 0.0f);
			var ipaFile = Path.Combine( folder, cl+"-adhoc.ipa");
			FileUtil.DeleteFileOrDirectory(ipaFile);
			
			iOSUtils.CompileiOSPlayer(_profile, _cert, ipaFile, _debug);
			
			string savedIpaDir = Path.Combine(Directory.GetCurrentDirectory(), "ipa_backup");
			Debug.Log("Copying IPA to "+(new DirectoryInfo(savedIpaDir)).FullName);
			try 
			{
				Directory.CreateDirectory(savedIpaDir);
				string destPath = Path.Combine(savedIpaDir, "last-adhoc.ipa");
				if (File.Exists(destPath))
				{
					File.Delete(destPath);
				}
				FileUtil.CopyFileOrDirectory(ipaFile, destPath);
			} 
			catch (System.Exception e)
			{
				Debug.Log("Failed to copy most recent build to "+savedIpaDir+ " e: "+e.ToString());
			}
			
			DisplayProgressBar("Building", "Uploading IPA", 0.0f);
			bool notify = true;
			if (CommandLineUtils.GetBatchModeCommandArgs().ContainsKey("autoBuilder")) {
				notify = false;
			}
			var result = TestFlightUtil.UploadBuild( ipaFile, desc, distributionList, notify);
			EB.Debug.Log("TF Upload: " + result);
			
			// build extended packs
			//BuildContentPacksWithOptions(true);
			Done();
		}
		catch(System.Exception e)
		{
			EB.Debug.Log("Build Failed: exception: " + e.ToString());
			Failed(e);
		}
		
		ClearProgressBar();
	}
#endif	
	
}
