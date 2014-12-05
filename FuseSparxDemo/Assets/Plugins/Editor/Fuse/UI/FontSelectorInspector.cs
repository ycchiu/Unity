using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FontSelector))]
public class FontSelectorInspector : Editor
{
	enum FontType
	{
		Normal,
		Reference
	}
	
	FontType mType = FontType.Normal;
	FontSelector mFont;
	FontSelector mReplacement;
	
	void OnSelectReplacementFont(Object obj)
	{
		mFont.replacement = obj as FontSelector;
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
			if (lbl.selectorFont == mFont)
			{
//				lbl.Update();
				lbl.selectorFont = null;
				lbl.selectorFont = mFont;
			}
		}
	}
	
	public override void OnInspectorGUI()
	{
		mFont = target as FontSelector;
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
			ComponentSelector.Draw<FontSelector>(mFont.replacement, OnSelectReplacementFont, true);

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
}

