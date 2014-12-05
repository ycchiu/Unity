using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.KabamSODAEditor;
using UnityEditor.KabamXCodeEditor;

namespace UnityEditor.KabamSODAEditor {
    public static class KabamSODAPostProcess {
                
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path) {

#if !SODA_SKIP_IOS
            if (target == BuildTarget.iPhone) {
                XCProject project = new XCProject(path);

                // Find and run through all projmods files to patch the project

                string projModPath = System.IO.Path.Combine(Application.dataPath, "Plugins/KabamSODA/iOS");
                var files = System.IO.Directory.GetFiles(projModPath, "*.projmods", System.IO.SearchOption.AllDirectories);
                foreach (var file in files) {
                    project.ApplyMod(Application.dataPath, file);
                }
                project.Save();

                // Update the plist file
                //PlistMod.UpdatePlist(path, settings.ClientId, settings.ClientMobileKey, settings.Env, settings.WSKEUrl);
            }
#endif
        }
    }
}
