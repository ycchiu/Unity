using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SkinnedMeshMerger : MonoBehaviour {
	
	public static Mesh SaveMesh(Mesh mesh, string savedName, string folderName)
	{
		char slash = System.IO.Path.DirectorySeparatorChar;

		//create directory we will save into / load from
		var dir = "Assets" + slash + "Merge" + slash + folderName + slash + "Smackables";
		Directory.CreateDirectory(dir);
		
		//figure out the name we will use
		mesh.name = "Smackables_"+savedName;
		string path = dir + slash + mesh.name + ".asset";
		
		//try to load, save
		Mesh savedMesh = AssetDatabase.LoadMainAssetAtPath (path) as Mesh;
		if (savedMesh != null)
		{
			EditorUtility.CopySerialized(mesh, savedMesh);
			AssetDatabase.SaveAssets();
		}
		else
		{
			AssetDatabase.CreateAsset(mesh, path);
		}
		return (Mesh)AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));
	}
	
	public static List<GameObject> Merge(MeshFilter[] meshFilters, string savedName, string folderName) {

		List<GameObject> skinnedMeshObjs = new List<GameObject>();
		Dictionary<Material, List<MeshFilter>> skinnedMeshInfos = new Dictionary<Material,  List<MeshFilter>>();
		int counter = 0;

		foreach(MeshFilter meshFilter in meshFilters)
		{
			if(meshFilter.renderer.sharedMaterial == null) 
			{
				Debug.LogError("Object is missing material "+meshFilter.gameObject.name);
				continue;
			}
			if(!skinnedMeshInfos.ContainsKey(meshFilter.renderer.sharedMaterial))
			{
				skinnedMeshInfos[meshFilter.renderer.sharedMaterial] = new List<MeshFilter>();
			}
			skinnedMeshInfos[meshFilter.renderer.sharedMaterial].Add(meshFilter);
		}

		foreach(KeyValuePair<Material, List<MeshFilter>> skinnedMeshInfo in skinnedMeshInfos)
		{
			List<Transform> bones = new List<Transform>();        
			List<BoneWeight> boneWeights = new List<BoneWeight>();        
			List<CombineInstance> combineInstances = new List<CombineInstance>();
			int numSubs = 0;

			foreach(MeshFilter meshFilter in skinnedMeshInfo.Value)
			{
				numSubs += meshFilter.sharedMesh.subMeshCount;
			}

			int[] meshIndex = new int[numSubs];
			int boneOffset = 0;
			
			for( int i = 0; i < skinnedMeshInfo.Value.Count; i++ ) 
			{
				MeshFilter meshFilter = skinnedMeshInfo.Value[i];
				int vertCount = meshFilter.sharedMesh.vertexCount;
				for(int j = 0; j < vertCount; j++)
				{
					BoneWeight bWeight = new BoneWeight();
					bWeight.boneIndex0 = boneOffset;
					bWeight.weight0 = 1;        
					boneWeights.Add( bWeight );
				}
				
				boneOffset += 1;
				
				Transform bone = meshFilter.transform;
				bones.Add( bone );

				CombineInstance ci = new CombineInstance();
				ci.mesh = meshFilter.sharedMesh;
				ci.transform = meshFilter.transform.localToWorldMatrix;

				combineInstances.Add( ci );
				DestroyImmediate(meshFilter.renderer);
				DestroyImmediate(meshFilter);
			}

			List<Matrix4x4> bindposes = new List<Matrix4x4>();
			GameObject skinnedMeshObject = new GameObject("SkinnedMesh_"+counter,typeof(SkinnedMeshRenderer));	

			for( int b = 0; b < bones.Count; b++ ) 
			{
				bindposes.Add( bones[b].worldToLocalMatrix * skinnedMeshObject.transform.worldToLocalMatrix );
			}

			SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshObject.GetComponent<SkinnedMeshRenderer>();
			Mesh mesh = new Mesh();
			mesh.CombineMeshes( combineInstances.ToArray(),true );
			skinnedMeshRenderer.sharedMaterial = skinnedMeshInfo.Key;
			skinnedMeshRenderer.bones = bones.ToArray();
			mesh.boneWeights = boneWeights.ToArray();
			mesh.bindposes = bindposes.ToArray();
			mesh.RecalculateBounds();
			mesh.Optimize();
			skinnedMeshRenderer.sharedMesh = SaveMesh(mesh,(savedName + "_" + counter), folderName);
			skinnedMeshObjs.Add(skinnedMeshObject);
			counter++;
		}
		return skinnedMeshObjs;
	}
}
