using UnityEngine;
using System.Collections;

public class SampleFlashTextureAdapter : MonoBehaviour, LWF.ITextureAdapter
{
	public static SampleFlashTextureAdapter Instance { get { return sInstance; } }

	void Awake()
	{
		sInstance = this;
	}

	public void LoadTexture(string textureName, System.Action<Texture2D> callback)
	{
		TexturePoolManager.Instance.LoadTexture(textureName, this, delegate(Texture2D obj) {
			callback(obj);
		});
	}
	
	public void UnloadTexture(string textureName, System.Action callback = null)
	{
		TexturePoolManager.Instance.ReleaseTexture(textureName);
		if (callback != null) callback();
	}
	
	public void UnloadTexture(Texture2D texture, System.Action callback = null)
	{
		TexturePoolManager.Instance.ReleaseTexture(texture.name);
		if (callback != null) callback();
	}
	
	public bool IsHD()
	{
		return false;
	}
	
	public string ProcessTextureName(string textureName)
	{
		if (IsHD ()) return textureName + HD_PREFIX;
		else return textureName + SD_PREFIX;
	}
	
	public float GetPixelSize()
	{
		return 1.0f;
	}
	
	public Shader GetDefaultShader()
	{
		return Shader.Find("EBG/UI/BlendColored");
	}

	public string TextureRootLocation
	{
		get { return "UI"; }
	}

	public string AtlasRootLocation
	{
		get { return "Bundles/Atlases"; }
	}


	private static string HD_PREFIX = "_HD";
	private static string SD_PREFIX = "_SD";
	private static SampleFlashTextureAdapter sInstance;
}

