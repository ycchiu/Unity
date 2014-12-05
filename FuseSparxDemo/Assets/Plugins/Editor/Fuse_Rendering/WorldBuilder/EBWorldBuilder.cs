using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public abstract class EBWorldBuilder<RenderSettingsType>  : MonoBehaviour where RenderSettingsType : RenderSettingsBase
{
	// CONFIG
	protected string _worldName;
	
	public EBWorldBuilder(string worldName)
	{
		_worldName = worldName;
	}
	
	public class BuildStages
	{
		public bool LoadLightProbes;
		public bool BakeLightProbes;
		public bool BakeLighting;
		public bool LoadExternalRenderSettings;
		public bool EnableVertexLightmaps;
		public bool Merge;
		public bool UseGlobalBakeLightmaps;
		public bool LoadRenderSettings;
		
		public BuildStages()
		{
		}
	
		public BuildStages(BuildStages stages)
		{
			LoadLightProbes = stages.LoadLightProbes;
			BakeLightProbes = stages.BakeLightProbes;
			BakeLighting = stages.BakeLighting;
			LoadExternalRenderSettings = stages.LoadExternalRenderSettings;
			EnableVertexLightmaps = stages.EnableVertexLightmaps;
			Merge = stages.Merge;
			UseGlobalBakeLightmaps = stages.UseGlobalBakeLightmaps;
			LoadRenderSettings = stages.LoadRenderSettings;
		}
	}
	
	//core
	public void BuildByName(BuildStages stages, string destinationRoot)
	{
		try
		{
			BuildWorld(stages, destinationRoot);
		}
		catch(System.Exception e)
		{
			EditorUtility.ClearProgressBar();
			Debug.LogError(e);
		}
		EditorUtility.ClearProgressBar();
	}
	
	//HELPER DATA STRUCTURES

	//when you call AssembleWorld() to implementing class, it will return this
	//this so to that implementing class doesn't know what's going on below it
	public class WorldInfo
	{
		public string[] pieceCodes;
	}
	
	//we may be able to simplify this, but it will be for internal use only
	public class PieceInfo
	{
		public int lightmapOffset;
	}
	
	public class WorldInfoInternal
	{
		public Dictionary<string, PieceInfo> pieces;
	}
	
	class LightMapInfo
	{
		public int index;			//The index of the lightmap within our partition lightmap rects
		public Vector2 localScale;  //These two map the small piece of the local lightmap to the 0-1 space,
		public Vector2 localOffset;	//since we are cropping out their local lightmaps from a larger lightmap before remerging
	}
	
	class Instance
	{
		public Renderer renderer;
		public LightMapInfo[] lightmapInfo;
	}
	
	class Partition
	{
		public string name;
		public List<Instance> instances = new List<Instance>();
		public Texture2D lightmap;
		public Rect[] lightmapRects;
		public Rect[] uvRects;
	}
	
	class Grouping
	{
		public Material material 			= null;
		public int layer					= 0;
		public List<CombineInstance> instances = new List<CombineInstance>();
	}

	//CLIENT FUNCTIONS
	public abstract void		 			AssembleWorld(GameObject root);
	public abstract WorldInfo				GetWorldInfo();
	public abstract EBMeshUtils.Channels 	GetChannelMap(Shader shader);
	public abstract int 					RemapLayer(int layer);
	public abstract GameObject 				GetPrefabForCode(string pieceCode);
	public abstract MeshFilter[] 			GetSkinnedMeshsFor(string pieceCode);
	public abstract float 					GetLightProbeVolumeFactor();
	public abstract Shader					GetShaderForMaterial(Material mat);
	public abstract RenderSettingsType		GetRenderSettings();
	public abstract int						GetDefaultLightmapSize();

	//CLIENT CALLBACKS, IN ORDER THEY ARE CALLED
	public abstract void					WillBuildWorld();
	public abstract void					DidMergeWorld();
	public abstract void					DidBuildWorld();

	//PATHS
	public abstract string GetSceneLightmapFolder();
	public abstract string GetLightmapFolderForPieceCode(string pieceCode);
	public abstract string GetWorldPainterDataPath(bool isMeta = false);
	public abstract string GetMergedScenePath(string destRoot, bool isMeta = false);
	public abstract string GetRenderSettingsPath(bool isMeta = false);
	public abstract string GetMergedResultsFolder();
	public abstract string GetPartitionLightmapPath(string partitionName);
	public abstract bool ParticleSystemIsMoveable(ParticleSystem particleSystem);

	private string GenerateName( Partition partition, Material material, int layer )
	{
		return material.name + "_" + layer + "_" + partition.name;
	}

	private int CheckoutFiles(BuildStages stages, bool revertUnchanged, string destRoot, int changelist = -1)
	{
		if (changelist == -1)
		{
			//to to find the changelist if we didn't pass one in
			changelist = EB.Perforce.Perforce.GetChangelist(_worldName);

			if (changelist == -1)
			{
				//didn't find one, create the changelist
				changelist = EB.Perforce.Perforce.CreateChangelist(_worldName);
			}
		}
		
		//checkout the merged scene
		EB.Perforce.Perforce.P4Checkout(GetMergedScenePath(destRoot), changelist);
		EB.Perforce.Perforce.P4Checkout(GetMergedScenePath(destRoot, true), changelist);
		
		//checkout the worldpainter data
		EB.Perforce.Perforce.P4Checkout(GetWorldPainterDataPath(), changelist);
		EB.Perforce.Perforce.P4Checkout(GetWorldPainterDataPath(true), changelist);
		
		//checkout the rendersettings data
		if (stages.LoadExternalRenderSettings)
		{
			EB.Perforce.Perforce.P4Checkout(GetRenderSettingsPath(), changelist);
			EB.Perforce.Perforce.P4Checkout(GetRenderSettingsPath(true), changelist);
		}
		
		//checkout the lighting folder
		if (stages.BakeLighting || stages.BakeLightProbes)
		{
			EB.Perforce.Perforce.P4CheckoutDirectory(GetSceneLightmapFolder(), changelist);
			EB.Perforce.Perforce.P4AddDirectory(GetSceneLightmapFolder(), changelist);
		}

		//checkout the merge folder
		if (stages.Merge)
		{
			EB.Perforce.Perforce.P4CheckoutDirectory(GetMergedResultsFolder(), changelist);
			EB.Perforce.Perforce.P4AddDirectory(GetMergedResultsFolder(), changelist);
		}

		if (revertUnchanged)
		{
			EB.Perforce.Perforce.P4RevertUnchanged(changelist);
		}

		return changelist;
	}

	private void BuildWorld(BuildStages stages,  string destRoot = null)
	{
		if (string.IsNullOrEmpty(destRoot))
		{
			destRoot = "Scenes_Merged";
		}

		ShowProgressBar(PROGRESS_BAR_STAGE.Assembling);
		WillBuildWorld();

		int changelist = CheckoutFiles(stages, false, destRoot);
		EditorApplication.NewScene();

		//save early, so that we have a scene name that we can link other data we save to
		SaveScene(destRoot);
		
		var world = new GameObject("z_track");

		AssembleWorld(world);
		WorldInfo worldInfo = GetWorldInfo();

		//convert to internal representation, so we can carry around extra data that we need
		WorldInfoInternal worldInfoInternal = new WorldInfoInternal();
		worldInfoInternal.pieces = new Dictionary<string, PieceInfo>();
		foreach(var code in worldInfo.pieceCodes)
		{
			worldInfoInternal.pieces[code] = new PieceInfo();
		}

		//assemble world
		RenderSettingsType renderSettings;
		if (stages.LoadExternalRenderSettings)
		{
			renderSettings = LoadOrCreateRenderSettings();
		}
		else
		{
			renderSettings = GetRenderSettings();
		}
		
		Dictionary<int, Texture2D> lightMaps = null;
		
		if (stages.BakeLighting)
		{
			ShowProgressBar(PROGRESS_BAR_STAGE.Baking_Lighting);
			Lightmapping.Bake();
		}

		if (stages.Merge)
		{
			lightMaps = LoadLightmaps(worldInfoInternal, stages.BakeLighting || stages.UseGlobalBakeLightmaps);
		}
		else
		{
			//reindex the lightmaps as we are going to directly load them
			LoadLightmapsDirect(worldInfoInternal); 
		}
		if (stages.LoadLightProbes)
		{
			SetupLightProbes(stages.BakeLightProbes, renderSettings, destRoot);
		}
		
		if (stages.Merge)
		{
			EBWorldPainterData worldPainterData = EBWorldPainter.DataForCurrentScene();
			MergeWorld(worldInfoInternal, lightMaps, worldPainterData, stages);
			MergeSkinnedMeshes(worldInfoInternal, worldPainterData);
			MoveParticles(worldInfoInternal, worldPainterData);
		}

		ShowProgressBar(PROGRESS_BAR_STAGE.Cleaning_Up);

		DidMergeWorld();
		
		SaveScene(destRoot);

		//checkout anything that we created in between when we started and now
		CheckoutFiles(stages, true, destRoot, changelist);

		DidBuildWorld();
	}
	
	private void SetupLightProbes(bool bakeLightProbes, RenderSettingsType renderSettings, string destRoot)
	{
		GameObject lightProbeSetup = new GameObject("LightProbes");
		lightProbeSetup.AddComponent<LightProbeGroup>();
		
		if (bakeLightProbes)
		{	
			ShowProgressBar(PROGRESS_BAR_STAGE.Baking_Light_Probes);

			LightProbeGenerator lpg = lightProbeSetup.AddComponent<LightProbeGenerator>();
			
			lpg.LightProbeVolumes = new LightProbeGenerator.LightProbeArea[1];
			lpg.LightProbeVolumes[0] = new LightProbeGenerator.LightProbeArea();

			float factor = GetLightProbeVolumeFactor();
			
			lpg.SetVolumeAuto(factor);
			
			lpg.PlacementAlgorithm = LightProbeGenerator.LightProbePlacementType.Grid;
			lpg.GenProbes();
				
			SaveScene(destRoot);

			//make temp file; a ghetto hack so that Unity doesn't error out when it removes the old light probes, 
			//causing perforce to remove the parent directory and the light probes to then fail to be re-saved
			string lightProbeDir = GetSceneLightmapFolder();
			Directory.CreateDirectory(lightProbeDir);

			string tmpFilePath = lightProbeDir + "tmp.txt";
			if (!System.IO.File.Exists(tmpFilePath))
			{
				System.IO.File.CreateText(tmpFilePath);
			}

			Lightmapping.BakeLightProbesOnly();

			FileUtil.DeleteFileOrDirectory(tmpFilePath);

			System.IO.File.Delete(tmpFilePath);
		}
		
		SetLightProbes slp = lightProbeSetup.AddComponent<SetLightProbes>();
		
		if (System.IO.Directory.Exists(GetSceneLightmapFolder()))
		{	
			LightProbes[] res = GetAtPath<LightProbes>(GetSceneLightmapFolder());
			slp.Probes = (res.Length > 0) ? res[0] : null;
		}

		//scale and offset the probes
		if (bakeLightProbes && (slp.Probes != null))
		{
			Vector3 grayscale = new Vector3(0.299f, 0.587f, 0.114f);
			
			float[] coefficients = slp.Probes.coefficients;
			
			//find max ambient, and scale
			for(int i = 0; i < slp.Probes.count; ++i)
			{
				float value = Vector3.Dot(grayscale, new Vector3(coefficients[i*27 + 0], coefficients[i*27 + 1], coefficients[i*27 + 2]));
				
				float scale = renderSettings.LightProbeScale;
				float offset = renderSettings.LightProbeOffset;
				
				/* Clamping Removed temporarily, coefficients for some odd reason ranged from 0 - 3;
				Debug.Log("LP VVALUES! "+ value +  " c1 " +coefficients[i*27 + 0] + " c2 " + coefficients[i*27 + 1] + " c3 " + coefficients[i*27 + 2]);
				
				float scaledValue = value * scale + offset;

				if (scaledValue > renderSettings.LightProbeMax) // > 1
				{
					scale *= renderSettings.LightProbeMax / scaledValue;
					offset *= renderSettings.LightProbeMax / scaledValue;
				}
				*/
				
				coefficients[i*27 + 0] = coefficients[i*27 + 0] * scale + offset;
				coefficients[i*27 + 1] = coefficients[i*27 + 1] * scale + offset;
				coefficients[i*27 + 2] = coefficients[i*27 + 2] * scale + offset;
			}
			
			slp.Probes.coefficients = coefficients;
		}
		
		if (slp.Probes != null)
		{
			LightmapSettings.lightProbes = slp.Probes;
		}
	}

	public T[] GetAtPath<T> (string path) 
	{
		ArrayList al = new ArrayList();
		
		char slash = System.IO.Path.DirectorySeparatorChar;
		
		Debug.Log("Searching for files of type " + typeof(T).ToString() + " in: " + path);
		
		string [] fileEntries = Directory.GetFiles(path);
		
		if (path.EndsWith(slash.ToString()))
		{
			path = path.Substring(0, path.Length - 1);
		}
		
		foreach(string fileName in fileEntries)
		{
			int index = fileName.LastIndexOf(slash);
			string localPath = path;
			if (index > 0)
				localPath += fileName.Substring(index);
			
			Object t = Resources.LoadAssetAtPath(localPath, typeof(T));
			
			if(t != null)
			{
				al.Add(t);
			}
		}
		
		T[] result = new T[al.Count];
		for(int i=0;i<al.Count;i++)
		{
			result[i] = (T)al[i];
		}   
		
		return result;
	}

	private void LoadLightmapsDirect(WorldInfoInternal worldInfo)
	{
		List<LightmapData> lightmaps = new List<LightmapData>();

		int offset = 0;
		foreach(KeyValuePair<string, PieceInfo> piece in worldInfo.pieces)
		{
			piece.Value.lightmapOffset = offset;
			GameObject prefab = GetPrefabForCode(piece.Key);
			string lightmapPath = GetLightmapFolderForPieceCode(piece.Key);
			var lightmapPaths = GeneralUtils.GetFilesRecursive(lightmapPath, "*.exr");

			for(int i = 0; i < lightmapPaths.Count; ++i)
			{
				string path = lightmapPaths[i];
				if (!path.Contains("LightmapFar"))
					continue; //only far lightmaps are used in forward rendering

				LightmapData lightmapData = new LightmapData();
				lightmapData.lightmapFar = (Texture2D)AssetDatabase.LoadMainAssetAtPath(path);
				lightmaps.Add(lightmapData);
			}

			Renderer[] renderers = EB.Util.FindAllComponents<Renderer>(prefab);
			foreach(var renderer in renderers)
			{
				if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex != 254)
				{
					renderer.lightmapIndex += offset;
				}
			}
			
			offset = lightmaps.Count;
		}
		LightmapSettings.lightmaps = lightmaps.ToArray();	
	}

	public RenderSettingsType LoadOrCreateRenderSettings()
	{
		char slash = System.IO.Path.DirectorySeparatorChar;
		var path = GetRenderSettingsPath();
		var renderSettingsPaths = GeneralUtils.GetFilesRecursive(path, "*.prefab");

		RenderSettingsType renderSettingsToReturn = null;

		for(int i = 0; i < renderSettingsPaths.Count; ++i)
		{
			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(renderSettingsPaths[i], typeof(GameObject));
		
			if (prefab != null)
			{
				GameObject gameObject = (GameObject)Object.Instantiate(prefab.gameObject);
				RenderSettingsType renderSettings = gameObject.GetComponent<RenderSettingsType>();
				if (renderSettings == null)
				{
					Debug.LogError(_worldName + " render settings doesn't have a ShaderGlobals component");
				}
				gameObject.name = "RenderSettings";
				renderSettingsToReturn = renderSettings;
			}
		}
	
		if (renderSettingsToReturn == null)
		{
			//we didn't have a single render settings; create one
			GameObject prefab = new GameObject("RenderSettings", typeof(RenderSettingsType));
			Directory.CreateDirectory(path);
			PrefabUtility.CreatePrefab(path + _worldName + "_render_settings.prefab", prefab, ReplacePrefabOptions.ReplaceNameBased);
			EditorApplication.SaveAssets();
			renderSettingsToReturn = prefab.GetComponent<RenderSettingsType>();
		}

		return renderSettingsToReturn;
	}

	private void SaveScene(string destRoot)
	{
		char slash = System.IO.Path.DirectorySeparatorChar;
		EditorApplication.SaveScene(GetMergedScenePath(destRoot));
	}

	private void MoveParticles(WorldInfoInternal worldInfo, EBWorldPainterData worldPainterData) 
	{
		var world = GameObject.Find("z_track");
		
		if (world == null)
		{
			return;
		}

		Dictionary<EBWorldPainterData.Region, List<ParticleSystem>> meshRegionMap = new Dictionary<EBWorldPainterData.Region, List<ParticleSystem>>();

		foreach(KeyValuePair<string, PieceInfo> piece in worldInfo.pieces)
		{
			GameObject prefab = GetPrefabForCode(piece.Key);
			if(!prefab)
			{
				Debug.LogError("No prefab for code " + piece.Key);
				continue;
			}

			GameObject go = GetPrefabForCode(piece.Key);

			ParticleSystem[] particleSystems = go.GetComponentsInChildren<ParticleSystem>();

			foreach(ParticleSystem p in particleSystems)
			{
				if (!ParticleSystemIsMoveable(p))
				{
					continue;
				}

				EBWorldPainterData.Point point = new EBWorldPainterData.Point(new Vector2(p.transform.position.x, p.transform.position.z));
				EBWorldPainterData.Region[] regions = worldPainterData.RegionsPointIsInside(point);
				if(regions.Length > 0)
				{
					if( !meshRegionMap.ContainsKey(regions[0]))
					{
						meshRegionMap[regions[0]] = new List<ParticleSystem>();
					}
					meshRegionMap[regions[0]].Add(p);
				}
				else
				{
					Debug.LogError("ParticleSystem "+p.name+" does not fall into any region");
				}
			}
		}

		foreach(KeyValuePair<EBWorldPainterData.Region, List<ParticleSystem>> meshRegion in meshRegionMap)
		{
			
			int id = meshRegion.Key.id;
			
			GameObject partition = GameObject.Find("Partition_"+id);
			if(partition == null)
			{
				partition = new GameObject("Partition_"+id);
				partition.transform.parent = GameObject.Find("z_merged").transform;
			}
			
			GameObject mergedParticles = new GameObject("MergedParticles");
			
			mergedParticles.transform.parent = partition.transform;
			foreach(ParticleSystem p in meshRegion.Value)
			{
				Vector3 pos = p.transform.position;
				p.transform.parent = mergedParticles.transform;
				p.transform.position = pos;
			}
		}
	}
	
	private void MergeSkinnedMeshes(WorldInfoInternal worldInfo, EBWorldPainterData worldPainterData)
	{
		var world = GameObject.Find("z_track");
		
		if (world == null)
		{
			return;
		}
		Dictionary<EBWorldPainterData.Region, List<MeshFilter>> meshRegionMap = new Dictionary<EBWorldPainterData.Region, List<MeshFilter>>();
		foreach(KeyValuePair<string, PieceInfo> piece in worldInfo.pieces)
		{
			GameObject prefab = GetPrefabForCode(piece.Key);
			if(!prefab)
			{
				Debug.LogError("No prefab for code " + piece.Key);
				continue;
			}

			MeshFilter[] meshFilters = GetSkinnedMeshsFor(piece.Key);

			if(meshFilters != null && meshFilters.Length > 0)
			{
				foreach(MeshFilter meshFilter in meshFilters)
				{
					EBWorldPainterData.Point point = new EBWorldPainterData.Point(new Vector2(meshFilter.renderer.bounds.center.x, meshFilter.renderer.bounds.center.z));
					EBWorldPainterData.Region[] regions = worldPainterData.RegionsPointIsInside(point);

					if(regions.Length > 0)
					{
						if( !meshRegionMap.ContainsKey(regions[0]))
						{
							meshRegionMap[regions[0]] = new List<MeshFilter>();
						}
						meshRegionMap[regions[0]].Add(meshFilter);
					}
					else
					{
						Debug.LogError("Meshfilter "+meshFilter.name+" does not fall into any region");
					}
				}
			}
		}

		foreach(KeyValuePair<EBWorldPainterData.Region, List<MeshFilter>> meshRegion in meshRegionMap)
		{
			int id = meshRegion.Key.id;
		
			GameObject partition = GameObject.Find("Partition_"+id);
			if(partition == null)
			{
				partition = new GameObject("Partition_"+id);
				partition.transform.parent = GameObject.Find("z_merged").transform;
			}
			string savedName = _worldName +"_"+id;
			List<GameObject> gos = SkinnedMeshMerger.Merge(meshRegion.Value.ToArray(), savedName, _worldName);
			GameObject mergedBreakableMeshes = new GameObject("MergedBreakableMeshes");

			mergedBreakableMeshes.transform.parent = partition.transform;

			foreach(GameObject g in gos) 
			{
				g.layer = LayerMask.NameToLayer("environment");
				g.transform.parent = mergedBreakableMeshes.transform;
			}
		}
	}

	private void MergeWorld(WorldInfoInternal worldInfo, Dictionary<int, Texture2D> lightmaps, EBWorldPainterData worldPainterData, BuildStages stages)
	{
		var world = GameObject.Find("z_track");
		if (world == null)
		{
			return;
		}
		
		List<Renderer> renderersList = new List<Renderer>();
		List<GameObject> z_unmerged = new List<GameObject>();

		foreach(KeyValuePair<string, PieceInfo> piece in worldInfo.pieces)
		{
			var prefab = GetPrefabForCode(piece.Key);
			if (!prefab)
			{
				Debug.LogError("No prefab for code " + piece.Key);
				continue;
			}
			
			GameObject unmerged = null;
			if (prefab.name == "z_unmerged")
			{
				//we have been passed in z_unmerged
				unmerged = prefab;
			}
			else
			{
				unmerged = EB.Util.GetObjectExactMatch(prefab,"z_unmerged"); 
			if (!unmerged)
			{
				Debug.LogError("No z_unmerged found in prefab " + prefab.name + ". Not merging this block.");
				continue;
			}
			}

			z_unmerged.Add(unmerged);

			var partitionRenderers = EB.Util.FindAllComponents<Renderer>(unmerged);

			foreach(var renderer in partitionRenderers)
			{
				if (!RendererHasLightmap(renderer))
					continue;
				renderer.lightmapIndex += piece.Value.lightmapOffset;
			}

			renderersList.AddRange(partitionRenderers);
		}	

		//skip hidden meshes
		for (int i = renderersList.Count - 1; i >= 0; --i)
		{
			Renderer renderer = renderersList[i];
			if (!renderer.gameObject.activeInHierarchy)
			{
				renderersList.RemoveAt(i);
			}
		}
		
		var renderers = renderersList.ToArray();

		//Paritition the meshes based of the World Painter data
		var partitions = PartitionMeshes(renderers, worldPainterData);
		
		//Process the partitions, creating the new merged geometry and lightmaps
		int saved = ProcessPartitions(partitions, lightmaps, world, stages);
		Debug.Log("Merged " + _worldName + ": saved " + saved + " draw calls");
		
		//Clean up the heirarchy from things we now merged
		foreach(GameObject unmerged in z_unmerged)
		{
			CleanUpHeirarchy(unmerged.transform);
			}

		EditorApplication.SaveAssets();
	}

	private Dictionary<int, Texture2D> LoadLightmaps(WorldInfoInternal worldInfo, bool fromGlobalBake)
	{
		var lightmaps = new Dictionary<int, Texture2D>();

		if (fromGlobalBake)
		{
			string baseLightmapsDir = GetSceneLightmapFolder();
			var lightmapPaths = GeneralUtils.GetFilesRecursive(baseLightmapsDir, "*.exr");
			for(int i = 0; i < lightmapPaths.Count; ++i)
			{
				string path = lightmapPaths[i];
				if (!path.Contains("LightmapFar"))
					continue;
				var ti = (TextureImporter)TextureImporter.GetAtPath(path);
				if (!ti.isReadable || ti.textureFormat != TextureImporterFormat.ARGB32)
				{
					ti.isReadable = true;
					ti.textureFormat = TextureImporterFormat.ARGB32;
					AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				}
				var texture = (Texture2D)AssetDatabase.LoadMainAssetAtPath(path);
				string[] nameSplit = texture.name.Split('-');
				int lightmapIndex;
				int.TryParse(nameSplit[nameSplit.Length-1], out lightmapIndex);
				lightmaps.Add(lightmapIndex, texture);
			}
		}
		else
		{
			int offset = 0;
			foreach(KeyValuePair<string, PieceInfo> piece in worldInfo.pieces)
			{
				piece.Value.lightmapOffset = offset;
				
				string lightmapPath = GetLightmapFolderForPieceCode(piece.Key);
				var lightmapPaths = GeneralUtils.GetFilesRecursive(lightmapPath, "*.exr");

				int maxLightmapIndex = 0;
				
				for(int i = 0; i < lightmapPaths.Count; ++i)
				{
					string path = lightmapPaths[i];
					if (!path.Contains("LightmapFar"))
						continue; //only far lightmaps are used in forward rendering

					var ti = (TextureImporter)TextureImporter.GetAtPath(path);
					if (!ti.isReadable || ti.textureFormat != TextureImporterFormat.ARGB32)
					{
						ti.isReadable = true;
						ti.textureFormat = TextureImporterFormat.ARGB32;
						AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
					}
					
					var texture = (Texture2D)AssetDatabase.LoadMainAssetAtPath(path);

					string[] nameSplit = texture.name.Split('-');
					int lightmapIndex;
					int.TryParse(nameSplit[nameSplit.Length-1], out lightmapIndex);
					
					maxLightmapIndex = Mathf.Max(maxLightmapIndex, lightmapIndex);

					lightmaps.Add(piece.Value.lightmapOffset + lightmapIndex, texture);
				}
				
				offset += maxLightmapIndex + 1;
			}
		}
		
		return lightmaps;
	}
	
	private Dictionary<EBWorldPainterData.Region, Partition> PartitionMeshes( Renderer[] renderers, EBWorldPainterData worldPainterData)
	{
		if (renderers.Length == 0)
		{
			return new Dictionary<EBWorldPainterData.Region, Partition>();
		}
		
		Dictionary<EBWorldPainterData.Region, Partition> partitions = new Dictionary<EBWorldPainterData.Region, Partition>();
		
		for(int i = 0; i < worldPainterData.regions.Count; ++i)
		{
			Partition partition = new Partition();
			partition.name = i.ToString();
			partitions.Add(worldPainterData.regions[i], partition);
		}
				
		foreach(Renderer renderer in renderers)
		{
			EBWorldPainterData.Point p = new EBWorldPainterData.Point(new Vector2(renderer.bounds.center.x, renderer.bounds.center.z));
			
			EBWorldPainterData.Region region;
			EBWorldPainterData.Region[] regions = worldPainterData.RegionsPointIsInside(p);
			if (regions.Length < 1)
			{
				//No regions, just add it to the first one
				Debug.LogError("Renderer " + renderer.name + " with bounds " + renderer.bounds + " didn't fall into any partitions");
				region = worldPainterData.ClosestRegionToPoint(p);
			}
			else
			{
				region = regions[0];
			}

			Instance instance = new Instance();
			instance.renderer = renderer;
			partitions[region].instances.Add(instance);
		}
		
		foreach(var region in worldPainterData.regions)
		{
			if (partitions[region].instances.Count == 0)
			{
				partitions.Remove(region);
			}
		}
		
		return partitions;
	}

	private int ProcessPartitions(Dictionary<EBWorldPainterData.Region, Partition> partitions, Dictionary<int, Texture2D> lightmaps, GameObject world, BuildStages stages)
	{
		var merged = EB.Util.GetObjectExactMatch(world, "z_merged");
		DestroyImmediate(merged, false);
		
		merged = new GameObject("z_merged");
		merged.transform.parent = world.transform;
		merged.isStatic = false;
		
		var saved = 0;
		var i = 0;

		foreach(var kv in partitions)
		{
			var region = kv.Key;
			var partition = kv.Value;

			string path = GetPartitionLightmapPath(partition.name);
			var ti = (TextureImporter)TextureImporter.GetAtPath(path);

			if (stages.EnableVertexLightmaps && (ti != null) && (!ti.isReadable || ti.textureFormat != TextureImporterFormat.ARGB32))
			{
				ti.isReadable = true;
				ti.textureFormat = TextureImporterFormat.ARGB32;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}

			ShowProgressBar(PROGRESS_BAR_STAGE.Merging, (float)i / (float)partitions.Count);
			i++;

			MergeLightmaps(partition, region, lightmaps);
			LightmapSkinnedMeshes(partition, lightmaps, stages);
			LightmapUnmergeableMeshes(partition, lightmaps, stages);
			var groupings = GroupPartition(partition, lightmaps);
			saved += MergePartitionGroups(merged, partition, region, groupings, stages);

			if (stages.EnableVertexLightmaps && (ti != null) && (ti.isReadable || ti.textureFormat != TextureImporterFormat.AutomaticCompressed))
			{
				ti.isReadable = false;
				ti.textureFormat = TextureImporterFormat.AutomaticCompressed;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}

		}
		return saved;
	}

	private int MergePartitionGroups(GameObject zMerged, Partition partition, EBWorldPainterData.Region region, Dictionary<string, Grouping> groupings, BuildStages stages)
	{	
		char slash = System.IO.Path.DirectorySeparatorChar;
		var dir = GetMergedResultsFolder();
		var geometryDir = dir + "Geometry" + slash;
		var materialsDir = dir + "Materials" + slash;
		System.IO.Directory.CreateDirectory(dir);
		System.IO.Directory.CreateDirectory(geometryDir);
		System.IO.Directory.CreateDirectory(materialsDir);
			
			GameObject parent = new GameObject("Partition_" + region.id);
			
			parent.transform.parent = zMerged.transform;

			bool firstMeshOfRegion = true;

			int saved = 0;
		
			foreach (Grouping grouping in groupings.Values)
			{
				var layername = LayerMask.LayerToName(grouping.layer);
				var layer = EB.Util.GetObjectExactMatch(parent, layername);
				if (!layer)
				{
					layer = new GameObject(layername);
					layer.layer = grouping.layer;
					layer.transform.parent = parent.transform;
				}
				
				var name = GenerateName(partition, grouping.material, grouping.layer);
			var go = EB.Util.GetObjectExactMatch(zMerged, name);
				if (go == null)
				{
					go = new GameObject(name, typeof(MeshFilter),typeof(MeshRenderer));	
					go.transform.parent = layer.transform;
					go.layer = grouping.layer;
					go.isStatic = false;
				}
				
				var shader = GetShaderForMaterial(grouping.material);
				var mat = new Material(grouping.material);
				
				if (shader != null)
				{
					mat.shader = shader;
					if (mat.HasProperty("_lm")) 
					{
						mat.SetTexture("_lm", partition.lightmap);
					}
				}

				AssetDatabase.CreateAsset(mat, materialsDir + name + ".mat");

				var mesh = new Mesh();
				mesh.CombineMeshes( grouping.instances.ToArray(), true);
				
				if(stages.EnableVertexLightmaps && (mesh.uv2 != null) && (mesh.uv2.Length != 0))
				{
					VertexLightmapping(mesh, partition.lightmap);
				}

				EBMeshUtils.Channels channels = GetChannelMap(grouping.material.shader);
				var optimizedMesh = EBMeshUtils.Optimize( EBMeshUtils.StripStreams(mesh, channels) );

				AssetDatabase.CreateAsset(optimizedMesh, geometryDir + name + ".asset");
				
				go.GetComponent<MeshFilter>().sharedMesh = optimizedMesh;
				var mr = go.GetComponent<MeshRenderer>();
				mr.sharedMaterial = mat;
				mr.lightmapIndex = -1;
				mr.lightmapTilingOffset = new Vector4(1,1,0,0);	

				if (firstMeshOfRegion)
				{
					region.bounds = optimizedMesh.bounds;
					firstMeshOfRegion = false;
				}
				else
				{
					region.bounds.Encapsulate(optimizedMesh.bounds);
				}
				 
				saved += grouping.instances.Count - 1;
			}
			
		return saved;
	}

	private void VertexLightmapping(Mesh mesh, Texture2D lm)
	{
		Vector2[] uvs = mesh.uv2;
		Color[] colors = new Color[uvs.Length];

		for (int i = 0; i < uvs.Length; ++i)
		{
			float r = 0f;
			if(mesh.colors != null && mesh.colors.Length > 0) 
			{
				r = mesh.colors[i].r;
			}
			Vector2 uv = uvs[i];
			Color c = lm.GetPixelBilinear(uv.x, uv.y);
			//don't need to worry about EXR vs. PNG as we are reading the merged (non exr) lightmap.
			colors[i] = new Color(r, c.r, c.g, c.b);
		}

		mesh.colors = colors;
	}
	
	private void LightmapSkinnedMeshes(Partition partition, Dictionary<int, Texture2D> lightmaps, BuildStages stages)
	{
		char slash = System.IO.Path.DirectorySeparatorChar;
		var dir = GetMergedResultsFolder();
		var skinnedMeshDir = dir + "SkinnedMesh" + slash;
		System.IO.Directory.CreateDirectory(skinnedMeshDir);

		int skinnedMeshInstance = 0;
		
		foreach( Instance instance in partition.instances )
		{
			var renderer = instance.renderer;
			
			//check to see if we are a skinned mesh renderer
			var skinnedMeshRenderer = renderer.GetComponent<SkinnedMeshRenderer>();
			
			if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null || !RendererHasLightmap(skinnedMeshRenderer) || !lightmaps.ContainsKey(skinnedMeshRenderer.lightmapIndex))
			{
				continue;
			}

			var skinnedMesh = skinnedMeshRenderer.sharedMesh;

			var mesh = (Mesh)Instantiate(skinnedMeshRenderer.sharedMesh);
			
			//scale by the mesh base lightmap scale / offset
			Vector2 baseScale = new Vector2(skinnedMeshRenderer.lightmapTilingOffset.x, skinnedMeshRenderer.lightmapTilingOffset.y);
			Vector2 baseOffset = new Vector2(skinnedMeshRenderer.lightmapTilingOffset.z, skinnedMeshRenderer.lightmapTilingOffset.w);
			mesh.uv2 = PremultUVs(mesh.uv2, baseScale, baseOffset);
			
			int submeshIndex = -1;
			foreach( var material in skinnedMeshRenderer.sharedMaterials )
			{
				++submeshIndex;
				
				//bring it into the 0-1 range based off the precomputed local scale / offset (e.g. the rect we used to crop with)
				LightMapInfo lmi = instance.lightmapInfo[submeshIndex];
				mesh.uv2 = PremultUV2sIndexed(mesh, submeshIndex, lmi.localScale, lmi.localOffset);
				
				//bring into the merged lightmap space
				Rect lightmapRect = partition.lightmapRects[lmi.index];
				var mergedScale = new Vector2(lightmapRect.width, lightmapRect.height);
				var mergedOffset= new Vector2(lightmapRect.x, lightmapRect.y);
				mesh.uv2 = PremultUV2sIndexed(mesh, submeshIndex, mergedScale, mergedOffset);
			}
			
			if(stages.EnableVertexLightmaps && (mesh.uv2 != null) && (mesh.uv2.Length != 0))
			{
				VertexLightmapping(mesh, partition.lightmap);
			}
			
			AssetDatabase.CreateAsset(mesh, skinnedMeshDir + skinnedMeshRenderer.name + "_" + skinnedMeshInstance + ".asset");
			++skinnedMeshInstance;
			
			skinnedMeshRenderer.sharedMesh = mesh;
			skinnedMeshRenderer.lightmapIndex = -1;
			skinnedMeshRenderer.lightmapTilingOffset = new Vector4(1,1,0,0);

		}
	}
	
	private void LightmapUnmergeableMeshes(Partition partition, Dictionary<int, Texture2D> lightmaps, BuildStages stages)
	{
		char slash = System.IO.Path.DirectorySeparatorChar;
		var dir = GetMergedResultsFolder();
		var unmergeableMeshDir = dir + "Geometry" + slash + "Unmergeable" + slash;
		System.IO.Directory.CreateDirectory(unmergeableMeshDir);
		
		int unmergeableMeshInstance = 0;
		
		foreach( Instance instance in partition.instances )
		{
			var renderer = instance.renderer;

			if (RendererIsMergeable(renderer) || !RendererHasLightmap(renderer))
			{
				continue;
			}

			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			
			if (filter == null)
			{
				continue;
			}

			//Debug.LogError("Unmergeable: " + renderer.name);
			
			var mesh = (Mesh)Instantiate(filter.sharedMesh);
			
			//scale by the mesh base lightmap scale / offset
			Vector2 baseScale = new Vector2(renderer.lightmapTilingOffset.x, renderer.lightmapTilingOffset.y);
			Vector2 baseOffset = new Vector2(renderer.lightmapTilingOffset.z, renderer.lightmapTilingOffset.w);
			mesh.uv2 = PremultUVs(mesh.uv2, baseScale, baseOffset);
			
			int submeshIndex = -1;
			foreach( var material in renderer.sharedMaterials )
			{
				++submeshIndex;
				
				//bring it into the 0-1 range based off the precomputed local scale / offset (e.g. the rect we used to crop with)
				LightMapInfo lmi = instance.lightmapInfo[submeshIndex];
				mesh.uv2 = PremultUV2sIndexed(mesh, submeshIndex, lmi.localScale, lmi.localOffset);
				
				//bring into the merged lightmap space
				Rect lightmapRect = partition.lightmapRects[lmi.index];
				var mergedScale = new Vector2(lightmapRect.width, lightmapRect.height);
				var mergedOffset= new Vector2(lightmapRect.x, lightmapRect.y);
				mesh.uv2 = PremultUV2sIndexed(mesh, submeshIndex, mergedScale, mergedOffset);
			}
			
			if(stages.EnableVertexLightmaps && (mesh.uv2 != null) && (mesh.uv2.Length != 0))
			{
				VertexLightmapping(mesh, partition.lightmap);
			}
			
			AssetDatabase.CreateAsset(mesh, unmergeableMeshDir + renderer.name + "_" + unmergeableMeshInstance + ".asset");
			++unmergeableMeshInstance;
			
			filter.sharedMesh = mesh;
			renderer.lightmapIndex = -1;
			renderer.lightmapTilingOffset = new Vector4(1,1,0,0);
		}
	}

	private Dictionary<string, Grouping> GroupPartition(Partition partition, Dictionary<int, Texture2D> lightmaps)
	{
		Dictionary<string, Grouping> groupings = new Dictionary<string, Grouping>();
		
		foreach( Instance instance in partition.instances )
		{
			var renderer = instance.renderer;
			
			if (!RendererIsMergeable(renderer))
			{
				continue;
			}
			
			bool hasLightmap = RendererHasLightmap(renderer) && lightmaps.ContainsKey(renderer.lightmapIndex);
			
			var filter = renderer.GetComponent<MeshFilter>();
			
			if (filter == null)
			{
				continue;
			}
			
			int submeshIndex = -1;
			foreach( var material in renderer.sharedMaterials )
			{
				++submeshIndex;
				
				if (!material)
				{
					Debug.LogError("No material for renderer " + renderer.name + " at submesh " + submeshIndex + ", skipping.");
					continue;
				}

				if (hasLightmap && ((instance.lightmapInfo == null) || (submeshIndex >= instance.lightmapInfo.Length)))
				{
					//our mesh must have extra materials on it
					Debug.LogWarning("Renderer " + renderer.name + " seems to have more materials that it does submeshes");
					continue;
				}
				
				//break out the submesh
				var tmpCi = new CombineInstance();
				tmpCi.mesh = filter.sharedMesh;
				tmpCi.subMeshIndex = submeshIndex;
				tmpCi.transform = Matrix4x4.identity;
				
				var newMesh = new Mesh();
				newMesh.CombineMeshes(new CombineInstance[]{tmpCi});	
				
				if (hasLightmap)
				{
					//scale by the mesh base lightmap scale / offset
					Vector2 baseScale = new Vector2(renderer.lightmapTilingOffset.x, renderer.lightmapTilingOffset.y);
					Vector2 baseOffset = new Vector2(renderer.lightmapTilingOffset.z, renderer.lightmapTilingOffset.w);
					newMesh.uv2 = PremultUVs(newMesh.uv2, baseScale, baseOffset);
					
					//bring it into the 0-1 range based off the precomputed local scale / offset (e.g. the rect we used to crop with)
					LightMapInfo lmi = instance.lightmapInfo[submeshIndex];
					newMesh.uv2 = PremultUVs(newMesh.uv2, lmi.localScale, lmi.localOffset);
					
					//bring into the merged lightmap space
					Rect lightmapRect = partition.lightmapRects[lmi.index];
					var mergedScale = new Vector2(lightmapRect.width, lightmapRect.height);
					var mergedOffset= new Vector2(lightmapRect.x, lightmapRect.y);
					newMesh.uv2 = PremultUVs(newMesh.uv2, mergedScale, mergedOffset);
				}

				if (material.shader.name != "EBG/Enviro/Self-Illumin/Interior")
				{
					newMesh.uv = PremultUVs(newMesh.uv, material.mainTextureScale, material.mainTextureOffset);
				}
				
				var ci = new CombineInstance();
				ci.mesh = newMesh;
				ci.subMeshIndex = 0;
				ci.transform = renderer.transform.localToWorldMatrix;
				
				Grouping grouping = null;
				var materialName = GenerateName(partition, material, RemapLayer(renderer.gameObject.layer));
				if (!groupings.TryGetValue(materialName, out grouping))
				{
					grouping = new Grouping();
					grouping.material = material;
					grouping.layer = RemapLayer(renderer.gameObject.layer);
					groupings.Add(materialName, grouping);
				}
				
				grouping.instances.Add(ci);
			}
		}
		
		return groupings;
	}
	
	private void MergeLightmaps(Partition partition, EBWorldPainterData.Region region, Dictionary<int, Texture2D> lightmaps)
	{	
		int maxLightMapIndex = 0;
		foreach(int key in lightmaps.Keys)
		{
			maxLightMapIndex = Mathf.Max(maxLightMapIndex, key);
		}
		
		List<Rect>[] lightmapRects = new List<Rect>[maxLightMapIndex + 1];
		for (int i = 0; i <= maxLightMapIndex; ++i)
		{
			lightmapRects[i] = new List<Rect>();
		}
		
		//crop each meshes lightmaps out of the partition
		foreach(var instance in partition.instances)
		{
			var renderer = instance.renderer;
			
			if (!RendererHasLightmap(renderer))
				continue;
			
			if (!lightmaps.ContainsKey(renderer.lightmapIndex))
			{
				Debug.LogError("Some lightmaps weren't loaded that geometry references..." + renderer.lightmapIndex);
				continue;
			}
			
			var mesh = GetMesh(renderer);
			if (mesh == null)
				continue;
			
			var lightmapUVs = mesh.uv2;
			
			int subMeshCount = mesh.subMeshCount;
			
			instance.lightmapInfo = new LightMapInfo[subMeshCount];
			
			for (int i = 0; i < subMeshCount; ++i)
			{
				Vector2 uvMin = new Vector2(float.MaxValue, float.MaxValue);
				Vector2 uvMax = new Vector2(float.MinValue, float.MinValue);
				
				var indices = mesh.GetIndices(i);
				foreach(var index in indices)
				{
					Vector2 uv = lightmapUVs[index];
					uv.x = uv.x * renderer.lightmapTilingOffset.x + renderer.lightmapTilingOffset.z;
					uv.y = uv.y * renderer.lightmapTilingOffset.y + renderer.lightmapTilingOffset.w;
					uvMin = Vector2.Min(uvMin, uv);
					uvMax = Vector2.Max(uvMax, uv);
				}
				
				var rect = new Rect(uvMin.x, uvMin.y, uvMax.x - uvMin.x, uvMax.y - uvMin.y);
				lightmapRects[renderer.lightmapIndex].Add(rect);
				
				instance.lightmapInfo[i] = new LightMapInfo();
				instance.lightmapInfo[i].index = lightmapRects[renderer.lightmapIndex].IndexOf(rect);
			}
		}
		
		List<KeyValuePair<int, Rect>> allRects = new List<KeyValuePair<int, Rect>>();
			
		for (int lm = 0; lm < lightmaps.Count; ++lm)
		{
			int[] remap = new int[lightmapRects[lm].Count];
			List<Rect> remappedRects = new List<Rect>();
			
			for (int i = 0; i < lightmapRects[lm].Count; ++i)
			{
				var curOrigRect = lightmapRects[lm][i];
				bool remapped = false;
				for (int j = 0; j < remappedRects.Count; ++j)
				{
					var curRemappedRect = remappedRects[j];
					if (Intersect(curOrigRect, curRemappedRect))
					{
						float l = Mathf.Min(curOrigRect.xMin, curRemappedRect.xMin);
						float r = Mathf.Max(curOrigRect.xMax, curRemappedRect.xMax);
						float t = Mathf.Min(curOrigRect.yMin, curRemappedRect.yMin);
						float b = Mathf.Max(curOrigRect.yMax, curRemappedRect.yMax);
						Rect encapsulatedRect = new Rect(l, t, r-l, b-t);
						
						remappedRects[j] = encapsulatedRect;
						remap[i] = j;
						remapped = true;
						break;
					}
				}
				
				if (!remapped)
				{
					remappedRects.Add(curOrigRect);
					remap[i] = remappedRects.IndexOf(curOrigRect);
				}
			}
			
			for(int i = 0; i < remappedRects.Count; ++i)
			{
				Rect rect = remappedRects[i];
				
				Texture2D lightmap = lightmaps[lm];
				float lightmapWidth = (float)lightmap.width;
				float lightmapHeight = (float)lightmap.height;
				
				//we need to bring this rect into a whole integer, as we crop on pixel boundaries;
				float left 		= Mathf.Clamp01(Mathf.Floor(rect.x * lightmapWidth) / lightmapWidth);
				float top 		= Mathf.Clamp01(Mathf.Floor(rect.y * lightmapHeight) / lightmapHeight);
				float width 	= Mathf.Clamp01(Mathf.Clamp01(Mathf.Ceil((rect.x + rect.width) * lightmapWidth) / lightmapWidth) - left);
				float height 	= Mathf.Clamp01(Mathf.Clamp01(Mathf.Ceil((rect.y + rect.height) * lightmapHeight) / lightmapHeight) - top);
				
				remappedRects[i] = new Rect(left, top, width, height);

				allRects.Add(new KeyValuePair<int, Rect>(lm, remappedRects[i]));
			}
			
			foreach(var instance in partition.instances)
			{
				var renderer = instance.renderer;
				
				if (!RendererHasLightmap(renderer) || (renderer.lightmapIndex != lm))
					continue;
				
				if (!lightmaps.ContainsKey(renderer.lightmapIndex))
				{
					Debug.LogError("Lightmap " + renderer.lightmapIndex + " wasn't loaded but some geometry references it - are the lightmaps in the right place?");
					continue;
				}
			
				var mesh = GetMesh(instance.renderer);
				if (mesh == null)
					continue;
				
				int subMeshCount = mesh.subMeshCount;
					
				for (int i = 0; i < subMeshCount; ++i)
				{
					var rect = remappedRects[remap[instance.lightmapInfo[i].index]];
					instance.lightmapInfo[i].index = allRects.IndexOf(new KeyValuePair<int, Rect>(renderer.lightmapIndex, rect));
					instance.lightmapInfo[i].localScale = new Vector2(1.0f / rect.width, 1.0f / rect.height);
					instance.lightmapInfo[i].localOffset = new Vector2(-rect.x / rect.width, -rect.y / rect.height);
				}
			}
		}
				
		List<Texture2D> croppedLightmaps = new List<Texture2D>();
		foreach(var kv in allRects)
		{
			int lm = kv.Key;
			Rect rect = kv.Value;
			Texture2D cropped = CropTexture(lightmaps[lm], rect);
			croppedLightmaps.Add(cropped);
		}
		
		//pack all the cropped lightmaps
		int lightmapSize = GetDefaultLightmapSize();
		if (region.dataLayers != null) 
		{
			int regionLightmapSize = region.dataLayers[(int)EBWorldPainterData.eDATA_LAYER.LightmapSize].value;
			if (regionLightmapSize != 0)
			{
				lightmapSize = regionLightmapSize;
			}
		}
		if (croppedLightmaps.Count == 0)
		{
			lightmapSize = 1;
		}
		Texture2D packedTexture = new Texture2D(lightmapSize, lightmapSize, TextureFormat.RGB24, true);
		partition.lightmapRects = packedTexture.PackTextures(croppedLightmaps.ToArray(), 0, lightmapSize, false);
		packedTexture.Apply(true, false);
		if ((BuildSettings.Target != BuildTarget.Android) && (BuildSettings.Target != BuildTarget.iPhone))
		{
			//multiply by 4 * alpha
			for(int y = 0; y < packedTexture.height; ++y)
			{
				for(int x = 0; x < packedTexture.width; ++x)
				{
					var color = packedTexture.GetPixel(x, y);
					color.r *= 4.0f * color.a;
					color.g *= 4.0f * color.a;
					color.b *= 4.0f * color.a;
					color.a = 1.0f;
					packedTexture.SetPixel(x, y, color);
				}
			}
		}
		else
		{
			for(int y = 0; y < packedTexture.height; ++y)
			{
				for(int x = 0; x < packedTexture.width; ++x)
				{
					var color = packedTexture.GetPixel(x, y);
					color.a = 1.0f;
					packedTexture.SetPixel(x, y, color);
				}
			}
		}
		packedTexture.Apply(true,false);

		string filename = GetPartitionLightmapPath(partition.name);
		if (File.Exists(filename))
		{
			//it may be checked into perforce, remove 'read-only' flag
			File.SetAttributes(filename, FileAttributes.Normal);
		}
		File.WriteAllBytes(filename, packedTexture.EncodeToPNG());
		AssetDatabase.Refresh();
		partition.lightmap = (Texture2D)AssetDatabase.LoadMainAssetAtPath(filename);
	}

	private Texture2D CropTexture(Texture2D baseTexture, Rect UVRect)
	{
		int x = (int)(UVRect.x * baseTexture.width);
		int y = (int)(UVRect.y * baseTexture.height);
		int width = (int)(UVRect.width * baseTexture.width);
		int height = (int)(UVRect.height * baseTexture.height);
		
		Color[] pixels = baseTexture.GetPixels(x, y, width, height, 0);
		
		Texture2D cropped = new Texture2D(width, height, baseTexture.format, false);
		cropped.SetPixels(pixels);
		cropped.Apply();
		
		return cropped;
	}
	
	private Vector2[] PremultUVs(Vector2[] uvs, Vector2 scale, Vector2 offset)
	{	
		Vector2[] res = new Vector2[uvs.Length];
		for (int i = 0; i < uvs.Length; ++i)
		{
			res[i] = new Vector2(uvs[i].x * scale.x + offset.x, uvs[i].y * scale.y + offset.y);
		}
		return res;
	}
	
	private Vector2[] PremultUV2sIndexed(Mesh mesh, int submeshIndex, Vector2 scale, Vector2 offset)
	{	
		Vector2[] res = (Vector2[])mesh.uv2.Clone();
		int[] indices = mesh.GetIndices(submeshIndex);
		bool[] multiplied = new bool[mesh.vertexCount];
		for (int i = 0; i < indices.Length; ++i)
		{
			int index = indices[i];

			if (multiplied[index])
				continue;

			res[index] = new Vector2(res[index].x * scale.x + offset.x, res[index].y * scale.y + offset.y);
			multiplied[index] = true;
		}
		return res;
	}

	private bool CleanUpHeirarchy(Transform transform, bool canDeleteTransforms = true)
	{
		if (transform.GetComponent<Animation>() != null)
		{
			//can't clean up animation heirarchies due to bones
			canDeleteTransforms = false;
		}

		bool destroy = true;

		for(int i = transform.childCount - 1; i >= 0; --i)
		{
			var child = transform.GetChild(i);
			destroy &= CleanUpHeirarchy(child, canDeleteTransforms);
		}

		if (destroy)
		{
			//we have no children, or we just cleaned them all up
			Component[] components = transform.GetComponents<Component>();
			if ((components.Length == 1) && canDeleteTransforms)
			{
				//empty transform
				DestroyImmediate(transform.gameObject);
				return true;
			}
			else if (components.Length == 3)
			{
				MeshRenderer renderer = transform.GetComponent<MeshRenderer>();
				if (renderer == null || !RendererIsMergeable(renderer))
					return false;
				
				//a transform, mesh filter, mesh renderer (which we merged), so kill the parent node
				DestroyImmediate(transform.gameObject);
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			//we have children, just destroy our mesh filter and renderer if possible
			MeshRenderer renderer = transform.GetComponent<MeshRenderer>();
			if (renderer == null || !RendererIsMergeable(renderer))
				return false;

			DestroyImmediate(renderer);

			MeshFilter filter = transform.GetComponent<MeshFilter>();
			if (filter != null)
			{
				DestroyImmediate(filter);
			}

			return false;
		}
	}
	
	private bool RendererIsMergeable(Renderer renderer)
	{
		if (renderer == null)
			return false;
		var filter = renderer.GetComponent<MeshFilter>();
		bool results0 = filter != null;
		bool results1 = results0 && (filter.sharedMesh != null);
		bool results2 = results1 && (filter.sharedMesh.subMeshCount <= renderer.sharedMaterials.Length);
		if (results1 && !results2)
		{
			Debug.LogError("Renderer " + renderer.name + " is not mergeable as it has more submeshes than materials");
		}
		bool results3 = results2 && (renderer.gameObject.GetComponents<Component>().Length == 3);
		Transform parent = renderer.transform.parent;
		while(results3 && (parent != null))
		{
			int componentCount = parent.GetComponents<Component>().Length;
			results3 &= (componentCount == 1) || ((componentCount == 3) && (parent.GetComponent<MeshFilter>() != null));
			parent = parent.parent;
		}
		if (results2 && !results3)
		{
			Debug.LogWarning("Renderer " + renderer.name + " is not mergeable as it has some scripts attached");
		}
		return results3;
	}
	
	private bool RendererHasLightmap(Renderer renderer)
	{
		if (renderer.lightmapIndex < 0 || renderer.lightmapIndex >= 254)
		{
			//Debug.LogWarning("Renderer " + renderer.name + " does not have lightmap " + renderer.lightmapIndex);
			return false;
		}
		
		Mesh mesh = GetMesh(renderer);

		if (mesh == null)
		{
			//Debug.LogWarning("Renderer " + renderer.name + " doesn't have a mesh");
			return false;
		}
		if (mesh.uv2 == null || mesh.uv2.Length == 0)
		{
			//Debug.LogWarning("Renderer " + renderer.name + " doesn't have a uv2 set");
			return false;
		}
		
		return true;
	}
	
	private Mesh GetMesh(Renderer renderer)
	{
		var filter = renderer.GetComponent<MeshFilter>();
		if (filter != null)
		{
			return filter.sharedMesh;
		}
		else
		{
			var skinnedMeshRenderer = renderer.GetComponent<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer != null)
			{
				return skinnedMeshRenderer.sharedMesh;
			}
		}
		return null;
	}
	
	private bool UVsIn01Range(Vector2[] uvs)
	{
		Vector2 uvMin = new Vector2(float.MaxValue, float.MaxValue);
		Vector2 uvMax = new Vector2(float.MinValue, float.MinValue);
		
		foreach(var uv in uvs)
		{
			uvMin = Vector2.Min(uvMin, uv);
			uvMax = Vector2.Max(uvMax, uv);
		}
		
		return !(uvMin.x < 0.0f || uvMin.y < 0.0f || uvMax.x > 1.0f || uvMax.y > 1.0f);
	}
					
	private bool Intersect( Rect a, Rect b ) 
	{
	 	bool c1 = a.x < b.xMax;
		bool c2 = a.xMax > b.x;
		bool c3 = a.y < b.yMax;
		bool c4 = a.yMax > b.y;
		return c1 && c2 && c3 && c4;
	}

	// PROGRESS BAR

	private void ShowProgressBar(PROGRESS_BAR_STAGE stage, float offset = 0.0f)
	{
		string stageName = stage.ToString().Replace('_', ' ');
		float progress = (((float)stage) + offset)/((float)PROGRESS_BAR_STAGE.COUNT);
		EditorUtility.DisplayProgressBar(_worldName, stageName, progress);
	}
	
	private enum PROGRESS_BAR_STAGE
	{
		Assembling,
		Baking_Lighting,
		Baking_Light_Probes,
		Merging,
		Cleaning_Up,
		COUNT
	}
}
