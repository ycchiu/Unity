using UnityEngine;
using System.Collections;

public class UIMaskedSprite : UISprite
{
	public UITexture maskingTexture;

	public override bool hasMultipleUVs { get { return true; } }

	public override Material material { get { return _material; } }
	private Material _material = null;

	private Texture lastMaskTex = null;

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Vector2> uvs2, BetterList<Color32> cols)
	{
		Texture tex = mainTexture;
		
		if (mSprite == null) mSprite = atlas.GetSprite(spriteName);
		if (mSprite == null) return;

		if (mSprite != null && tex != null)
		{
			mOuterUV.Set(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
			mInnerUV.Set(mSprite.x + mSprite.borderLeft, mSprite.y + mSprite.borderTop,
			             mSprite.width - mSprite.borderLeft - mSprite.borderRight,
			             mSprite.height - mSprite.borderBottom - mSprite.borderTop);
			
			mOuterUV = NGUIMath.ConvertToTexCoords(mOuterUV, tex.width, tex.height);
			mInnerUV = NGUIMath.ConvertToTexCoords(mInnerUV, tex.width, tex.height);
		}
		
		switch (type)
		{
		case Type.Simple:
			SimpleFill(verts, uvs, uvs2, cols);
			break;
			
		case Type.Sliced:
			SlicedFill(verts, uvs, uvs2, cols);
			break;

		default:
			SimpleFill(verts, uvs, uvs2, cols);
			break;
		}
	}

	// Abstraction of the math required to calculate UV space
	private float GetRatio(float v1, float v2, float w1, float w2)
	{
		float vRatio = v1 - v2;
		float wDelta = w2 - w1;
		float wRatio = w2 / w1;
		// This is how I'm handling division by zero.
		if (EB.Util.FloatEquals(wDelta, 0f, 0.001f))
		{
			return (GetRatio(v1, v2, w1, w2 - 1) + GetRatio(v1, v2, w1, w2 + 1)) / 2f;
		}
		return ((vRatio) / (wDelta) * (1f - (wRatio)));
	}

	private Rect GetMaskRect()
	{
		Rect maskWorldRect = GetRectFromPoints(maskingTexture.worldCorners);
		Rect spriteWorldRect = GetRectFromPoints(worldCorners);

		float widthRatio = spriteWorldRect.width / maskWorldRect.width;
		float heightRatio = spriteWorldRect.height / maskWorldRect.height;

		float widthDelta = spriteWorldRect.width - maskWorldRect.width;

		float relativeX = maskWorldRect.x - spriteWorldRect.x;
		float relativeY = maskWorldRect.yMin - spriteWorldRect.yMin;

		// First, convert to a number which is between 0 and 1.
		float ratioX = GetRatio(maskWorldRect.x, spriteWorldRect.x, maskWorldRect.width, spriteWorldRect.width);
		float ratioY = GetRatio(maskWorldRect.yMin, spriteWorldRect.yMin, maskWorldRect.height, spriteWorldRect.height);

		// Now map to a value which is 0 to 1-(widthRatio) which represents UV space.
		Rect result = new Rect(ratioX, ratioY, widthRatio, heightRatio);

		return result;
	}

	protected void SimpleFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Vector2> uvs2, BetterList<Color32> cols)
	{
		Vector4 v = drawingDimensions;
		Vector4 u = drawingUVs;
		
		verts.Add(new Vector3(v.x, v.y));
		verts.Add(new Vector3(v.x, v.w));
		verts.Add(new Vector3(v.z, v.w));
		verts.Add(new Vector3(v.z, v.y));
		
		uvs.Add(new Vector2(u.x, u.y));
		uvs.Add(new Vector2(u.x, u.w));
		uvs.Add(new Vector2(u.z, u.w));
		uvs.Add(new Vector2(u.z, u.y));

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
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;
		
		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
	}

	// NGUI: "Static variables to reduce garbage collection"
	static Vector2[] mTempPos = new Vector2[4];
	static Vector2[] mTempUVs = new Vector2[4];

	protected void SlicedFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Vector2> uvs2, BetterList<Color32> cols)
	{
		if (!mSprite.hasBorder)
		{
			SimpleFill(verts, uvs, cols);
			return;
		}
		
		Vector4 dr = drawingDimensions;
		Vector4 br = border * atlas.pixelSize;
		
		mTempPos[0].x = dr.x;
		mTempPos[0].y = dr.y;
		mTempPos[3].x = dr.z;
		mTempPos[3].y = dr.w;

		if (mFlip == Flip.Horizontally || mFlip == Flip.Both)
		{
			mTempPos[1].x = mTempPos[0].x + br.z;
			mTempPos[2].x = mTempPos[3].x - br.x;
			
			mTempUVs[3].x = mOuterUV.xMin;
			mTempUVs[2].x = mInnerUV.xMin;
			mTempUVs[1].x = mInnerUV.xMax;
			mTempUVs[0].x = mOuterUV.xMax;
		}
		else
		{
			mTempPos[1].x = mTempPos[0].x + br.x;
			mTempPos[2].x = mTempPos[3].x - br.z;
			
			mTempUVs[0].x = mOuterUV.xMin;
			mTempUVs[1].x = mInnerUV.xMin;
			mTempUVs[2].x = mInnerUV.xMax;
			mTempUVs[3].x = mOuterUV.xMax;
		}
		
		if (mFlip == Flip.Vertically || mFlip == Flip.Both)
		{
			mTempPos[1].y = mTempPos[0].y + br.w;
			mTempPos[2].y = mTempPos[3].y - br.y;
			
			mTempUVs[3].y = mOuterUV.yMin;
			mTempUVs[2].y = mInnerUV.yMin;
			mTempUVs[1].y = mInnerUV.yMax;
			mTempUVs[0].y = mOuterUV.yMax;
		}
		else
		{
			mTempPos[1].y = mTempPos[0].y + br.y;
			mTempPos[2].y = mTempPos[3].y - br.w;
			
			mTempUVs[0].y = mOuterUV.yMin;
			mTempUVs[1].y = mInnerUV.yMin;
			mTempUVs[2].y = mInnerUV.yMax;
			mTempUVs[3].y = mOuterUV.yMax;
		}

		Color colF = color;
		colF.a = finalAlpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		for (int x = 0; x < 3; ++x)
		{
			int x2 = x + 1;
			
			for (int y = 0; y < 3; ++y)
			{
				if (centerType == AdvancedType.Invisible && x == 1 && y == 1) continue;
				
				int y2 = y + 1;
				
				verts.Add(new Vector3(mTempPos[x].x, mTempPos[y].y));
				verts.Add(new Vector3(mTempPos[x].x, mTempPos[y2].y));
				verts.Add(new Vector3(mTempPos[x2].x, mTempPos[y2].y));
				verts.Add(new Vector3(mTempPos[x2].x, mTempPos[y].y));
				
				uvs.Add(new Vector2(mTempUVs[x].x, mTempUVs[y].y));
				uvs.Add(new Vector2(mTempUVs[x].x, mTempUVs[y2].y));
				uvs.Add(new Vector2(mTempUVs[x2].x, mTempUVs[y2].y));
				uvs.Add(new Vector2(mTempUVs[x2].x, mTempUVs[y].y));
				
				cols.Add(col);
				cols.Add(col);
				cols.Add(col);
				cols.Add(col);

				// EBG START - This part is what differs from the base class SlicedFill method.

				if (maskingTexture != null)
				{
					// Convert the positions to ratios relative to the texture size, ie.
					// 0->1 values for lerping the mask UVs.
					Rect uvLerpRect = new Rect();
					uvLerpRect.xMin = mTempPos[x].x / width;
					uvLerpRect.xMax = mTempPos[x2].x / width;
					uvLerpRect.yMin = mTempPos[y].y / height;
					uvLerpRect.yMax = mTempPos[y2].y / height;

					// This allows us to ignore pivot values.
					uvLerpRect.x += pivotOffset.x;
					uvLerpRect.y += pivotOffset.y;

					Rect maskRect = GetMaskRect();
					float xMin = NGUIMath.Lerp(maskRect.xMin, maskRect.xMax, uvLerpRect.xMin);
					float xMax = NGUIMath.Lerp(maskRect.xMin, maskRect.xMax, uvLerpRect.xMax);
					float yMin = NGUIMath.Lerp(maskRect.yMin, maskRect.yMax, uvLerpRect.yMin);
					float yMax = NGUIMath.Lerp(maskRect.yMin, maskRect.yMax, uvLerpRect.yMax);

					uvs2.Add(new Vector2(xMin, yMin));
					uvs2.Add(new Vector2(xMin, yMax));
					uvs2.Add(new Vector2(xMax, yMax));
					uvs2.Add(new Vector2(xMax, yMin));
				}
				else
				{
					uvs2.Add(new Vector2(0, 0));
					uvs2.Add(new Vector2(0, 1));
					uvs2.Add(new Vector2(1, 1));
					uvs2.Add(new Vector2(1, 0));
				}

				// EBG END
			}
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// Check if the masking texture has changed.
		Texture maskTex = null;
		if (maskingTexture != null)
		{
			maskTex = maskingTexture.mainTexture;
		}
		if (lastMaskTex != maskTex)
		{
			UpdateMaterial();
		}
	}

	protected override void OnEnable()
	{
		base.OnStart();

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

	private void ReleaseMaterial()
	{
		if (_material == null)
		{
			return;
		}

		Texture baseTex = null;
		if (atlas != null)
		{
			baseTex = atlas.spriteMaterial.mainTexture;
		}
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
		
		Texture baseTex = null;
		if (atlas != null)
		{
			baseTex = atlas.spriteMaterial.mainTexture;
		}
		Texture maskTex = null;
		if (maskingTexture != null)
		{
			maskTex = maskingTexture.mainTexture;
			lastMaskTex = maskTex;
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
}
