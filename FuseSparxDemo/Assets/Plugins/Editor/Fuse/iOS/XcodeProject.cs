using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB.Editor
{
	public class XcodeProject : FileHelper
	{
		public XcodeProject( string path ) : base(path)
		{
		}
		
		public void UseSymbols()
		{
			ReplaceAll("DEBUG_INFORMATION_FORMAT = dwarf", "DEBUG_INFORMATION_FORMAT = \"dwarf-with-dsym\"");
			ReplaceAll("GCC_GENERATE_DEBUGGING_SYMBOLS = NO", "GCC_GENERATE_DEBUGGING_SYMBOLS = YES");
			//ReplaceAll("COPY_PHASE_STRIP = YES","COPY_PHASE_STRIP = NO");
		}
		
		public void AddLibrary( string lib, string fileId, string fileRef )
		{
			InsertLine("/* Begin PBXBuildFile section */", 				4, "		"+fileId+" = {isa = PBXBuildFile; fileRef = "+fileRef+"; };" );  
			InsertLine("1D60588F0D05DD3D006BFB54 /* Frameworks */", 	5, "				"+fileId+" /* "+lib+".framework in Frameworks */," );  
			InsertLine("29B97314FDCFA39411CA2CEA /* CustomTemplate */ ",4, "				"+fileRef+" /* "+lib+".framework in Frameworks */," );
			InsertLine("/* Begin PBXFileReference section */", 			1, "		"+fileRef+" = {isa = PBXFileReference; lastKnownFileType = \"compiled.mach-o.dylib\"; name = "+lib+"; path = usr/lib/"+lib+"; sourceTree = SDKROOT; };");
		}
		
		public enum Linkage
		{
			Weak,
			Required,
			None,
		}
		
		public string GenId()
		{
			var fr = "";
			var hex = "0123456789ABCDEF".ToCharArray();
			for (int i = 0; i < 24; ++i )
			{
				fr += hex[Random.Range(0,hex.Length)];
			}
			return fr;
		}
		
		public void LocalizeFile( string name, string fileId, string fileRef  ) 
		{
			//1F7CED4C1732E2FE00DB9262 /* InfoPlist.strings in Resources */ = {isa = PBXBuildFile; fileRef = 1F7CED491732E2FE00DB9262 /* InfoPlist.strings */; };
			InsertLine("/* Begin PBXBuildFile section */", 				4, "		"+fileId+" = {isa = PBXBuildFile; fileRef = "+fileRef+"; };" );  
			InsertLine("/* Begin PBXResourcesBuildPhase section */", 	5, "			"+fileId+",");
			InsertLine("29B97314FDCFA39411CA2CEA /* CustomTemplate */ = {", 4, "				"+fileRef+",");
			
			var dirInfo = new DirectoryInfo( Path.GetDirectoryName(this._path)+"/../" );
			
			var lines = new List<string>();
			lines.Add("");
			lines.Add("/* Begin PBXVariantGroup section */");
			lines.Add("		"+fileRef+" /* "+name+" */ = {");
			lines.Add("			isa = PBXVariantGroup;");
			lines.Add("			children = (");
			
			foreach( var dir in dirInfo.GetDirectories("*.lproj"))
			{
				EB.Debug.Log("dir:" +  dir.Name);
				var language = dir.Name.Split('.')[0];
				var id = GenId();
				lines.Add("				"+id+" /* "+language+" */,");					
				InsertLine("/* Begin PBXFileReference section */", 2, "		"+id+" /* "+language+" */ = {isa = PBXFileReference; lastKnownFileType = text.plist.strings; name = "+language+"; path = "+language+".lproj/"+name+"; sourceTree = \"<group>\"; };");
				
				try {
					File.Delete( Path.Combine(dir.FullName, name+".meta" ));	
				}
				catch {}
				
			}
			
			lines.Add("			);");
			lines.Add("			name = "+name+";");
			lines.Add("			sourceTree = \"<group>\";");
			lines.Add("		};");
			lines.Add("/* End PBXVariantGroup section */");
			lines.Add("");
			
			this.InsertLines("/* End PBXSourcesBuildPhase section */", 1, lines.ToArray());
		}
		
		public void AddFramework( string frameworkName, string fileId, string fileRef, Linkage linkage = Linkage.Required, string baseFolder ="System/Library/Frameworks/", string sourceTree = "SDKROOT" )
		{
			if (linkage == Linkage.None)
			{
				InsertLine("/* Begin PBXBuildFile section */", 				4, "		"+fileId+" /* "+frameworkName+".framework in Frameworks */ = {isa = PBXBuildFile; fileRef = "+fileRef+" /* "+frameworkName+".framework */; };" );  
			}
			else
			{
				InsertLine("/* Begin PBXBuildFile section */", 				4, "		"+fileId+" /* "+frameworkName+".framework in Frameworks */ = {isa = PBXBuildFile; fileRef = "+fileRef+" /* "+frameworkName+".framework */; settings = {ATTRIBUTES = ("+linkage+", ); }; };" );  	
			}
			InsertLine("1D60588F0D05DD3D006BFB54 /* Frameworks */", 	5, "				"+fileId+" /* "+frameworkName+".framework in Frameworks */," );  
			InsertLine("29B97314FDCFA39411CA2CEA /* CustomTemplate */ ",4, "				"+fileRef+" /* "+frameworkName+".framework in Frameworks */," ); 
			
			if (sourceTree == "SDKROOT")
			{
				InsertLine("/* Begin PBXFileReference section */", 			1, "		"+fileRef+" /* "+frameworkName+".framework */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = "+frameworkName+".framework; path = "+baseFolder+frameworkName+".framework; sourceTree = "+sourceTree+"; };");  
			}
			else
			{
				InsertLine("/* Begin PBXFileReference section */", 			1, "		"+fileRef+" /* "+frameworkName+".framework */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; path = "+baseFolder +frameworkName+".framework; sourceTree = "+sourceTree+"; };");  
			}
		}
		
		public void AddIcon( string iconName, string fileId, string fileRef)
		{
			EB.Debug.Log ("AddIcon: " + iconName);
			InsertLine("56DBF99D15E3CDC9007A4A8D /* iPhone_Sensors.mm in Sources */ = {isa = PBXBuildFile; fileRef = 56DBF99C15E3CDC9007A4A8D /* iPhone_Sensors.mm */; };", 1, "		"+fileId+" /* "+iconName+" in Sources */ = {isa = PBXBuildFile; fileRef = "+fileRef+" /* "+iconName+" */; };" );  
			InsertLine("8A851BA816FB3AD000E911DB /* UnityAppController.h */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = sourcecode.c.h; path = UnityAppController.h; sourceTree = \"<group>\"; };", -1," " + fileRef + " /* "+iconName+" */ = {isa = PBXFileReference; lastKnownFileType = image.png; path = \"" + iconName + "\"; sourceTree = SOURCE_ROOT; };");
			InsertLine(" /* Libraries */ = {", 4, "				"+fileRef+",");			
			InsertLine ("D8A1C7009E80637F000160D4 /* App.mm in Sources */,", 0, "   " + fileId + " /* " + iconName + " in Sources */,");
		} 
		
		public void AddBundle(string bundleName, string fileId, string fileRef)
		{
			EB.Debug.Log("AddBundle: " + bundleName);
			string bundle = bundleName + ".bundle";
			InsertLine("/* Begin PBXBuildFile section */", 10, "   " + fileId + " /* " + bundle + " in Resources */ = {isa = PBXBuildFile; fileRef = " + fileRef + " /* " + bundle + " */; };");
			InsertLine("/* Begin PBXFileReference section */", 10, "   " + fileRef + " /* " + bundle + " */ = { isa = PBXFileReference; lastKnownFileType = \"wrapper.plug-in\"; path = " + bundle + "; sourceTree = SOURCE_ROOT; };");
			InsertLine("/* Libraries */ = {", 4, "				"+fileRef + " /* " + bundle + " */,");
			InsertLine("/* Begin PBXResourcesBuildPhase section */", 6, "				"+ fileId + " /* " + bundle + " in Resources */,");
		}
		
		public void AddLinkerFlag(string flag)
		{
			EB.Debug.Log("AddLinkerFlag: " + flag);
			ReplaceAll("OTHER_LDFLAGS = (", "OTHER_LDFLAGS = (\n					\"" + flag + "\",");
		}
		
		public void SetCodeSigner( string signer )
		{
			ReplaceAll("iPhone Developer", signer);			
		}
		
		public void EnableObjC_Exceptions()
		{
			//GCC_ENABLE_OBJC_EXCEPTIONS = YES;
			InsertLine("1D6058940D05DD3E006BFB54 /* Debug */ = {", 3, "				GCC_ENABLE_OBJC_EXCEPTIONS = YES;");
			InsertLine("1D6058950D05DD3E006BFB54 /* Release */ = {", 3, "				GCC_ENABLE_OBJC_EXCEPTIONS = YES;");
		}
		
	}
}
	

