using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UITextureRefs.
/// </summary>

[CustomEditor(typeof(UITextureRef))]
public class UITextureRefInspector : UIWidgetInspector
{
	UITextureRef mTex;
	
	protected override void OnEnable ()
	{
		base.OnEnable();
		mTex = target as UITextureRef;
	}
	protected override void DrawCustomProperties ()
	{
		const float hintSize = 64f;
		EditorGUILayout.BeginHorizontal();
		mTex.baseTexturePath = EditorGUILayout.TextField("Base Path", mTex.baseTexturePath, GUILayout.Width(Screen.width - hintSize));
		string hintText = mTex.fullPath.Substring(mTex.baseTexturePath.Length) + ".PNG";
		EditorGUILayout.LabelField(hintText, GUILayout.Width(hintSize));
		EditorGUILayout.EndHorizontal();
		if (mTex.mOverrideTexture == null)
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField("Texture not found!");
			GUI.color = Color.white;
		}
		else
		{
			if (GUILayout.Button("Ping Texture"))
			{
				string path = "Assets/Resources/" + mTex.fullPath;
				EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(path + ".PNG", typeof(UnityEngine.Object)));
			}
		}
		
		mTex.isImportant = GUILayout.Toggle(mTex.isImportant, "Is Important");
		mTex.fadeOnLoad = GUILayout.Toggle(mTex.fadeOnLoad, "Fade On Load");
		
		if (mTex.material != null || mTex.mainTexture == null)
		{
			Material mat = EditorGUILayout.ObjectField("Material", mTex.material, typeof(Material), false) as Material;
			
			if (mTex.material != mat)
			{
				NGUIEditorTools.RegisterUndo("Material Selection", mTex);
				mTex.material = mat;
			}
		}
		
		if (mTex.material == null)// || mTex.hasDynamicMaterial)
		{
			Shader shader = EditorGUILayout.ObjectField("Shader", mTex.shader, typeof(Shader), false) as Shader;
			
			if (mTex.shader != shader)
			{
				NGUIEditorTools.RegisterUndo("Shader Selection", mTex);
				mTex.shader = shader;
			}
		}
		
		if (mTex.mainTexture != null)
		{
			Rect rect = EditorGUILayout.RectField("UV Rectangle", mTex.uvRect);
			
			if (rect != mTex.uvRect)
			{
				NGUIEditorTools.RegisterUndo("UV Rectangle Change", mTex);
				mTex.uvRect = rect;
			}
			
			Rect r = mTex.uvRect;
			r.x *= mTex.mainTexture.width;
			r.y *= mTex.mainTexture.height;
			r.width *= mTex.mainTexture.width;
			r.height *= mTex.mainTexture.height;
			r = EditorGUILayout.RectField("UV Rectangle (Pixels)", r);
			
			r.x /= mTex.mainTexture.width;
			r.y /= mTex.mainTexture.height;
			r.width /= mTex.mainTexture.width;
			r.height /= mTex.mainTexture.height;
			if (r != mTex.uvRect)
			{
				NGUIEditorTools.RegisterUndo("UV Rectangle Change", mTex);
				mTex.uvRect = r;
			}
			
			if (GUILayout.Button("Fit texture size"))
			{
				mTex.ResizeToFit(false);
			}
			if (!(string.IsNullOrEmpty(mTex.metadata)) && GUILayout.Button("Fit alpha trimmed texture size"))
			{
				mTex.ResizeToFit(true);
			}
		}
		
		base.DrawCustomProperties();
	}
	
	/// <summary>
	/// Allow the texture to be previewed.
	/// </summary>
	
	public override bool HasPreviewGUI ()
	{
		return (mTex != null) && (mTex.mainTexture as Texture2D != null);
	}
	
	/// <summary>
	/// Draw the sprite preview.
	/// </summary>
	
	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		Texture2D tex = mTex.mainTexture as Texture2D;
		if (tex != null) NGUIEditorTools.DrawTexture(tex, rect, mTex.uvRect, mTex.color);
	}
}
