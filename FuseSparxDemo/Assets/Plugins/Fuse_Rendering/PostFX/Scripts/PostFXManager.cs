//#define USE_GET_TEMPORARY
#define EBG_POSTFX_BLOOM
#define EBG_POSTFX_VIGNETTE
#define EBG_POSTFX_WARP
//#define EBG_POSTFX_TONE_MAP
//#define EBG_POSTFX_COLOR_GRADE

using UnityEngine;
using System.Collections.Generic;
using System;
using EB.Rendering;

namespace EB.Rendering
{

	public class PostFXManager : MonoBehaviour
	{
		private enum POSTFX_KEYWORDS
		{
			POSTFX,
			BLOOMPASS
		}

		private bool			_paused = false;
		private bool			_inited = false;

		public List<ePOSTFX> CurrentPostFX { get; private set; }
		public ePOSTFX_QUALITY Quality { get; private set; }

		public enum ePOSTFX
		{
			#if EBG_POSTFX_VIGNETTE
			Vignette,
			#endif
			#if EBG_POSTFX_BLOOM
			Bloom,
			#endif
			#if EBG_POSTFX_WARP
			Warp,
			#endif
			#if EBG_POSTFX_TONE_MAP
			ToneMap,
			#endif
			#if EBG_POSTFX_COLOR_GRADE
			ColorGrade
			#endif

		}

		public enum ePOSTFX_QUALITY
		{
			Off = -1,
			Low = 0,
			High = 1,
		}

		public const int ePOSTFX_COUNT = 3;

		//Composite
		public Shader			_CompositeShader;
		public Material			_CompositeMaterial;

		#if EBG_POSTFX_BLOOM
		//Blur
		private Shader			_GaussianBlurShader;
		private Material		_GaussianBlurMaterial;
		
		//Bloom
		private string 			_BloomTextureName = "_BloomTex";
		private string			_BloomColorName = "_PostFXBloomColor";
		private string			_BloomOverbrightColorName = "_PostFXBloomOverbrightColor";
		private string			_BloomIntensityName = "_PostFXBloomIntensity";
		private Color			_BloomColor = Color.black;
		private Color			_BloomOverbrightColor = Color.black;
		private float			_BloomIntensity = 1.0f;
		private float			_BloomRamp = 1.0f;
		private int[][] 		_BloomBlurWidth = new int[2][];
		private int[][]			_BloomBlurHeight = new int[2][];
		private RenderTexture[] _BloomBlurTextures;
		private float 			_BloomBlurSigma = 1.0f;
		public float 			_GBlurModifier = 1.0f;
		
		#if EBG_POSTFX_TONE_MAP
		//Tone Mapping
		private int[][] 		_ToneMappingWidth = new int[2][];
		private int[][]			_ToneMappingHeight = new int[2][];
		private RenderTexture[] _ToneMappingTextures;
		private string			_ToneMappingName = "_PostFXToneMapping";
		private float			_ToneMappingTargetBrightness = 0.4f;
		private float			_ToneMappingCurrentBrightness = 0.4f;
		private float			_ToneMappingResponsiveness = 0.1f;
		#endif

		#endif

		#if EBG_POSTFX_VIGNETTE
		//Vignette
		private string 			_VignetteTextureName = "_VignetteTex";
		private string 			_VignetteIntensityName = "_PostFXVignetteIntensity";
		private Texture2D		_VignetteTexture;
		private float			_VignetteIntensity;
		#endif

		#if EBG_POSTFX_WARP
		//Warp
		private string			_WarpTextureName = "_WarpTex";
		private string			_WarpIntensityName = "_PostFXWarpIntensity";
		private RenderTextureFormat _WarpRenderTextureFormat;
		private RenderTexture	_WarpRenderTexture;
		public Camera			_WarpCamera;
		private int[] 			_WarpRenderTextureWidth = new int[2] { 512, 256 };
		private int[] 			_WarpRenderTextureHeight = new int[2] { 256, 128 };
		public string			_WarpLayerName = "Warp";
		public string			_WarpAndSceneLayerName = "WarpAndScene";
		private Vector2			_WarpIntensity = Vector2.zero;
		private Shader			_WarpReplacementShader;
		#endif

		#if EBG_POSTFX_COLOR_GRADE
		//ColorGrading
		private Texture3D		_ColorGradeTexture;
		private string			_ColorGradeTextureName = "_ColorGradeTex";
		#endif
		
		#if UNITY_EDITOR
		public bool				UseSceneView = false;
		#endif
		
		static PostFXManager _this;

		public static PostFXManager Instance
		{ 
			get 
			{ 
				if (_this == null)
				{
					GameObject pM = GameObject.Find("PostFXManager");

					if (pM != null)
					{
	#if UNITY_EDITOR
						if (Application.isPlaying)
						{
							Destroy(pM);
						}
						else
						{
							DestroyImmediate(pM);
						}
	#else
						Destroy(pM);
	#endif
					}
					pM = new GameObject("PostFXManager");
					_this = (PostFXManager)pM.AddComponent<PostFXManager>();
					_this.InitBase();
					DontDestroyOnLoad(pM);			
				}
				return _this; 

			} 
		} 
	
		public void Init(Camera camera, ePOSTFX_QUALITY quality, ePOSTFX[] newPostFX)
		{
			if (camera == null)
			{
				EB.Debug.LogWarning("Couldn't init PostFX as we don't have a camera");
			}

			if (quality != Quality)
			{
				//quality mode has changed, destroy any previously active postfx
				foreach(ePOSTFX postfx in Enum.GetValues(typeof(ePOSTFX)))
				{
					if (IsActive(postfx))
					{
						DestroyEffect(postfx);
					}
				}
			}

			Quality = quality;
			
			var trigger = camera.gameObject.GetComponent<PostFXManagerTrigger>();

			if ((Quality == ePOSTFX_QUALITY.Off) || (newPostFX.Length == 0))
			{
				if (trigger != null)
				{
					DestroyEntity(trigger);
				}
				return;
			}
			else if (trigger == null)
			{
				//create the trigger if it doesn't exist
				camera.gameObject.AddComponent<PostFXManagerTrigger>();
			}

			foreach(ePOSTFX postfx in Enum.GetValues(typeof(ePOSTFX)))
			{
				//destroy any previously active postfx that are no longer active
				if (IsActive(postfx) && (Array.IndexOf(newPostFX, postfx) == -1))
				{
					DestroyEffect(postfx);
				}
				//init any active post fx that weren't previously active
				if (!IsActive(postfx) && (Array.IndexOf(newPostFX, postfx) != -1))
				{
					InitEffect(postfx, camera);
				}
			}
			
			_inited = true;
		}

		private bool DoPostFX()
		{
			return !_paused && _inited && (CurrentPostFX.Count > 0);
		}

		public void PostRender(Camera camera, RenderTexture src, RenderTexture dst)
		{
			if (!DoPostFX())
				return;
			
		#if EBG_POSTFX_BLOOM
			if (IsActive(ePOSTFX.Bloom))
			{
				Bloom(src);
				
				#if EBG_POSTFX_TONE_MAP
				if (IsActive(ePOSTFX.ToneMap))
				{
					ToneMap(src);
				}
				#endif
			}
		#endif

		#if EBG_POSTFX_VIGNETTE
			if (IsActive(ePOSTFX.Vignette))
			{
				Vignette(src);
			}
		#endif

		#if EBG_POSTFX_WARP
			if (IsActive(ePOSTFX.Warp))
			{
				Warp(src, camera);
			}
		#endif

		#if EBG_POSTFX_COLOR_GRADE
			if (IsActive(ePOSTFX.ColorGrade))
			{
				ColorGrade(src, camera);
			}
		#endif

			Graphics.Blit(src, dst, _CompositeMaterial);
		}

		//Things that use little-to-no memory, and we always keep around
		public void InitBase()
		{
			CurrentPostFX = new List<ePOSTFX>(ePOSTFX_COUNT);

		#if EBG_POSTFX_BLOOM
			_BloomBlurWidth[(int)ePOSTFX_QUALITY.Low] 	= new int[] {512, 128, 64, 64};
			_BloomBlurHeight[(int)ePOSTFX_QUALITY.Low] 	= new int[] {256, 64,  32, 32};
		
			_BloomBlurWidth[(int)ePOSTFX_QUALITY.High] 	= new int[] {1024, 512, 256, 128, 128, 128};
			_BloomBlurHeight[(int)ePOSTFX_QUALITY.High] = new int[] {512,  256, 128, 64, 64, 64};

			_GaussianBlurShader = Shader.Find("EBG/Effects/GaussianBlur");
			_GaussianBlurMaterial = new Material(_GaussianBlurShader);
			_GaussianBlurMaterial.hideFlags = HideFlags.NotEditable;
		#endif

		#if EBG_POSTFX_TONE_MAP
			_ToneMappingWidth[(int)ePOSTFX_QUALITY.Low]  = new int[] {256, 64, 16};
			_ToneMappingHeight[(int)ePOSTFX_QUALITY.Low] = new int[] {128, 32,  8};
		
			_ToneMappingWidth[(int)ePOSTFX_QUALITY.High]  = new int[] {512, 128, 32, 16};
			_ToneMappingHeight[(int)ePOSTFX_QUALITY.High] = new int[] {256,  64, 16, 8};
		#endif
		}
		
		private void InitEffect(ePOSTFX postfx, Camera camera)
		{
			EnablePostFX(postfx, true);

			switch(postfx)
			{
			#if EBG_POSTFX_BLOOM
				case(ePOSTFX.Bloom):
				EB.Debug.Log("PostFXManager: Initing Bloom");
				InitBloom();
				break;
			#endif
			#if EBG_POSTFX_VIGNETTE
				case(ePOSTFX.Vignette):
				EB.Debug.Log("PostFXManager: Initing Vignette");
				InitVignette();
				break;
			#endif
			#if EBG_POSTFX_WARP
				case(ePOSTFX.Warp):
				EB.Debug.Log("PostFXManager: Initing Warp");
				InitWarp(camera);
				break;
			#endif
			#if EBG_POSTFX_TONE_MAP
				case(ePOSTFX.ToneMap):
				EB.Debug.Log("PostFXManager: Initing Tone Mapping");
				InitToneMapping();
				break;
			#endif
			#if EBG_POSTFX_COLOR_GRADE
				case(ePOSTFX.ColorGrade):
				EB.Debug.Log("PostFXManager: Initing Color Grading");
				InitColorGrade();
				break;
			#endif
			default:
				EB.Debug.LogError("Don't know how to init postfx " + postfx.ToString());
				break;
			}
		}
		
		private void DestroyEffect(ePOSTFX postfx)
		{
			EnablePostFX(postfx, false);

			switch(postfx)
			{
			#if EBG_POSTFX_BLOOM
				case(ePOSTFX.Bloom):
				EB.Debug.Log("PostFXManager: Destroying Bloom");
				DestroyBloom();
				break;
			#endif
			#if EBG_POSTFX_VIGNETTE
				case(ePOSTFX.Vignette):
				EB.Debug.Log("PostFXManager: Destroying Vignette");
				DestroyVignette();
				break;
			#endif
			#if EBG_POSTFX_WARP
				case(ePOSTFX.Warp):
				EB.Debug.Log("PostFXManager: Destroying Warp");
				DestroyWarp();
				break;
			#endif
			#if EBG_POSTFX_TONE_MAP
				case(ePOSTFX.ToneMap):
				EB.Debug.Log("PostFXManager: Destroying Tone Mapping");
				DestroyToneMapping();
				break;
			#endif
			#if EBG_POSTFX_COLOR_GRADE
				case(ePOSTFX.ColorGrade):
				EB.Debug.Log("PostFXManager: Destroying Color Grading");
				DestroyColorGrade();
				break;
			#endif
			default:
				EB.Debug.LogError("Don't know how to destroy postfx " + postfx.ToString());
				break;
			}
		}
		
		#if EBG_POSTFX_BLOOM
		
		private void InitBloom()
		{
			EnableShaderKeyword(POSTFX_KEYWORDS.BLOOMPASS, true);

			int bloomBlurTexturesCount = _BloomBlurWidth[(int)Quality].Length;

			_BloomBlurTextures = new RenderTexture[bloomBlurTexturesCount];

			for (int i = 0; i < bloomBlurTexturesCount; ++i)
			{
				#if USE_GET_TEMPORARY
				_BloomBlurTextures[i] = null;
				#else
				_BloomBlurTextures[i] = new RenderTexture(_BloomBlurWidth[(int)Quality][i], _BloomBlurHeight[(int)Quality][i], 0, RenderTextureFormat.ARGB32);
				DontDestroyOnLoad(_BloomBlurTextures[i]);
				_BloomBlurTextures[i].Create();
				#endif
			}
		}

		void DestroyEntity( UnityEngine.Object g)
		{
		#if UNITY_EDITOR
			if (Application.isPlaying==false)
			{
				DestroyImmediate(g);
			}
			else
			{
				Destroy(g);
			}
		#else
			Destroy(g);
		#endif
		}
		private void DestroyBloom()
		{
			EnableShaderKeyword(POSTFX_KEYWORDS.BLOOMPASS, false);

			int bloomBlurTextureCount = _BloomBlurWidth[(int)Quality].Length;

			for (int i = 0; i < bloomBlurTextureCount; ++i)
			{
				if (_BloomBlurTextures[i] != null)
				{
					#if USE_GET_TEMPORARY
					RenderTexture.ReleaseTemporary(_BloomBlurTextures[i]);
					#else
					DestroyEntity(_BloomBlurTextures[i]);
					#endif
				}
			}
			_BloomBlurTextures = null;
		}

		public void SetBloomColors(Color bloomColor, Color bloomOverbrightColor)
		{
			_BloomColor = bloomColor;
			_BloomOverbrightColor = bloomOverbrightColor;
		}
		
		public void SetBloomRamp(float bloomRamp)
		{
			_BloomRamp = bloomRamp;
		}

		public void SetBloomBlur(float bloomBlur)
		{
			_BloomBlurSigma = bloomBlur;
		}
		
		public void SetBloomIntensity(float intensity)
		{
			_BloomIntensity = intensity;
		}
		
		private static Vector4 GenerateGaussianBlurKernel(POSTFX_GUASSIAN_BLUR_MODE mode, float sigma)
		{
			Vector4 res = Vector4.zero;
			if (mode == POSTFX_GUASSIAN_BLUR_MODE.TAP_3 || mode == POSTFX_GUASSIAN_BLUR_MODE.TAP_3_RAMP)
			{
				res.x = Mathf.Exp(-0.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				res.y = Mathf.Exp(-1.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				float t = res.y + res.x + res.y;
				res /= t;
			}
			else
			{
				res.x = Mathf.Exp(-0.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				res.y = Mathf.Exp(-1.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				res.z = Mathf.Exp(-4.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				res.w = Mathf.Exp(-9.0f / (2.0f * sigma * sigma)) / (2.0f * Mathf.PI * sigma * sigma);
				float t = res.w + res.z + res.y + res.x + res.y + res.z + res.w;
				res /= t;
			}
			return res;
		}
		
		enum POSTFX_GAUSSIAN_BLUR_DIRECTION
		{
			HORIZONTAL,
			VERTICAL
		}

		enum POSTFX_GUASSIAN_BLUR_MODE
		{
			//maps to the pass ordering in the gaussian blur shader
			TAP_3 		= 0,
			TAP_3_RAMP 	= 1,
			TAP_7 		= 2,
			TAP_7_RAMP 	= 3
		}
			
		private void GaussianBlur(POSTFX_GAUSSIAN_BLUR_DIRECTION dir, POSTFX_GUASSIAN_BLUR_MODE mode, RenderTexture src, RenderTexture dst, float sigma, float ramp = 1.0f)
		{
			_GaussianBlurMaterial.SetVector("_BlurKernel", GenerateGaussianBlurKernel(mode, sigma));
			if (dir == POSTFX_GAUSSIAN_BLUR_DIRECTION.HORIZONTAL)
			{
				_GaussianBlurMaterial.SetVector("_StepSize", new Vector2(_GBlurModifier / src.width, 0.0f));
			}
			else
			{
				_GaussianBlurMaterial.SetVector("_StepSize", new Vector2(0.0f, _GBlurModifier / src.height));
			}
			if (mode == POSTFX_GUASSIAN_BLUR_MODE.TAP_7_RAMP || mode == POSTFX_GUASSIAN_BLUR_MODE.TAP_3_RAMP)
			{
				_GaussianBlurMaterial.SetFloat("_Ramp", ramp);
			}
			Graphics.Blit(src, dst, _GaussianBlurMaterial, (int)mode);
		}
		
		private void Bloom(RenderTexture baseTexture)
		{
			RenderTexture src = baseTexture;

			int bloomBlurTextureCount = _BloomBlurWidth[(int)Quality].Length;

			#if USE_GET_TEMPORARY
			for (int i = 0; i < bloomBlurTextureCount; ++i)
			{
				if (_BloomBlurTextures[i] != null)
				{
					RenderTexture.ReleaseTemporary(_BloomBlurTextures[i]);
				}
				_BloomBlurTextures[i] = RenderTexture.GetTemporary(_BloomBlurWidth[(int)Quality][i], _BloomBlurHeight[(int)Quality][i], 0, RenderTextureFormat.ARGB32);
			}
			#else
			//discard last frames texture
			_BloomBlurTextures[bloomBlurTextureCount - 1].DiscardContents();
			#endif

			//first two passes are cheap blurs
			GaussianBlur(POSTFX_GAUSSIAN_BLUR_DIRECTION.VERTICAL, 		POSTFX_GUASSIAN_BLUR_MODE.TAP_3_RAMP, 	src, 					_BloomBlurTextures[0], 	_BloomBlurSigma, 	_BloomRamp);
			GaussianBlur(POSTFX_GAUSSIAN_BLUR_DIRECTION.HORIZONTAL, 	POSTFX_GUASSIAN_BLUR_MODE.TAP_3, 		_BloomBlurTextures[0], 	_BloomBlurTextures[1], 	_BloomBlurSigma);
			_BloomBlurTextures[0].DiscardContents();

			//blur
			for (int i = 2; i < bloomBlurTextureCount; i += 2)
			{
				GaussianBlur(POSTFX_GAUSSIAN_BLUR_DIRECTION.VERTICAL, POSTFX_GUASSIAN_BLUR_MODE.TAP_7, _BloomBlurTextures[i-1], _BloomBlurTextures[i], _BloomBlurSigma);
				
				#if !USE_GET_TEMPORARY
				_BloomBlurTextures[i-1].DiscardContents();
				#endif
				
				GaussianBlur(POSTFX_GAUSSIAN_BLUR_DIRECTION.HORIZONTAL, POSTFX_GUASSIAN_BLUR_MODE.TAP_7, _BloomBlurTextures[i], _BloomBlurTextures[i + 1], _BloomBlurSigma);

				#if !USE_GET_TEMPORARY
				_BloomBlurTextures[i].DiscardContents();
				#endif
			}
			
			_CompositeMaterial.SetTexture(_BloomTextureName, _BloomBlurTextures[bloomBlurTextureCount-1]);
			_CompositeMaterial.SetColor(_BloomColorName, _BloomColor);
			_CompositeMaterial.SetColor(_BloomOverbrightColorName, _BloomOverbrightColor);
			_CompositeMaterial.SetFloat(_BloomIntensityName, _BloomIntensity);
		}

		#endif

		#if EBG_POSTFX_VIGNETTE
		
		private void InitVignette()
		{
			_VignetteTexture = (Texture2D)Resources.Load("Rendering/Textures/VignetteMask");
			_VignetteIntensity = 0.0f;
		}
		
		private void DestroyVignette()
		{
			Resources.UnloadAsset(_VignetteTexture);
			_VignetteTexture = null;
		}

		private void Vignette(RenderTexture baseTexture)
		{
			_CompositeMaterial.SetFloat(_VignetteIntensityName, _VignetteIntensity);
			_CompositeMaterial.SetTexture(_VignetteTextureName, _VignetteTexture);
		}
		
		public void SetVignetteIntensity(float intensity)
		{
			_VignetteIntensity = intensity;
		}
		
		#endif

		#if EBG_POSTFX_WARP
		
		private void InitWarp(Camera camera)
		{
			RenderTextureFormat[] desiredFormats = { RenderTextureFormat.RGHalf, RenderTextureFormat.RGFloat, RenderTextureFormat.ARGB32 };

			_WarpRenderTextureFormat = RenderTextureFormat.Default;
			foreach (RenderTextureFormat format in desiredFormats)
			{
				if (SystemInfo.SupportsRenderTextureFormat(format))
				{
					_WarpRenderTextureFormat = format;
					break;
				}
			}

			#if USE_GET_TEMPORARY
			_WarpRenderTexture = null;
			#else
			_WarpRenderTexture = new RenderTexture(_WarpRenderTextureWidth[(int)Quality], _WarpRenderTextureHeight[(int)Quality], 24, _WarpRenderTextureFormat);
			_WarpRenderTexture.isPowerOfTwo = true;
			_WarpRenderTexture.hideFlags = HideFlags.HideAndDontSave;
			_WarpRenderTexture.name = "WarpRenderTexture";
			_WarpRenderTexture.Create();
			#endif

			GameObject go = new GameObject( "Warp Camera", typeof(Camera), typeof(Skybox) );
			DontDestroyOnLoad(go);
			go.hideFlags = HideFlags.HideAndDontSave;
			_WarpCamera = go.camera;
			_WarpCamera.enabled = false;
			_WarpCamera.orthographic = false;
			_WarpCamera.targetTexture = _WarpRenderTexture;
			_WarpCamera.backgroundColor = Color.gray;
			_WarpCamera.clearFlags = CameraClearFlags.SolidColor;

			_WarpReplacementShader = Shader.Find("EBG/Effects/WarpReplacement");
		}
		
		private void DestroyWarp()
		{
			#if USE_GET_TEMPORARY
			RenderTexture.ReleaseTemporary(_WarpRenderTexture);
			_WarpRenderTexture = null;
			#else
			_WarpRenderTexture.Release();
			_WarpRenderTexture = null;
			#endif

			DestroyEntity(_WarpCamera.gameObject);
			_WarpCamera = null;
		}

		private void Warp(RenderTexture src, Camera camera)
		{
			_WarpCamera.transform.position = camera.transform.position;
			_WarpCamera.transform.rotation = camera.transform.rotation;
			_WarpCamera.aspect = camera.aspect;
			_WarpCamera.fieldOfView = camera.fieldOfView;
			_WarpCamera.farClipPlane = camera.farClipPlane;
			_WarpCamera.nearClipPlane = camera.nearClipPlane;
			#if USE_GET_TEMPORARY
			if (_WarpRenderTexture != null)
			{
				RenderTexture.ReleaseTemporary(_WarpRenderTexture);
				_WarpRenderTexture = null;
			}
				_WarpRenderTexture = RenderTexture.GetTemporary(_WarpRenderTextureWidth[(int)Quality], _WarpRenderTextureHeight[(int)Quality], 24, _WarpRenderTextureFormat);
			#endif

			//render depth pre-pass
			_WarpCamera.cullingMask = camera.cullingMask;
			_WarpCamera.clearFlags = CameraClearFlags.SolidColor;
			_WarpCamera.SetReplacementShader(_WarpReplacementShader, "RenderType");
			_WarpCamera.Render();
			_WarpCamera.ResetReplacementShader();

			//render warp stuff
			_WarpCamera.clearFlags = CameraClearFlags.Nothing;
			_WarpCamera.cullingMask = (1 << LayerMask.NameToLayer(_WarpLayerName)) | (1 << LayerMask.NameToLayer(_WarpAndSceneLayerName));
			_WarpCamera.Render();			

			_CompositeMaterial.SetTexture(_WarpTextureName, _WarpRenderTexture);
			_CompositeMaterial.SetVector(_WarpIntensityName, _WarpIntensity);
		}

		public void SetWarpIntensity(Vector2 intensity)
		{
			_WarpIntensity = intensity;
		}
		
		#endif

		#if EBG_POSTFX_TONE_MAP
		
		private void InitToneMapping()
		{
			int toneMapTexturesCount = _ToneMappingWidth[(int)Quality].Length;
			
			_ToneMappingTextures = new RenderTexture[toneMapTexturesCount];
			
			for (int i = 0; i < toneMapTexturesCount; ++i)
			{
				#if USE_GET_TEMPORARY
				_ToneMappingTextures[i] = null;
				#else
				_ToneMappingTextures[i] = new RenderTexture(_ToneMappingWidth[(int)Quality][i], _ToneMappingHeight[(int)Quality][i], 0, RenderTextureFormat.ARGB32);
				DontDestroyOnLoad(_ToneMappingTextures[i]);
				_ToneMappingTextures[i].Create();
				#endif
			}
		}
		
		private void DestroyToneMapping()
		{
			int toneMapTexturesCount = _ToneMappingWidth[(int)Quality].Length;

			for (int i = 0; i < toneMapTexturesCount; ++i)
			{
				if (_BloomBlurTextures[i] != null)
				{
					#if USE_GET_TEMPORARY
					RenderTexture.ReleaseTemporary(_ToneMappingTextures[i]);
					#else
					Destroy(_ToneMappingTextures[i]);
					#endif
				}
			}
			_ToneMappingTextures = null;
		}
		
		private void ToneMap(RenderTexture baseTexture)
		{
			RenderTexture src = baseTexture;
			
			int toneMapTexturesCount = _ToneMappingWidth[(int)Quality].Length;
			
			#if USE_GET_TEMPORARY
			for (int i = 0; i < toneMapTexturesCount; ++i)
			{
				if (_ToneMappingTextures[i] != null)
				{
					RenderTexture.ReleaseTemporary(_ToneMappingTextures[i]);
				}
				_ToneMappingTextures[i] = RenderTexture.GetTemporary(_ToneMappingWidth[(int)Quality][i], _ToneMappingHeight[(int)Quality][i], 0, RenderTextureFormat.ARGB32);
			}
			#endif

			Graphics.Blit(baseTexture, _ToneMappingTextures[0]);

			for(int i = 1; i < toneMapTexturesCount - 1; ++i)
			{
				Graphics.Blit(_ToneMappingTextures[i-1], _ToneMappingTextures[i]);
				#if !USE_GET_TEMPORARY
				_ToneMappingTextures[i - 1].DiscardContents();
				#endif
			}
			
			_GaussianBlurMaterial.SetTexture(_BloomTextureName, _BloomBlurTextures[_BloomBlurTextures.Length-1]);
			_GaussianBlurMaterial.SetColor(_BloomColorName, _BloomColor);
			Graphics.Blit(_ToneMappingTextures[toneMapTexturesCount - 2], _ToneMappingTextures[toneMapTexturesCount - 1], _GaussianBlurMaterial, 4);
			#if !USE_GET_TEMPORARY
			_ToneMappingTextures[toneMapTexturesCount-2].DiscardContents();
			#endif
			
			var tex = _ToneMappingTextures[toneMapTexturesCount - 1];
			
			RenderTexture.active = tex;

			Texture2D dest = new Texture2D(tex.width, tex.height);
			dest.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
			dest.Apply();

			#if !USE_GET_TEMPORARY
			_ToneMappingTextures[toneMapTexturesCount - 1].DiscardContents();
			#endif

			Color averageColor = Color.black;
			for (int x = 0; x < tex.width; ++x)
			{
				for (int y = 0; y < tex.height; ++y)
				{
					averageColor += dest.GetPixel(x,y);
				}
			}

			averageColor /= tex.width * tex.height;

			float brightness = (averageColor.r * 0.299f) + (averageColor.g * 0.587f) + (averageColor.b * 0.114f);

			float multiplier = _ToneMappingTargetBrightness / brightness;

			_ToneMappingCurrentBrightness = Mathf.Lerp(_ToneMappingCurrentBrightness, multiplier, _ToneMappingResponsiveness);

			_CompositeMaterial.SetFloat(_ToneMappingName, _ToneMappingCurrentBrightness);
		}

		public void SetToneMappingTargetBrightness(float target)
		{
			_ToneMappingTargetBrightness = target;
		}

		public void SetToneMappingResponsiveness(float target)
		{
			_ToneMappingResponsiveness = target;
		}
		
		#endif

		#if EBG_POSTFX_COLOR_GRADE
		
		private void InitColorGrade()
		{
		}
		
		private void DestroyColorGrade()
		{
			_ColorGradeTexture = null;
		}
		
		private void ColorGrade(RenderTexture baseTexture, Camera camera)
		{
			_CompositeMaterial.SetTexture(_ColorGradeTextureName, _ColorGradeTexture);
		}

		public void SetColorGradeTexture(Texture3D colourGradeTexture)
		{
			_ColorGradeTexture = _ColorGradeTexture;
		}

		#endif

		//UTILITY
		
		public void Pause()
		{
			_paused = true;
		}
		
		public void Resume()
		{
			_paused = false;
		}
		
		private void EnableShaderKeyword(POSTFX_KEYWORDS keyword, bool enabled)
		{
			string shaderKeywordPrefix = keyword.ToString();
			if (enabled)
			{
				Shader.DisableKeyword("EBG_" + shaderKeywordPrefix + "_OFF");
				Shader.EnableKeyword("EBG_" + shaderKeywordPrefix + "_ON");
			}
			else
			{
				Shader.DisableKeyword("EBG_" + shaderKeywordPrefix + "_ON");
				Shader.EnableKeyword("EBG_" + shaderKeywordPrefix + "_OFF");
			}
		}
		
		private void EnablePostFX(ePOSTFX keyword, bool enabled)
		{
			if (enabled)
			{
				CurrentPostFX.Add(keyword);
			}
			else
			{
				CurrentPostFX.Remove(keyword);
			}

			CurrentPostFX.Sort();

			if (CurrentPostFX.Count > 0)
			{
				string shaderName = "EBG/PostFX/";
				foreach(ePOSTFX effect in CurrentPostFX)
				{
					shaderName += effect;
				}
				_CompositeShader = Shader.Find(shaderName);
				if (_CompositeShader == null)
				{
					Debug.LogError("Could not load postfx shader " + shaderName);
				}
				else
				{
					_CompositeMaterial = new Material(_CompositeShader);
					_CompositeMaterial.hideFlags = HideFlags.NotEditable;
				}
			}
		}
		
		private bool IsActive(ePOSTFX postfx)
		{
			return CurrentPostFX.Contains(postfx);
		}
	
	}
}