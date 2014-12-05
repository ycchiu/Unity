using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Rendering
{
	[ExecuteInEditMode]
	public class EBLensFlare : MonoBehaviour {

		public enum eFLARE_QUALITY
		{
			Off = -1,
			Low = 0,
			High = 1,
		}

		public class Config
		{
			public delegate EB.Rendering.EBLensFlare.eFLARE_QUALITY GetQuality();
			public GetQuality GetQualityHandler = null;

			public delegate Camera GetCurrentCamera();
			public GetCurrentCamera GetCameraHandler = null;
		}

		private static Config _config = null;
		
		public static void SetConfig(Config config)
		{
			_config = config;
		}

		public bool _ShowGizmos = true;
		public Vector2 _TextureSize = Vector2.one;
		public Texture _TextureFlare;
		public Color _Color;
		public List<string> _TextureIndicies = new List<string>();
		public float _FadeDuration = 1f;
		public int _lowFlareCount = 4;
		public int _highFlareCount = 8;
		public bool _updateMesh = false;
		public Vector3 OffsetFromCamera = Vector3.zero;

		private GameObject _TargetPoint;
		private Camera _camera;
		private Vector3 _cameraPosition = Vector3.zero;
		private Vector3 _sunPosition; 
		private Vector3 _targetPoint;
		private GameObject _LensFlare;
		private Mesh _MeshFlare;
		private Material _MaterialFlare;
		private Renderer _RendererFlare;
		private MeshFilter _MeshFilterFlare;
		private float _currentFade = 1f; 
		private float _timer = 0;
		private bool _hideFlare = true;

		public eFLARE_QUALITY _FlareQuality = eFLARE_QUALITY.High;

		[System.Serializable]
		public class EB_Flare
		{
			public float distance;
			public float scale;
			public int imageIndex;
			public Mesh mesh;
			public Color color;
			public float rotationOffset;
			public bool enableRotation;

			public EB_Flare()
			{
				scale = 1.0f;
				color = Color.white;
				rotationOffset = 0.0f;
				enableRotation = true;
			}
		}

		[SerializeField]
		public List<EB_Flare> flares = new List<EB_Flare>();

		public eFLARE_QUALITY GetQuality()
		{
			if(Application.isPlaying)
			{
				if(_config == null)
				{
					return eFLARE_QUALITY.High;
				}
				else
				{
					return _config.GetQualityHandler();
				}
			}
			return _FlareQuality;
		}

		public Camera GetPerformanceCamera()
		{
			if(Application.isPlaying)
			{
				if(_config == null)
				{
					return null;
				}
				else
				{
					return _config.GetCameraHandler();
				}
			}
			return null;
		}

		void Start () 
		{
			_FlareQuality = GetQuality();

			if(_TargetPoint == null)
			{
				AddTargetPoint();
			}
			CleanUpEditorFlares();

			if(_LensFlare == null)
			{
				_targetPoint = _TargetPoint.transform.position;
				_sunPosition = this.transform.position;
				Init();
			}
		}

		void AddTargetPoint()
		{
			_camera = GetCamera();
			if(_camera == null)
			{
				Debug.LogWarning("A Camera cannot be found, this is needed for lens flares");
				return;
			}
			_TargetPoint = EB.Util.GetObjectExactMatch(_camera.gameObject, "TargetPoint");
			if (!_TargetPoint)
			{
				_TargetPoint = new GameObject("TargetPoint");
				_TargetPoint.transform.parent = _camera.transform;
			}
			_TargetPoint.transform.localPosition = OffsetFromCamera;
		}

		void Init()
		{
			DestroyFlare();
			if(_FlareQuality == eFLARE_QUALITY.Off)
			{
				return;
			}
			_currentFade = 1;

			_LensFlare = new GameObject("LensFlare", typeof(MeshFilter), typeof(MeshRenderer));
			_LensFlare.transform.parent = transform;
			_LensFlare.transform.localPosition = new Vector3(0, 0, 10);
			_MeshFilterFlare = _LensFlare.GetComponent<MeshFilter>();
			_MaterialFlare = new Material(Shader.Find("EBG/Misc/LensFlare"));

			if(_TextureFlare) 
			{
				_MaterialFlare.SetTexture("_MainTex", _TextureFlare);
			}
			_RendererFlare = _MeshFilterFlare.renderer;
			_RendererFlare.material = _MaterialFlare;
			
			_MeshFilterFlare.sharedMesh = MakeMesh();
		}

		Vector4 UVData(int index) //Custom 2 big 8 small
		{
			float scaleX = 0f;
			float scaleY = 0f;

			float offsetX = 0f; 
			float offsetY = 0f;	

			// 2 big 8 small case

			if(index == 0 || index == 1)
			{
				scaleX = 0.5f;
				scaleY = 0.5f;
				offsetY = 0.5f;
			}
			else
			{
				scaleX = 0.25f;
				scaleY = 0.25f;
			}

			if(index == 2 || index == 3 || index == 4 || index == 5) 
			{
				offsetY = 0.25f; 
			}
			else if(index == 6 || index == 7 || index == 8 || index == 9) 
			{
				offsetY = 0f; 
			}

			switch(index)
			{
				case 0:
				{
					offsetX = 0f;
					offsetY = 0.5f;
				}
				break;

				case 1:
				{
					offsetX = 0.5f;
					offsetY = 0.5f;
				}
				break;

				case 2:
				case 6:
				{
					offsetX = 0f;
				}
				break;

				case 3:
				case 7:
				{
					offsetX = 0.25f;
				}
				break;

				case 4:
				case 8:
				{
					offsetX = 0.5f;
				}
				break;

				case 5:
				case 9:
				{
					offsetX = 0.75f;
				}
				break;

				default:
				break;
			}

			return new Vector4(scaleX,scaleY,offsetX,offsetY);
		}

		Mesh MakeMesh()
		{
			_MaterialFlare.SetTexture("_MainTex", _TextureFlare);

			Mesh newMesh = new Mesh();
			int vertCount = 0;
			int indCount = 0;
			int vertOffset = 0;
			int indOffset = 0;

			for(int i = 0; i < flares.Count; i++)	// gets the counts of all verts and indicies
			{
				EB_Flare flare = flares[i];
				if(flare.mesh)
				{
					vertCount += flare.mesh.vertexCount;

					for(int j = 0; j < flare.mesh.subMeshCount; j++)
					{
						indCount += flare.mesh.GetIndices(j).Length;
					}
				}
				else
				{
					vertCount += 4;
					indCount += 6;
				}
			}

			Vector3[] verticies = new Vector3[vertCount];
			Vector2[] uvs = new Vector2[vertCount];
			int[] indicies = new int[indCount];
			Color[] colors = new Color[vertCount];

			int flareCount = flares.Count;
			if(_FlareQuality == eFLARE_QUALITY.Low)
			{
				flareCount = Mathf.Min(flareCount,_lowFlareCount);
			}

			for(int i = 0; i < flareCount; i++)
			{
				EB_Flare flare = flares[i];
				float scale = flares[i].scale;

				Vector4 uvData = UVData(flare.imageIndex);

				if(flare.mesh != null)	// HAS A MESH
				{
					int vertexCount = flare.mesh.vertexCount;
					float centerX = 0;
					float centerY = 0;

					for(int j = 0; j < vertexCount; j++)
					{
						Vector3 v = flare.mesh.vertices[j];
						centerX += v.x;
						centerY += v.y;

						Vector2 uv = flare.mesh.uv[j];
						Vector2 s = new Vector2(uvData.x,uvData.y);
						Vector2 o = new Vector2(uvData.z,uvData.w);
						uvs[vertOffset + j] = Vector2.Scale(uv, s) + o; 

						colors[vertOffset + j] = flare.color;
					}

					centerX = centerX/vertexCount;
					centerY = centerY/vertexCount;

					for(int j = 0; j < vertexCount; j++)
					{
						Vector3 v = flare.mesh.vertices[j];
						float x = v.x-centerX;
						float y = v.y-centerY;
						verticies[vertOffset + j] = new Vector3(x * scale, y * scale ,i);
					} 

					for(int j = 0; j < flare.mesh.subMeshCount; j++)
					{
						int[] flareIndicies = flare.mesh.GetIndices(j);
						for(int k = 0; k < flareIndicies.Length; k++)
						{
							indicies[indOffset + k] = vertOffset + flareIndicies[k];
						}
						indOffset += flareIndicies.Length;
					}
					vertOffset += vertexCount;
				}
				else // NO MESH MAKE A QUAD
				{
					colors[vertOffset + 0] = flare.color;
					colors[vertOffset + 1] = flare.color;
					colors[vertOffset + 2] = flare.color;
					colors[vertOffset + 3] = flare.color;

					verticies[vertOffset + 0] = new Vector3(-scale, -scale ,i);
					verticies[vertOffset + 1] = new Vector3(-scale, scale ,i);
					verticies[vertOffset + 2] = new Vector3(scale, -scale ,i);
					verticies[vertOffset + 3] = new Vector3(scale, scale ,i);
				
					uvs[vertOffset + 0] = new Vector2(0 * uvData.x + uvData.z, 0 * uvData.y + uvData.w);
					uvs[vertOffset + 1] = new Vector2(0 * uvData.x + uvData.z, 1 * uvData.y + uvData.w);
					uvs[vertOffset + 2] = new Vector2(1 * uvData.x + uvData.z, 0 * uvData.y + uvData.w);
					uvs[vertOffset + 3] = new Vector2(1 * uvData.x + uvData.z, 1 * uvData.y + uvData.w);

					indicies[indOffset + 0] = vertOffset + 0;
					indicies[indOffset + 1] = vertOffset + 1;
					indicies[indOffset + 2] = vertOffset + 2;
					indicies[indOffset + 3] = vertOffset + 2;
					indicies[indOffset + 4] = vertOffset + 1;
					indicies[indOffset + 5] = vertOffset + 3;
					
					vertOffset += 4;
					indOffset += 6;
				}
			}

			newMesh.vertices = verticies;
			newMesh.uv = uvs;
			newMesh.colors = colors;
			newMesh.triangles = indicies;
			return newMesh;
		}

		public void UpdateMesh()
		{
			if(_TextureFlare && _MaterialFlare && _FlareQuality != eFLARE_QUALITY.Off && _MeshFilterFlare != null)
			{
				_MeshFilterFlare.sharedMesh = MakeMesh();
			}
		}

		void Update () 
		{
			if(Application.isPlaying && _FlareQuality != GetQuality()) 
			{
				_FlareQuality = GetQuality();
				Init();
			}

			if(_FlareQuality == eFLARE_QUALITY.Off)
			{
	#if UNITY_EDITOR
				if(!Application.isPlaying)
				{
					DestroyFlare();
				}
	#endif
				return;
			}

			if(_TargetPoint == null)
			{
				AddTargetPoint();
			}
	#if UNITY_EDITOR
			if(!Application.isPlaying && _LensFlare == null && _TargetPoint != null) 
			{
				Init();
			}
			if(_TargetPoint == null)
			{
				return;
			}
			//store off the position of the target point, for when we go to recreate it next time
			OffsetFromCamera = _TargetPoint.transform.localPosition;
	#endif
			_camera = GetCamera();
			if(_camera == null)
			{
				Debug.LogWarning("A Camera cannot be found, this is needed for lens flares");
				return;
			}

			_cameraPosition = _camera.transform.position;
			_sunPosition = this.transform.position;
			_targetPoint = _TargetPoint.transform.position;

			Ray ray = new Ray(_sunPosition, _camera.transform.position - _sunPosition);
			RaycastHit hit;
			if(_camera.collider == null)
			{
				_camera.gameObject.AddComponent<BoxCollider>();
			}
			if (Physics.Raycast(ray, out hit))
			{
				Vector3 screenSunPos = _camera.WorldToScreenPoint(_sunPosition);
				if(!Application.isPlaying)
				{
					_timer = 1;
				}
				if ((!OnScreen(screenSunPos) || hit.collider == null || hit.collider != _camera.collider) && !hit.collider.isTrigger) // HIDE FLARE
				{
					if(_hideFlare)
					{
						_timer = 1 - _currentFade;
					}
					_timer += Time.deltaTime/_FadeDuration;
					_currentFade = Mathf.Lerp(1f, 0f, _timer);		
					_hideFlare = false;
				}
				else 																					// SHOW FLARE
				{	
					if(!_hideFlare)
					{
						_timer = _currentFade;
					}
					_timer += Time.deltaTime/_FadeDuration;
					_currentFade = Mathf.Lerp(0f, 1f, _timer);
					_hideFlare = true;
				}
			}

			_currentFade = Mathf.Clamp(_currentFade, 0f, 1f);
			_MaterialFlare.SetFloat("_Alpha", _currentFade);

			Vector3 direction = _targetPoint - _sunPosition;
			Matrix4x4 positions1 = new Matrix4x4();
			Matrix4x4 positions2 = new Matrix4x4();
			Vector3 targetScreenPos = _camera.WorldToScreenPoint(_targetPoint);

			for(int i = 0; i < flares.Count; i++)
			{
				Vector3 flarePosition = _sunPosition + (direction * flares[i].distance);
				Vector3 flareScreenPosition = _camera.WorldToScreenPoint(flarePosition);
				float angle = Vector2.Angle(Vector2.up, targetScreenPos - flareScreenPosition);
				if(targetScreenPos.x > flareScreenPosition.x)
				{
					angle = 360f - angle;
				}
				float radians = 0;
				if(flares[i].enableRotation)
				{
					radians = Mathf.Deg2Rad * (angle + flares[i].rotationOffset);
				}
				else
				{
					radians = Mathf.Deg2Rad * flares[i].rotationOffset;
				}
				if(i <= 3)
				{
					positions1.SetRow(i, new Vector4(flarePosition.x,flarePosition.y,flarePosition.z,radians));
				}
				else
				{
					int ii = i - 4;
					positions2.SetRow(ii, new Vector4(flarePosition.x,flarePosition.y,flarePosition.z,radians));
				}
			}
			_MaterialFlare.SetMatrix("_Positions1", positions1);
			_MaterialFlare.SetMatrix("_Positions2", positions2);
		}

		bool OnScreen(Vector3 screenSunPos)
		{
			if(screenSunPos.x < 0 || screenSunPos.x > Screen.width || screenSunPos.y < 0 || screenSunPos.y > Screen.height) 
			{
				return false;
			}
			return true;
		}

		Camera GetCamera()
		{
			Camera cam;
			if(!Application.isPlaying)
			{
				cam = Camera.main;
			}
			else 
			{
				if(GetPerformanceCamera() == null)
				{
					cam = Camera.main;
				}
				else 
				{	
					cam = GetPerformanceCamera();
				}
			}

			if(cam == null)
			{
				return null;
			}
			return cam;
		}

		public void DestroyFlare()
		{
			if(_LensFlare != null)
			{
				if(Application.isPlaying)
				{
					Destroy(_LensFlare);
				}
				else
				{
					DestroyImmediate(_LensFlare);
				}
				_LensFlare = null;
			}
		}

		public void OnDestroy()
		{
			DestroyFlare();
		}


		public void CleanUpEditorFlares()
		{
			GameObject go = EB.Util.GetObjectExactMatch(gameObject, "LensFlare");
			if(Application.isPlaying)
			{
				Destroy(go);
			}
			else
			{
				DestroyImmediate(go);
			}
		}
	#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			if(_ShowGizmos)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(_sunPosition,0.3f);
				Gizmos.DrawLine(_sunPosition,_targetPoint);
				Gizmos.color = Color.red;
				Gizmos.DrawLine(_sunPosition,_cameraPosition);
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(_targetPoint,0.3f);
				
				Vector3 direction = _targetPoint - _sunPosition;

				foreach(EB_Flare flare in flares)
				{
					Vector3 flarePosition = _sunPosition + (direction * flare.distance);
					Gizmos.DrawWireCube(flarePosition, new Vector3(flare.scale * 2.0f, flare.scale * 2.0f, 0.01f));
				}
			}
		}
	#endif

	}
}