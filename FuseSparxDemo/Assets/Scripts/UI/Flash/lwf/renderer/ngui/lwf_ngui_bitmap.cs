// EBG START
// Custom NGUI renderer

using UnityEngine;
using ResourceCache = LWF.UnityRenderer.ResourceCache;
using MeshContext = LWF.UnityRenderer.MeshContext;

namespace LWF {

	namespace NGUIRenderer {

		public delegate void BitmapLoadedDelegate(BaseRenderer renderer, Material material);

		public partial class Factory : IRendererFactory
		{
			private BitmapContext[] m_bitmapContexts;
			private BitmapContext[] m_bitmapExContexts;
			
			private void CreateBitmapContexts(Data data)
			{
				m_bitmapContexts = new BitmapContext[data.bitmaps.Length];
				for (int i = 0; i < data.bitmaps.Length; ++i) {
					Format.Bitmap bitmap = data.bitmaps[i];
					// Ignore null texture
					if (bitmap.textureFragmentId == -1)
						continue;
					int bitmapExId = -i - 1;
					Format.BitmapEx bitmapEx = new Format.BitmapEx();
					bitmapEx.matrixId = bitmap.matrixId;
					bitmapEx.textureFragmentId = bitmap.textureFragmentId;
					bitmapEx.u = 0;
					bitmapEx.v = 0;
					bitmapEx.w = 1;
					bitmapEx.h = 1;
					m_bitmapContexts[i] =
						new BitmapContext(this, data, bitmapEx, bitmapExId);
					m_bitmapContexts[i].Load();
				}
				
				m_bitmapExContexts = new BitmapContext[data.bitmapExs.Length];
				for (int i = 0; i < data.bitmapExs.Length; ++i) {
					Format.BitmapEx bitmapEx = data.bitmapExs[i];
					// Ignore null texture
					if (bitmapEx.textureFragmentId == -1)
						continue;
					m_bitmapExContexts[i] = new BitmapContext(this, data, bitmapEx, i);
					m_bitmapExContexts[i].Load();
				}
			}
			
			public override void Destruct()
			{
				for (int i = 0; i < m_bitmapContexts.Length; ++i)
					if (m_bitmapContexts[i] != null)
						m_bitmapContexts[i].Destruct();
				for (int i = 0; i < m_bitmapExContexts.Length; ++i)
					if (m_bitmapExContexts[i] != null)
						m_bitmapExContexts[i].Destruct();
				base.Destruct();
			}
		}
		
		public class BitmapContext
		{
			private enum BitmapSource
			{
				GAME_ATLAS,
				STREAMED_TEXTURE,
				LWF
			}

			public delegate void BitmapContextLoadedDelegate(Material material);

			private class MaterialCache
			{
				private class Record
				{
					public int refCount;
					public Material material;

					public Record(Material mat)
					{
						refCount = 1;
						material = mat;
					}
				}

				private System.Collections.Generic.Dictionary<string, Record> mCache = new System.Collections.Generic.Dictionary<string, Record>();

				public void Add(string name, Material mat)
				{
					mCache.Add(name, new Record(mat));
				}

				public Material Request(string name)
				{
					if (mCache.ContainsKey(name))
					{
						Record rec = mCache[name];
						rec.refCount += 1;
						return rec.material;
					}
					return null;
				}

				public void Release(string name)
				{
					if (mCache.ContainsKey(name))
					{
						Record rec = mCache[name];
						rec.refCount -= 1;
						if (rec.refCount <= 0)
						{
							rec.material = null;
							mCache.Remove(name);
						}
					}
				}
			}

			private Factory m_factory;
			private Material m_material;
			private Data m_data;
			private float m_height;
			private string m_textureName;
			private string m_fragmentName;
			private int m_bitmapExId;
			private bool m_premultipliedAlpha;
			private Texture m_texture;
			private Shader m_shader;

			private Format.TextureFragment m_fragmentData;
			private Format.Texture m_textureData;
			private Format.BitmapEx m_bitmapEx;

			private BitmapSource m_source;

			private Vector3 [] m_verts;
			private Vector2 [] m_uvs;

			public Factory factory {get {return m_factory;}}
			public Material material {get {return m_material;}}
			public Shader shader { get { return m_shader; } }
			public Texture texture { get { return m_texture; } }
			public float height {get {return m_height;}}
			public int bitmapExId {get {return m_bitmapExId;}}
			public bool premultipliedAlpha {get {return m_premultipliedAlpha;}}
			public string fragmentName { get { return m_fragmentName; } }

			private static string ATLAS_PREFIX = "ATLAS_";
			private static string TEXTURE_PREFIX = "TEXTURE_";

			private static MaterialCache sMaterialCache = new MaterialCache();

			public BitmapContext(BitmapContext other)
			{
				m_factory = other.m_factory;
				m_data = other.m_data;
				m_bitmapEx = other.m_bitmapEx;
				m_bitmapExId = other.m_bitmapExId;

				m_fragmentData = other.m_fragmentData;
				m_textureData = other.m_textureData;
				
				m_premultipliedAlpha = other.m_premultipliedAlpha;
				
				m_fragmentName = other.m_fragmentName;
			}

			public BitmapContext(Factory factory, Data data, Format.BitmapEx bitmapEx, int bitmapExId)
			{
				m_factory = factory;
				m_data = data;
				m_bitmapExId = bitmapExId;
				m_bitmapEx = bitmapEx;
				
				m_fragmentData =
					data.textureFragments[m_bitmapEx.textureFragmentId];
				m_textureData = data.textures[m_fragmentData.textureId];

				m_premultipliedAlpha = (m_textureData.format ==
				                        (int)Format.Constant.TEXTUREFORMAT_PREMULTIPLIEDALPHA);

				m_fragmentName = data.strings[m_fragmentData.stringId];
				int extIndex = m_fragmentName.LastIndexOf(".png");
				if (extIndex != -1)
				{
					m_fragmentName = m_fragmentName.Substring(0, extIndex);	
				}
			}

			public void Load(BitmapContextLoadedDelegate cb = null)
			{
				if (m_fragmentName.StartsWith(ATLAS_PREFIX))
				{
					// Load from atlas
					// e.g. ATLAS_RefUiAtlasPrefab_Icon_Perf_Engine.png
					string nameSansPrefix = m_fragmentName.Substring(ATLAS_PREFIX.Length);
					string atlasName = nameSansPrefix.Substring(0, nameSansPrefix.IndexOf("_"));
					string spriteName = nameSansPrefix.Substring (atlasName.Length + 1);
					LoadBitmapFromAtlas(atlasName, spriteName, cb);
				}
				else if (m_fragmentName.StartsWith(TEXTURE_PREFIX))
				{
					// Load from streamed textures
					// Format: TEXTURE_{Hyphen-Separated-Directory-Path}_{TextureName}.png
					// where {Hyphen-Separated-Directory-Path} is assumed to be under Resources, plus some root texture location that is optionally defined by our texture adapter.
					// the following string crap should resolve to a path something like Bundles/UITextures/SomeDir/SomeChildDir/TextureName
					string nameSansPrefix = m_fragmentName.Substring (TEXTURE_PREFIX.Length);
					string streamPath = nameSansPrefix.Substring(0, nameSansPrefix.IndexOf ("_")).Replace ("-", "/");
					string bareName = nameSansPrefix.Substring(nameSansPrefix.IndexOf("_") + 1);
					string textureRoot = factory.textureAdapter != null ? factory.textureAdapter.TextureRootLocation : string.Empty;
					string textureName;
					if (string.IsNullOrEmpty(textureRoot))
					{
						textureName = string.Format("{0}/{1}", streamPath, bareName);
					}
					else
					{
						if (textureRoot.EndsWith ("/")) textureRoot.Substring(0, textureRoot.Length - 1);	// ensure no double-slashing!
						textureName = string.Format("{0}/{1}/{2}", textureRoot, streamPath, bareName);
					}
					LoadBitmapFromTexture(textureName, cb);
				}
				else
				{
					// Load from LWF spritesheet
					LoadFromLWF(cb);
				}
			}

			public void LoadBitmapFromAtlas(string atlasName, string spriteName, BitmapContextLoadedDelegate cb = null)
			{
				m_source = BitmapSource.GAME_ATLAS;
				string atlasRoot = factory.textureAdapter != null ? factory.textureAdapter.AtlasRootLocation : "Atlases";
				if (atlasRoot.EndsWith("/")) atlasRoot = atlasRoot.Substring (0, atlasRoot.Length - 1);
				string atlasPath;
				if (string.IsNullOrEmpty(atlasRoot))
				{
					atlasPath = atlasName;
				}
				else
				{
					atlasPath = string.Format("{0}/{1}", atlasRoot, atlasName);
				}

				GameObject go = Resources.Load (atlasPath) as GameObject;
				if (go != null)
				{
					UIAtlas atlas = go.GetComponent<UIAtlas>();
					if (atlas != null)
					{
						UISpriteData sd = atlas.GetSprite(spriteName);
						if (sd != null)
						{
							Texture atlasTex = atlas.texture;
							
							float texScale = GetLWFTextureScale(atlas.pixelSize);
							m_material = atlas.spriteMaterial;
							m_texture = atlasTex;
							m_shader = atlas.spriteMaterial.shader;
							FillTextureData(atlasTex.width, atlasTex.height, texScale, 0f, 0f, sd.x, sd.y, sd.width, sd.height, m_bitmapEx);
							if (cb != null)
							{
								cb(m_material);
							}
						}
						else
						{
							Debug.LogError ("Sprite '" + spriteName + "' does not exist in atlas '" + atlasName + "'");
						}
					}
					else
					{
						Debug.LogError ("Atlas '" + atlasName + "' is not valid");
					}
				}
				else
				{
					Debug.LogError ("Atlas '" + atlasName + "' does not exist");
				}
			}

			public void LoadBitmapFromTexture(string textureName, BitmapContextLoadedDelegate cb = null)
			{
				m_source = BitmapSource.STREAMED_TEXTURE;
				m_textureName = factory.textureAdapter != null ? factory.textureAdapter.ProcessTextureName(textureName) : textureName;
				if (factory.textureAdapter != null)
				{
					factory.textureAdapter.LoadTexture (m_textureName, delegate(Texture2D tex)
					{
						if (tex != null)
						{
							m_shader = factory.textureAdapter.GetDefaultShader();
							m_texture = tex;
							m_material = sMaterialCache.Request(m_textureName);
							if (m_material == null)
							{
								m_material = new Material(m_shader);
								m_material.color = UnityEngine.Color.white;
								m_material.mainTexture = m_texture;
								m_material.name = m_textureName;
								sMaterialCache.Add(m_textureName, m_material);
							}
							float texScale = factory.textureAdapter != null ? GetLWFTextureScale(factory.textureAdapter.GetPixelSize()) : GetLWFTextureScale(1.0f);
							//Debug.Log ("KL: loaded texture H/W: [" + tex.height + "/" + tex.width + "], fragment H/W: [" + m_fragmentData.w + "/" + m_fragmentData.h + "]");
							// attempt to force the in-game texture to the same size as what we are expecting had this texture come from spritesheet
							FillTextureData (m_fragmentData.w, m_fragmentData.h, texScale, 0f, 0f, 0f, 0f, m_fragmentData.w, m_fragmentData.h, m_bitmapEx);
							if (cb != null)
							{
								cb(m_material);
							}
						}
					});
				}
				else
				{
					m_material = ResourceCache.SharedInstance().LoadTexture(
							m_data.name, m_textureName, m_textureData.format,
							factory.textureLoader, factory.textureUnloader);
	
					if (factory.renderQueueOffset != 0)
						m_material.renderQueue += factory.renderQueueOffset;

					m_shader = m_material.shader;
					m_texture = m_material.mainTexture;
	
					FillTextureData(m_textureData.width, m_textureData.height, m_textureData.scale, m_fragmentData.x, m_fragmentData.y, m_fragmentData.u, m_fragmentData.v, m_fragmentData.w, m_fragmentData.h, m_bitmapEx, m_fragmentData.rotated);
					if (cb != null)
					{
						cb(m_material);
					}
				}
			}

			public void LoadFromLWF(BitmapContextLoadedDelegate cb = null)
			{
				// Load from spritesheet
				m_source = BitmapSource.LWF;
				m_textureName = factory.texturePrefix + m_textureData.filename;
				if (factory.textureAdapter != null)
				{
					factory.textureAdapter.LoadTexture (m_textureName, delegate(Texture2D tex)
					{
						m_shader = factory.textureAdapter.GetDefaultShader();
						m_texture = tex;
						
						m_material = sMaterialCache.Request(m_textureName);
						if (m_material == null)
						{
							m_material = new Material(m_shader);
							m_material.color = UnityEngine.Color.white;
							m_material.mainTexture = m_texture;
							m_material.name = m_textureName;
							sMaterialCache.Add (m_textureName, m_material);
						}
						FillTextureData(m_textureData.width, m_textureData.height, m_textureData.scale, m_fragmentData.x, m_fragmentData.y, m_fragmentData.u, m_fragmentData.v, m_fragmentData.w, m_fragmentData.h, m_bitmapEx, m_fragmentData.rotated);
						if (cb != null)
						{
							cb(m_material);
						}
					});
				}
				else
				{
					m_material = ResourceCache.SharedInstance().LoadTexture(
							m_data.name, m_textureName, m_textureData.format,
							factory.textureLoader, factory.textureUnloader);

					if (factory.renderQueueOffset != 0)
						m_material.renderQueue += factory.renderQueueOffset;

					FillTextureData(m_textureData.width, m_textureData.height, m_textureData.scale, m_fragmentData.x, m_fragmentData.y, m_fragmentData.u, m_fragmentData.v, m_fragmentData.w, m_fragmentData.h, m_bitmapEx, m_fragmentData.rotated);
					if (cb != null)
					{
						cb(m_material);
					}
				}
			}

			private float GetLWFTextureScale(float gameTextureScale)
			{
				// HACK: see if there is a way to rectify this?
				// LWF pixel size is HD=1.0f, SD=0.5f;
				// FillTextureData requires LWF pixel size standards for the texScale parameter
				return (gameTextureScale == 1.0f) ? 0.5f : 1.0f;
			}

			// NOTE: This code is based off of LWF.CombinedMeshRenderer.BitmapContext's constructor
			private void FillTextureData(float texWidth, float texHeight, float texScale, float fragmentX, float fragmentY, float fragmentU, float fragmentV, float fragmentWidth, float fragmentHeight,
			                             Format.BitmapEx bitmapEx, int fragmentRotated = 0)
			{
				float tw = texWidth;
				float th = texHeight;
				
				float x = fragmentX;
				float y = - (float)fragmentY;
				float u = (float)fragmentU;
				float v = th - (float)fragmentV;
				float w = (float)fragmentWidth;
				float h = (float)fragmentHeight;
				
				float bu = bitmapEx.u * w;
				float bv = bitmapEx.v * h;
				float bw = bitmapEx.w;
				float bh = bitmapEx.h;
				
				x += bu;
				y += bv;
				u += bu;
				v += bv;
				w *= bw;
				h *= bh;
				
				m_height = h / texScale;
				float x0 = x / texScale;
				float y0 = y / texScale;
				float x1 = (x + w) / texScale;
				float y1 = (y + h) / texScale;
				
				// mesh vertices and uvs are in a different order compared to what NGUI is expecting
				/* NGUI:						LWF:
				 * 		1 -------- 2			2 -------- 0
				 * 		|		   |			|		   |
				 * 		|		   |			|		   |
				 * 		|		   |			|		   |
				 *      0 -------- 3			3 -------- 1
				 * 
				 * Given that (0,0) is on the bottom-left corner.
				 */

				m_verts = new Vector3[]{
					new Vector3(x0, y1, 0),
					new Vector3(x0, y0, 0),
					new Vector3(x1, y0, 0),
					new Vector3(x1, y1, 0),
				};
				
				if (fragmentRotated == 0) {
					float u0 = u / tw;
					float v0 = (v - h) / th;
					float u1 = (u + w) / tw;
					float v1 = v / th;
					m_uvs = new Vector2[]{
						new Vector2(u0, v1),
						new Vector2(u0, v0),
						new Vector2(u1, v0),
						new Vector2(u1, v1),
					};
				} else {
					float u0 = u / tw;
					float v0 = (v - w) / th;
					float u1 = (u + h) / tw;
					float v1 = v / th;
					m_uvs = new Vector2[]{
						new Vector2(u1, v1),
						new Vector2(u0, v1),
						new Vector2(u0, v0),
						new Vector2(u1, v0),
					};
				}
			}

			public void Destruct()
			{
				if (m_source == BitmapSource.STREAMED_TEXTURE || m_source == BitmapSource.LWF)
				{
					if (factory.textureAdapter != null)
					{
						factory.textureAdapter.UnloadTexture (m_textureName);
						sMaterialCache.Release (m_textureName);
					}
					else
					{
						ResourceCache.SharedInstance().UnloadTexture(m_data.name, m_textureName);
					}
				}

				m_material = null;
				m_shader = null;
				m_texture = null;
				m_verts = null;
				m_uvs = null;
			}

			public void Fill(Matrix4x4 matrix, UnityEngine.Color color, BetterList<UnityEngine.Vector3> verts, BetterList<UnityEngine.Vector2> uvs, BetterList<UnityEngine.Color32> cols)
			{
				if (m_verts != null && m_uvs != null)
				{
					for (int i = 0; i < m_verts.Length; ++i)
					{
						verts.Add(matrix.MultiplyPoint(m_verts[i]));
						cols.Add(color);
						uvs.Add(m_uvs[i]);
					}
				}
			}
		}

		public class BitmapRenderer : BaseRenderer
		{
			BitmapContext m_activeContext;
			BitmapContext m_context;
			public Matrix4x4 m_matrix;
			UnityEngine.Color m_colorMult;
			#if LWF_USE_ADDITIONALCOLOR
			UnityEngine.Color m_colorAdd;
			#endif
			#if UNITY_EDITOR
			bool m_visible;
			#endif

			private BitmapContext m_substitutionContext;

			public BitmapContext context { get { return m_activeContext; } }
			public override Material material { get { return m_activeContext.material; } }
			public override Shader shader { get { return m_activeContext.shader; } }
			public override Texture texture { get { return m_activeContext.texture; } }

			public BitmapRenderer(LWF lwf, BitmapContext context) : base(lwf)
			{
				m_context = context;
				m_activeContext = m_context;
				m_matrix = new Matrix4x4();
				m_colorMult = new UnityEngine.Color();
				#if LWF_USE_ADDITIONALCOLOR
				m_colorAdd = new UnityEngine.Color();
				#endif
			}

			public void LoadTexture(string textureName)
			{
				BitmapContext sub = new BitmapContext(m_context);
				sub.LoadBitmapFromTexture(textureName, delegate(Material material)
				{
					if (m_substitutionContext != null) m_substitutionContext.Destruct();
					m_substitutionContext = sub;
					m_activeContext = m_substitutionContext;
					if (onRenderMaterialsChanged != null)
					{
						onRenderMaterialsChanged(this, material);
					}
				});
			}

			public void LoadAtlasSprite(string atlasName, string spriteName)
			{
				BitmapContext sub = new BitmapContext(m_context);
				sub.LoadBitmapFromAtlas(atlasName, spriteName, delegate(Material material)
				{
					if (m_substitutionContext != null) m_substitutionContext.Destruct();
					m_substitutionContext = sub;
					m_activeContext = m_substitutionContext;
					if (onRenderMaterialsChanged != null)
					{
						onRenderMaterialsChanged(this, material);
					}
				});
			}

			public void LoadDefault()
			{
				if (m_substitutionContext != null)
				{
					m_substitutionContext.Destruct();
					m_substitutionContext = null;
				}
				m_activeContext = m_context;
				if (onRenderMaterialsChanged != null)
				{
					onRenderMaterialsChanged(this, m_activeContext.material);
				}
			}

			////////////////////////////////////////////////////////////
			#region LWF.Renderer implementation
			// All this method will do is just set up any properties that will be required in Fill(), no actual rendering will be done here.
			public override void Render(Matrix matrix, ColorTransform colorTransform,
			                            int renderingIndex, int renderingCount, bool visible)
			{
				// Ignore null texture
				#if UNITY_EDITOR
				m_visible = visible;
				#endif
				if (m_activeContext == null || !visible)
					return;
				
				Factory factory = m_activeContext.factory;
				#if LWF_USE_ADDITIONALCOLOR
				factory.ConvertColorTransform(
					ref m_colorMult, ref m_colorAdd, colorTransform);
				#else
				factory.ConvertColorTransform(ref m_colorMult, colorTransform);
				#endif
				if (m_colorMult.a <= 0)
					return;
				if (m_activeContext.premultipliedAlpha) {
					m_colorMult.r *= m_colorMult.a;
					m_colorMult.g *= m_colorMult.a;
					m_colorMult.b *= m_colorMult.a;
				}

				factory.ConvertMatrix(ref m_matrix, matrix, 1,
				                      renderingCount - renderingIndex, m_activeContext.height);
			}
			#endregion

			////////////////////////////////////////////////////////////
			#region NGUICommonRenderer implementation
			public override void Fill(BetterList<UnityEngine.Vector3> verts, BetterList<UnityEngine.Vector2> uvs, BetterList<UnityEngine.Color32> cols)
			{
				if (m_activeContext == null)
					return;

			    #if UNITY_EDITOR
				if (!m_visible)
					return;
				#endif
			    
				m_activeContext.Fill(m_matrix, m_colorMult, verts, uvs, cols);
			}
			#endregion
		}
		
	}	// namespace NGUIRenderer
}	// namespace LWF


// EBG END