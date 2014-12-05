using UnityEngine;
using UnityEditor;
using System.Collections;
 
public class InspectorUtils
{
    private class TransformClipboard
    {
        public Vector3      position;
        public Quaternion   rotation;
        public Vector3      scale;
    }
    private static TransformClipboard clipboard = new TransformClipboard();
    
	public static Vector3 GetClipboard_Position()
	{
		return clipboard.position;
	}
	
	public static Quaternion GetClipboard_Rotation()
	{
		return clipboard.rotation;
	}
	
	public static Vector3 GetClipboard_Scale()
	{
		return clipboard.scale;
	}
 
	//------------------- COPY --------------------------------
	
    [MenuItem("CONTEXT/Transform/Copy Position", false, 150)]
    static void CopyPosition()
    {
        clipboard.position = Selection.activeTransform.localPosition;
    }
 
    [MenuItem("CONTEXT/Transform/Copy Rotation", false, 151)]
    static void CopyRotation()
    {
        clipboard.rotation = Selection.activeTransform.localRotation;
    }
 
    [MenuItem("CONTEXT/Transform/Copy Scale", false, 152)]
    static void CopyScale()
    {
        clipboard.scale = Selection.activeTransform.localScale;
    }
	
	[MenuItem("CONTEXT/Transform/Copy All", false, 153)]
    static void CopyAll()
    {
        CopyPosition();
        CopyRotation();
        CopyScale();
    }
 

    //------------------- PASTE --------------------------------
    [MenuItem("CONTEXT/Transform/Paste Position", false, 200)]
    static void PastePosition()
    {
		Undo.RecordObject(Selection.activeTransform, "Paste Position");
        Selection.activeTransform.localPosition = clipboard.position;
    }
 
    [MenuItem("CONTEXT/Transform/Paste Rotation", false, 201)]
    static void PasteRotation()
    {
		Undo.RecordObject(Selection.activeTransform, "Paste Rotation");
        Selection.activeTransform.localRotation = clipboard.rotation;
    }
 
    [MenuItem("CONTEXT/Transform/Paste Scale", false, 202)]
    static void PasteScale()
    {
		Undo.RecordObject(Selection.activeTransform, "Paste Scale");
        Selection.activeTransform.localScale = clipboard.scale;
    }
	
	[MenuItem("CONTEXT/Transform/Paste All", false, 203)]
    static void PasteAll()
    {
        PastePosition();
        PasteRotation();
        PasteScale();
    }
}