using UnityEngine;
using System.Collections;

public class UIMaskedTextureRef : UITextureRef
{
	public UITexture maskingTexture;
	
	public override bool hasMultipleUVs { get { return true; } }
	
	public override Material material { get { return _material; } }
	private Material _material = null;
	
	public override Texture mainTexture
	{
		get
		{
			return mOverrideTexture;
		}
		set
		{
			if (mOverrideTexture != value)
			{
				RemoveFromPanel();
				mOverrideTexture = value;
				MarkAsChanged();
				UpdateMaterial();
			}
		}
	}
	
	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Vector2> uvs2, BetterList<Color32> cols)
	{
		Vector4 v = drawingDimensions;

		verts.Add(new Vector3(v.x, v.y));
		verts.Add(new Vector3(v.x, v.w));
		verts.Add(new Vector3(v.z, v.w));
		verts.Add(new Vector3(v.z, v.y));
		
		uvs.Add(new Vector2(mRect.xMin, mRect.yMin));
		uvs.Add(new Vector2(mRect.xMin, mRect.yMax));
		uvs.Add(new Vector2(mRect.xMax, mRect.yMax));
		uvs.Add(new Vector2(mRect.xMax, mRect.yMin));

		if (maskingTexture != null)
		{
			Rect maskRect = GetMaskRect();
			
			uvs2.Add(new Vector2(maskRect.xMin, maskRect.yMin));
			uvs2.Add(new Vector2(maskRect.xMin, maskRect.yMax));
			uvs2.Add(new Vector2(maskRect.xMax, maskRect.yMax));
			uvs2.Add(new Vector2(maskRect.xMax, maskRect.yMin));
		}
		else
		{
			uvs2.Add(new Vector2(0, 0));
			uvs2.Add(new Vector2(0, 1));
			uvs2.Add(new Vector2(1, 1));
			uvs2.Add(new Vector2(1, 0));
		}
		
		Color colF = color;
		colF.a = finalAlpha;

		cols.Add(colF);
		cols.Add(colF);
		cols.Add(colF);
		cols.Add(colF);
	}

	private Rect GetMaskRect()
	{
		Rect maskWorldRect = GetRectFromPoints(maskingTexture.worldCorners);
		Rect maskedWorldRect = GetRectFromPoints(worldCorners);
		
		float widthRatio = maskedWorldRect.width / maskWorldRect.width;
		float heightRatio = maskedWorldRect.height / maskWorldRect.height;
		
		float relativeX = maskWorldRect.x - maskedWorldRect.x;
		float relativeY = maskWorldRect.yMin - maskedWorldRect.yMin;
		// First, convert to a number which is between 0 and 1.
		float ratioX;
		if (!EB.Util.FloatEquals(maskedWorldRect.width, maskWorldRect.width, 0.01f))
		{
			ratioX = relativeX / (maskedWorldRect.width - maskWorldRect.width);
			ratioX = NGUIMath.Lerp(0f, 1f - widthRatio, ratioX);
		}
		else
		{
			ratioX = 0f - relativeX;
		}
		float ratioY;
		if (!EB.Util.FloatEquals(maskedWorldRect.height, maskWorldRect.height, 0.01f))
		{
			ratioY = relativeY / (maskedWorldRect.height - maskWorldRect.height);
			ratioY = NGUIMath.Lerp(0f, 1f - heightRatio, ratioY);
		}
		else
		{
			ratioY = 0f - relativeY;
		}
		// Now map to a value which is 0 to 1-(widthRatio) which represents UV space.
		Rect result = new Rect(ratioX, ratioY, widthRatio, heightRatio);
		
		return result;
	}

	private Rect GetRectFromPoints(Vector3[] points)
	{
		Rect rect = new Rect();
		if (points.Length < 1)
		{
			return rect;
		}
		
		rect.x = points[0].x;
		rect.y = points[0].y;
		rect.width = 0f;
		rect.height = 0f;
		for (int i = 1; i < points.Length; ++i)
		{
			if (rect.xMin > points[i].x)
			{
				rect.xMin = points[i].x;
			}
			if (rect.xMax < points[i].x)
			{
				rect.xMax = points[i].x;
			}
			if (rect.yMin > points[i].y)
			{
				rect.yMin = points[i].y;
			}
			if (rect.yMax < points[i].y)
			{
				rect.yMax = points[i].y;
			}
		}
		
		return rect;
	}

	private void ReleaseMaterial()
	{
		if (_material == null)
		{
			return;
		}
		if (mOverrideTexture == null)
		{
			return;
		}
		
		Texture baseTex = mOverrideTexture;
		Texture maskTex = null;
		if (maskingTexture != null)
		{
			maskTex = maskingTexture.mainTexture;
		}
		
		if (baseTex != null && maskTex != null)
		{
			UIMaskMaterialManager.ReleaseMaterial(baseTex, maskTex);
			_material = null;
		}
	}

	private void UpdateMaterial()
	{
		ReleaseMaterial();
		if (mOverrideTexture == null)
		{
			return;
		}

		Texture baseTex = mOverrideTexture;
		Texture maskTex = null;
		if (maskingTexture != null)
		{
			maskTex = maskingTexture.mainTexture;
		}
		
		if (baseTex != null && maskTex != null)
		{
			_material = UIMaskMaterialManager.UseMaterial(baseTex, maskTex);
			mChanged = true;

			if (panel != null)
			{
				panel.singleFrameUpdate = true;
			}
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		
		if (maskingTexture != null)
		{
			maskingTexture.isRendering = false;
			maskingTexture.onChange += OnMaskDimensionsChanged;
		}
		UpdateMaterial();
	}
	
	protected override void OnDisable()
	{
		base.OnDisable();
		ReleaseMaterial();
	}
	
	private void OnMaskDimensionsChanged()
	{
		// When the mask changes, we need to update.
		MarkAsChanged();
	}
}
