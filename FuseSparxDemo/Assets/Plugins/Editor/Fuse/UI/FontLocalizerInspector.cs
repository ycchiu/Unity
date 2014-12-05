using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FontLocalizer))]
public class FontLocalizerInspector : Editor
{
	enum FontType
	{
		Normal,
		Reference
	}
	
	FontType mType = FontType.Normal;
	FontLocalizer mFont;
	FontLocalizer mReplacement;
	
	void OnSelectReplacementFont(Object obj)
	{
		mFont.replacement = obj as FontLocalizer;
		mReplacement = mFont.replacement;
		UnityEditor.EditorUtility.SetDirty(mFont);
		if (mReplacement == null) mType = FontType.Normal;
		
		MarkAsChanged();
	}
	
	void MarkAsChanged()
	{
		System.Collections.Generic.List<UILabel> labels = NGUIEditorTools.FindAll<UILabel>();

		foreach (UILabel lbl in labels)
		{
			if (lbl.customFont == mFont)
			{
				lbl.customFont = null;
				lbl.customFont = mFont;
			}
		}
	}
	
	public override void OnInspectorGUI()
	{
		mFont = target as FontLocalizer;
		NGUIEditorTools.SetLabelWidth(80f);

		GUILayout.Space(6f);
		
		if (mFont.replacement != null)
		{
			mType = FontType.Reference;
			mReplacement = mFont.replacement;
		}
		
		GUILayout.BeginHorizontal();
		FontType fontType = (FontType)EditorGUILayout.EnumPopup("Font Type", mType);
		GUILayout.Space(18f);
		GUILayout.EndHorizontal();
		
		if (mType != fontType)
		{
			if (fontType == FontType.Normal)
			{
				OnSelectReplacementFont(null);
			}
			else
			{
				mType = fontType;
			}
		}
		
		if (mType == FontType.Reference)
		{
			ComponentSelector.Draw<FontLocalizer>(mFont.replacement, OnSelectReplacementFont, true);

			GUILayout.Space(6f);
			EditorGUILayout.HelpBox("You can have one font simply point to " +
				"another one. This is useful if you want to be " +
				"able to quickly replace the contents of one " +
				"font with another one, for example for " +
				"swapping between two different fonts for a given style.  " + 
				"All the labels referencing this font " +
				"will update their references to the new one.", MessageType.Info);

			if (mReplacement != mFont && mFont.replacement != mReplacement)
			{
				NGUIEditorTools.RegisterUndo("Font Change", mFont);
				mFont.replacement = mReplacement;
				UnityEditor.EditorUtility.SetDirty(mFont);
			}
			return;
		}
		else
		{
			DrawDefaultInspector();
		}
	}

	[MenuItem("EBG/Convert FontSelector to FontLocalizer")]
	public static void ConvertFontSelectorToFontLocalizer()
	{
		string [] files = System.IO.Directory.GetFiles("Assets/Resources/UI", "*.prefab", System.IO.SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; ++i)
		{
			FontLocalizerConverter_ProcessFile(files[i]);
		}

		for (int i = 0; i < files.Length; ++i)
		{
			FontLocalizerConverter_ProcessLabels(files[i]);
		}

		AssetDatabase.SaveAssets();
	}
	
	private static void FontLocalizerConverter_ProcessFile(string path)
	{
		if (string.IsNullOrEmpty(path)) return;
		
		GameObject go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
		if (go == null)
		{
			Debug.LogError ("Failed to load from file " + path);
			return;
		}

		FontSelector [] fontSelectors = EB.Util.FindAllComponents<FontSelector>(go);
		if (fontSelectors.Length > 0)
		{
			Debug.Log ("Converting FontSelectors to FontLocalizers in " + path);
			for (int i = 0; i < fontSelectors.Length; ++i)
			{
				FontSelector fs = fontSelectors[i];
				FontLocalizer fl = fs.gameObject.GetComponent<FontLocalizer>();
				if (fl == null)
				{
					fl = fs.gameObject.AddComponent<FontLocalizer>();
				}
				
				fl.Fonts = new FontLocalizer.Mapping[fs.Fonts.Length];
				for (int j = 0; j < fs.Fonts.Length; ++j)
				{
					FontLocalizer.Mapping m = new FontLocalizer.Mapping();
					m.fontPath = fs.Fonts[j].fontPath;
					m.language = fs.Fonts[j].language;
					fl.Fonts[j] = m;
				}

				fl._defaultFontPath = fs._defaultFontPath;
				fl._defaultFontSize = fs._defaultFontSize;

				Object.DestroyImmediate(fs, true);
			}
			NGUITools.SetDirty(go);
			//AssetDatabase.SaveAssets();
		}
	}

	private static void FontLocalizerConverter_ProcessLabels(string path)
	{
		if (string.IsNullOrEmpty(path)) return;

		GameObject go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
		if (go == null)
		{
			Debug.LogError ("Failed to load from file " + path);
			return;
		}

		UILabel [] labels = EB.Util.FindAllComponents<UILabel>(go);
		if (labels.Length > 0)
		{
			bool shouldSave = false;
			for (int i = 0; i < labels.Length; ++i)
			{
				UILabel label = labels[i];
				if (label.selectorFont != null)
				{
					FontLocalizer fl = label.selectorFont.gameObject.GetComponent<FontLocalizer>();
					if (fl != null)
					{
						label.selectorFont = null;
						label.trueTypeFont = null;
						label.bitmapFont = null;
						label.customFont = null;
						label.customFont = fl;
						shouldSave = true;
						Debug.Log ("Converting label '" + label.gameObject.name + "' in " + path);
					}
				}
			}

			if (shouldSave)
			{
				NGUITools.SetDirty(go);
				//AssetDatabase.SaveAssets();
			}
		}
	}
}

