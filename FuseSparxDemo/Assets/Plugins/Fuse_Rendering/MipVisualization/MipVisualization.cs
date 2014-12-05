#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MipVisualization : MonoBehaviour 
{	
	[MenuItem("EBG/Rendering/Texture Resolution/Visualize Diffuse")]
	static void MipStartDiffuse() 
	{
		Start("_MainTex");
	}
	
	[MenuItem("EBG/Rendering/Texture Resolution/Visualize Normal Map")]
	static void MipStartNormal() 
	{
		Start("_BumpMap");
	}
	
	[MenuItem("EBG/Rendering/Texture Resolution/Visualize Specular and Emissive")]
	static void MipStartSpec() 
	{
		Start("_SpecEmissiveTex");
	}
	
	[MenuItem("EBG/Rendering/Texture Resolution/Visualize Lightmap (Merged scene)")]
	static void MipStartLM() 
	{
		Start("_lm");
	}

	[MenuItem("EBG/Rendering/Texture Resolution/Stop Visualization")]
	static void MipStop() 
	{
		Stop();
	}

	private enum eTEX_SIZE
	{
		_2 = 2,
		_4 = 4,
		_8 = 8,
		_16 = 16,
		_32 = 32,
		_64 = 64,
		_128 = 128,
		_256 = 256,
		_512 = 512,
		_1024 = 1024,
		_2048 = 2048
	}

	private static Color[] mipColors = new Color[] { 
		new Color(0.0f,  1.0f,  0.0f),	//first two mips are purposely green, to compensate for the retina displays of our devices
		new Color(0.0f,  1.0f,  0.0f),
		new Color(0.0f,  1.0f,  0.62f),
		new Color(0.0f,  0.82f, 0.98f),
		new Color(0.0f,  0.29f, 0.97f),
		new Color(0.23f, 0.0f,  0.96f),
		new Color(0.75f, 0.0f,  0.96f),
		new Color(1.0f,  0.0f,  0.62f),
		new Color(1.0f,  0.0f,  0.0f)
	};

	static Dictionary<Material, Material> materialMapping = new Dictionary<Material, Material>();
	static int originalLOD;

	static void Start(string textureToVisualize)
	{
		//reset things
		Stop();

		Shader mipVisShader = Shader.Find("Hidden/EBG/MipVis");
		
		if (mipVisShader == null)
		{
			Debug.LogError("Could not load shader Hidden/EBG/MipVis");
			return;
		}

		var enumValues = System.Enum.GetValues(typeof(eTEX_SIZE));

		Dictionary<eTEX_SIZE, Texture> textures = new Dictionary<eTEX_SIZE, Texture>();

		foreach(eTEX_SIZE size in enumValues)
		{
			textures[size] = CreateTexture((int)size);
		}

		originalLOD = Shader.globalMaximumLOD;
		Shader.globalMaximumLOD = 25;
		
		materialMapping.Clear();

		GameObject[] gameObjects = (GameObject[])FindSceneObjectsOfType(typeof (GameObject));

		foreach(GameObject g in gameObjects) 
		{
			if((g.renderer == null) || (g.renderer.sharedMaterials == null))
				continue;

			Material[] newMaterials = new Material[g.renderer.sharedMaterials.Length];

			for(int i = 0; i < g.renderer.sharedMaterials.Length; ++i)
			{
				Material m = g.renderer.sharedMaterials[i];

				if (!m.HasProperty(textureToVisualize) || (m.GetTexture(textureToVisualize) == null) || (m.shader == null))
				{
					newMaterials[i] = m;
					if (!materialMapping.ContainsKey(m))
					{
						materialMapping.Add(m, m);
					}
					continue;
				}

				newMaterials[i] = new Material(m);

				materialMapping.Add(newMaterials[i], m);

				int size = m.GetTexture(textureToVisualize).width;

				newMaterials[i].shader = mipVisShader;
				newMaterials[i].SetTexture("_MipVis", textures[(eTEX_SIZE)size]);
			}

			g.renderer.sharedMaterials = newMaterials;
		}
	}

	static void Stop()
	{
		if (materialMapping.Count == 0)
			return;

		Shader mipVisShader = Shader.Find("Hidden/EBG/MipVis");
		
		if (mipVisShader == null)
		{
			Debug.LogError("Could not load shader Hidden/EBG/MipVis");
			return;
		}

		Shader.globalMaximumLOD = originalLOD;
		
		GameObject[] gameObjects = (GameObject[])FindSceneObjectsOfType(typeof (GameObject));
		
		foreach(GameObject g in gameObjects) 
		{
			if((g.renderer == null) || (g.renderer.sharedMaterials == null))
				continue;
			
			Material[] newMaterials = new Material[g.renderer.sharedMaterials.Length];
			
			for(int i = 0; i < g.renderer.sharedMaterials.Length; ++i)
			{
				if (!materialMapping.ContainsKey(g.renderer.sharedMaterials[i]))
				{
					//really only in case stop is called before start
					newMaterials[i] = g.renderer.sharedMaterials[i];
				}
				else
				{
					newMaterials[i] = materialMapping[g.renderer.sharedMaterials[i]];
				}
			}
			
			g.renderer.sharedMaterials = newMaterials;
		}

		materialMapping.Clear();
	}

	static Texture2D CreateTexture(int size)
	{
		Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, true);
		tex.filterMode = FilterMode.Point;
		tex.Apply();
		
		for (int i = 0; i < tex.mipmapCount; ++i)
		{
			Color mipColor = mipColors[Mathf.Min(i, mipColors.Length - 1)];

			Color[] colors = tex.GetPixels(i);

			for (int j = 0; j < colors.Length; ++j)
			{
				colors[j] = mipColor;
			}

			tex.SetPixels(colors, i);
		}
		
		tex.Apply(false);

		return tex;
	}
}

#endif
