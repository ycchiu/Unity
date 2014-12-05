using UnityEngine;
using UnityEditor;

using ScaleType = UILWFObject.ScaleType;

[CustomEditor(typeof(UILWFObject))]
public class UILWFObjectInspector : UIWidgetInspector
{
	protected UILWFObject mLWFObject;

	void LookLikeControls(float labelWidth)
	{
		EditorGUIUtility.labelWidth = labelWidth;
	}

	void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("UILWFObject Change", mLWFObject);
	}
	
	protected override void DrawCustomProperties ()
	{
		mLWFObject = (UILWFObject)target;

		LookLikeControls(130f);
		string path =
			string.IsNullOrEmpty(mLWFObject.path) ? "" : mLWFObject.path;
		path = EditorGUILayout.TextField("LWF Path: Resources/", path);
		if (!path.Equals(mLWFObject.path))
		{
			RegisterUndo();
			mLWFObject.path = path;
		}

		LookLikeControls(60f);
		ScaleType scaleType = (ScaleType)EditorGUILayout.EnumPopup("Scale Type", mLWFObject.scaleType);
		if (scaleType != mLWFObject.scaleType)
		{
			RegisterUndo();
			mLWFObject.scaleType = scaleType;
		}

		base.DrawCustomProperties ();
	}

	[MenuItem("NGUI/Create/LWFObject")]
	static public void AddLWFObject()
	{
		GameObject root = NGUIMenu.SelectedRoot();

		if (NGUIEditorTools.WillLosePrefab(root))
		{
			NGUIEditorTools.RegisterUndo("Add a LWFObject", root);

			GameObject obj = NGUITools.AddChild(root);
			obj.name = "UILWFObject";
			obj.AddComponent<UILWFObject>();

			Selection.activeGameObject = obj;
		}
	}
}
