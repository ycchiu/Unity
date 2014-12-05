//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIMaskedTextureRef))]
#else
[CustomEditor(typeof(UIMaskedTextureRef), true)]
#endif
public class UIMaskedTextureRefInspector : UITextureRefInspector
{
	protected override void DrawCustomProperties()
	{
		GUILayout.Space(6f);
		
		UIMaskedTextureRef maskedTexture = target as UIMaskedTextureRef;
		if (maskedTexture != null)
		{
			maskedTexture.maskingTexture = EditorGUILayout.ObjectField("Masking Tex", maskedTexture.maskingTexture, typeof(UITexture), true) as UITexture;
		}
		
		base.DrawCustomProperties();
	}
}
