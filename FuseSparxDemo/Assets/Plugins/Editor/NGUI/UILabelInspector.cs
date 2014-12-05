//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_FLASH
#define DYNAMIC_FONT
#endif

using UnityEngine;
using UnityEditor;

/// <summary>
/// Inspector class used to edit UILabels.
/// </summary>

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UILabel))]
#else
[CustomEditor(typeof(UILabel), true)]
#endif
public class UILabelInspector : UIWidgetInspector
{
	public enum FontType
	{
		NGUI,
		Unity,
		Custom,
		// EBG START
		EBUI_FontSelector
		// EBG END
	}

	UILabel mLabel;
	FontType mFontType;

	protected override void OnEnable ()
	{
		base.OnEnable();
		SerializedProperty bit = serializedObject.FindProperty("mFont");
		mFontType = (bit != null && bit.objectReferenceValue != null) ? FontType.NGUI : FontType.Unity;

		// EBG START
		SerializedProperty efs = serializedObject.FindProperty("mSelectorFont");
		SerializedProperty cf = serializedObject.FindProperty("mCustomFont");
		if (efs != null && efs.objectReferenceValue != null)
		{
			mFontType = FontType.EBUI_FontSelector;
		}
		if (cf != null && cf.objectReferenceValue != null)
		{
			mFontType = FontType.Custom;
		}
		// EBG END
	}

	void OnNGUIFont (Object obj)
	{
		serializedObject.Update();
		SerializedProperty sp = serializedObject.FindProperty("mFont");
		sp.objectReferenceValue = obj;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}

	void OnUnityFont (Object obj)
	{
		serializedObject.Update();
		SerializedProperty sp = serializedObject.FindProperty("mTrueTypeFont");
		sp.objectReferenceValue = obj;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}
	
	// EBG START
	void OnEBSelectorFont(Object obj)
	{
		serializedObject.Update();
		SerializedProperty sp = serializedObject.FindProperty("mSelectorFont");
		sp.objectReferenceValue = obj;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}

	void OnCustomFont(Object obj)
	{
		serializedObject.Update();
		SerializedProperty sp = serializedObject.FindProperty("mCustomFont");
		sp.objectReferenceValue = obj;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}
	// EBG END
	
	/// <summary>
	/// Draw the label's properties.
	/// </summary>

	protected override bool ShouldDrawProperties ()
	{
		mLabel = mWidget as UILabel;

		// EBG START - Performance testing
		if (Application.isPlaying)
		{
			GUI.color = Color.red;
			GUILayout.Label("Label Processed: " + mLabel.timesProcessed + " times");
			GUI.color = Color.white;
		}
		// EBG END - Performance testing
		
		GUILayout.BeginHorizontal();

#if DYNAMIC_FONT
		mFontType = (FontType)EditorGUILayout.EnumPopup(mFontType, "DropDown", GUILayout.Width(74f));
		if (NGUIEditorTools.DrawPrefixButton("Font", GUILayout.Width(64f)))
#else
		mFontType = FontType.NGUI;
		if (NGUIEditorTools.DrawPrefixButton("Font", GUILayout.Width(74f)))
#endif
		{
			if (mFontType == FontType.NGUI)
			{
				ComponentSelector.Show<UIFont>(OnNGUIFont);
			}
			// EBG START
			else if (mFontType == FontType.Custom)
			{
				ComponentSelector.Show<CustomFontBase>(OnCustomFont);
			}
			else if (mFontType == FontType.EBUI_FontSelector)
			{
				ComponentSelector.Show<FontSelector>(OnEBSelectorFont);
			}
			// EBG END
			else
			{
				ComponentSelector.Show<Font>(OnUnityFont, new string[] { ".ttf", ".otf" });
			}
		}

		bool isValid = false;
		SerializedProperty fnt = null;
		SerializedProperty ttf = null;
		// EBG START
		SerializedProperty cf = null;
		SerializedProperty efs = null;
		// EBG END
		
		if (mFontType == FontType.NGUI)
		{
			fnt = NGUIEditorTools.DrawProperty("", serializedObject, "mFont", GUILayout.MinWidth(40f));
			
			if (fnt.objectReferenceValue != null)
			{
				NGUISettings.ambigiousFont = fnt.objectReferenceValue;
				isValid = true;
			}
		}
		// EBG START
		else if (mFontType == FontType.Custom)
		{
			cf = NGUIEditorTools.DrawProperty("", serializedObject, "mCustomFont", GUILayout.MinWidth (40f));
			if (cf.objectReferenceValue != null)
			{
				NGUISettings.ambigiousFont = cf.objectReferenceValue;
				isValid = true;
			}
		}
		else if (mFontType == FontType.EBUI_FontSelector)
		{
			efs = NGUIEditorTools.DrawProperty("", serializedObject, "mSelectorFont", GUILayout.MinWidth(40f));
			
			if (efs.objectReferenceValue != null)
			{
				NGUISettings.ambigiousFont = efs.objectReferenceValue;
				isValid = true;
			}
		}
		// EBG END
		else
		{
			ttf = NGUIEditorTools.DrawProperty("", serializedObject, "mTrueTypeFont", GUILayout.MinWidth(40f));

			if (ttf.objectReferenceValue != null)
			{
				NGUISettings.ambigiousFont = ttf.objectReferenceValue;
				isValid = true;
			}
		}

		GUILayout.EndHorizontal();
		
		// EBG START
		if (efs != null)
		{
			FontSelector selector = efs.objectReferenceValue as FontSelector;
			if (selector != null)
			{
				string fontName = (selector.selectedFont != null) ? selector.selectedFont.name : (selector.selectedTrueTypeFont != null ? selector.selectedTrueTypeFont.name : "None");
				GUILayout.Label ("Active Font: " + fontName);
			}
		}
		else if (cf != null)
		{
			CustomFontBase customFont = cf.objectReferenceValue as CustomFontBase;
			if (customFont != null)
			{
				string fontName = customFont.isBitmap ? customFont.bitmapFont.name : (customFont.isTrueType ? customFont.trueTypeFont.name : "None");
				GUILayout.Label("Active Font: " + fontName);
			}
		}
		// EBG END
		
		EditorGUI.BeginDisabledGroup(!isValid);
		{
			UIFont uiFont = (fnt != null) ? fnt.objectReferenceValue as UIFont : null;
			Font dynFont = (ttf != null) ? ttf.objectReferenceValue as Font : null;
			// EBG START
			FontSelector efsFont = (efs != null) ? efs.objectReferenceValue as FontSelector : null;
			CustomFontBase cfFont = (cf != null) ? cf.objectReferenceValue as CustomFontBase : null;
			// EBG END
			
			if (uiFont != null && uiFont.isDynamic)
			{
				dynFont = uiFont.dynamicFont;
				uiFont = null;
			}

			// EBG START - Resolution toggle.  This isn't serialized by the UILabel, and will modify the current resolution used by UIResolutionManager!
			if ((dynFont != null || efsFont != null || cfFont != null) && UIResolutionManager.Instance != null)
			{
				GUILayout.BeginHorizontal();
				{
					UIResolutionManager.Resolution initialRes = UIResolutionManager.Instance.CurrentResolution;
					UIResolutionManager.Resolution res = (UIResolutionManager.Resolution)EditorGUILayout.EnumPopup("Resolution", initialRes);
					if (res != initialRes)
					{
						UIResolutionManager.Instance.SwitchResolution(res);
						(target as UILabel).MarkAsChanged();
					}
					GUILayout.Space(18f);
				}
				GUILayout.EndHorizontal();
			}
			// EBG END

			if (dynFont != null)
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup((ttf != null) ? ttf.hasMultipleDifferentValues : fnt.hasMultipleDifferentValues);
					
					SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));
					NGUISettings.fontSize = prop.intValue;
					
					prop = NGUIEditorTools.DrawProperty("", serializedObject, "mFontStyle", GUILayout.MinWidth(40f));
					NGUISettings.fontStyle = (FontStyle)prop.intValue;
					
					GUILayout.Space(18f);
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();

				NGUIEditorTools.DrawProperty("Material", serializedObject, "mMaterial");
			}
			// EBG START
			else if (cfFont != null)
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(cf.hasMultipleDifferentValues);
					
					SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));
					NGUISettings.fontSize = prop.intValue;
					
					prop = NGUIEditorTools.DrawProperty("", serializedObject, "mFontStyle", GUILayout.MinWidth(40f));
					NGUISettings.fontStyle = (FontStyle)prop.intValue;
					
					GUILayout.Space(18f);
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();
				
				NGUIEditorTools.DrawProperty("Material", serializedObject, "mMaterial");
			}
			else if (efsFont != null)
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(efs.hasMultipleDifferentValues);
					
					SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));
					NGUISettings.fontSize = prop.intValue;
					
					prop = NGUIEditorTools.DrawProperty("", serializedObject, "mFontStyle", GUILayout.MinWidth(40f));
					NGUISettings.fontStyle = (FontStyle)prop.intValue;
					
					GUILayout.Space(18f);
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();

				NGUIEditorTools.DrawProperty("Material", serializedObject, "mMaterial");
			}
			// EBG END
			else if (uiFont != null)
			{
				GUILayout.BeginHorizontal();
				SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));

				EditorGUI.BeginDisabledGroup(true);
				if (!serializedObject.isEditingMultipleObjects)
					GUILayout.Label(" Default: " + mLabel.defaultFontSize);
				EditorGUI.EndDisabledGroup();

				NGUISettings.fontSize = prop.intValue;
				GUILayout.EndHorizontal();
			}

			bool ww = GUI.skin.textField.wordWrap;
			GUI.skin.textField.wordWrap = true;
			SerializedProperty sp = serializedObject.FindProperty("mText");
#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2
			GUI.changed = false;
			string text = EditorGUILayout.TextArea(sp.stringValue, GUI.skin.textArea, GUILayout.Height(100f));
			if (GUI.changed) sp.stringValue = text;
#else
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2
			GUILayout.Space(-16f);
#endif
			if (sp.hasMultipleDifferentValues)
			{
				NGUIEditorTools.DrawProperty("", sp, GUILayout.Height(128f));
			}
			else
			{
				GUIStyle style = new GUIStyle(EditorStyles.textField);
				style.wordWrap = true;

				float height = style.CalcHeight(new GUIContent(sp.stringValue), Screen.width - 100f);
				bool offset = true;

				if (height > 90f)
				{
				    offset = false;
				    height = style.CalcHeight(new GUIContent(sp.stringValue), Screen.width - 20f);
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical(GUILayout.Width(76f));
					GUILayout.Space(3f);
					GUILayout.Label("Text");
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
				}
				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));

				GUI.changed = false;
				string text = EditorGUI.TextArea(rect, sp.stringValue, style);
				if (GUI.changed) sp.stringValue = text;

				if (offset)
				{
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				}
			}
#endif
			GUI.skin.textField.wordWrap = ww;

			SerializedProperty ov = NGUIEditorTools.DrawPaddedProperty("Overflow", serializedObject, "mOverflow");
			NGUISettings.overflowStyle = (UILabel.Overflow)ov.intValue;

			NGUIEditorTools.DrawPaddedProperty("Alignment", serializedObject, "mAlignment");
			
			// EBG START
			if (dynFont != null || (efsFont != null && efsFont.selectedTrueTypeFont != null) || (cfFont != null && cfFont.trueTypeFont != null))
			// EBG END
				NGUIEditorTools.DrawPaddedProperty("Keep crisp", serializedObject, "keepCrispWhenShrunk");

			EditorGUI.BeginDisabledGroup(mLabel.bitmapFont != null && mLabel.bitmapFont.packedFontShader);
			GUILayout.BeginHorizontal();
			SerializedProperty gr = NGUIEditorTools.DrawProperty("Gradient", serializedObject, "mApplyGradient",
#if UNITY_3_5
				GUILayout.Width(93f));
#else
				GUILayout.Width(95f));
#endif
			EditorGUI.BeginDisabledGroup(!gr.hasMultipleDifferentValues && !gr.boolValue);
			{
				NGUIEditorTools.SetLabelWidth(30f);
				NGUIEditorTools.DrawProperty("Top", serializedObject, "mGradientTop", GUILayout.MinWidth(40f));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				NGUIEditorTools.SetLabelWidth(50f);
#if UNITY_3_5
				GUILayout.Space(81f);
#else
				GUILayout.Space(79f);
#endif
				NGUIEditorTools.DrawProperty("Bottom", serializedObject, "mGradientBottom", GUILayout.MinWidth(40f));
				NGUIEditorTools.SetLabelWidth(80f);
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Effect", GUILayout.Width(76f));
			sp = NGUIEditorTools.DrawProperty("", serializedObject, "mEffectStyle", GUILayout.MinWidth(16f));

			EditorGUI.BeginDisabledGroup(!sp.hasMultipleDifferentValues && !sp.boolValue);
			{
				NGUIEditorTools.DrawProperty("", serializedObject, "mEffectColor", GUILayout.MinWidth(10f));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(" ", GUILayout.Width(56f));
					NGUIEditorTools.SetLabelWidth(20f);
					NGUIEditorTools.DrawProperty("X", serializedObject, "mEffectDistance.x", GUILayout.MinWidth(40f));
					NGUIEditorTools.DrawProperty("Y", serializedObject, "mEffectDistance.y", GUILayout.MinWidth(40f));
					GUILayout.Space(18f);
					NGUIEditorTools.SetLabelWidth(80f);
				}
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Spacing", GUILayout.Width(56f));
			NGUIEditorTools.SetLabelWidth(20f);
			NGUIEditorTools.DrawProperty("X", serializedObject, "mSpacingX", GUILayout.MinWidth(40f));
			NGUIEditorTools.DrawProperty("Y", serializedObject, "mSpacingY", GUILayout.MinWidth(40f));
			GUILayout.Space(18f);
			NGUIEditorTools.SetLabelWidth(80f);
			GUILayout.EndHorizontal();

			NGUIEditorTools.DrawProperty("Max Lines", serializedObject, "mMaxLineCount", GUILayout.Width(110f));

			GUILayout.BeginHorizontal();
			sp = NGUIEditorTools.DrawProperty("BBCode", serializedObject, "mEncoding", GUILayout.Width(100f));
			EditorGUI.BeginDisabledGroup(!sp.boolValue || mLabel.bitmapFont == null || !mLabel.bitmapFont.hasSymbols);
			NGUIEditorTools.SetLabelWidth(60f);
			NGUIEditorTools.DrawPaddedProperty("Symbols", serializedObject, "mSymbols");
			NGUIEditorTools.SetLabelWidth(80f);
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
		}
		EditorGUI.EndDisabledGroup();
		return isValid;
	}
}
