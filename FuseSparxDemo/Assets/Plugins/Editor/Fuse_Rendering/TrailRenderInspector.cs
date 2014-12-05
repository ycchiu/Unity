using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using EB.Rendering;

[CustomEditor(typeof(GenericTrailRendererInstance))]
class TrailRenderInspector : Editor
{
	SerializedProperty colorGradient;
	SerializedObject serializedGradient;

	SerializedProperty lifeGradient;

	public void OnEnable() 
	{
		GameObject tempObject = GameObject.Find("GradientContainter");
		if(tempObject == null)
		{
			tempObject = new GameObject();
			tempObject.name = "GradientContainter";
		}
		tempObject.hideFlags = HideFlags.DontSave;
		GradientContainer gradientContainer = (GradientContainer) tempObject.GetComponent<GradientContainer>();
		if(gradientContainer == null) 
		{
			gradientContainer = (GradientContainer) tempObject.AddComponent("GradientContainer");
		}

		GenericTrailRendererInstance trailSettings = (GenericTrailRendererInstance)target;
		gradientContainer.ColorGradient = trailSettings._ColorGradient;
		
		serializedGradient = new SerializedObject(gradientContainer);
		colorGradient = serializedGradient.FindProperty("ColorGradient");

		gradientContainer.LifeGradient = trailSettings._LifeGradient;
		
		lifeGradient = serializedGradient.FindProperty("LifeGradient");
	}

	public override void OnInspectorGUI()  
	{
		GenericTrailRendererInstance trailSettings = (GenericTrailRendererInstance)target;

		if(NGUIEditorTools.DrawHeader("Global Settings"))
		{
			NGUIEditorTools.BeginContents();
			trailSettings._AutoPlay = EditorGUILayout.Toggle("Auto Play", trailSettings._AutoPlay);
			trailSettings._IgnoreZ = EditorGUILayout.Toggle("Ignore Z", trailSettings._IgnoreZ);
			trailSettings._TrailLength = (TrailRendererManager.eTRAIL_LENGTH)EditorGUILayout.EnumPopup("Trail Size", trailSettings._TrailLength);
			trailSettings._DistanceThreshold = EditorGUILayout.FloatField("Distance Threshold)", trailSettings._DistanceThreshold);
			trailSettings._TimeUnits = (EB.Rendering.TrailRenderer.eTIME_UNITS)EditorGUILayout.EnumPopup("Time Units", trailSettings._TimeUnits);

			switch(trailSettings._TimeUnits)
			{
				case EB.Rendering.TrailRenderer.eTIME_UNITS.Seconds:
					trailSettings._TrailTime = EditorGUILayout.FloatField("Trail Length (seconds)", trailSettings._TrailTime);
					break;
				case EB.Rendering.TrailRenderer.eTIME_UNITS.Frames:
					trailSettings._TrailTimeInFrames = EditorGUILayout.IntField("Trail Length (frames)", trailSettings._TrailTimeInFrames);
					break;
			}

			trailSettings._WidthCurve = EditorGUILayout.CurveField("Width Curve", trailSettings._WidthCurve);
			trailSettings._Point1 = EditorGUILayout.ObjectField("Point One", trailSettings._Point1, typeof(GameObject), true) as GameObject;
			trailSettings._Point2 = EditorGUILayout.ObjectField("Point Two", trailSettings._Point2, typeof(GameObject), true) as GameObject;
			trailSettings._TextureRepeat = EditorGUILayout.FloatField("Texture length (meters)", trailSettings._TextureRepeat);
			trailSettings._TextureMetersSecond = EditorGUILayout.FloatField("Texture animate speed (meters/seconds)", trailSettings._TextureMetersSecond);
			trailSettings._TextureYSplit = EditorGUILayout.IntField("Vertical texture frames", trailSettings._TextureYSplit);
			trailSettings._SpanOverTrail = EditorGUILayout.Toggle("Color Gradient Over Trail", trailSettings._SpanOverTrail);
			trailSettings._AddColor = EditorGUILayout.Toggle("Life Gradient Add Color", trailSettings._AddColor);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(colorGradient, true, null);
			EditorGUILayout.PropertyField(lifeGradient, true, null);
			if(EditorGUI.EndChangeCheck())
			{
				serializedGradient.ApplyModifiedProperties();
			}
			trailSettings._Material = EditorGUILayout.ObjectField("Material", trailSettings._Material, typeof(Material), false) as Material;
			
			NGUIEditorTools.EndContents();
		}

		trailSettings._TrailType = (EB.Rendering.TrailRenderer.eTRAIL_TYPE)EditorGUILayout.EnumPopup("Trail Type", trailSettings._TrailType);

		switch (trailSettings._TrailType) 
		{
			case EB.Rendering.TrailRenderer.eTRAIL_TYPE.Uniform:
			{
				if(NGUIEditorTools.DrawHeader("Uniform Fade Settings"))
				{
					NGUIEditorTools.BeginContents();
					
					switch(trailSettings._TimeUnits)
					{
						case EB.Rendering.TrailRenderer.eTIME_UNITS.Seconds:
							trailSettings._FadeStartTime = EditorGUILayout.FloatField("Fade Start Time (seconds)", trailSettings._FadeStartTime);
							trailSettings._FadeDuration = EditorGUILayout.FloatField("Fade Duration (seconds)", trailSettings._FadeDuration);
							break;
						case EB.Rendering.TrailRenderer.eTIME_UNITS.Frames:
							trailSettings._FadeStartTimeInFrames = EditorGUILayout.IntField("Fade Start Time (frames)", trailSettings._FadeStartTimeInFrames);
							trailSettings._FadeDurationInFrames = EditorGUILayout.IntField("Fade Duration (frames)", trailSettings._FadeDurationInFrames);
							break;
					}
					
					NGUIEditorTools.EndContents();
				}
				break;
			}

			case EB.Rendering.TrailRenderer.eTRAIL_TYPE.Catchup:
			{
				if(NGUIEditorTools.DrawHeader("Catchup Fade Settings"))
				{
					NGUIEditorTools.BeginContents();

					switch(trailSettings._TimeUnits)
					{
						case EB.Rendering.TrailRenderer.eTIME_UNITS.Seconds:
							trailSettings._FadeStartTime = EditorGUILayout.FloatField("Catchup Start Time (seconds)", trailSettings._FadeStartTime);
							trailSettings._FadeDuration = EditorGUILayout.FloatField("Catchup Duration (seconds)", trailSettings._FadeDuration);
							break;
						case EB.Rendering.TrailRenderer.eTIME_UNITS.Frames:
							trailSettings._FadeStartTimeInFrames = EditorGUILayout.IntField("Catchup Start Time (frames)", trailSettings._FadeStartTimeInFrames);
							trailSettings._FadeDurationInFrames = EditorGUILayout.IntField("Catchup Duration (frames)", trailSettings._FadeDurationInFrames);
							break;
					}
					
					NGUIEditorTools.EndContents();
				}
				break;
			}
			case EB.Rendering.TrailRenderer.eTRAIL_TYPE.Drag:
			{
				if(NGUIEditorTools.DrawHeader("Catchup Fade Settings"))
				{
					NGUIEditorTools.BeginContents();
					
					switch(trailSettings._TimeUnits)
					{
					case EB.Rendering.TrailRenderer.eTIME_UNITS.Seconds:
						trailSettings._FadeDuration = EditorGUILayout.FloatField("FadeIn Duration (seconds)", trailSettings._FadeDuration);
						break;
					case EB.Rendering.TrailRenderer.eTIME_UNITS.Frames:
						trailSettings._FadeDurationInFrames = EditorGUILayout.IntField("FadeIn Duration (frames)", trailSettings._FadeDurationInFrames);
						break;
					}
					
					NGUIEditorTools.EndContents();
				}
				break;
			}
			default: 
			{
				EB.Debug.LogError("Bad trail type in inspector");
				break;
			}
		}

		if(trailSettings._TimeUnits == EB.Rendering.TrailRenderer.eTIME_UNITS.Frames) 
		{
			trailSettings.ConvertFramesToSeconds();
		}
		else 
		{
			trailSettings.ConvertSecondsToFrames();
		}

		GradientContainer gradientColorContainer = (GradientContainer)serializedGradient.targetObject;
		trailSettings._ColorGradient = gradientColorContainer.ColorGradient;

		GradientContainer gradientLifeContainer = (GradientContainer)serializedGradient.targetObject;
		trailSettings._LifeGradient = gradientLifeContainer.LifeGradient;

        EditorUtility.SetDirty(target);
	}
}