using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class DownSampleImages : ScriptableWizard
{
	public string atlasSrcDir = "Assets/OriginalAssets/UI/HdUi";
	public string atlasDstDir = "Assets/OriginalAssets/UI/SdUi";
	public string streamSrcDir = "Assets/Resources/Bundles";
	public bool onlyNewFiles = false;
	public bool onlyModifiedFiles = false;
	
	private class ApplyTextureImportSettings
	{
		public TextureImporter srcImporter;
		public string srcUnityPath;
		public string dstUnityPath;
	}
	
	private List<ApplyTextureImportSettings> pendingSettings = new List<ApplyTextureImportSettings>();
	
	[MenuItem("EBG/Down Sample UI Images")]
	static void DownSample()
	{
		ScriptableWizard.DisplayWizard<DownSampleImages>("Down Sample UI Images");
	}
	
	private class TexturePathError
	{
		public string errorMsg;
		public string path;
	}
	
	private class DownSampleResult
	{
		public bool success;
		public string message;
		public string path;
	}
	
	private List<string> selectedTexturePaths = new List<string>();
	private List<TexturePathError> pathErrorMessages = new List<TexturePathError>();
	private List<DownSampleResult> downsampleResults = new List<DownSampleResult>();
	
	private void OnSelectionChange()
	{
		selectedTexturePaths.Clear();
		pathErrorMessages.Clear();
		
		UnityEngine.Object[] objs = Selection.objects;
		
		foreach (UnityEngine.Object obj in objs)
		{
			string path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path))
			{
				// Make sure this asset is a texture
				if (AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) != null)
				{
					if (path.StartsWith(streamSrcDir))
					{
						if (!path.EndsWith("_HD.png"))
						{
							TexturePathError error = new TexturePathError();
							error.errorMsg = string.Format("The texture '{0}' does not match streaming naming conventions.", obj.name);
							error.path = path;
							pathErrorMessages.Add(error);
						}
						else
						{
							selectedTexturePaths.Add(path);
						}
					}
					else if (path.StartsWith(atlasSrcDir))
					{
						selectedTexturePaths.Add(path);
					}
					else
					{
						TexturePathError error = new TexturePathError();
						error.errorMsg = string.Format("The texture '{0}' is not in an atlas or streaming directory.", obj.name);
						error.path = path;
						pathErrorMessages.Add(error);
					}
				}
				else
				{
					TexturePathError error = new TexturePathError();
					error.errorMsg = string.Format("The selected file '{0}' is not a texture.", obj.name);
					error.path = path;
					pathErrorMessages.Add(error);
				}
			}
		}
		
		Repaint();
	}
	
	private Vector2 scrollPosition = Vector2.zero, scrollPosition2 = Vector2.zero;
	private void OnGUI()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Ping Atlas Directory"))
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(atlasSrcDir, typeof(UnityEngine.Object)));
		}
		if (GUILayout.Button("Ping Streaming Directory"))
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(streamSrcDir, typeof(UnityEngine.Object)));
		}
		GUILayout.EndHorizontal();
		GUI.color = Color.yellow;
		foreach (TexturePathError tpe in pathErrorMessages)
		{
			if (GUILayout.Button(tpe.errorMsg))
			{
				EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(tpe.path, typeof(UnityEngine.Object)));
			}
		}
		
		if (pathErrorMessages.Count > 0)
		{
			GUILayout.Space(10f);
		}
		
		GUI.color = Color.white;
		GUILayout.Label(string.Format("There are {0} valid textures selected for down sampling:", selectedTexturePaths.Count));
		
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		foreach (string path in selectedTexturePaths)
		{
			GUILayout.BeginHorizontal();
			GUIStyle gs = new GUIStyle();
			gs.fixedWidth = 16;
			gs.fixedHeight = 16;
			GUILayout.Label(AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture, gs);
			GUILayout.Label(path);
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
		
		GUILayout.Space(10f);
		
		if (selectedTexturePaths.Count > 0)
		{
			GUI.color = Color.green;
			if (GUILayout.Button("Down Sample Selected Textures"))
			{
				DoDownsampleOfSelectedTextures();
				EditorApplication.update += OnEditorUpdate;
			}
			GUI.color = Color.white;
		}
		
		GUILayout.Space(10f);
		
		scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);
		foreach (DownSampleResult result in downsampleResults)
		{
			if (result.success)
			{
				GUILayout.Label(result.message);
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUI.color = Color.red;
				if (GUILayout.Button("!", GUILayout.MinWidth(40f), GUILayout.MaxWidth(40f)))
				{
					EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(result.path, typeof(UnityEngine.Object)));
				}
				GUI.color = Color.white;
				GUILayout.Label(result.message);
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndScrollView();
	}
	
	private void DoDownsampleOfSelectedTextures()
	{
		downsampleResults.Clear();
		
		foreach (string path in selectedTexturePaths)
		{
			if (path.StartsWith(atlasSrcDir))
			{
				string srcUnityPath = path;
				string dstUnityPath = srcUnityPath.Replace(atlasSrcDir, atlasDstDir);
				downsampleResults.Add(DownSampleTexture(srcUnityPath, dstUnityPath));
			}
			else if (path.StartsWith(streamSrcDir))
			{
				string srcUnityPath = path;
				string dstUnityPath = path.Replace("_HD.png", "_SD.png");
				downsampleResults.Add(DownSampleTexture(srcUnityPath, dstUnityPath));
			}
		}
		
		Repaint();
		AssetDatabase.Refresh();
	}
	
	private DownSampleResult DownSampleTexture(string srcUnityPath, string dstUnityPath)
	{
		DownSampleResult result = new DownSampleResult();
		result.path = srcUnityPath;
		TextureImporter srcImporter = AssetImporter.GetAtPath(srcUnityPath) as TextureImporter;
		TextureImporterFormat initialFormat = TextureImporterFormat.ARGB32;
		if( srcImporter != null )
		{
			initialFormat = srcImporter.textureFormat;
			srcImporter.mipmapEnabled = false;
			srcImporter.isReadable = true;
			srcImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			
			AssetDatabase.ImportAsset(srcUnityPath, ImportAssetOptions.ForceUpdate);
		}
		
		Texture2D tex = AssetDatabase.LoadAssetAtPath(srcUnityPath, typeof(Texture2D)) as Texture2D;
		if (tex == null)
		{
			result.success = false;
			result.message = "Could not load a texture at this path.";
			return result;
		}
		
		Texture2D downSampled = DownSampleTextureBilinear(tex);
		byte[] bytes = downSampled.EncodeToPNG();
		try
		{
			File.WriteAllBytes(dstUnityPath, bytes);
		}
		catch (System.UnauthorizedAccessException e)
		{
			result.success = false;
			result.message = "You do not have access to write to this file! Maybe it needs to be checked out for edit? (" + e.Message + ")";
			result.path = dstUnityPath;
			return result;
		}
		
		// Generate metadatas for streamed files only:
		if (srcUnityPath.StartsWith(streamSrcDir))
		{
			string srcMetaPath = srcUnityPath.Replace(".png", "_meta.txt");
			try
			{
				File.WriteAllText(srcMetaPath, GenerateMetadata(tex));
			}
			catch (System.UnauthorizedAccessException e)
			{
				result.success = false;
				result.message = "You do not have access to write to this file! Maybe it needs to be checked out for edit? (" + e.Message + ")";
				result.path = srcMetaPath;
				return result;
			}
			string dstMetaPath = dstUnityPath.Replace(".png", "_meta.txt");
			try
			{
				File.WriteAllText(dstMetaPath, GenerateMetadata(downSampled));
			}
			catch (System.UnauthorizedAccessException e)
			{
				result.success = false;
				result.message = "You do not have access to write to this file! Maybe it needs to be checked out for edit? (" + e.Message + ")";
				result.path = dstMetaPath;
				return result;
			}
		}
		
		if (srcImporter != null)
		{
			srcImporter.isReadable = false;
			srcImporter.textureFormat = initialFormat;
			
			AssetDatabase.ImportAsset(srcUnityPath, ImportAssetOptions.ForceUpdate);
		}
		
		ApplyTextureImportSettings atis = new ApplyTextureImportSettings();
		atis.srcUnityPath = srcUnityPath;
		atis.dstUnityPath = dstUnityPath;
		atis.srcImporter = srcImporter;
		pendingSettings.Add(atis);
		
		result.success = true;
		result.message = string.Format("OK: {0} -> {1}", srcUnityPath, dstUnityPath);
		
		return result;
	}
	
	private void DoPostProcessing()
	{
		AssetDatabase.Refresh();
		
		foreach (ApplyTextureImportSettings atis in pendingSettings)
		{
			TextureImporter dstImporter = AssetImporter.GetAtPath(atis.dstUnityPath) as TextureImporter;
			if( dstImporter != null )
			{
				dstImporter.textureType = TextureImporterType.Advanced;
				dstImporter.npotScale = atis.srcImporter.npotScale;
				dstImporter.mipmapEnabled = false;
				dstImporter.isReadable = false;
				dstImporter.wrapMode = atis.srcImporter.wrapMode;
				dstImporter.filterMode = atis.srcImporter.filterMode;
				dstImporter.anisoLevel = atis.srcImporter.anisoLevel;
				dstImporter.maxTextureSize = atis.srcImporter.maxTextureSize;
				dstImporter.textureFormat = atis.srcImporter.textureFormat;
				
				AssetDatabase.ImportAsset(atis.dstUnityPath, ImportAssetOptions.ForceUpdate);
			}
		}
		
		pendingSettings.Clear();
	}
	
	private void DisplayResults()
	{
		foreach (ApplyTextureImportSettings atis in pendingSettings)
		{
			Texture2D existingTexture = AssetDatabase.LoadAssetAtPath(atis.srcUnityPath, typeof(Texture2D)) as Texture2D;
			Texture2D createdTexture = AssetDatabase.LoadAssetAtPath(atis.dstUnityPath, typeof(Texture2D)) as Texture2D;
			
			Debug.Log("> Downsampled source texture '" + atis.srcUnityPath + "'", existingTexture);
			Debug.Log("> Downsampled destination texture '" + atis.dstUnityPath + "'", createdTexture);
		}
		
		Debug.Log("> Downsampling completed.");
	}
	
	private Texture2D DownSampleTextureLinear(Texture2D tex)
	{
		Texture2D downsampled = new Texture2D(tex.width / 2, tex.height / 2, TextureFormat.ARGB32, false);
		
		// First pass... linear downsample
		for(int y = 0; y < downsampled.height; ++y)
		{
			for(int x = 0; x < downsampled.width; ++x)
			{
				Color c = tex.GetPixel(x * 2, y * 2);
				downsampled.SetPixel(x, y, c);
			}
		}
		
		return downsampled;
	}
	
	private Texture2D DownSampleTextureBilinear(Texture2D tex)
	{
		Texture2D downsampled = new Texture2D(tex.width / 2, tex.height / 2, TextureFormat.ARGB32, false);
		
		// This is a naive way to compensate for downgrading to an odd number of pixels.
		bool destOddX = ((downsampled.width & 1) == 1);
		bool destOddY = ((downsampled.height & 1) == 1);
		
		for(int y = 0; y < downsampled.height; ++y)
		{
			for(int x = 0; x < downsampled.width; ++x)
			{
				float totalWeight = 0f;
				float r = 0f, g = 0f, b = 0f, a = 0f;
				int px = x * 2;
				if (destOddX && x >= downsampled.width / 2) px ++;
				int py = y * 2;
				if (destOddY && y >= downsampled.height / 2) py ++;
				GetNearPixel(tex, px, py, 2f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px + 1, py, 0.5f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px - 1, py, 0.5f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px, py + 1, 0.5f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px, py - 1, 0.5f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px + 1, py + 1, 0.25f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px + 1, py - 1, 0.25f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px - 1, py + 1, 0.25f, ref totalWeight, ref r, ref g, ref b, ref a);
				GetNearPixel(tex, px - 1, py - 1, 0.25f, ref totalWeight, ref r, ref g, ref b, ref a);
				Color c = new Color(r / totalWeight, g / totalWeight, b / totalWeight, a / totalWeight);
				downsampled.SetPixel(x, y, c);
			}
		}
		
		return downsampled;
	}
	
	private void GetNearPixel(Texture2D tex, int x, int y, float weight, ref float weightTotal, ref float r, ref float g, ref float b, ref float a)
	{
		if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
		{
			Color c = tex.GetPixel(x, y);
			r += c.r * weight;
			g += c.g * weight;
			b += c.b * weight;
			a += c.a * weight;
			weightTotal += weight;
		}
	}
	
	private string GenerateMetadata(Texture2D tex)
	{
		// Figure out the bounds of this texture...
		int xMin = tex.width;
		int xMax = 0;
		int yMin = tex.height;
		int yMax = 0;
		for(int y = 0; y < tex.height; ++y)
		{
			for(int x = 0; x < tex.width; ++x)
			{
				Color c = tex.GetPixel(x, y);
				if (c.a > 0)
				{
					xMin = Mathf.Min(xMin, x);
					xMax = Mathf.Max(xMax, x);
					yMin = Mathf.Min(yMin, y);
					yMax = Mathf.Max(yMax, y);
				}
			}
		}
		
		float fxMin = xMin / (float)(tex.width - 1);
		float fxMax = xMax / (float)(tex.width - 1);
		float fyMin = yMin / (float)(tex.height - 1);
		float fyMax = yMax / (float)(tex.height - 1);
		
		string meta = string.Format("{0},{1},{2},{3}", fxMin, fyMin, fxMax, fyMax);
		
		return meta;
	}
	
	private void OnEditorUpdate()
	{
		if (pendingSettings.Count > 0)
		{
			DoPostProcessing();
		}
		EditorApplication.update -= OnEditorUpdate;
	}
}
