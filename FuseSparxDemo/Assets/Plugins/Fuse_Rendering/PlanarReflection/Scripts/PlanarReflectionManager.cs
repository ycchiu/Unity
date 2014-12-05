using UnityEngine;
using System.Collections.Generic;

namespace EB.Rendering
{
	public class PlanarReflectionManager : MonoBehaviour
	{
		public class Config
		{
			//render quality
			public eREFLECTION_QUALITY Quality = eREFLECTION_QUALITY.High;

			//layer bitmask to render
			public int ReflectionLayerMask = 0;

			//replacement shaders
			public bool UseReplacementShaders = false;
			public string ReplacementShaderName = string.Empty;
			public string ReplacementShaderTag = "RenderType";
		}

		public enum 			eCLEAR_MODE
		{
			Color,
			Skybox
		};

		public enum eREFLECTION_QUALITY
		{
			Off = -1,
			Low = 0,
			High = 1,
		}

		public LayerMask		m_layerMask = -1;
		private Color			m_backgroundColor = Color.black;
		private eCLEAR_MODE	m_clearMode = eCLEAR_MODE.Color;
		
		private string 			m_textureName = "_PlanarReflectionTex";

	    private float			m_ClipPlaneOffset = 0.07f;
		public Camera			m_ReflectionCamera;
		
		//reflection
		private int[] 			m_ReflectionWidth = {256, 512};
		private int[] 			m_ReflectionHeight = {128, 256};
		private RenderTexture	m_renderTexture = null;
		
		//env blur
		private int[] 			m_blurWidth = {128, 256};
		private int[] 			m_blurHeight = {64, 128};
		private int[] 			m_blurWidth2 = {128, 256};
		private int[] 			m_blurHeight2 = {64, 128};
		public RenderTexture 	m_blurTexture1 = null;
		public RenderTexture 	m_blurTexture2 = null;
		private Shader 			m_blurShader;
		private Material 		m_blurMaterial;
		
		private bool			m_paused = false;
		private bool 			m_setup = false;

		public eREFLECTION_QUALITY Quality { get; private set; }

		public bool 			m_UseReplacementShaders = false;
		private Shader			m_ReplacementShader;
		private string			m_ReplacementShaderTag = "RenderType";
		
		#if UNITY_EDITOR
		public bool				UseSceneView = false;
		#endif


		
		static PlanarReflectionManager _this;
		public static PlanarReflectionManager Instance
		{ 
			get 
			{ 
				if (_this == null)
				{
					GameObject go = new GameObject("PlanarReflectionManager");
					_this = (PlanarReflectionManager)go.AddComponent<PlanarReflectionManager>();
					DontDestroyOnLoad(go);
				}
				
				return _this; 
			} 
		} 
		
		private void Setup(Config config)
		{
			//things that only have to happen once

			if (m_setup)
			{
				return;
			}

			Quality = eREFLECTION_QUALITY.Off;
			
	        GameObject go = new GameObject( "Mirror Refl Camera", typeof(Camera), typeof(Skybox) );
			DontDestroyOnLoad(go);
	        //go.hideFlags = HideFlags.HideAndDontSave;
	        m_ReflectionCamera = go.camera;
	        m_ReflectionCamera.enabled = false;
	        m_ReflectionCamera.transform.position = Vector3.zero;
	        m_ReflectionCamera.transform.rotation = Quaternion.identity;
	    	m_ReflectionCamera.backgroundColor = Color.black;
	    	m_ReflectionCamera.clearFlags = CameraClearFlags.SolidColor;

			m_blurShader = Shader.Find("EBG/Effects/ReflectionBlur");
			m_blurMaterial = new Material(m_blurShader);
			m_blurMaterial.hideFlags = HideFlags.NotEditable;

			m_UseReplacementShaders = config.UseReplacementShaders;
			if (m_UseReplacementShaders)
			{
				m_ReplacementShader = Shader.Find(config.ReplacementShaderName);
			}
			m_ReplacementShaderTag = config.ReplacementShaderTag;
			
			m_setup = true;
		}
		
		public void Init(Config config)
		{
			Setup(config);

			m_layerMask = config.ReflectionLayerMask;

			if (config.Quality == Quality)
			{
				Debug.Log("PlanarReflectionManager: No quality change " + Quality);
				return;
			}

			if (Quality != PlanarReflectionManager.eREFLECTION_QUALITY.Off)
			{
				//De-init if we weren't previously "off"
				Debug.Log("PlanarReflectionManager: DeInit " + Quality);
				DeInit();
			}

			if ((config.Quality != PlanarReflectionManager.eREFLECTION_QUALITY.Off) && (m_layerMask != 0))
			{
				//init if we aren't off and have something to reflect
				Debug.Log("PlanarReflectionManager: Init " + config.Quality);
				InitQuality(config.Quality);
				Quality = config.Quality;
			}
			else
			{
				Debug.Log("PlanarReflectionManager: Off");
				Quality = PlanarReflectionManager.eREFLECTION_QUALITY.Off;
			}

		}

		private void InitQuality(PlanarReflectionManager.eREFLECTION_QUALITY quality)
		{
			m_renderTexture = new RenderTexture(m_ReflectionWidth[(int)quality], m_ReflectionHeight[(int)quality], 24);
			m_renderTexture.isPowerOfTwo = true;
			m_renderTexture.hideFlags = HideFlags.DontSave;
			m_renderTexture.name = "PlanarReflectionTexture";
			m_renderTexture.Create();

			m_ReflectionCamera.targetTexture = m_renderTexture;

			m_blurTexture1 = new RenderTexture(m_blurWidth[(int)quality], m_blurHeight[(int)quality], 0);
			DontDestroyOnLoad(m_blurTexture1);

			m_blurTexture2 = new RenderTexture(m_blurWidth2[(int)quality], m_blurHeight2[(int)quality], 0);
			DontDestroyOnLoad(m_blurTexture2);

			m_blurTexture1.Create();
			m_blurTexture2.Create();
		}

		private void DeInit()
		{
			m_ReflectionCamera.targetTexture = null;
			EB.Coroutines.EndOfFrame(EB.SafeAction.Wrap(this, delegate() {
				m_renderTexture.Release();
				m_blurTexture1.Release();
				m_blurTexture2.Release();
			}));
		}
		
		public void Render(Camera sceneCamera)
		{
			if ((Quality == PlanarReflectionManager.eREFLECTION_QUALITY.Off) || m_paused || (m_layerMask == 0))
			{
				return;
			}

	        int oldPixelLightCount = QualitySettings.pixelLightCount;
	        QualitySettings.pixelLightCount = 0;

	#if UNITY_EDITOR
			if ( UseSceneView )
			{
				sceneCamera = Camera.current;
			}
	#endif
			
			if( !sceneCamera )
				return;
	 		
	        // find out the reflection plane: position and normal in world space
	        Vector3 pos = Vector3.zero;
			pos.y = 0;//PerformanceManager.Instance.CurrentRoadHeight;
	        Vector3 normal = Vector3.up;
	        UpdateCameraModes( sceneCamera, m_ReflectionCamera );

	        // Reflect camera around reflection plane
	        float d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
	        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
	 
	        Matrix4x4 reflection = CalculateReflectionMatrix(reflectionPlane);
	        Vector3 oldpos = sceneCamera.transform.position;
	        Vector3 newpos = reflection.MultiplyPoint( oldpos );
	        m_ReflectionCamera.worldToCameraMatrix = sceneCamera.worldToCameraMatrix * reflection;
	        // Setup oblique projection matrix so that near plane is our reflection
	        // plane. This way we clip everything below/above it for free.
	        Vector4 clipPlane = CameraSpacePlane(m_ReflectionCamera, pos, normal, 1.0f);
	        Matrix4x4 projection = sceneCamera.projectionMatrix;
	        CalculateObliqueMatrix(ref projection, clipPlane);
	        m_ReflectionCamera.projectionMatrix = projection;
			m_ReflectionCamera.farClipPlane = m_ReflectionCamera.farClipPlane;

			// Render reflection
	        GL.SetRevertBackfacing(true);
	        m_ReflectionCamera.transform.position = newpos;
	        Vector3 euler = sceneCamera.transform.eulerAngles;
	        m_ReflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
			m_ReflectionCamera.cullingMask = m_layerMask.value;

			if (m_UseReplacementShaders)
			{
				m_ReflectionCamera.RenderWithShader(m_ReplacementShader, m_ReplacementShaderTag);
			}
			else
			{
				m_ReflectionCamera.Render();
			}
	        m_ReflectionCamera.transform.position = oldpos;
	        GL.SetRevertBackfacing(false);
	        QualitySettings.pixelLightCount = oldPixelLightCount;

			m_blurTexture2.DiscardContents();
			m_blurMaterial.SetVector("_HalfTexelOffset", new Vector2(0.5f / ((float)m_renderTexture.width), 0.0f));
			Graphics.Blit(m_renderTexture, m_blurTexture1, m_blurMaterial, 0);
			m_renderTexture.DiscardContents();
			m_blurMaterial.SetVector("_HalfTexelOffset", new Vector2(0.0f, 0.5f / ((float)m_blurTexture2.height)));
			Graphics.Blit(m_blurTexture1, m_blurTexture2, m_blurMaterial, 1);
			m_blurTexture1.DiscardContents();

			//Set reflection texture
			Shader.SetGlobalTexture(m_textureName, m_blurTexture2);
		}
		
		public void Pause()
		{
			m_paused = true;
		}
		
		public void Resume()
		{
			m_paused = false;
		}

		public void SetPlanarReflectionRamp(float ramp)
		{
			if(m_blurMaterial != null)
			{
				m_blurMaterial.SetFloat("_Ramp", ramp);
			}
		}

		public void SetBackgroundColor(Color color)
		{
			m_backgroundColor = color;
		}

		public void SetClearMode(eCLEAR_MODE clearMode)
		{
			m_clearMode = clearMode;
		}
		
		private void UpdateCameraModes( Camera src, Camera dest )
	    {
	        if( dest == null )
	            return;
	        // set camera to clear the same way as current camera
	        dest.clearFlags = src.clearFlags;
			if (m_clearMode == eCLEAR_MODE.Color)
			{
				dest.clearFlags = CameraClearFlags.SolidColor;
	        	dest.backgroundColor = m_backgroundColor;        
			}
			else
			{
				dest.GetComponent<Skybox>().material = src.GetComponent<Skybox>().material;
				dest.clearFlags = CameraClearFlags.Skybox; 
			}
	        dest.farClipPlane = src.farClipPlane;
	        dest.nearClipPlane = src.nearClipPlane;
	        dest.orthographic = src.orthographic;
	        dest.fieldOfView = src.fieldOfView;
	        dest.aspect = src.aspect;
	        dest.orthographicSize = src.orthographicSize;
	    }
	 
	    // Extended sign: returns -1, 0 or 1 based on sign of a
	    private static float sgn(float a)
	    {
	        if (a > 0.0f) return 1.0f;
	        if (a < 0.0f) return -1.0f;
	        return 0.0f;
	    }
	 
	    // Given position/normal of the plane, calculates plane in camera space.
	    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
	    {
	        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
	        Matrix4x4 m = cam.worldToCameraMatrix;
	        Vector3 cpos = m.MultiplyPoint( offsetPos );
	        Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
	        return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
	    }
	 
	    // Adjusts the given projection matrix so that near plane is the given clipPlane
	    // clipPlane is given in camera space. See article in Game Programming Gems 5 and
	    // http://aras-p.info/texts/obliqueortho.html
	    private static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
	    {
	        Vector4 q = projection.inverse * new Vector4(
	            sgn(clipPlane.x),
	            sgn(clipPlane.y),
	            1.0f,
	            1.0f
	        );
	        Vector4 c = clipPlane * (2.0F / (Vector4.Dot (clipPlane, q)));
	        // third row = clip plane - fourth row
	        projection[2] = c.x - projection[3];
	        projection[6] = c.y - projection[7];
	        projection[10] = c.z - projection[11];
	        projection[14] = c.w - projection[15];
	    }
	 
	    // Calculates reflection matrix around the given plane
	    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
	    {
			Matrix4x4 reflectionMat;
			
	        reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
	        reflectionMat.m01 = (   - 2F*plane[0]*plane[1]);
	        reflectionMat.m02 = (   - 2F*plane[0]*plane[2]);
	        reflectionMat.m03 = (   - 2F*plane[3]*plane[0]);
	 
	        reflectionMat.m10 = (   - 2F*plane[1]*plane[0]);
	        reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
	        reflectionMat.m12 = (   - 2F*plane[1]*plane[2]);
	        reflectionMat.m13 = (   - 2F*plane[3]*plane[1]);
	 
	        reflectionMat.m20 = (   - 2F*plane[2]*plane[0]);
	        reflectionMat.m21 = (   - 2F*plane[2]*plane[1]);
	        reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
	        reflectionMat.m23 = (   - 2F*plane[3]*plane[2]);
	 
	        reflectionMat.m30 = 0F;
	        reflectionMat.m31 = 0F;
	        reflectionMat.m32 = 0F;
	        reflectionMat.m33 = 1F;
			
			return reflectionMat;
	    }
	}
}