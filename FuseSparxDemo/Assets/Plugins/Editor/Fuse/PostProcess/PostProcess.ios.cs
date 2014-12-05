#if UNITY_IPHONE	
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

namespace EB.Editor
{
	public static class PostProcess
	{
		public static string Signer = string.Empty;
		
		[PostProcessBuild(1)]
		public static void Process( BuildTarget target, string path )
		{
			if ( target != BuildTarget.iPhone )
			{
				EB.Debug.Log("PostProcess.ios.cs is not going to be executed. Current plaftorm is {0}", target);
				return;
			}
			
			EB.Debug.Log("PostProcess {0}, {1}", target, path);	

			iOSUtils.CopyProvisioningProfiles();
			
			// copy over optimized xcode assets
			CommandLineUtils.Run("/bin/cp", "-f Assets/Xcode/*.png " + path);
			
			// copy over the lproj files
			CommandLineUtils.Run("/bin/cp", "-rf Assets/Xcode/*.lproj " + path);
			
			var project = Path.Combine( path, "Unity-iPhone.xcodeproj/project.pbxproj"); 
			var xcode = new XcodeProject(project);
			//xcode.UseSymbols();
			xcode.AddLibrary("libsqlite3.dylib", "1F171D2615ED3C8F0037379A", "1F171D2515ED3C8F0037379A");
			xcode.AddFramework("StoreKit","1FA2838815E3E67D00C5F814", "1FA2838715E3E67D00C5F814");
			xcode.AddFramework("Security", "1F19369016360C8900522693", "1F19368F16360C8900522693");
			xcode.AddFramework("CoreTelephony", "F48F87C316364627008D4B95", "F48F87C216364627008D4B95" );
			xcode.AddFramework("MobileCoreServices", "F48F87C516364640008D4B95", "F48F87C416364640008D4B95" );
			xcode.AddFramework("AdSupport", "F4439D1916363AB5005BD56C", "F4439D1816363AB5005BD56C", XcodeProject.Linkage.Weak);
			xcode.AddFramework("iAd", "F4439D1916363AB5005BD56E", "F4439D1816363AC5005BD56C", XcodeProject.Linkage.Weak);
			xcode.AddFramework("Social", "1F7996A11655807900B9E48B", "1F7996A01655807900B9E48B", XcodeProject.Linkage.Weak);
			xcode.AddFramework("Accounts", "1F79969F1655806A00B9E48B", "1F79969E1655806A00B9E48B", XcodeProject.Linkage.Weak);

			var searchPaths = new string[]
            {
                "               FRAMEWORK_SEARCH_PATHS = (",
                "                   \"$(inherited)\",",
                "                   \"\\\"$(SRCROOT)\\\"\"",
                "               );",
            };
            
            xcode.InsertLines("1D6058940D05DD3E006BFB54 /* Debug */", 7, searchPaths);
            xcode.InsertLines("1D6058950D05DD3E006BFB54 /* Release */", 7, searchPaths);
            			
			if ( !string.IsNullOrEmpty(Signer) )
			{
				xcode.SetCodeSigner(Signer);
			}
			
			xcode.LocalizeFile("InfoPlist.strings","1F7CED4C1732E2FE00DB9262", "1F7CED491732E2FE00DB9262");
			
			xcode.UseSymbols();
			xcode.EnableObjC_Exceptions();
			
			// delete the portrait splash screen files
			xcode.RemoveLines("Default-Portrait.png");
			xcode.RemoveLines("Default-Portrait@2x.png");
			FileUtil.DeleteFileOrDirectory( Path.Combine(path,"Default-Portrait.png") );
			FileUtil.DeleteFileOrDirectory( Path.Combine(path,"Default-Portrait@2x.png") );
			
			xcode.Save();
			
			
			// fixup plist
			var plist = new PList(Path.Combine(path,"Info.plist"));
			plist.AddDeviceCapabilities("armv7","gyroscope");
			
			var settings = FBSettings.Instance;
			var fbAppIds = new List<string>();
			foreach( var appId in settings.AppIds )
			{
				fbAppIds.Add("fb"+appId);
			}
			plist.AddUrlSchemes(fbAppIds.ToArray());
			plist.Save();
			
			var dest = Path.Combine(path, "Data");
			CommandLineUtils.Run("/bin/mkdir", "-p "+ dest );
			if (BuildSettings.DevBundleMode == EB.Assets.DevBundleType.StandardBundles) 
			{
				// copy build data
				var src = BuildSettings.PlatformFolder;
				CommandLineUtils.Run("/bin/cp", "-rf " + src + " " + dest );
			}
			
			// copy the Build Config file every time.
			CommandLineUtils.Run("/bin/cp", " -f "+BuildSettings.BuildConfigPath+" "+dest);

			EB.Debug.Log("PostProcess {0}, {1} finished", target, path);
		}
	}
}

#endif
