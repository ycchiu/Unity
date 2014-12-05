using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class EBWorldPainter : EditorWindow 
{
	[MenuItem ("EBG/Performance/World Painter")]
	static void Init() 
	{
		EditorWindow.GetWindow(typeof(EBWorldPainter));
	}
	
	private bool inited = false;
	private GameObject toPaint = null;
	private Camera cam = null;
	private RenderTexture rt;
	private Texture[] textures;
	private GUISkin guiskin;
	private EBWorldPainterData worldPainterData;
	
	//the global "mode" of the editor
	private enum EditMode
	{
		Tesselate,
		Visibility,
		BoundingBoxes,
		Hover,
		DataLayer
	}
	
	private EditMode editMode = EditMode.Tesselate;
	
	//the local modes within each global mode
	private enum Mode
	{
		None,

		//tesselation
		AddPoints,
		RemovePoints,
		MovePoints,
		SplitRegions,
		CombineRegions,
		
		//visibility
		PickVisibleBase,
		PaintVisible
	}

	private Mode mode = Mode.AddPoints;

	private EBWorldPainterData.eDATA_LAYER dataLayer;
	
	private enum TextureFills
	{
		White = 0,
		Gray,
		Blue,
		Green,
		Red,
		Count
	};
	
	const int textureSize = 768;
	
	void OnFocus()
	{
	    SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	   	SceneView.onSceneGUIDelegate += this.OnSceneGUI;
		
		InitWorldPainter();
		SetUpBounds();
	}
	
	void OnDestroy()
	{
		SaveChanges();
	}
	
	private static string DataPathForCurrentScene()
	{
		char slash = '/';
		var scenePath = EditorApplication.currentScene;
		var scenePathComponents = scenePath.Split(slash);
		string sceneName = scenePathComponents[scenePathComponents.Length - 1].Split('.')[0];
		if (sceneName == string.Empty)
			return null;
		
		if(sceneName.Contains("_variant_"))
		{
			sceneName = sceneName.Substring(0,(sceneName.IndexOf("_variant_")));
		}
		
		return "Assets" + slash + "TrackData" + slash + "WorldPainter" + slash + sceneName + ".prefab";
	}
	
	public static EBWorldPainterData DataForCurrentScene()
	{
		EBWorldPainterData data = null;
		var path = DataPathForCurrentScene();

		//try to load it
		if (path != null)
		{
			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
			if (prefab)
			{
				//found it, load the data
				GameObject worldPainterGO = (GameObject)Instantiate(prefab);
				worldPainterGO.name = "WorldPainterData";
				data = worldPainterGO.GetComponent<EBWorldPainterData>();
				data.Unpack();
			}
		}
		
		if (data == null)
		{
			//didn't end up with data, create a new one
			GameObject worldPainterGO = new GameObject("WorldPainterData", typeof(EBWorldPainterData) );
			data = worldPainterGO.GetComponent<EBWorldPainterData>();
			var renderers = (MeshRenderer[])FindObjectsOfType(typeof(MeshRenderer));
			data.SetWorldBounds(renderers);
			data.SetRegionBounds(renderers);
		}
		return data;
	}
	
	public static void SaveDataForCurrentScene(EBWorldPainterData data)
	{
		//update region bounds
		var renderers = (MeshRenderer[])FindObjectsOfType(typeof(MeshRenderer));
		data.SetRegionBounds(renderers);

		//save
		var path = DataPathForCurrentScene();
		if (path == null)
			return;
		GameObject worldPainterGO = new GameObject("WorldPainterData", typeof(EBWorldPainterData) );
		EBWorldPainterData worldPainterData = worldPainterGO.GetComponent<EBWorldPainterData>();
		worldPainterData.Clone(data);
		PrefabUtility.CreatePrefab(path, worldPainterGO, ReplacePrefabOptions.ReplaceNameBased);
		DestroyImmediate(worldPainterGO);
		EditorApplication.SaveAssets();
	}
	
	void InitWorldPainter()
	{
		if (inited)
			return;
		
		worldPainterData = DataForCurrentScene();

		if (worldPainterData == null)
		{
			Debug.LogError("World Painter needs to be opened within a scene that has some MeshRenders in it");
			return;
		}
		
		wantsMouseMove = true;
		
		var wpc = new GameObject("WorldPainterCamera", typeof(Camera), typeof(Skybox) );
    	wpc.hideFlags = HideFlags.HideAndDontSave;
		cam = wpc.camera;
		cam.transform.position = new Vector3(0, 100, 0);
		cam.transform.LookAt(Vector3.zero);
		cam.orthographic = true;
		cam.backgroundColor = Color.black;
		rt = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
		cam.targetTexture = rt;
		
		guiskin = (GUISkin)Resources.Load("EBWorldPainterAssets/wp_gui_skin");
		
		textures = new Texture[(int)TextureFills.Count];
		
		textures[(int)TextureFills.White] = GenerateColorTexture(Color.white);
		textures[(int)TextureFills.Gray] = GenerateColorTexture(Color.gray);
		textures[(int)TextureFills.Blue] = GenerateColorTexture(Color.blue);
		textures[(int)TextureFills.Green] = GenerateColorTexture(Color.green);
		textures[(int)TextureFills.Red] = GenerateColorTexture(Color.red);
		
		inited = true;
	}
	
	void SaveChanges()
	{
		if (cam != null)
		{
			DestroyImmediate(cam.gameObject);
		}
		
		SaveDataForCurrentScene(worldPainterData);
	}
	
	void SetUpBounds()
	{
		if (!inited)
			return;
		
		var meshRenderers = (MeshRenderer[])FindObjectsOfType(typeof(MeshRenderer));
		worldPainterData.SetWorldBounds(meshRenderers);
		
		cam.transform.position = new Vector3(worldPainterData.worldBounds.center.x, 100, worldPainterData.worldBounds.center.z);
	}
	
	float orthographicSize = 1500.0f;
	
	void Render(float posX, float posZ, float size)
	{
		if (cam.transform.position.z != posX || cam.transform.position.x != posZ || cam.orthographicSize != Mathf.Round(size))
		{
			cam.transform.position = new Vector3(posX, cam.transform.position.y, posZ);
			cam.orthographicSize = Mathf.Round(size);
			int lod = Shader.globalMaximumLOD;
			Shader.globalMaximumLOD = 100;
			cam.Render();
			Shader.globalMaximumLOD = lod;
		}
	}
		
	EBWorldPainterData.Point[] closestPoints = new EBWorldPainterData.Point[]{};
	EBWorldPainterData.Point closestPoint = null;
	EBWorldPainterData.Point lastClickedPoint = null;
	EBWorldPainterData.Region lastClickedRegion = null;
	EBWorldPainterData.Region[] currentRegions = new EBWorldPainterData.Region[]{};
	EBWorldPainterData.Point pointToAdd = new EBWorldPainterData.Point(0.0f, 0.0f);
	
	void UpdateClosestPoint(EBWorldPainterData.Point point)
	{
		if (closestPoint != point)
		{
			closestPoint = point;
			Repaint();
		}
	}
	
	void UpdateClosestPoints(EBWorldPainterData.Point[] points)
	{
		if (closestPoints.Length != points.Length)
		{
			closestPoints = points;
			Repaint();
			return;
		}
		
		for(var i = 0; i < points.Length; ++i)
		{
			if (closestPoints[i] != points[i])
			{
				closestPoints = points;
				Repaint();
				return;
			}
		}
	}
	
	void UpdateCurrentRegions(EBWorldPainterData.Region[] regions)
	{
		if (currentRegions.Length != regions.Length)
		{
			currentRegions = regions;
			Repaint();
			return;
		}
		
		for(var i = 0; i < regions.Length; ++i)
		{
			if (currentRegions[i] != regions[i])
			{
				currentRegions = regions;
				Repaint();
				return;
			}
		}
	}
	
	void SwitchEditMode(EditMode em)
	{
		editMode = em;
		lastClickedPoint = null;
		lastClickedRegion = null;
		if (em == EditMode.BoundingBoxes)
		{
			var renderers = (MeshRenderer[])FindObjectsOfType(typeof(MeshRenderer));
			worldPainterData.SetRegionBounds(renderers);
		}
		Repaint();
	}
 
    void OnGUI()
    {
		if (!inited)
		{
			return;
		}
		
		const int offset = 20;
		Rect textureRect = Rect.MinMaxRect(offset, offset, textureSize + offset, textureSize + offset);
		Rect texturSize = Rect.MinMaxRect(0, 0, textureSize, textureSize);
		
		SetMode();
		
		//Deal with the mouse
		if ((Event.current != null) && Event.current.isMouse && textureRect.Contains(Event.current.mousePosition))
		{
			Vector3 worldMousePos3 = cam.ViewportToWorldPoint(new Vector3(((float)Event.current.mousePosition.x - textureRect.x)/(float)textureSize, 1.0f - ((float)Event.current.mousePosition.y - textureRect.y)/(float)textureSize, 0.0f));
			EBWorldPainterData.Point worldMousePos = new EBWorldPainterData.Point(worldMousePos3.x, worldMousePos3.z);
			
			UpdateClosestPoint(worldPainterData.ClosestPoint(worldMousePos));
			UpdateClosestPoints(worldPainterData.ClosestLine(worldMousePos));
			UpdateCurrentRegions(worldPainterData.RegionsPointIsInside(worldMousePos));
			
			switch(mode)
			{
			case(Mode.None):
				break;
			case(Mode.AddPoints):
			{
				Vector2 point0 = closestPoints[0].location;
				Vector2 point1 = closestPoints[1].location;
				float distToPoint0 = Vector2.Distance(point0, worldMousePos.location);
				float distToPoint1 = Vector2.Distance(point1, worldMousePos.location);
				float t = distToPoint0 / (distToPoint0 + distToPoint1);
				pointToAdd = new EBWorldPainterData.Point(Vector2.Lerp(point0, point1, t));
				
				if (Event.current.type == EventType.MouseDown)
				{
					var result = worldPainterData.InsertPoint(pointToAdd, closestPoints[0], closestPoints[1]);
					Repaint();
				}
				break;
			}
			case(Mode.RemovePoints):
			{
				if (Event.current.type == EventType.MouseDown)
				{
					var result = worldPainterData.RemovePoint(closestPoint);
					Repaint();
				}
				break;
			}
			case(Mode.MovePoints):
			{
				if (Event.current.type == EventType.MouseDown)
				{
					lastClickedPoint = closestPoint;
					Repaint();
				}
				else if (Event.current.type == EventType.MouseDrag && lastClickedPoint != null)
				{
					lastClickedPoint.location = worldMousePos.location;
					Repaint();
				}
				break;
			}
			case(Mode.SplitRegions):
			{
				if (Event.current.type == EventType.MouseDown)
				{
					if (lastClickedPoint == null)
					{
						lastClickedPoint = closestPoint;
						Repaint();
					}
					else
					{
						EBWorldPainterData.Point nextClickedPoint = worldPainterData.ClosestPoint(worldMousePos);
						if (nextClickedPoint != lastClickedPoint)
						{
							EBWorldPainterData.Region[] regions = worldPainterData.RegionsContainingPoints(lastClickedPoint, nextClickedPoint);
							if (regions.Length == 1)
							{
								//don't want to split if the points are in more than one region, as they must be on a boundary of some sort, so splitting wouldn't make sense
								//TODO: don't split things that are sequential
								worldPainterData.SplitRegionAcrossPoints(regions[0], lastClickedPoint, nextClickedPoint);
							}
						}
						
						lastClickedPoint = null;
						Repaint();
					}
				}
				break;
			}
			case(Mode.CombineRegions):
			{
				if (Event.current.type == EventType.MouseDown)
				{
					worldPainterData.RemoveLine(closestPoints[0], closestPoints[1]);
					UpdateCurrentRegions(worldPainterData.RegionsPointIsInside(worldMousePos));
					Repaint();
				}
				break;
			}
			case(Mode.PickVisibleBase):
			{
				if (Event.current.type == EventType.MouseDown)
				{
					lastClickedRegion = (currentRegions.Length > 0) ? currentRegions[0] : null;
					Repaint();
				}
				break;
			}
			case(Mode.PaintVisible):
			{
				if ((lastClickedRegion != null) && (currentRegions.Length > 0) && (Event.current.type == EventType.MouseDown))
				{
					lastClickedRegion.ToggleSeesRegion(currentRegions[0]);
					Repaint();
				}
				break;
			}
			}
		}
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Zoom", GUILayout.Width(100));
		orthographicSize = GUILayout.HorizontalSlider(orthographicSize, 100.0f, 4000.0f);
		GUILayout.EndHorizontal();
		
		GUI.skin = guiskin;
		
		GUILayout.BeginHorizontal();
		float camPosZ = GUILayout.VerticalSlider(cam.transform.position.z, worldPainterData.WorldBounds().max.z, worldPainterData.WorldBounds().min.z);
		
		GUI.BeginGroup(textureRect);
		GUI.Box(texturSize, rt);
		GUI.EndGroup();
		
		GUI.BeginGroup(textureRect);

		switch(editMode)
		{
		case EditMode.Tesselate:
			//Draw all the points
			for(int i = 0; i < worldPainterData.points.Count; ++i)
			{
				EBWorldPainterData.Point point = worldPainterData.points[i]; 
				
				TextureFills tex = TextureFills.Gray;
				switch(mode)
				{
				case(Mode.AddPoints):
					if (point == closestPoint)
						tex = TextureFills.Blue;
					break;
				case(Mode.RemovePoints):
					if (point == closestPoint)
						tex = TextureFills.Red;
					break;
				case(Mode.MovePoints):
					if (point == closestPoint)
						tex = TextureFills.Green;
					break;
				case(Mode.SplitRegions):
					if (point == closestPoint)
						tex = TextureFills.Green;
					if (point == lastClickedPoint)
						tex = TextureFills.Blue;
					break;
				}
				DrawPoint(point, tex);
			}
			
			//Draw the line we may add if we are splitting regions
			if (mode == Mode.AddPoints)
			{
				DrawPoint(pointToAdd, TextureFills.Blue);
			}
			
			//Draw the line we may add if we are splitting regions
			if (mode == Mode.SplitRegions && lastClickedPoint != null)
			{
				DrawLine(lastClickedPoint, closestPoint, Color.blue);
			}
			
			//Draw all the region outlines
			foreach(EBWorldPainterData.Region region in worldPainterData.regions)
			{
				DrawRegionOutline(region, Color.white);
			}
			
			//Draw the line we may remove if we are combining regions
			if (mode == Mode.CombineRegions)
			{
				DrawLine(closestPoints[0], closestPoints[1], Color.red);
			}

			break;

		case EditMode.Visibility:

			//Draw all the points
			for(int i = 0; i < worldPainterData.points.Count; ++i)
			{
				DrawPoint(worldPainterData.points[i], TextureFills.Gray);
			}
			
			//Draw all the region outlines
			foreach(EBWorldPainterData.Region region in worldPainterData.regions)
			{
				DrawRegionOutline(region, Color.red);
			}

			//outline all the regions our current region sees, and our current region
			if (lastClickedRegion != null)
			{
				foreach(EBWorldPainterData.Region region in worldPainterData.regions)
				{
					if (lastClickedRegion.SeesRegion(region))
					{
						DrawRegionOutline(region, Color.green);
					}
				}
				DrawRegionOutline(lastClickedRegion, new Color(0.5f, 0.5f, 1.0f));
			}
			
			break;

		case EditMode.Hover:
			
			//Draw all the region outlines
			foreach(EBWorldPainterData.Region region in worldPainterData.regions)
			{
				DrawRegionOutline(region, Color.white);
			}

			//outline the one we are hovering over
			if (currentRegions.Length > 0)
			{
				DrawRegionOutline(currentRegions[0], new Color(0.0f, 0.5f, 0.0f));
			}

			break;
			
		case EditMode.BoundingBoxes:

			//Draw all the region bounding boxes
			foreach(EBWorldPainterData.Region region in worldPainterData.regions)
			{
				DrawRegionBoundingBox(region, Color.white);
			}

			break;

		case EditMode.DataLayer:

			//Draw all the region outlines
			foreach(EBWorldPainterData.Region region in worldPainterData.regions)
			{
				DrawRegionOutline(region, Color.white);
				DrawDataLayer(region);
			}

			break;

		}

		GUI.EndGroup();
		
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		float camPosX = GUILayout.HorizontalSlider(cam.transform.position.x, worldPainterData.WorldBounds().min.x, worldPainterData.WorldBounds().max.x);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		foreach(EditMode m in Enum.GetValues(typeof(EditMode)))
		{
			if (GUILayout.Button(m.ToString())) { SwitchEditMode(m); }
		}
		dataLayer = (EBWorldPainterData.eDATA_LAYER)EditorGUILayout.EnumPopup("Data Layer: ", dataLayer);
		GUILayout.EndHorizontal();
		
		Render (camPosX, camPosZ, orthographicSize);
		
		GUI.skin = null;
    }
	
	void SetMode()
	{
		if (Event.current != null)
		{
			switch(editMode)
			{
			case EditMode.Tesselate:
			{
				if (Event.current.alt && Event.current.shift)
				{
					mode = Mode.RemovePoints;
				}
				else if (Event.current.shift)
				{
					mode = Mode.AddPoints;
				}
				else if (Event.current.command)
				{
					if (mode != Mode.SplitRegions)
						lastClickedPoint = null;
					mode = Mode.SplitRegions;
				}
				else if (Event.current.alt)
				{
					mode = Mode.CombineRegions;
				}
				else 
				{
					mode = Mode.MovePoints;
				}
				break;
			}
			case EditMode.Visibility:
			{
				if (Event.current.shift)
				{
					mode = Mode.PaintVisible;
				}
				else
				{
					mode = Mode.PickVisibleBase;
				}
				break;
			}
			case EditMode.DataLayer:
			default:
			{
				mode = Mode.None;
				break;
			}
			}
		}
	}
	
	void OnSceneGUI(SceneView sceneView) 
	{
	}
	
	void DrawLine(EBWorldPainterData.Point worldPoint1, EBWorldPainterData.Point worldPoint2, Color color)
	{
		Vector3 p1 = new Vector3(worldPoint1.location.x, 0.0f, worldPoint1.location.y);
		Vector3 p2 = new Vector3(worldPoint2.location.x, 0.0f, worldPoint2.location.y);
		Vector2 screenPoint0 = cam.WorldToViewportPoint(p1);
		Vector2 screenPoint1 = cam.WorldToViewportPoint(p2);
		screenPoint0.y = 1.0f - screenPoint0.y;
		screenPoint1.y = 1.0f - screenPoint1.y;
		DrawingUtils.Line(screenPoint0 * textureSize, screenPoint1 * textureSize, color);
	}
	
	void DrawRegionOutline(EBWorldPainterData.Region region, Color outlineColor)
	{
		for(var i = 0; i < region.points.Count; ++i)
		{
			EBWorldPainterData.Point p0 = region.points[i];
			EBWorldPainterData.Point p1 = region.points[(i+1) % region.points.Count];
			DrawLine(p0, p1, outlineColor);
		}
	}

	void DrawRegionBoundingBox(EBWorldPainterData.Region region, Color outlineColor)
	{
		for(var i = 0; i < region.points.Count; ++i)
		{
			EBWorldPainterData.Point p0 = new EBWorldPainterData.Point(region.bounds.min.x, region.bounds.min.z);
			EBWorldPainterData.Point p1 = new EBWorldPainterData.Point(region.bounds.max.x, region.bounds.min.z);
			EBWorldPainterData.Point p2 = new EBWorldPainterData.Point(region.bounds.max.x, region.bounds.max.z);
			EBWorldPainterData.Point p3 = new EBWorldPainterData.Point(region.bounds.min.x, region.bounds.max.z);
			DrawLine(p0, p1, outlineColor);
			DrawLine(p1, p2, outlineColor);
			DrawLine(p2, p3, outlineColor);
			DrawLine(p3, p0, outlineColor);
		}
	}
		
	void DrawPoint(EBWorldPainterData.Point worldPoint, TextureFills textureFill)
	{
		var point = new Vector3(worldPoint.location.x, 0.0f, worldPoint.location.y);
		Vector2 screenPoint = cam.WorldToViewportPoint(point) * textureSize;
		Rect pos = Rect.MinMaxRect(screenPoint.x - 5, (textureSize - screenPoint.y) - 5, screenPoint.x + 5, (textureSize - screenPoint.y) + 5); 
		GUI.DrawTexture(pos, textures[(int)textureFill], ScaleMode.StretchToFill);
	}

	void DrawDataLayer(EBWorldPainterData.Region region)
	{
		var point = new Vector3(region.Center().location.x, 0.0f, region.Center().location.y);
		Vector2 center = cam.WorldToViewportPoint(point) * textureSize;
		Rect rect = new Rect(center.x - 25.0f, textureSize - center.y - 5.0f, 50.0f, 10.0f);
		switch(dataLayer)
		{
		case EBWorldPainterData.eDATA_LAYER.LightmapSize:
			region.dataLayers[(int)dataLayer].value = (int)(EBWorldPainterData.eLIGHTMAP_SIZE)EditorGUI.EnumPopup(rect, (EBWorldPainterData.eLIGHTMAP_SIZE)(region.dataLayers[(int)dataLayer].value));
			break;
		case EBWorldPainterData.eDATA_LAYER.RenderSetting:
			region.dataLayers[(int)dataLayer].value = (int)(EBWorldPainterData.eLIGHTMAP_SIZE)EditorGUI.EnumPopup(rect, (EBWorldPainterData.eRENDER_SETTING)(region.dataLayers[(int)dataLayer].value));
			break;
		}
	}
	
	Texture2D GenerateColorTexture(Color color)
	{
		Texture2D tex = new Texture2D(1, 1);
		tex.SetPixel(0, 0, color);
		tex.Apply();
		return tex;
	}
}