//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UISprites.
/// </summary>

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIMaskedSprite))]
#else
[CustomEditor(typeof(UIMaskedSprite), true)]
#endif
public class UIMaskedSpriteInspector : UISpriteInspector
{
	protected override void DrawCustomProperties ()
	{
		GUILayout.Space(6f);

		UIMaskedSprite maskedSprite = target as UIMaskedSprite;
		if (maskedSprite != null)
		{
			maskedSprite.maskingTexture = EditorGUILayout.ObjectField("Masking Tex", maskedSprite.maskingTexture, typeof(UITexture), true) as UITexture;
		}

		base.DrawCustomProperties();
	}
}
