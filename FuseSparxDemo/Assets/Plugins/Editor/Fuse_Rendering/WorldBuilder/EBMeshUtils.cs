using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class EBMeshUtils
{
	public enum CHANNEL
	{
		UV1,
		UV2,
		NORMAL,
		TANGENT,
		COLOR,
		COUNT
	};

	const int CHANNEL_COUNT = 5;

	public class Channels
	{
		public bool[] channels;

		public Channels(bool allChannels = true)
		{
			channels = new bool[(int)CHANNEL.COUNT];
			for (int i = 0; i < (int)CHANNEL.COUNT; ++i)
			{
				channels[i] = allChannels;
			}
		}

		public Channels(CHANNEL[] c)
		{
			channels = new bool[(int)CHANNEL.COUNT];
			foreach(CHANNEL channel in c)
			{
				channels[(int)channel] = true;
			}
		}

		public bool Has(CHANNEL channel)
		{
			return channels[(int)channel];
		}
	}
	
	//Splits the mesh into two parts along the plane.
	//Notes
	//- no new geomtery is generated; triangles are simply moved into the part based of the side of the plane they most fall in.
	//- if the plane doesn't interesect the mesh then the second mesh is returned as null.
	public static Mesh[] Split(Mesh mesh, Plane splitPlane, bool optimizeAfterSplit = true)
	{	
		var bounds = mesh.bounds;
		var minTest = splitPlane.GetSide(bounds.min);
		var maxTest = splitPlane.GetSide(bounds.max);
		if (minTest == maxTest)
		{
			Debug.LogError("Plane doesn't interesect mesh");
			return new [] { Clone(mesh), null };
		}
		
		List<int>[] meshAIndices = new List<int>[mesh.subMeshCount];
		List<int>[] meshBIndices = new List<int>[mesh.subMeshCount];
		
		var vertices = mesh.vertices;
		
		var usedA = new bool[mesh.vertexCount];
		var usedB = new bool[mesh.vertexCount];
		
		for ( var s = 0; s < mesh.subMeshCount; ++s )
		{
			meshAIndices[s] = new List<int>();
			meshBIndices[s] = new List<int>();
			
			var indices = mesh.GetIndices(s);
		
			// figure out what triangles fall into which part
			
			for ( var i = 0; i < indices.Length; i += 3 )
			{
				var index1 = indices[i+0];
				var index2 = indices[i+1];
				var index3 = indices[i+2];
				
				var triangleCenter = (vertices[index1] + vertices[index2] + vertices[index3]) / 3.0f;
				
				if (splitPlane.GetSide(triangleCenter))
				{
					meshAIndices[s].Add(index1);
					meshAIndices[s].Add(index2);
					meshAIndices[s].Add(index3);
					usedA[index1] = true;
					usedA[index2] = true;
					usedA[index3] = true;
				}
				else
				{
					meshBIndices[s].Add(index1);
					meshBIndices[s].Add(index2);
					meshBIndices[s].Add(index3);
					usedB[index1] = true;
					usedB[index2] = true;
					usedB[index3] = true;
				}
			}
		}
		
		//create the two meshes, cloning the various streams
		
		var meshA = Clone(mesh);
		var meshB = Clone(mesh);
		
		for ( var s = 0; s < mesh.subMeshCount; ++s )
		{
			meshA.SetIndices(meshAIndices[s].ToArray(), MeshTopology.Triangles, s);
			meshB.SetIndices(meshBIndices[s].ToArray(), MeshTopology.Triangles, s);
		}
		
		if (optimizeAfterSplit)
		{
			//get rid of the elemtents that aren't in the part anymore becuase of our split
			meshA = StripUnusedVertices(meshA);
			meshB = StripUnusedVertices(meshB);
			
			//optimize
			meshA = Optimize(meshA);
			meshB = Optimize(meshB);
		}
		
		return new [] { meshA, meshB };
	}

	public static Mesh Clone(Mesh mesh)
	{
		return StripStreams(mesh, new Channels(true));
	}
	
	//Strips whatever streams for the mesh that you don't want, returning a new mesh
	public static Mesh StripStreams(Mesh mesh, Channels channels)
	{
		Mesh newMesh = new Mesh();
		
		newMesh.vertices = (Vector3[])mesh.vertices.Clone();
		
		for (var s = 0; s < mesh.subMeshCount; ++s)
		{
			newMesh.SetIndices( (int[])mesh.GetIndices(s).Clone(), MeshTopology.Triangles, s );
		}
		
		if (channels.Has(CHANNEL.COLOR) && (mesh.colors != null) && (mesh.colors.Length > 0))
		{
			newMesh.colors = (Color[])mesh.colors.Clone();
		}
		
		if (channels.Has(CHANNEL.UV1) && (mesh.uv != null) && (mesh.uv.Length > 0))
		{
			newMesh.uv = (Vector2[])mesh.uv.Clone();
		}
		
		if (channels.Has(CHANNEL.UV2) && (mesh.uv != null) && (mesh.uv2.Length > 0))
		{
			newMesh.uv2 = (Vector2[])mesh.uv2.Clone();
		}
		
		if (channels.Has(CHANNEL.NORMAL) && (mesh.normals != null) && (mesh.normals.Length > 0))
		{
			newMesh.normals = (Vector3[])mesh.normals.Clone();
		}
		
		if (channels.Has(CHANNEL.TANGENT) && (mesh.tangents != null) && (mesh.tangents.Length > 0))
		{
			newMesh.tangents = (Vector4[])mesh.tangents.Clone();
		}
		
		return newMesh;
	}
	
	//Finds any duplicate verts (because of hard edges, say) and collapses them down. 
	//Note that you want to remove the streams you don't want with RemoveStreamsFromMesh () first, for optimal collapsing
	public static Mesh Optimize(Mesh mesh)
	{
		bool hasColors = (mesh.colors != null) && (mesh.colors.Length > 0);
		bool hasNormals = (mesh.normals != null) && (mesh.normals.Length > 0);
		bool hasUV1s = (mesh.uv != null) && (mesh.uv.Length > 0);
		bool hasUV2s = (mesh.uv2 != null) && (mesh.uv2.Length > 0);
		bool hasTangents = (mesh.tangents != null) && (mesh.tangents.Length > 0);
			
		var verts = mesh.vertices;
		var colors = mesh.colors;
		var uv1s = mesh.uv;
		var uv2s = mesh.uv2;
		var normals = mesh.normals;
		var tangents = mesh.tangents;
		
		//find any duplicates we can collpase
		
		bool[] duplicateVertices = new bool[mesh.vertexCount];
		int[] indexRemap = new int[mesh.vertexCount];
		int duplicate = 0;
		
		for (var x = 0; x < mesh.vertexCount - 1; ++x)
		{
			if (duplicateVertices[x])
			{
				continue;
			}
			for (var y = x + 1; y < mesh.vertexCount; ++y)
			{	
				if ((verts[x] == verts[y]) &&
					(!hasUV1s || (uv1s[x] == uv1s[y])) &&
					(!hasUV2s || (uv2s[x] == uv2s[y])) &&
					(!hasNormals || (normals[x] == normals[y])) &&
					(!hasTangents || (tangents[x] == tangents[y])) && 
					(!hasColors || (colors[x] == colors[y])))
				{
					duplicateVertices[y] = true;
					indexRemap[y] = x; //first we remap to point the duplicate vertice at the original one
					duplicate += 1;
				}
			}
		}
		
		int[] indexRemap2 = new int[mesh.vertexCount];
		
		//now we remap the already remapped things, to account for the 'holes' from the verts we are removing. OH GOD.
		int remap = 0;
		for ( var i = 0; i < mesh.vertexCount; ++i )
		{
			if (!duplicateVertices[i])
			{
				indexRemap2[i] = remap;
				for( var j = 0; j < mesh.vertexCount; ++j )
				{
					if (indexRemap[j] == i)
					{
						indexRemap2[j] = remap;
					}
				}
				remap += 1;
			}
		}
		
		//Debug.LogError("Collapsed " + duplicate + " vertices of " + mesh.vertexCount + " in " + mesh.name);

		mesh = StripVerts(mesh, duplicateVertices, indexRemap2);
		mesh = StripUnusedVertices(mesh);
		mesh.Optimize();
		return mesh;
	}
	
	static Mesh StripVerts(Mesh mesh, bool[] toRemove, int[] indexRemap)
	{
		bool hasColors = (mesh.colors != null) && (mesh.colors.Length > 0);
		bool hasNormals = (mesh.normals != null) && (mesh.normals.Length > 0);
		bool hasUV1s = (mesh.uv != null) && (mesh.uv.Length > 0);
		bool hasUV2s = (mesh.uv2 != null) && (mesh.uv2.Length > 0);
		bool hasTangents = (mesh.tangents != null) && (mesh.tangents.Length > 0);
		
		var verticesList = new List<Vector3>(mesh.vertices);
		var colorsList = new List<Color>(mesh.colors);
		var normalsList = new List<Vector3>(mesh.normals);
		var uv1sList = new List<Vector2>(mesh.uv);
		var uv2sList = new List<Vector2>(mesh.uv2);
		var tangentsList = new List<Vector4>(mesh.tangents);
		
		for ( var i = mesh.vertexCount - 1; i >= 0; --i )
		{
			if (toRemove[i])
			{
				verticesList.RemoveAt(i);
				if (hasColors) { colorsList.RemoveAt(i); }
				if (hasNormals) { normalsList.RemoveAt(i); }
				if (hasUV1s) { uv1sList.RemoveAt(i); }
				if (hasUV2s) { uv2sList.RemoveAt(i); }
				if (hasTangents) { tangentsList.RemoveAt(i); }
			}
		}
		
		//create the new mesh with what we stripped down
		
		var newMesh = new Mesh();
		
		newMesh.vertices = verticesList.ToArray();
		
		if (hasColors)
		{
			newMesh.colors = colorsList.ToArray();
		}
		
		if (hasNormals)
		{
			newMesh.normals = normalsList.ToArray();
		}
		
		if (hasUV1s)
		{
			newMesh.uv = uv1sList.ToArray();
		}
		
		if (hasUV2s)
		{
			newMesh.uv2 = uv2sList.ToArray();
		}
		
		if (hasTangents)
		{
			newMesh.tangents = tangentsList.ToArray();
		}
		
		//remap the indices
		
		List<int>[] meshIndices = new List<int>[mesh.subMeshCount];
		
		for ( var s = 0; s < mesh.subMeshCount; ++s )
		{
			meshIndices[s] = new List<int>(mesh.GetIndices(s));
			for ( var i = 0; i < meshIndices[s].Count; ++i )
			{
				meshIndices[s][i] = indexRemap[meshIndices[s][i]];
			}
			newMesh.SetIndices(meshIndices[s].ToArray(), MeshTopology.Triangles, s);
		}
		
		return newMesh;
	}

	//Walks the mesh and removes any vertices, normals, etc. that are no longer used in the index buffer (due to pulling out a submesh, or splitting, say).
	//Can also be used to outright strip out any streams (tangents, etc.) that we don't want
	public static Mesh StripUnusedVertices(Mesh mesh)
	{
		//figure out what vertices we actually use
		
		List<int>[] meshIndices = new List<int>[mesh.subMeshCount];
		
		var usedVertices = new bool[mesh.vertexCount];
		
		for ( var s = 0; s < mesh.subMeshCount; ++s )
		{
			var indices = mesh.GetIndices(s);
			
			meshIndices[s] = new List<int>(indices);
			
			for ( var i = 0; i < indices.Length; i += 1 )
			{
				usedVertices[indices[i]] = true;
			}
		}
		
		var unusedVertices = new bool[usedVertices.Length];
		for (var i = 0; i < usedVertices.Length; ++i)
		{
			unusedVertices[i] = !usedVertices[i];
		}
		
		//reindex the index buffer, based off the removed verts for each part
		
		int[] indexRemap = new int[mesh.vertexCount];
		
		int remap = 0;
		for ( var i = 0; i < mesh.vertexCount; ++i )
		{
			if (usedVertices[i])
			{
				indexRemap[i] = remap;
				remap += 1;
			}
		}
		
		return StripVerts(mesh, unusedVertices, indexRemap);
	}
}


