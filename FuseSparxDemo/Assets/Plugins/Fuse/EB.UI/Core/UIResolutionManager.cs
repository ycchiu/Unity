using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIResolutionManager : MonoBehaviour
{
	public enum Resolution
	{
		Sd,
		Hd
	}

	// If no override config path is found, we'll use the default.
	public const string defaultConfigJSON = "{\"uiAtlasPaths\":[{\"src\":\"Atlases/{0}UiAtlasPrefab\",\"dst\":\"Atlases/RefUiAtlasPrefab\"},{\"src\":\"Atlases/Point{0}UiAtlasPrefab\",\"dst\":\"Atlases/PointRefUiAtlasPrefab\"}]}";
	public const string overrideConfigPath = "UI/atlasConfig";

	public static UIResolutionManager Instance { get; private set; }
	public Resolution CurrentResolution { get { return _resolution; } }

	/////////////////////////////////////////////////////////////////////////
	#region AtlasRemapper
	/////////////////////////////////////////////////////////////////////////
	/// Builds from a JSON config object. Can remap a single atlas based on 
	/// the current resolution.
	/// 
	/// References to components are intentionally not cached. We don't want
	/// to force an atlas to stick around in memory when not in use.
	/////////////////////////////////////////////////////////////////////////
	private class AtlasRemapper
	{
		private string src;
		private string dst;

		// Early out on error.
		public void Remap(Resolution resolution)
		{
			// Update Atlas reference:
			string targetPath = GetSourcePath(resolution);
			GameObject targetAtlasContainer = Resources.Load(targetPath) as GameObject;
			if (targetAtlasContainer == null)
			{
				EB.Debug.LogError("AtlasRemapper was unable to load the atlas at '{0}'", targetPath);
				return;
			}
			
			UIAtlas targetAtlas = EB.Util.FindComponent<UIAtlas>(targetAtlasContainer);
			if (targetAtlas == null)
			{
				EB.Debug.LogError("AtlasRemapper was unable to find a UIAtlas component on the prefab at '{0}'", targetPath);
				return;
			}
			
			string refPath = GetDestinationPath();
			GameObject refAtlasContainer = Resources.Load(refPath) as GameObject;
			if (refAtlasContainer == null)
			{
				EB.Debug.LogError("AtlasRemapper was unable to load the reference atlas at '{0}'", refPath);
				return;
			}
			
			UIAtlas refAtlas = EB.Util.FindComponent<UIAtlas>(refAtlasContainer);
			if (refAtlas == null)
			{
				EB.Debug.LogError("AtlasRemapper was unable to find a UIAtlas component on the reference atlas at '{0}'", refPath);
				return;
			}

			refAtlas.replacement = targetAtlas;
		}

		public string GetSourcePath(Resolution res)
		{
			string result = "";

			try
			{
				result = string.Format(src, res.ToString());
			}
			catch (System.Exception)
			{
				EB.Debug.LogError("Invalid uiAtlasPaths source format. Expects somewhere to put the resolution.");
			}

			return result;
		}

		public string GetDestinationPath()
		{
			return dst;
		}

		public AtlasRemapper(Hashtable jsonObj)
		{
			src = EB.Dot.String("src", jsonObj, "");
			dst = EB.Dot.String("dst", jsonObj, "");
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region AtlasRemapperManager
	/////////////////////////////////////////////////////////////////////////
	/// Creates and stores AtlasRemapper instances.
	/////////////////////////////////////////////////////////////////////////
	private class AtlasRemapperManager
	{
		public List<AtlasRemapper> atlasRemappers = new List<AtlasRemapper>();

		public AtlasRemapperManager(Hashtable json)
		{
			ArrayList uiAtlasPaths = EB.Dot.Array("uiAtlasPaths", json, new ArrayList());
			foreach (Hashtable pathObj in uiAtlasPaths)
			{
				AtlasRemapper remapper = new AtlasRemapper(pathObj);
				this.atlasRemappers.Add(remapper);
			}
		}

		public void Remap(Resolution res)
		{
			foreach (AtlasRemapper remapper in this.atlasRemappers)
			{
				remapper.Remap(res);
			}
		}
	}
	#endregion
	/////////////////////////////////////////////////////////////////////////

	private Resolution _resolution;
	private AtlasRemapperManager _atlasRemapperManager;

	/////////////////////////////////////////////////////////////////////////
	#region Public Interface
	/////////////////////////////////////////////////////////////////////////
	public static Resolution GetResolution(float pixelSize)
	{
		Resolution result = Resolution.Sd;
		
		if (EB.Util.FloatEquals(pixelSize, 0.5f, 0.01f))
		{
			result = Resolution.Hd;
		}
		else if (EB.Util.FloatEquals(pixelSize, 1.0f, 0.01f))
		{
			result = Resolution.Sd;
		}
		
		return result;
	}
	
	public static float GetPixelSizeForResolution(Resolution res)
	{
		float pixelSize = 1f;
		
		switch (res)
		{
		case Resolution.Hd:
			pixelSize = 0.5f;
			break;
		case Resolution.Sd:
			pixelSize = 1f;
			break;
		}
		
		return pixelSize;
	}
	
	public void SwitchResolution(Resolution resolution)
	{
		if (CurrentResolution != resolution)
		{
			_resolution = resolution;
			UpdateResolution();
		}
	}

	public float GetPixelSize()
	{
		return GetPixelSizeForResolution(CurrentResolution);
	}
	
	public Vector2 GetUiDimensions()
	{
		Vector2 dimensions = Vector2.zero;
		
		if (WindowManager.Instance != null)
		{
			GameObject rootContainer = WindowManager.Instance.GetUiRoot();
			Camera uiCam = EB.Util.FindComponent<Camera>(rootContainer);
			Rect area = uiCam.pixelRect;
			dimensions.x = (area.xMax - area.xMin);
			dimensions.y = (area.yMax - area.yMin);
		}
		
		return dimensions;
	}

	public float GetAspectRatio()
	{
		float ratio = 0f;

		Vector2 dimensions = GetUiDimensions();

		if (dimensions.y > 0f)
		{
			ratio = dimensions.x / dimensions.y;
		}

		return ratio;
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////

	/////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	/////////////////////////////////////////////////////////////////////////
	private void Awake()
	{
		Instance = this;
		// This will be switched by the PerformanceManager if necessary.
		_resolution = (Misc.HDAtlas() ? Resolution.Hd : Resolution.Sd);
		ParseConfig();
		UpdateResolution();
	}

	private void ParseConfig()
	{
		Object overrideConfigFile = Resources.Load(overrideConfigPath);
		string configJSON;
		if (overrideConfigFile != null)
		{
			TextAsset configFile = overrideConfigFile as TextAsset;
			configJSON = configFile.text;
		}
		else
		{
			configJSON = defaultConfigJSON;
		}

		Hashtable configObject = EB.JSON.Parse(configJSON) as Hashtable;
		_atlasRemapperManager = new AtlasRemapperManager(configObject);
	}

	private void UpdateResolution()
	{
		_atlasRemapperManager.Remap(CurrentResolution);
		UpdateStreamedTextures();
		UpdateFonts();
		UpdatePanels();

		// Delay by a frame to allow existing sprites to switch over to the new atlas.
		EB.Coroutines.NextFrame(EB.SafeAction.Wrap(this, delegate() {
			Resources.UnloadUnusedAssets();
		}));
	}
	
	private void UpdateStreamedTextures()
	{
		bool isHD = (CurrentResolution == Resolution.Hd);
		UITextureRef[] streamedTextures = null;

		GameObject root = null;
		if (WindowManager.Instance != null)
		{
			root = WindowManager.Instance.GetUiRoot();
		}
		if (root != null)
		{
			streamedTextures = EB.Util.FindAllComponents<UITextureRef>(root);
		}
		else // Fallback cannot handle components on disabled gameobjects.
		{
			streamedTextures = (UITextureRef[])GameObject.FindObjectsOfType(typeof(UITextureRef));
		}
		foreach (UITextureRef t in streamedTextures)
		{
			t.isHD = isHD;
			t.RefreshTexture();
		}
	}
	
	private void UpdateFonts()
	{
		// If there are any labels onscreen, make them update their contents.
		UILabel[] labels = (UILabel[])GameObject.FindObjectsOfType(typeof(UILabel));
		foreach (UILabel l in labels)
		{
			l.MarkAsChanged();
		}
	}

	private void UpdatePanels()
	{
		// Refresh static UI Panels.
		UIPanel[] panels = (UIPanel[])GameObject.FindObjectsOfType(typeof(UIPanel));
		foreach (UIPanel p in panels)
		{
			p.singleFrameUpdate = true;
		}
	}
	/////////////////////////////////////////////////////////////////////////
	#endregion
	/////////////////////////////////////////////////////////////////////////
}
