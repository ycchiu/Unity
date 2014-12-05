using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UILWFObjectFragment))]
public class UILWFObjectFragmentInspector : UIWidgetInspector
{
	protected UILWFObjectFragment mLWFObject;

	protected override void DrawCustomProperties()
	{
		mLWFObject = target as UILWFObjectFragment;

		EditorGUILayout.LabelField ("Is Rendering: " + mLWFObject.isRendering);
		if (mLWFObject.lwfRenderer != null)
		{
			EditorGUILayout.ObjectField("Material", mLWFObject.material, typeof(Material), false);
			EditorGUILayout.ObjectField("Shader", mLWFObject.shader, typeof(Shader), false);
			EditorGUILayout.ObjectField("Texture", mLWFObject.mainTexture, typeof(Texture), false);
		}
		NGUIEditorTools.DrawProperty("NGUI Depth", serializedObject, "mDepth", GUILayout.MinWidth(20f));
	}
}
