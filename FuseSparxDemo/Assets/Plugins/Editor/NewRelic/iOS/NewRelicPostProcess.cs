using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.XCodeEditor;

public static class NewRelicPostProcess
{
	[PostProcessBuild(200)]
	public static void OnPostProcessBuild(BuildTarget target, string path)
	{
		if (target == BuildTarget.iPhone)
		{
			using (UnityEditor.XCodeEditor.XCProject project = new UnityEditor.XCodeEditor.XCProject(path))
			{
				// Find and run through all projmods files to patch the project
				string projModPath = System.IO.Path.Combine(Application.dataPath, "Plugins/Editor/NewRelic/iOS");
				var files = System.IO.Directory.GetFiles(projModPath, "*.projmods", System.IO.SearchOption.AllDirectories);
				foreach (var file in files)
				{
					project.ApplyMod(Application.dataPath, file);
				}
				project.Save();		
			
			}
			

		}
	}
}
