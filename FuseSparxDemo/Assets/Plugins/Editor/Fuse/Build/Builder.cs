using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public partial class Builder 
{
	private static void BuilderPost(string uri, Hashtable data)
	{
		var statusUrl = EnvironmentUtils.Get("BUILD_STATUS_URL", string.Empty);
		if ( !string.IsNullOrEmpty(statusUrl) )
		{			
			var jsonData = EB.Encoding.GetBytes(EB.JSON.Stringify(data));
			var url = statusUrl + uri;
			
			Hashtable headers = new Hashtable();
			headers["Content-Type"] = "application/json";
            //headers["Content-Length"] = jsonData.Length;	// moko: unity 4.3 will handle www raw data lenght for you
			//EB.Debug.Log("Headers: {0}", headers);
			
			var www = new WWW(url, jsonData, headers);
			//EB.Debug.Log("Status: " + www.url);
			WWWUtils.WaitOnWWW(www, false);
			//EB.Debug.Log("result: " + www.text);
		}
		else
		{
			EB.Debug.LogWarning("Can not fetch env var 'BUILD_STATUS_URL' for posting builder email");
		}
	}
	
	public static void WriteVersionFile( string version ) 
	{
		var versionFile = "Assets/Resources/version.txt";
		FileInfo fInfo = VersionControllUtils.CheckOutFile(versionFile, VersionControllUtils.CheckOutType.ForceWriteable);

		if (fInfo != null)
		{
			File.WriteAllBytes(versionFile, EB.Encoding.GetBytes(version));
			AssetDatabase.Refresh();
			AssetDatabase.ImportAsset(versionFile,ImportAssetOptions.ForceSynchronousImport);
			string updatedVersion = EB.Version.GetVersion();
		
			if (updatedVersion == version)
			{
				EB.Debug.Log("WriteVersionFile updatedVersion: " + updatedVersion);
			}
			else
			{
				EB.Debug.Log( "WriteVersionFile Failed version: " + version + " updatedVersion: " + updatedVersion );
				throw new System.Exception( "WriteVersionFile Failed version: " + version + " updatedVersion: " + updatedVersion );
			}
		}
	}
	
	public static void DisplayProgressBar( string title, string status, float progress )
	{
		Hashtable data = new Hashtable();
		data["status"] = status;
		data["progress"] = progress;
		BuilderPost("/status", data);
	}
	
	private static void Done()
	{
		BuilderPost("/complete", new Hashtable());
	}
	
	private static void Email( string to, string subject, string body, Hashtable ejsData )
	{
		Hashtable data = new Hashtable();
		data["to"] = to;
		data["subject"] = subject;
		data["body"] = body;
		data["params"] = ejsData;
		BuilderPost("/email", data);
	}
	
	private static void Failed(System.Exception e)
	{
		Hashtable data = new Hashtable();
		data["error"] = e.Message;
		data["stack"] = e.StackTrace;
		BuilderPost("/failed", data);
	}
	
	public static void ClearProgressBar()
	{
		EditorUtility.ClearProgressBar();
	}
	
	static void UploadContentManifest( WWWUtils.Environment env, string url, int enabled )
	{
		var stoken = WWWUtils.AdminLogin(env);
		var postUrl = WWWUtils.GetAdminUrl(env) + "/bundlemanager/content/from-manifest?stoken="+stoken;
		var form = new WWWForm();
		form.AddField("url", url);
		form.AddField("enabled", enabled);
		WWWUtils.Post(postUrl, form);
	}
	

	/*
	public static void BuildContentPacksWithOptions( string distList, bool skipBase, bool uploadProduction = false ) 
	{
		try
		{			
			var tmpfoler = "/tmp/packs";
            GeneralUtils.DeleteDirectory(tmpfoler, true);   // mko: cleaning up build folder
			Directory.CreateDirectory(tmpfoler);
			
			var version = "1.0";
			try {
				var parts= PlayerSettings.bundleVersion.Split('.');
				version = parts[0] + "." + parts[1];
			}
			catch {
			}
			
			var platform = BuildSettings.Target;
			
			var date 	= System.DateTime.Now.ToString("dd/MM/yy HH:mm");
			var cl 		= EnvironmentUtils.Get("BUILD_CL", "0" );
			var desc	= "Content Package " + BuildSettings.Target + " " + date + " CL: " + cl + "\n";
			var notes	= (ArrayList)EB.JSON.Parse( EnvironmentUtils.Get("BUILD_NOTES", "[]") );
			
			PlayerSettings.bundleVersion = version + "." + cl;
			
			// step1 build all the bundles (extended bundles)
			var options = Bundler.BundleOptions.Force | Bundler.BundleOptions.Extended;
			if (skipBase)
			{
				options |= Bundler.BundleOptions.SkipBase;
			}
			
			var packs = Bundler.BuildAll( BuildSettings.BundlerConfigFolder, options);
			
			var files 		= new ArrayList();			
			foreach( var pack in packs )
			{
				var tarPath = Path.Combine(tmpfoler, pack+".tar");
				var packPath= Path.Combine(BuildSettings.BuildFolder, pack);
				var gzipPath = tarPath + ".gz";
				
				// turn into gz, tar archive
				using ( var gzFile = new FileStream(gzipPath, FileMode.Create, FileAccess.ReadWrite) )
				{
					using( var gzStream = new Ionic.Zlib.GZipStream(gzFile, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression) )
					{
#if !UNITY_WEBPLAYER	// moko: tar_cs doesnt exist in webplayer, print warning for now if ever get here
						var writer = new tar_cs.TarWriter(gzStream);
						foreach( var packFile in Directory.GetFiles(packPath,"*",SearchOption.AllDirectories) ) 	
						{
							var relativeName = packFile.Substring(packPath.Length+1);
							//Debug.Log("file: " + relativeName);	
							using( var f = new FileStream(packFile, FileMode.Open, FileAccess.Read) )
							{
								writer.Write(f, f.Length, relativeName, string.Empty, string.Empty, 511, System.DateTime.UtcNow);  
							}
						}
						writer.Close();
#else
						EB.Debug.LogWarning("TAR IO is not implemented for Webplayer");
#endif
					}
				}
				
				//CommandLineUtils.Run("/usr/bin/tar", string.Format("-cv --format ustar -f {0} ./", tarPath), packPath); 
				
				// gzip up
				//CommandLineUtils.Run("/usr/bin/gzip", string.Format("-9f {0}", tarPath), tmpfoler);
				
				
				var info = new Hashtable();
				var size = new FileInfo(gzipPath).Length;
				
				info["size"] 	= size;
				info["url"]  	= S3Utils.Put(gzipPath, Path.Combine(cl,Path.GetFileName(gzipPath)) );
				info["md5"] 	= S3Utils.CalculateMD5(gzipPath);
				info["pack"]	= pack;
				info["included"]= skipBase;
				files.Add(info);
			}
			
			// send email
			var data = new Hashtable();
			data["cl"] = int.Parse(cl);
			data["minVersion"] = int.Parse(cl);
			data["title"] = desc;
			data["notes"] = notes;
			data["files"] = files;
			data["platform"] = platform;
			
			var manifest = EB.JSON.Stringify(data);
			var manifestUrl = S3Utils.PutData( EB.Encoding.GetBytes(manifest), "manifest.json", Path.Combine(cl,"manifest.json")  );
			data["manifest"] = manifestUrl;
			
			if (!string.IsNullOrEmpty(manifestUrl))
			{
				UploadContentManifest(WWWUtils.Environment.Dev, manifestUrl, skipBase ? 1 : 0);
				
				if (uploadProduction)
				{
					UploadContentManifest(WWWUtils.Environment.Prod, manifestUrl, skipBase ? 1 : 0);	
				}
			}
			//UploadContentManifest(WWWUtils.Environment.Prod, manifest, 0);
			
			Email( distList, "New " + platform + "  Content Build: " + cl, File.ReadAllText("Assets/Editor/EB.Core.Editor/Build/Email/contentbuild.txt"), data );  
			
			Done();
		}
		catch( System.Exception ex )
		{
			EB.Debug.Log("BuildContentPacks Failed: exception: " + ex.ToString());
			Failed(ex);
		}
		
		ClearProgressBar();
	}
	*/
}
