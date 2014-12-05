using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using EB.Rendering;

[CustomEditor(typeof(EBLensFlare))]
class LensFlareInspector : Editor
{
	public override void OnInspectorGUI()  
	{
		EBLensFlare lensFlareSettings = (EBLensFlare)target;
		NGUIEditorTools.BeginContents();
		lensFlareSettings._TextureIndicies.Clear();
		for(int j = 0; j < 10; j++)
		{
			lensFlareSettings._TextureIndicies.Add(j.ToString());
		}
		if(Application.isPlaying)
		{
			GUILayout.Label("Flare Quality: " + lensFlareSettings.GetQuality());
		}
		else
		{
			lensFlareSettings._FlareQuality = (EBLensFlare.eFLARE_QUALITY)EditorGUILayout.EnumPopup("Flare Quality",lensFlareSettings._FlareQuality);
		}
		lensFlareSettings._ShowGizmos = EditorGUILayout.Toggle("Show Gizmos",lensFlareSettings._ShowGizmos);
		lensFlareSettings._TextureFlare = (Texture)EditorGUILayout.ObjectField("Flare Texture",lensFlareSettings._TextureFlare, typeof(Texture));
		lensFlareSettings._FadeDuration = EditorGUILayout.FloatField("Fade Time",lensFlareSettings._FadeDuration);

		int i = 0;
		EBLensFlare.EB_Flare toRemove = null;
		foreach(var f in lensFlareSettings.flares)
		{
			i++;
			GUI.color = Color.white;
			GUILayout.Label("Flare: " + i);
			f.distance = EditorGUILayout.FloatField("Flare Distance",f.distance);
			f.scale = EditorGUILayout.FloatField("Flare Size",f.scale);
			f.imageIndex = EditorGUILayout.Popup("Image Index",f.imageIndex, lensFlareSettings._TextureIndicies.ToArray());
			f.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh",f.mesh, typeof(Mesh));
			f.color = EditorGUILayout.ColorField("Color",f.color);
			f.rotationOffset = EditorGUILayout.FloatField("Rotational Offset (Degrees",f.rotationOffset);
			f.enableRotation = EditorGUILayout.Toggle("Enable Rotation",f.enableRotation);
			GUI.color = Color.red;
			if(GUILayout.Button("Remove", GUILayout.Width(100)))
			{
				toRemove = f;
			}
		}
		if(toRemove != null)
		{
			lensFlareSettings.flares.Remove(toRemove);
		}
		GUI.color = Color.green;
		if(lensFlareSettings.flares.Count < lensFlareSettings._highFlareCount)
		{
			if(GUILayout.Button("Add"))
			{
				lensFlareSettings.flares.Add(new EBLensFlare.EB_Flare());
			}
		}
		else
		{
			GUILayout.Label("Max number of flares reached");
		}
		lensFlareSettings.UpdateMesh();
		NGUIEditorTools.EndContents();
		EditorUtility.SetDirty(target);
	}
}