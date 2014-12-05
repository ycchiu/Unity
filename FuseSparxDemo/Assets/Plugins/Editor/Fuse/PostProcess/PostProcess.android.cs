#if UNITY_ANDROID
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace EB.Editor
{
	public static class PostProcessAndroid
	{
		readonly static string kAndroidSDK = "/opt/android-sdk-macosx";
		
		static string FindAAPT()
		{
			if( Directory.Exists( kAndroidSDK ) == false )
			{
				string msg = "PostProcess in PostProcess.andriod.cs requires the AndriodSDK to be at " + kAndroidSDK;
				throw new System.IO.DirectoryNotFoundException( msg );
			}
			
			var aaptPath = Path.Combine( kAndroidSDK, "platform-tools/aapt" );
			if (!File.Exists(aaptPath))
			{
				var buildTools = new DirectoryInfo( Path.Combine( kAndroidSDK, "build-tools" ) );
				aaptPath = buildTools.GetFiles("aapt", SearchOption.AllDirectories)[0].FullName;
			}
			if (!File.Exists(aaptPath))
			{
				EB.Debug.LogError("Didn't find aapt binary!");
				throw new System.Exception("AAPT binary not foudn. Please Install AAPT from the android sdk tools");
			}
			return aaptPath;
		}
		
		[PostProcessBuild]
		public static void AndroidProcess( BuildTarget target, string path )
		{
			if ( target != BuildTarget.Android )
			{
				return;
			}
			
			EB.Debug.Log("PostProcess {0}, {1}", target, path);	
			
			var res = "";
		
            GeneralUtils.DeleteDirectory("/tmp/android", true);   // mko: cleaning up build folder
			CommandLineUtils.Run("/bin/mkdir", "-p /tmp/android/assets");

			var aaptPath = FindAAPT();
			EB.Debug.Log("aapt Path: " + aaptPath);
			
			// add files via apk
			var src = BuildSettings.BuildFolder;

			EB.Debug.Log("src " + src);
			
			var start = System.DateTime.Now;

			using ( var zipFile = new ZipFile(path) )
			{
				zipFile.BeginUpdate();
				if (BuildSettings.DevBundleMode == EB.Assets.DevBundleType.StandardBundles)
				{					
					if (Directory.Exists(src))
					{
						foreach(var file in System.IO.Directory.GetFiles(src, "*", SearchOption.AllDirectories))
						{
							EB.Debug.Log("file : "+ file);
							var shortname 	= file.Substring(src.Length+1); 
							EB.Debug.Log("shortname " + shortname);
							zipFile.Add(file, "assets/"+shortname);
	
						}
					}
				}
				
				// copy the Build Config file every time.
				zipFile.Add(BuildSettings.BuildConfigPath, Path.Combine("assets", Path.GetFileName(BuildSettings.BuildConfigPath)));
				
				EB.Debug.Log("Committing apk updates....");
				zipFile.CommitUpdate();
			}


			var diff = System.DateTime.Now - start;
			EB.Debug.Log("Inject assets took: " + diff.ToString() );
		
			// remove meta-inf
			res = CommandLineUtils.Run("/usr/bin/zip", "-d " + path + " \"META-INF*\"");
			EB.Debug.Log(res);
			
			var cmdline = string.Format("-verbose -sigalg MD5withRSA -digestalg SHA1 -keystore Assets/Editor/Android/android.keystore -storepass 123456 -keypass 123456 {0} ebg", path );
			res = CommandLineUtils.Run("/usr/bin/jarsigner", cmdline );
			EB.Debug.Log(res);
			
			res = CommandLineUtils.Run("/opt/android-sdk-macosx/tools/zipalign", string.Format("-f -v 4 {0} /tmp/android/aligned.apk", path) );
			EB.Debug.Log(res);
			
			try {
				// copy over the aligned apk
				File.Copy("/tmp/android/aligned.apk", path, true); 
			}
			catch (System.Exception ex ){
				EB.Debug.LogError( "Problems Copying the Aligned Build: {0}", ex.ToString() );
			}
			
			if (AndroidUtils.HasDevicesConnected())
			{
				EB.Debug.Log("Installing APK...");
				AndroidUtils.Reinstall( path );
				EB.Debug.Log("Installing APK Completed.");
			}

			EB.Debug.Log("PostProcess {0}, {1} finished", target, path);	
		}
	}
}
#endif // UNITY_ANDROID	
