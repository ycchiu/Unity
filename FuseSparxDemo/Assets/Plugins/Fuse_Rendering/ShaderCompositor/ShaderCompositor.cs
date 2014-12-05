//#define SHADER_COMPOSITOR_DEBUG

#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EB.Rendering
{
	public class ShaderCompositor 
	{
		public enum ePROPERTY_TYPE
		{
			Color,
			Float,
			Texture2D,
			TextureCube,
			Vector
		}
		
		public enum eDEFAULT_VALUE
		{
			Black,
			White,
			Bump,
			One,
			Zero
		}

		public struct ShaderProperty
		{
			public string id;
			public string name;
			public ePROPERTY_TYPE type;
			public eDEFAULT_VALUE defaultValue;

			public ShaderProperty(string _id, string _name, ePROPERTY_TYPE _type, eDEFAULT_VALUE _defaultValue)
			{
				id = _id;
				name = _name;
				type = _type;
				defaultValue = _defaultValue;
			}
		}

		public struct CategoryBlock
		{
			public List<string> Tags;
			public UnityEngine.Rendering.CullMode CullMode;
			public UnityEngine.Rendering.BlendMode SrcBlendMode;
			public UnityEngine.Rendering.BlendMode DstBlendMode;
			public bool ZWrite;
			public UnityEngine.Rendering.CompareFunction ZTest;
		}

		public struct LOD
		{
			public int lod;
			public List<string> defines;
			public List<string> features;
			public List<string> cgs;
			
			public LOD(int _lod, List<string> _defines, List<string> _features, List<string> _cgs)
			{
				lod = _lod;
				defines = _defines;
				features = _features;
				cgs = _cgs;
			}
		}

		public static string GenerateShaderName(Material material)
		{
			string shaderName = material.shader.name;
			
			shaderName = shaderName.Replace("EBG/","Hidden/");
			
			if (material.shaderKeywords.Length == 0)
			{
				shaderName += "_BASE";
			}
			else
			{
				shaderName += "_" + string.Join("_", material.shaderKeywords);
			}

			if (material.HasProperty("_VertexLightmapOption"))
			{
				shaderName += "_" + material.GetFloat("_VertexLightmapOption");
			}

			return shaderName;
		}
		
		public static Shader Composite(Material material, string dir, string includeFile, string keywordPrefix, List<ShaderProperty> properties, CategoryBlock categoryBlock, List<LOD> lods)
		{
			string filePath = string.Empty;
			return Composite(material, dir, includeFile, keywordPrefix, properties, categoryBlock, lods, out filePath);
		}
		
		public static Shader Composite(string shaderName, Material material, string dir, string includeFile, string keywordPrefix, List<ShaderProperty> properties, CategoryBlock categoryBlock, List<LOD> lods)
		{
			string filePath = string.Empty;
			return Composite(shaderName, material, dir, includeFile, keywordPrefix, properties, categoryBlock, lods, out filePath);
		}
		
		public static Shader Composite(Material material, string dir, string includeFile, string keywordPrefix, List<ShaderProperty> properties, CategoryBlock categoryBlock, List<LOD> lods, out string filePath)
		{
			string shaderName = GenerateShaderName(material);
			return Composite(shaderName, material, dir, includeFile, keywordPrefix, properties, categoryBlock, lods, out filePath);
		}

		public static Shader Composite(string shaderName, Material material, string dir, string includeFile, string keywordPrefix, List<ShaderProperty> properties, CategoryBlock categoryBlock, List<LOD> lods, out string filePath)
		{
			filePath = dir + shaderName.Replace("/","_") + ".shader";

			StreamWriter sw = File.CreateText(filePath);

			//SHADER NAME

			sw.WriteLine("Shader \"" + shaderName + "\"");
			sw.WriteLine("{");

			//SHADER PROPERTIES

			sw.WriteLine("\tProperties");
			sw.WriteLine("\t{");
			foreach(ShaderProperty property in properties)
			{
				//_NDotLWrap("N.L Wrap", Float) = 0
				sw.Write("\t\t" + property.id + "(\"" + property.name + "\", ");
				switch(property.type)
				{
				case(ePROPERTY_TYPE.Color):
				case(ePROPERTY_TYPE.Float):
				case(ePROPERTY_TYPE.Vector):
					sw.Write(property.type.ToString());
					break;
				case(ePROPERTY_TYPE.Texture2D):
					sw.Write("2D");
					break;
				case(ePROPERTY_TYPE.TextureCube):
					sw.Write("Cube");
					break;
				}

				sw.Write(") = ");

				switch(property.type)
				{
				case(ePROPERTY_TYPE.Color):
					switch(property.defaultValue)
					{
					case(eDEFAULT_VALUE.Black):
						sw.WriteLine("(0, 0, 0, 0)");
						break;
					case(eDEFAULT_VALUE.White):
						sw.WriteLine("(1, 1, 1, 1)");
						break;
					default:
						EB.Debug.LogError("Invalid default value for " + property.name);
						break;
					}
					break;
				case(ePROPERTY_TYPE.Float):
					switch(property.defaultValue)
					{
					case(eDEFAULT_VALUE.Zero):
						sw.WriteLine("0");
						break;
					case(eDEFAULT_VALUE.One):
						sw.WriteLine("1");
						break;
					default:
						EB.Debug.LogError("Invalid default value for " + property.name);
						break;
					}
					break;
				case(ePROPERTY_TYPE.Vector):
					switch(property.defaultValue)
					{
					case(eDEFAULT_VALUE.Zero):
						sw.WriteLine("(0, 0, 0, 0)");
						break;
					case(eDEFAULT_VALUE.One):
						sw.WriteLine("(1, 1, 1, 1)");
						break;
					default:
						EB.Debug.LogError("Invalid default value for " + property.name);
						break;
					}
					break;
				case(ePROPERTY_TYPE.Texture2D):
				case(ePROPERTY_TYPE.TextureCube):
					switch(property.defaultValue)
					{
					case(eDEFAULT_VALUE.Black):
						sw.WriteLine("\"black\" {}");
						break;
					case(eDEFAULT_VALUE.White):
						sw.WriteLine("\"white\" {}");
						break;
					case(eDEFAULT_VALUE.Bump):
						sw.WriteLine("\"bump\" {}");
						break;
					default:
						EB.Debug.LogError("Invalid default value for " + property.name);
						break;
					}
					break;
				}
			}
			sw.WriteLine("\t}");

			//SHADER CATEGORY

			sw.WriteLine("\tCategory");
			sw.WriteLine("\t{");

			sw.WriteLine("\t\tTags");
			sw.WriteLine("\t\t{");
			foreach(string tag in categoryBlock.Tags)
			{
				sw.WriteLine("\t\t\t" + tag);
			}
			sw.WriteLine("\t\t}");
			sw.WriteLine("\t\tLighting Off");
			sw.WriteLine("\t\tFog { Mode Off }");
			sw.WriteLine("\t\tCull " + categoryBlock.CullMode.ToString());
			sw.WriteLine("\t\tZWrite " + (categoryBlock.ZWrite ? "On" : "Off"));
			switch(categoryBlock.ZTest)
			{
			case(UnityEngine.Rendering.CompareFunction.Always):
			case(UnityEngine.Rendering.CompareFunction.Equal):
			case(UnityEngine.Rendering.CompareFunction.Greater):
			case(UnityEngine.Rendering.CompareFunction.Less):
			case(UnityEngine.Rendering.CompareFunction.Never):
			case(UnityEngine.Rendering.CompareFunction.NotEqual):
				sw.WriteLine("\t\tZTest " + categoryBlock.ZTest.ToString());
				break;
			case(UnityEngine.Rendering.CompareFunction.LessEqual):
				sw.WriteLine("\t\tZTest LEqual");
				break;
			case(UnityEngine.Rendering.CompareFunction.GreaterEqual):
				sw.WriteLine("\t\tZTest GEqual");
				break;
			case(UnityEngine.Rendering.CompareFunction.Disabled):
				sw.WriteLine("\t\tZTest Always");
				break;
			};
			if ((categoryBlock.SrcBlendMode == UnityEngine.Rendering.BlendMode.One) && (categoryBlock.DstBlendMode == UnityEngine.Rendering.BlendMode.Zero))
			{
				sw.WriteLine("\t\tBlend Off");
			}
			else
			{
				sw.WriteLine("\t\tBlend " + categoryBlock.SrcBlendMode + " " + categoryBlock.DstBlendMode);
			}

			//SUBSHADERS
			lods.Sort((l1, l2) => l2.lod.CompareTo(l1.lod));

			for(int i = 0; i < lods.Count; ++i)
			{
				//try to skip any LODs that would compile the same thing as the next set
				if (i < lods.Count - 1)
				{
					bool identical = true;

					LOD current = lods[i];
					LOD next = lods[i+1];

				
					//make sure our defines are the same length
					identical &= (current.defines.Count == next.defines.Count);

					//make sure all defines in current are in next
					foreach(string define in current.defines)
					{
						identical &= next.defines.Contains(define);
					}

					foreach(string keyword in material.shaderKeywords)
					{
						if(keyword.EndsWith("_ON"))
						{
							string keywordOff = keyword.Replace("_ON", "_OFF");
							if(current.features.Contains(keywordOff))
							{
								if (!next.features.Contains(keywordOff))
								{
									identical = false;
								}
							}
							else if (next.features.Contains(keywordOff))
							{
								if (!current.features.Contains(keywordOff))
								{
									identical = false;
								}
							}
						}
					}
					
					if (identical)
					{
						continue;
					}
				}

				LOD lod = lods[i];
				sw.WriteLine("\t\tSubshader");
				sw.WriteLine("\t\t{");
				sw.WriteLine("\t\t\tLOD " + lod.lod);
				sw.WriteLine("\t\t\tPass");
				sw.WriteLine("\t\t\t{");
				sw.WriteLine("\t\t\t\tCGPROGRAM");

				foreach(string cg in lod.cgs)
				{
					sw.WriteLine("\t\t\t\t" + cg);
				}

				var shader = CompositeMaterial(material, includeFile, "EBG_", lod.defines, lod.features);

				foreach(string line in shader.Split('\n'))
				{
					sw.WriteLine("\t\t\t\t" + line);
				}

				sw.WriteLine("\t\t\t\tENDCG");
				sw.WriteLine("\t\t\t}");
				sw.WriteLine("\t\t}");
			}

			sw.WriteLine("\t}");

			sw.WriteLine("}");
			sw.Close();

			//trigger a refresh due to newly saved shader
			AssetDatabase.Refresh();

			return Shader.Find(shaderName);
		}

		static string CompositeMaterial(Material material, string includeFile, string keywordPrefix, List<string> defines, List<string> features)
		{
			List<string> keywords = new List<string>();

			//add all the fixed defines
			foreach(string define in defines)
			{
				keywords.Add(define);
			}

			//add all the features that are explictely "OFF"; those that are "ON" we only want on if the material defines it so
			foreach(string feature in features)
			{
				if (feature.EndsWith("_OFF"))
				{
					keywords.Add(feature);
				}
			}

			//add all the material defines that we haven't forced
			string[] materialKeywords = material.shaderKeywords;
			foreach(string keyword in materialKeywords)
			{
				if (!keyword.StartsWith(keywordPrefix) || keywords.Contains(keyword))
				{
					continue;
				}

				if (keyword.EndsWith("_OFF") && !keywords.Contains(keyword.Replace("_OFF", "_ON")))
				{
					keywords.Add(keyword);
				}
				else if (keyword.EndsWith("_ON") && !keywords.Contains(keyword.Replace("_ON", "_OFF")))
				{
					keywords.Add(keyword);
				}
			}

			//add -D in front of all our defines
			List<string> keywordArguments = new List<string>();
			foreach(string keyword in keywords)
			{
				keywordArguments.Add("-D" + keyword);
			}

			string arguments = "-P -E " + string.Join(" ", keywordArguments.ToArray()) + " -undef " + includeFile;

			#if SHADER_COMPOSITOR_DEBUG
				EB.Debug.LogError(arguments);
			#endif

			ShellResult result = ShellCommand("cpp", arguments);
			
			#if SHADER_COMPOSITOR_DEBUG
				EB.Debug.LogError(result.stdout);
			#endif
		
			return "//" + string.Join(" ", keywords.ToArray()) + "\n" + result.stdout;
		}
		
		public class ShellResult
		{
			public int resultCode = -1;
			public string stdout = "";
			public string stderr = "";	
		}
		
		private static ShellResult ShellCommand(string command, string arguments)
		{
			ShellResult result = new ShellResult();
			
			try {	
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = command;
				processStartInfo.Arguments = arguments;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
				Process proc = Process.Start(processStartInfo);
				
				while (true)
				{
					string outLine = proc.StandardOutput.ReadLine();
					string errLine = proc.StandardError.ReadLine();

					if (outLine == null && errLine == null)
					{
						break;
					}

					if (outLine != null && outLine.Trim() != "") 
					{
						result.stdout += outLine + "\n";
					}

					if (errLine != null && errLine.Trim() != "")
					{
						result.stderr += errLine + "\n";
					}
				}
				proc.WaitForExit();
				result.resultCode = proc.ExitCode;
			} 		
			catch(System.Exception e)
			{
				EB.Debug.LogError(e);
			}
			return result;
		}
	}
}

#endif
