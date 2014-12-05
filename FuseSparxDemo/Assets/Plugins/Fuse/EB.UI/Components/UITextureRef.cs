using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/TextureRef")]
public class UITextureRef : UITexture, UIDependency
{
	///////////////////////////////////////////////////////////////////////////
	#region Recompile Refresh
	///////////////////////////////////////////////////////////////////////////
	#if UNITY_EDITOR
	private class RecompileChecker
	{
		public static bool CheckRecompile()
		{
			bool result = recompiled;
			if (result)
			{
				recompiled = false;
			}
			return result;
		}
		
		private static bool recompiled = false;
		static RecompileChecker()
		{
			recompiled = true;
		}
	}
	
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (RecompileChecker.CheckRecompile())
		{
			// Only one instance will get this check, so pass it along to all the others.
			UITextureRef[] all = GameObject.FindObjectsOfType(typeof(UITextureRef)) as UITextureRef[];
			foreach (UITextureRef instance in all)
			{
				instance.RefreshTexture();
			}
		}
	}
	#endif
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////
	
	const string SDPostFix = "_SD";
	const string HDPostFix = "_HD";

	// NGUI HACK: This value should be zero, but doing so causes NGUI 3.0.8 f7
	// to make the element invisible then never set it to be visible again.
	const float LowAlpha = 0.002f;
	
	[System.NonSerialized]
	public bool isHD;
	public event EB.Action onLoaded;
	// If a texture is marked as important, it will block window entry until
	// loaded.
	public bool isImportant = true;
	// If true, fade the texture in when it loads.
	public bool fadeOnLoad = false;
	
	[System.NonSerialized]
	public string metadata;
	
	///////////////////////////////////////////////////////////////////////////
	#region textureLoaded
	public bool textureLoaded
	{
		get
		{
			return _textureLoaded;
		}
		private set
		{
			_textureLoaded = value;
			if (_textureLoaded && onLoaded != null)
			{
				onLoaded();
			}
			if (_textureLoaded && onReadyCallback != null)
			{
				onReadyCallback();
			}
		}
	}
	private bool _textureLoaded = false;
	#endregion
	///////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////
	#region UIDependency Implementation
	public EB.Action onReadyCallback
	{
		get
		{
			return _onReadyCallback;
		}
		set
		{
			_onReadyCallback = value;
		}
	}
	private EB.Action _onReadyCallback;
	
	public bool IsReady()
	{
		return textureLoaded;
	}

	public EB.Action onDeactivateCallback
	{
		get
		{
			return _onDeactivateCallback;
		}
		set
		{
			_onDeactivateCallback = value;
		}
	}
	private EB.Action _onDeactivateCallback;

	#endregion
	///////////////////////////////////////////////////////////////////////////
	
	private bool initialized = false;
	private string lastTexturePath;

	///////////////////////////////////////////////////////////////////////////
	#region baseTexturePath
	public string baseTexturePath
	{
		set
		{
			// Before initialization, simply store the change to the requested path.
			if (!initialized)
			{
				_baseTexturePath = value;
				return;
			}
			else if (_baseTexturePath != value)
			{
				// After initialization, store the change and update the texture immediately.
				_baseTexturePath = value;
				RefreshTexture();
			}
		}
		get { return _baseTexturePath; }
	}
	[SerializeField]
	protected string _baseTexturePath = string.Empty;
	#endregion
	///////////////////////////////////////////////////////////////////////////

	public string fullPath
	{
		get { return _baseTexturePath + (isHD ? HDPostFix : SDPostFix); }
	}
	
	protected List<string> potentialPaths
	{
		get
		{
			List<string> paths = new List<string>();
			
			// Prefer the current HD / SD path, then the alternate SD / HD path, then just the base path.
			paths.Add(_baseTexturePath + (isHD ? HDPostFix : SDPostFix));
			paths.Add(_baseTexturePath + (isHD ? SDPostFix : HDPostFix));
			paths.Add(_baseTexturePath);
			
			return paths;
		}
	}
	
	public void RefreshTexture()
	{
		if (!string.IsNullOrEmpty(_baseTexturePath))
		{
			LoadTexture(potentialPaths);
		}
		else
		{
			// This is considered 'loaded' as there is nothing to do.
			textureLoaded = true;
			mainTexture = null;
		}
	}

	public void ResizeToFit(bool trimAlpha)
	{
		Rect r = uvRect;
		float pixelSize = 1f;
		if (UIResolutionManager.Instance != null)
		{
			pixelSize = UIResolutionManager.Instance.GetPixelSize();
		}
		if (trimAlpha)
		{
			if (string.IsNullOrEmpty(metadata))
			{
				return;
			}

			string[] values = metadata.Split(',');
			if (values.Length == 4)
			{
				float f = 0f;
				float.TryParse(values[0], out f);
				r.x = f;
				float.TryParse(values[1], out f);
				r.y = f;
				float.TryParse(values[2], out f);
				r.width = (Mathf.Round((f - r.x) * mainTexture.width) / mainTexture.width);
				float.TryParse(values[3], out f);
				r.height = (Mathf.Round((f - r.y) * mainTexture.height) / mainTexture.height);
				uvRect = r;
				width = Mathf.RoundToInt(mainTexture.width * r.width * pixelSize);
				height = Mathf.RoundToInt(mainTexture.height * r.height * pixelSize);
			}
		}
		else // no trim
		{
			r.x = 0;
			r.y = 0;
			r.width = 1;
			r.height = 1;
			uvRect = r;
			width = Mathf.RoundToInt(mainTexture.width * pixelSize);
			height = Mathf.RoundToInt(mainTexture.height * pixelSize);
		}
	}
	
	///////////////////////////////////////////////////////////////////////////
	#region UITexture Overrides
	///////////////////////////////////////////////////////////////////////////
	/// In this region we are overriding the default, serialized versions of 
	/// the material and texture. This forces them to be loaded at runtime 
	/// instead of being stored with the component, which they would be with 
	/// the base UITexture class.
	///////////////////////////////////////////////////////////////////////////
	
	// This has been changed to non-serialized so that it is not stored with the component!
	[HideInInspector][System.NonSerialized] protected Material mOverrideMat;
	
	// This has been changed to non-serialized so that it is not stored with the component!
	[HideInInspector][System.NonSerialized] public Texture mOverrideTexture;

	public override Texture mainTexture
	{
		get
		{
			return mOverrideTexture;
		}
		set
		{
			if (mOverrideTexture != value)
			{
				mOverrideTexture = value;
				RemoveFromPanel();
			}
		}
	}

	/// <summary>
	/// Automatically destroy the dynamically-created material.
	/// </summary>

	public override Material material
	{
		get
		{
			if (mOverrideMat != null) return mOverrideMat;

			if (mShader == null) mShader = Shader.Find("EBG/UI/Opaque");
			mPMA = 0;
			return mOverrideMat;
		}
		set
		{
			if (mOverrideMat != value)
			{
				RemoveFromPanel();
				mOverrideMat = value;
				mPMA = -1;
				MarkAsChanged();
			}
		}
	}
	
	protected override void OnStart()
	{
		base.OnStart();
		if (UIResolutionManager.Instance != null)
		{
			isHD = (UIResolutionManager.Instance.CurrentResolution == UIResolutionManager.Resolution.Hd);
		}
		else
		{
			isHD = false;
		}
		RefreshTexture();
		initialized = true;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}
	
	protected override void OnDestroy()
	{
		base.OnDestroy();
		ReleaseLastTexture();
		if (onDeactivateCallback != null)
		{
			onDeactivateCallback();
		}
	}

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		// Don't render when we have no texture path assigned.
		// This prevents trying to render a released texture.
		if (textureLoaded)
		{
			base.OnFill(verts, uvs, cols);
		}
	}

	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////
	#region Private Implementation
	///////////////////////////////////////////////////////////////////////////
	private void LoadTexture(List<string> paths)
	{
		textureLoaded = false;
		EB.Action<List<string>, EB.Action<Texture2D>> textureLoadStrategy = null;
		
#if UNITY_EDITOR
		if (!Application.isPlaying || (EB.Assets.CurrentBundleMode == EB.Assets.DevBundleType.NoBundles))
#else
		if (EB.Assets.CurrentBundleMode == EB.Assets.DevBundleType.NoBundles)
#endif
		{
			textureLoadStrategy = LoadFromResources;
		}
		else
		{
			textureLoadStrategy = LoadViaTexturePoolManager;
		}
		
		ReleaseLastTexture();
		if (textureLoadStrategy != null)
		{
			textureLoadStrategy(paths, EB.SafeAction.Wrap<Texture2D>(this, delegate(Texture2D loadedTex) {
				mainTexture = loadedTex;
				textureLoaded = (loadedTex != null);
				if (textureLoaded && fadeOnLoad && Application.isPlaying)
				{
					StartFadeTextureIn();
				}
				if (panel != null)
				{
					panel.singleFrameUpdate = true;
				}
			}));
		}
		
		// Use the preferred path for Metadata?
		string mdPath = paths[0] + "_meta";
		TextAsset ta = Resources.Load(mdPath, typeof(TextAsset)) as TextAsset;
		metadata = (ta != null) ? ta.text : null;
	}
	
	///////////////////////////////////////////////////////////////////////////
	#region Texture Load Strategies
	///////////////////////////////////////////////////////////////////////////
	private static void LoadFromResources(List<string> paths, EB.Action<Texture2D> callback)
	{
		Texture2D texture = null;
		
		foreach (string path in paths)
		{
			texture = Resources.Load(path, typeof(Texture2D)) as Texture2D;
			if (texture != null)
			{
				break;
			}
		}
		
		callback(texture);
	}
	
	private void LoadViaTexturePoolManager(List<string> paths, EB.Action<Texture2D> callback)
	{
		lastTexturePath = "";
		if (TexturePoolManager.Instance != null && paths != null && paths.Count > 0)
		{
			TexturePoolManager.Instance.LoadTexture(paths[0], this, delegate(Texture2D tex) {
				if (tex != null)
				{
					lastTexturePath = paths[0];
					callback(tex);
				}
				else // no match, try next in list
				{
					paths.RemoveAt(0);
					LoadViaTexturePoolManager(paths, callback);
				}
			});
		}
		else
		{
			callback(null);
		}
	}
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////
	
	private void StartFadeTextureIn()
	{
		alpha = LowAlpha;
		EB.Coroutines.Run(UpdateAlpha());
	}

	private IEnumerator UpdateAlpha()
	{
		const float alphaUpdateDeltaPerSec = 2.0f;
		float startFadeTime = Time.realtimeSinceStartup;
		while (alpha < 1f)
		{
			float progress = Time.realtimeSinceStartup - startFadeTime;
			alpha = Mathf.Clamp(progress * alphaUpdateDeltaPerSec, LowAlpha, 1f);
			if (panel != null)
			{
				panel.singleFrameUpdate = true;
			}
			yield return null;
		}
	}

	private void ReleaseLastTexture()
	{
		textureLoaded = false;
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			return;
		}
#endif
		if (EB.Assets.CurrentBundleMode == EB.Assets.DevBundleType.NoBundles)
		{
			return;
		}
		if (string.IsNullOrEmpty(lastTexturePath))
		{
			return;
		}

		TexturePoolManager.Instance.ReleaseTexture(lastTexturePath);
		lastTexturePath = null;
		metadata = null;
	}
	///////////////////////////////////////////////////////////////////////////
	#endregion
	///////////////////////////////////////////////////////////////////////////
}
