using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class LocalizationUtils
{
	static EB.Language[] Languages = new EB.Language[]
	{
		EB.Language.French,
		EB.Language.Italian,
		EB.Language.German,
		EB.Language.Spanish,
		EB.Language.Portuguese,
		EB.Language.Russian,
		EB.Language.Korean,
		EB.Language.ChineseSimplified,
		EB.Language.ChineseTraditional,
		EB.Language.Japanese,
		EB.Language.Turkish,
	};
	
	static WWWUtils.Environment Environment = WWWUtils.Environment.Prod;	
	
	static Dictionary<string,EB.LocFile> _files = new Dictionary<string, EB.LocFile>();
	
	public const string LocSourceFolder = "Assets/Resources/Languages/en";
	
	class LocWatcher : UnityEditor.AssetModificationProcessor
	{
		public static void OnWillSaveAssets(string[] paths)
		{
			foreach( string path in paths )
			{
				if (path.StartsWith(path,System.StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}
			SaveDbs();
		}
	}
	
	public static void SaveDbs()
	{
		if (_files.Count == 0)
		{
			return;
		}
		
		EB.Debug.Log("Saving Loc dbs");
		foreach( var kvp in _files )
		{
			var file = kvp.Value.Write();
			var name = kvp.Key;
			File.WriteAllText( Path.Combine(LocSourceFolder,name+".txt"), file);
		}
		_files.Clear();
		AssetDatabase.Refresh();
	}
	
	public static EB.LocFile GetLocDb( string name )
	{
		EB.LocFile file = null;
		if (!_files.TryGetValue(name, out file))
		{
			file = new EB.LocFile();
			try {
				file.Read( File.ReadAllText(Path.Combine(LocSourceFolder,name+".txt")) );
			}
			catch {
				//Debug.LogError("Failed to load db " + name);
			}
			_files[name] = file;
		}
		return file;
	}
	
	public static bool LocTextField( string name, string locId, string database, params GUILayoutOption[] options )
	{
		var db = GetLocDb(database);
		var str = string.Empty;
		db.Get(locId, out str);
	
		var res = EditorGUILayout.TextField(name, str, options);
		if ( res != str )
		{
			db.Add(locId, res);
			return true;
		}
		return false;
	}
	
	[MenuItem("EBG/Localizer/Upload Translations")]
	public static void UploadTranslations()
	{
		WWWUtils.env = Environment;
		
		var stoken = WWWUtils.AdminLogin();
		
		var url = WWWUtils.AdminUrl("/localization/upload/english");
		
		EB.Localizer.Clear();
		var result = EB.Localizer.LoadAllFromResources(EB.Language.English, false);
		
		// fixup all the \n
		var data = new Hashtable();
		foreach( DictionaryEntry entry in result)
		{
			data[entry.Key] = entry.Value.ToString().Trim().Replace("\n","\\n");
		}
		
		EB.Debug.Log(" source string count: " + data.Count );
		
		var form = new WWWForm();
		form.AddField("body", EB.JSON.Stringify(data));
		form.AddField("stoken", stoken);
		form.AddField("format", "json");
		WWWUtils.PostJson(url, form); 
	}
	
	
	[MenuItem("EBG/Localizer/Export TSV")]
	public static void ExportTSV()
	{
		var tmp = new List<EB.Language>();
		tmp.Add(EB.Language.English);
		tmp.AddRange(Languages);
		
		foreach( var locale in tmp )
		{
			EB.Localizer.Clear ();
			EB.Localizer.LoadAllFromResources(locale, false);
			
			EB.LocFile file = new EB.LocFile(EB.Localizer.Strings);
			var contents = file.WriteTSV();
			File.WriteAllText(locale+".txt", "\r\n"+contents, System.Text.Encoding.Unicode); 
		}
		
	}
	
	[MenuItem("EBG/Localizer/Download Translations")]
	public static void DownloadTranslations()
	{
		WWWUtils.env = Environment;
		var stoken = WWWUtils.AdminLogin();
		
		// get all the translations
		foreach( var locale in Languages )
		{
			var code = EB.Localizer.GetSparxLanguageCode(locale);
			var url = WWWUtils.AdminUrl( string.Format("/localization/export/{0}?stoken={1}&format=csv&status=1&code=1", code, stoken)  );
			
			try
			{
				var csv = WWWUtils.Get(url);
				if (!string.IsNullOrEmpty(csv))
				{
					// fixup endlines
					csv = csv.Replace(@"\\n", @"\n");
					
					var dir = "Assets/Resources/Languages/"+code;
					if (!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					var bytes = EB.Encoding.GetBytes(csv);
					File.WriteAllBytes(Path.Combine(dir,"all.txt"), bytes); 
				}
			}
			catch 
			{
				
			}
		}
		
		AssetDatabase.Refresh();
	}
				
				
}
