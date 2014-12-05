//#define LWF_HIERARCHY_DEBUG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("NGUI/UI/UILWFObject")]
public class UILWFObject : UIWidget
{
	public enum ScaleType
	{
		NORMAL,
		FIT_FOR_HEIGHT,
		FIT_FOR_WIDTH,
		SCALE_FOR_HEIGHT,
		SCALE_FOR_WIDTH,
	}

	[SerializeField] protected string mPath;
	[SerializeField] protected ScaleType mScaleType;
	
	protected LWFObject mLWFObject;
	protected bool mPropertyChanged;
	protected int mCachedHeight;
	protected int mCachedWidth;
	protected int mCachedDepth;
	protected Pivot mCachedPivot;
	protected Color mCachedColor;
	protected LWF.IFontAdapter mFontAdapter;
	protected LWF.ITextureAdapter mTextureAdapter;

	private static string HD_SUFFIX = "@2x";
	private FragmentPool mFragmentPool = new FragmentPool();

	public FragmentPool fragmentPool { get { return mFragmentPool; } }

	public bool isReady { get { return mLWFObject != null && mLWFObject.lwf != null && mLWFObject.lwf.property != null && mLWFObject.lwf.rootMovie != null; } }

	public string path {
		get {return mPath;}
		set {mPath = value; mPropertyChanged = true;}
	}
	public ScaleType scaleType {
		get {return mScaleType;}
		set {mScaleType = value; mPropertyChanged = true;}
	}
	public LWFObject lwfObject {
		get {return mLWFObject;}
	}
	public LWF.IFontAdapter fontAdapter {
		get {return mFontAdapter; }
		set {mFontAdapter = value; mPropertyChanged = true;}
	}
	public LWF.ITextureAdapter textureAdapter {
		get {return mTextureAdapter; }
		set {mTextureAdapter = value; mPropertyChanged = true;}
	}
	
	void DestroyLWF()
	{
		if (mLWFObject != null) 
		{
			if (mLWFObject.lwf != null)
			{
				mLWFObject.lwf.onHierarchyUpdatedDelegate = null;
			}
			if (Application.isPlaying)
			{
				Destroy(mLWFObject.gameObject);
			}
			else
			{
				DestroyImmediate(mLWFObject.gameObject);
			}
			mLWFObject = null;
		}
	}

	// This is a coroutine because of a potential destruction and re-creating of the LWF - destruction may not be immediate.
	// There was an observed problem with stale bitmap contexts being referred to by the new LWF.
	IEnumerator InitLWF()
	{
		DestroyLWF();

		while (true)
		{
			LWFObject lwf = gameObject.GetComponentInChildren<LWFObject>();
			if (lwf == null)
				break;
			
			yield return new WaitForFixedUpdate();
		}

		if (string.IsNullOrEmpty(mPath)) {
			Debug.LogWarning(
				"UILWFObject: path should be a correct lwf bytes path");
			yield break;
		}

		string texturePrefix = System.IO.Path.GetDirectoryName(mPath);
		if (texturePrefix.Length > 0)
			texturePrefix += "/";

		GameObject o = NGUITools.AddChild(gameObject);
		o.name = mPath;
		o.hideFlags = HideFlags.HideAndDontSave;

		// Find the nearest camera above us matching our layer
		// using NGUITools.FindCameraForLayer might not retrieve the closest one...
		Camera camera = EB.Util.FindComponentUpwards<Camera>(o);
		int layerMask = 1 << o.layer;
		while (camera != null && (camera.cullingMask & layerMask) == 0)
		{
			camera = EB.Util.FindComponentUpwards<Camera>(camera.gameObject);
		}
		mLWFObject = o.AddComponent<LWFObject>();

		mLWFObject.UseNGUIRenderer();
		bool isHD = mTextureAdapter != null ? mTextureAdapter.IsHD() : false;
		if (isHD && !mPath.EndsWith(HD_SUFFIX))
		{
			mPath += HD_SUFFIX;
		}
		if (!isHD && mPath.EndsWith(HD_SUFFIX))
		{
			mPath = mPath.Substring(0, mPath.IndexOf(HD_SUFFIX));
		}

		mLWFObject.Load(mPath, texturePrefix, string.Empty,
	                0, 0, 0, camera, true,
					fontAdapter: mFontAdapter, 
					textureAdapter: mTextureAdapter);

		if (mScaleType != ScaleType.NORMAL)
		{
			int height = (int)camera.orthographicSize * 2;
			int width = (int)(camera.aspect * (float)height);
			switch (mScaleType) {
			case ScaleType.FIT_FOR_HEIGHT:
				mLWFObject.FitForHeight(height);
				break;
			case ScaleType.FIT_FOR_WIDTH:
				mLWFObject.FitForWidth(width);
				break;
			case ScaleType.SCALE_FOR_HEIGHT:
				mLWFObject.ScaleForHeight(height);
				break;
			case ScaleType.SCALE_FOR_WIDTH:
				mLWFObject.ScaleForWidth(width);
				break;
			}
		}

		StartCoroutine(WaitForLWFLoad());
	}

	protected override void OnStart()
	{
		StartCoroutine(InitLWF());
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (mPropertyChanged)
		{
			mPropertyChanged = false;
			StopAllCoroutines();
			StartCoroutine(InitLWF());
		}

#if UNITY_EDITOR
		if (isReady && Application.isPlaying)
		{
			// Need the following checks to ensure our properties update via inspector manipulation
			if (mCachedColor != mColor || mCachedDepth != mDepth || mCachedWidth != mWidth || mCachedHeight != mHeight || mCachedPivot != mPivot)
			{
				MarkAsChanged();
			}
		}
#endif
	}

	void OnDestroy()
	{
		StopAllCoroutines();
		DestroyLWF();
		mFragmentPool.Clear();
		mFragmentPool = null;
	}

	IEnumerator WaitForLWFLoad()
	{
		while (!isReady)
		{
			yield return new WaitForFixedUpdate();
		}
		
		mLWFObject.lwf.onHierarchyUpdatedDelegate = RootMovieHierarchyChanged;

		mHeight = Mathf.RoundToInt(mLWFObject.lwf.height);
		mWidth = Mathf.RoundToInt(mLWFObject.lwf.width);

		AdjustLWFForPivot();
		
		yield break;
	}
	
	void RootMovieHierarchyChanged(LWF.LWF lwf)
	{
		UILWFObjectFragment.RefreshFragments(this, null, lwf.rootMovie);
	}

	public void OnPress(bool isPressed)
	{
		mLWFObject.OnPressed(isPressed);
	}

	public override void MarkAsChanged()
	{
		if (isReady)
		{
			bool shouldAdjustScaling = false;
			float xscale = mLWFObject.lwf.property.m_scaleX;
			float yscale = mLWFObject.lwf.property.m_scaleY;
			if (mWidth != mCachedWidth)
			{
				mCachedWidth = mWidth;
				xscale = mWidth / mLWFObject.lwf.width;
				shouldAdjustScaling = true;
			}
			if (mHeight != mCachedHeight)
			{
				mCachedHeight = mHeight;
				yscale = mHeight / mLWFObject.lwf.height;
				shouldAdjustScaling = true;
			}

			if (shouldAdjustScaling)
			{
				mLWFObject.lwf.property.ScaleTo(xscale, yscale);
				if (autoResizeBoxCollider) ResizeCollider();
			}

			if (mPivot != mCachedPivot || shouldAdjustScaling)
			{
				mCachedPivot = mPivot;
				AdjustLWFForPivot();
			}

			if (mColor != mCachedColor)
			{
				mCachedColor = mColor;
				LWF.Color color = mLWFObject.lwf.property.colorTransform.multi;
				if (mColor.r != color.red || mColor.g != color.green || mColor.b != color.blue || mColor.a != color.alpha)
				{
					mLWFObject.SetColorTransform(new LWF.ColorTransform(mColor.r, mColor.g, mColor.b, mColor.a));
				}
			}

			if (mCachedDepth != mDepth)
			{
				mCachedDepth = mDepth;
				if (isReady)
				{
					UILWFObjectFragment.RefreshFragments(this, null, mLWFObject.lwf.rootMovie);
				}
			}
		}
		base.MarkAsChanged();
	}

	protected void AdjustLWFForPivot()
	{
		float x = 0f;
		float y = 0f;
		switch(mPivot)
		{
		case Pivot.BottomLeft:
			x = 0;
			y = -mHeight;
			break;
		case Pivot.Bottom:
			x = -mWidth/2f;
			y = -mHeight;
			break;
		case Pivot.BottomRight:
			x = -mWidth;
			y = -mHeight;
			break;
		case Pivot.TopLeft:
			x = 0;
			y = 0;
			break;
		case Pivot.Top:
			x = -mWidth/2f;
			y = 0;
			break;
		case Pivot.TopRight:
			x = -mWidth;
			y = 0;
			break;
		case Pivot.Left:
			x = 0;
			y = -mHeight/2f;
			break;
		case Pivot.Center:
		default:
			x = -mWidth/2f;
			y = -mHeight/2f;
			break;
		case Pivot.Right:
			x = -mWidth;
			y = -mHeight/2f;
			break;
		}

		mLWFObject.MoveTo(x, y);
	}

	override public void MakePixelPerfect()
	{
		height = Mathf.RoundToInt(mLWFObject.lwf.height);
		width = Mathf.RoundToInt(mLWFObject.lwf.width);
		MarkAsChanged();
	}

	///
	/// Fragment pooling, it's costly to destroy then re-create components
	/// The cost can be mitigated by modifying Flash assets to always have objects present from the beginning and never leave scope
	/// But just in case... we have the pool.
	/// 
	public class FragmentPool
	{
		private Stack<UILWFObjectFragment> mPool = new Stack<UILWFObjectFragment>();
		
		public UILWFObjectFragment Request(GameObject parent)
		{
			if (parent == null) return null;
			
			UILWFObjectFragment obj = null;
			if (mPool.Count > 0)
			{
				obj = mPool.Pop();
				obj.gameObject.transform.parent = parent.transform;
			}
			
			if (obj == null)
			{
				GameObject objGO = NGUITools.AddChild(parent);
				#if LWF_HIERARCHY_DEBUG
				objGO.hideFlags = HideFlags.DontSave;
				#else
				objGO.hideFlags = HideFlags.HideAndDontSave;
				#endif
				obj = objGO.AddComponent<UILWFObjectFragment>();
			}
			return obj;
		}

		public void Release(UILWFObjectFragment obj)
		{
			if (obj == null) return;
			if (!mPool.Contains(obj))
			{
				obj.Reset();
				mPool.Push(obj);
			}
			else
			{
				Debug.LogWarning("Attempting to release already-released fragment");
			}
		}

		public void Clear()
		{
			while (mPool.Count > 0)
			{
				GameObject go = mPool.Pop().gameObject;
				Destroy(go);
			}
		}
	}
}
