using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Editor
{
	class PList : FileHelper
	{
		public PList(string file) : base(file)
		{
			
		}
		
		public void AddUrlSchemes( params string[] schemes )
		{
			List<string> lines = new List<string>();
			lines.Add("	<key>CFBundleURLTypes</key>");
			lines.Add("	<array>");
			lines.Add("		<dict>");
			lines.Add("			<key>CFBundleURLSchemes</key>");
			lines.Add("			<array>");
			
			foreach( var scheme in schemes )
			{
				lines.Add("				<string>"+scheme+"</string>");
			}
			
			lines.Add("			</array>");
			lines.Add("		</dict>");
			lines.Add("	</array>");
			
			this.InsertLines( "<dict>", 1, lines.ToArray() ); 
		}
		
		public void AddDeviceCapabilities( params string[] capabilities )
		{
			string replace = "";
			foreach( var capability in capabilities)
			{
				replace += "<string>"+capability+"</string>";
			}
			
			this.ReplaceAll("<string>armv7</string>", replace);
		}
		
		public void DisableViewControllerBasedStatusBarAppearance()
		{
			List<string> lines = new List<string>();
			lines.Add(" <key>UIViewControllerBasedStatusBarAppearance</key>");
			lines.Add(" <false/>");
			
			this.InsertLines( "<dict>", 1, lines.ToArray());
		}
		
		public void AddIconFiles( params string[] iconFiles )
		{
			List<string> lines = new List<string>();

			foreach( string icon in iconFiles)
			{
				lines.Add("<string>"+icon+"</string>");
			}
			this.InsertLines("<string>Icon-144.png</string>",1, lines.ToArray());
		}
		
		public void AddBundleVersion(string version)
		{
			this.ReplaceAll("1.0.0", version);
		}
		
	}
}
