using UnityEngine;
using System.Collections;
using System.IO;

public static class TestFlightUtil
{
	private const string upload_url= "https://testflightapp.com/api/builds.json";
	private const string api_token = "470b957638cf681ec937f4eadf026e17_NDQ2NDM4MjAxMi0wNS0xNyAxOTozMzo0Mi4zNTA2NDM";
	private const string team_token= "c8d426868b4b5a57a496b1015628d635_OTEzMzIyMDEyLTA1LTI1IDExOjUwOjM1LjIzOTg0Mg";
	
	public static string UploadBuild( string ipaFile, string notes, string distribution_lists, bool notify = true)
	{
		UnityEditor.EditorUtility.UnloadUnusedAssets();
		System.GC.Collect();
		
		var zipName = Path.GetFileNameWithoutExtension(ipaFile) + ".dSYM.zip";
		var zipDir  = Path.GetDirectoryName(ipaFile);
		var zipFile = Path.Combine(zipDir, zipName);

		notes = notes.Replace('\'', '`');	// moko: remove single quote from notes

		string args = string.Format("{0} -F file=@{1} -F api_token='{2}' -F team_token='{3}' -F notes='{4}' -F distribution_lists='{5}' -F replace=True -F notify={6}", upload_url, ipaFile, api_token, team_token, notes, distribution_lists, (notify?"True":"False"));
		EB.Debug.Log("Testflight upload: "+args);
		if ( File.Exists(zipFile) )
		{
			args += " -F dsym=@"+zipFile;
		}
		string result = CommandLineUtils.Run("curl", args);
	
		return result;
	}
}
