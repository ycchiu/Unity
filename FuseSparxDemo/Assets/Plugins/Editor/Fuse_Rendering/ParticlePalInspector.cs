using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using EB.Rendering;

[CustomEditor(typeof(ParticlePal))]
class ParticlePalInspector : Editor
{
    public override void OnInspectorGUI() 
	{
		ParticlePal pp = (ParticlePal)target;
		
		GUIStyle rightAlignLabelStyle = new GUIStyle("label");
		rightAlignLabelStyle.alignment = TextAnchor.MiddleRight;
		
		bool hasVelocity = false;
		
		if (pp.isEnabled == null || pp.isEnabled.Count != 3)
		{
			pp.isEnabled = new List<bool>() { true, true, true };
		}

		foreach( ParticlePal.QUALITY mode in System.Enum.GetValues(typeof(ParticlePal.QUALITY)))
		{
			if (mode == ParticlePal.QUALITY.Off)
				continue;

			pp.isEnabled[(int)mode] = EditorGUILayout.Toggle("Enable " + mode, pp.isEnabled[(int)mode]);
		}

		//list every condition
		for (var i = 0; i < pp.conditions.Count; ++i)
		{
			var condition = pp.conditions[i];
			
			hasVelocity = condition.trigger == ParticlePal.TRIGGER.Velocity;
			
			GUILayout.BeginHorizontal();
			condition.expanded = EditorGUILayout.Foldout(condition.expanded, condition.parameter.ToString() + " <- " + condition.trigger.ToString());
			if (GUILayout.Button("X", GUILayout.Width(25)))
			{
				#if UNITY_EDITOR
				pp.RemoveCondition(condition);
				#endif
			}
			GUILayout.EndHorizontal();
			
			if (!condition.expanded)
			{
				continue;
			}
			
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			condition.parameter = (ParticlePal.PARAMETER)EditorGUILayout.EnumPopup(condition.parameter);
			GUILayout.Label(" <- ");
			condition.trigger = (ParticlePal.TRIGGER)EditorGUILayout.EnumPopup(condition.trigger);
			GUILayout.EndHorizontal();
			
			if (condition.parameter == ParticlePal.PARAMETER.None)
			{
				GUILayout.Space(5);
				continue;
			}
			
			//list each variable for every condition
			for (var j = 0; j < ParticlePal.QUALITY_COUNT; ++j)
			{
				if (!pp.isEnabled[j])
				{
					continue;
				}

				var tuning = condition.tunings[j];
				
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				GUILayout.Label(((ParticlePal.QUALITY)j).ToString(), GUILayout.Width(50));

				if (condition.trigger == ParticlePal.TRIGGER.Constant)
				{
					if (condition.parameter == ParticlePal.PARAMETER.StartingColor)
					{
						tuning.constantColor = EditorGUILayout.ColorField(tuning.constantColor);
					}
					else
					{
						tuning.constant = EditorGUILayout.Slider(tuning.constant, 0.0f, 5000.0f);
					}
					GUILayout.EndHorizontal();
					continue;
				}
				
				bool isColor = (condition.parameter == ParticlePal.PARAMETER.StartingColor);
				
				GUILayout.BeginVertical();
					
				tuning.type = (ParticlePal.TUNING)EditorGUILayout.EnumPopup(tuning.type);
				
				switch (tuning.type)
				{
				case (ParticlePal.TUNING.Constant):
					if (isColor)
					{
						tuning.constantColor = EditorGUILayout.ColorField(tuning.constantColor);
					}
					else
					{
						tuning.constant = EditorGUILayout.Slider(tuning.constant, 0.0f, 500.0f);
					}
					break;
				case (ParticlePal.TUNING.Linear):
					GUILayout.BeginHorizontal();
					if (isColor)
					{
						tuning.minColor = EditorGUILayout.ColorField(tuning.minColor, GUILayout.Width(50));
					}
					else
					{
						tuning.minY = EditorGUILayout.FloatField(tuning.minY, GUILayout.Width(50));
					}
        			GUILayout.FlexibleSpace();
					GUILayout.Label(condition.parameter.ToString());
        			GUILayout.FlexibleSpace();
					if (isColor)
					{
						tuning.maxColor = EditorGUILayout.ColorField(tuning.maxColor, GUILayout.Width(50));
					}
					else
					{
						tuning.maxY = EditorGUILayout.FloatField(tuning.maxY, GUILayout.Width(50));
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					tuning.minX = EditorGUILayout.FloatField(tuning.minX, GUILayout.Width(50));
        			GUILayout.FlexibleSpace();
					GUILayout.Label(condition.trigger.ToString());
        			GUILayout.FlexibleSpace();
					tuning.maxX = EditorGUILayout.FloatField(tuning.maxX, GUILayout.Width(50));
					GUILayout.EndHorizontal();
					break;
				case (ParticlePal.TUNING.Curve):
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical();
					if (isColor)
					{
						tuning.maxColor = EditorGUILayout.ColorField(tuning.maxColor, GUILayout.Width(50));
					}
					else
					{
						tuning.maxY = EditorGUILayout.FloatField(tuning.maxY, GUILayout.Width(50));
					}
					GUILayout.Space(10);
					if (isColor)
					{
						tuning.minColor = EditorGUILayout.ColorField(tuning.minColor, GUILayout.Width(50));
					}
					else
					{
						tuning.minY = EditorGUILayout.FloatField(tuning.minY, GUILayout.Width(50));
					}
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					tuning.curve = EditorGUILayout.CurveField(tuning.curve, GUILayout.Height(50));
					GUILayout.BeginHorizontal();
					tuning.minX = EditorGUILayout.FloatField(tuning.minX, GUILayout.Width(50));
        			GUILayout.FlexibleSpace();
					GUILayout.Label(condition.trigger.ToString());
        			GUILayout.FlexibleSpace();
					tuning.maxX = EditorGUILayout.FloatField(tuning.maxX, GUILayout.Width(50));
					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					break;
				}
				
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
    
			GUILayout.Space(10);
		}
		
		if (hasVelocity)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Velocity Damping", GUILayout.Width(100));
			pp.VelocityDamping = EditorGUILayout.Slider(pp.VelocityDamping, 0.0f, 0.99f);
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
		}
		
		if (GUILayout.Button("Add Condition"))
		{
			#if UNITY_EDITOR
			pp.AddCondition();
			#endif
		}
		
        if (GUI.changed)
            EditorUtility.SetDirty (target);
    }
}