using UnityEngine;
using System.Collections.Generic;

//////////////////////////////////////////////////////////////////////////////
/// EBG Class for management of materials for UIMaskedSprite.
/// To allow dynamic batching of draw calls, UIMaskedSprite instances that
/// share the same pair of textures should also share a material.
//////////////////////////////////////////////////////////////////////////////
public static class UIMaskMaterialManager
{
	private class MaterialInstance
	{
		public int refCount = 1;
		public Material mat;
	}

	private static Dictionary<string, MaterialInstance> cachedMaterials = new Dictionary<string, MaterialInstance>();
	private static Shader maskingShader = Shader.Find("EBG/UI/BlendColoredMask");

	public static Material UseMaterial(Texture baseTex, Texture maskTex)
	{
		string key = GetKey(baseTex, maskTex);
		MaterialInstance instance = null;
		if (cachedMaterials.ContainsKey(key))
		{
			instance = cachedMaterials[key];
			instance.refCount ++;
		}
		else
		{
			instance = new MaterialInstance();
			Material mat = new Material(maskingShader);
			mat.SetTexture("_MainTex", baseTex);
			mat.SetTexture("_MaskTex", maskTex);
			instance.mat = mat;
			cachedMaterials.Add(key, instance);
		}

		return instance.mat;
	}

	public static void ReleaseMaterial(Texture baseTex, Texture maskTex)
	{
		string key = GetKey(baseTex, maskTex);
		if (cachedMaterials.ContainsKey(key))
		{
			MaterialInstance mat = cachedMaterials[key];
			mat.refCount --;
			if (mat.refCount < 1)
			{
				cachedMaterials.Remove(key);
			}
		}
	}

	public static List<EB.Collections.Tuple<string, int>> GetUseCounts()
	{
		var results = new List<EB.Collections.Tuple<string, int>>();

		foreach (var kvp in cachedMaterials)
		{
			Material mat = kvp.Value.mat;
			int refCount = kvp.Value.refCount;
			Texture tex1 = mat.GetTexture("_MainTex");
			Texture tex2 = mat.GetTexture("_MaskTex");
			string n1 = tex1.name;
			string n2 = tex2.name;

			var tuple = new EB.Collections.Tuple<string, int>(string.Format ("{0} / {1}", n1, n2), refCount);
			results.Add(tuple);
		}

		return results;
	}

	private static string GetKey(Texture t1, Texture t2)
	{
		return t1.GetInstanceID() + "_" + t2.GetInstanceID();
	}
}
