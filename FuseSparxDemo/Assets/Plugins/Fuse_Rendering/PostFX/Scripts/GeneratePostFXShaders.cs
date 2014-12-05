#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace EB.Rendering
{
	public class GeneratePostFXShaders 
	{
		[MenuItem("EBG/Rendering/PostFX/Generate Shaders")]
		static void GenerateShaders() 
		{
			List<PostFXManager.ePOSTFX> enabled = new List<PostFXManager.ePOSTFX>();

			for(int i = 1; i < Mathf.Pow(2, PostFXManager.ePOSTFX_COUNT); ++i)
			{
				enabled.Clear();
				
				foreach(PostFXManager.ePOSTFX effect in System.Enum.GetValues(typeof(PostFXManager.ePOSTFX)))
				{
					if ((i & (1 << (int)effect)) != 0)
					{
						enabled.Add(effect);
					}
				}

				if (enabled.Count == 0)
					continue;

				CompositePostFX(enabled);
			}
		}
		
		private static Shader CompositePostFX(List<PostFXManager.ePOSTFX> postFX)
		{
			var include = Application.dataPath + "/Plugins/Fuse_Rendering/PostFX/Resources/PostFXComposite.cginc";
			
			List<ShaderCompositor.ShaderProperty> properties = new List<ShaderCompositor.ShaderProperty>() {
				new ShaderCompositor.ShaderProperty("_MainTex",			"",				ShaderCompositor.ePROPERTY_TYPE.Texture2D, 		ShaderCompositor.eDEFAULT_VALUE.Black),
				new ShaderCompositor.ShaderProperty("_BloomTex",			"Bloom",			ShaderCompositor.ePROPERTY_TYPE.Texture2D,		ShaderCompositor.eDEFAULT_VALUE.Black),
				new ShaderCompositor.ShaderProperty("_VignetteTex",		"Vignette",		ShaderCompositor.ePROPERTY_TYPE.Texture2D,		ShaderCompositor.eDEFAULT_VALUE.Black),
				new ShaderCompositor.ShaderProperty("_WarpTex", 			"Warp",			ShaderCompositor.ePROPERTY_TYPE.Texture2D,		ShaderCompositor.eDEFAULT_VALUE.White),
			};
			
			ShaderCompositor.CategoryBlock categoryBlock = new ShaderCompositor.CategoryBlock();
			categoryBlock.Tags = new List<string>() {"\"Queue\"=\"Geometry\""};
			categoryBlock.CullMode = UnityEngine.Rendering.CullMode.Off;
			categoryBlock.ZWrite = false;
			categoryBlock.ZTest = UnityEngine.Rendering.CompareFunction.Always;
			categoryBlock.SrcBlendMode = UnityEngine.Rendering.BlendMode.One;
			categoryBlock.DstBlendMode = UnityEngine.Rendering.BlendMode.Zero;
			
			List<string> cgs = new List<string>() 
			{ 
				"#include \"UnityCG.cginc\"", 
				"#pragma target 3.0", 
				"#pragma vertex vertex_prog", 
				"#pragma fragment fragment_prog" 
			};

			List<string> defines = new List<string>();
			foreach(EB.Rendering.PostFXManager.ePOSTFX effect in postFX)
			{
				defines.Add(effect.ToString().ToUpper() + "_ON");
			}
			defines.Sort();
			
			List<ShaderCompositor.LOD> lods = new List<ShaderCompositor.LOD>() {
				new ShaderCompositor.LOD(
					100,
					defines,
					new List<string>(),
					cgs
				)
			};
			
			char slash = System.IO.Path.DirectorySeparatorChar;
			string dir = Application.dataPath + slash + "Plugins" + slash + "Fuse_Rendering" + slash + "PostFX" + slash + "Resources" + slash;
			System.IO.Directory.CreateDirectory(dir);

			Material material = new Material(Shader.Find("Diffuse"));

			string shaderName = "EBG/PostFX/";
			foreach(EB.Rendering.PostFXManager.ePOSTFX effect in postFX)
			{
				shaderName += effect;
			}

			Shader composited = ShaderCompositor.Composite(shaderName, material, dir, include, "EBG_", properties, categoryBlock, lods);
			
			AssetDatabase.Refresh();
			
			return Shader.Find(composited.name);
		}
	}
}

#endif
