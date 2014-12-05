//#define LWF_HIERARCHY_DEBUG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UILWFObjectFragment : UIWidget
{
	/// <summary>
	/// Refreshes the fragment hierarchy given an LWF.Object and its parent.  This will recurse down the structure, retrieving or creating fragments for its children
	/// </summary>
	public static void RefreshFragments(UILWFObject rootLWF, LWF.Object parent, LWF.Object element)
	{
		UILWFObjectFragment fragment = null;
#if LWF_HIERARCHY_DEBUG
		GameObject parentGO = (parent == null) ? rootLWF.cachedGameObject : (parent.linkedObject as MonoBehaviour).gameObject;
#else
		GameObject parentGO = rootLWF.cachedGameObject;
#endif

		// generate a fragment for this element if debug is on or if the element is a leaf node
		int numChildren = element.numChildren;
		// only create a fragment for this element if we are a leaf node
#if !LWF_HIERARCHY_DEBUG
		if (numChildren == 0)
#endif
		{
			if (element.linkedObject != null)
			{
				fragment = element.linkedObject as UILWFObjectFragment;
			}
			else
			{
				fragment = rootLWF.fragmentPool.Request(parentGO);
			}
			fragment.Initialize(rootLWF, element);
		}

		if (numChildren > 0)
		{
			LWF.Object [] children = element.children;
			for (int i = 0; i < numChildren; ++i)
			{
				if (children[i] != null)
				{
					RefreshFragments(rootLWF, element, children[i]);
				}
			}
		}
	}

	protected void Initialize(UILWFObject lwfParent, LWF.Object lwfElement)
	{
		mRootLWF = lwfParent;
		mLWFObject = lwfElement;
		mRenderer = mLWFObject.renderer as LWF.NGUIRenderer.BaseRenderer;
		mLWFObject.linkedObject = this;
		mLWFObject.onDestroy = onLWFRemovedFromHierarchy;
		if (mLWFObject.IsButton())
		{
			// ensure root object has a box collider so NGUI can forward input events down to it!
			BoxCollider bc = lwfParent.gameObject.GetComponent<BoxCollider>();
			if (bc == null)
			{
				lwfParent.gameObject.AddComponent<BoxCollider>();
				lwfParent.autoResizeBoxCollider = true;
				lwfParent.ResizeCollider();
			}
		}

		mLWFObject.onUpdateDelegate = LWF.SafeAction.Wrap<LWF.Object>(this, onLWFUpdate);
		if (mRenderer != null)
		{
			mRenderer.onRenderMaterialsChanged = LWF.SafeAction.Wrap<LWF.NGUIRenderer.BaseRenderer, Material>(this, onRenderMaterialsChanged);
		}

		mIsRendering = true;

		// MUST use depth so that the panel refreshes!
		depth = lwfParent.depth + mLWFObject.layerDepth;

#if LWF_HIERARCHY_DEBUG
		if (mRenderer is LWF.NGUIRenderer.BitmapRenderer)
		{
			gameObject.name = mLWFObject.objectId + "|" + mLWFObject.type + "|" + mLWFObject.GetHashCode() + "|" + (mRenderer as LWF.NGUIRenderer.BitmapRenderer).context.fragmentName;
		}
		else
		{
			gameObject.name = mLWFObject.objectId + "|" + mLWFObject.type + "|" + mLWFObject.GetHashCode();
		}
#endif

		MarkAsChanged();
	}


	private void onRenderMaterialsChanged(LWF.Renderer renderer, Material material)
	{
		if (panel != null)
		{
			panel.RemoveWidget(this);
			panel.AddWidget(this);
			
			if (!Application.isPlaying)
			{
				panel.SortWidgets();
				panel.RebuildAllDrawCalls();
			}
		}
	}

	private void onLWFUpdate(LWF.Object obj)
	{
		if (mLWFObject == obj && mIsRendering)
		{
			mChanged = true;
		}
	}


	private void onLWFRemovedFromHierarchy()
	{
		if (mRootLWF != null)
		{
			mRootLWF.fragmentPool.Release(this);
		}
	}

	public void Reset(bool isDestroy = false)
	{
		mRootLWF = null;
		mLWFObject = null;
		mIsRendering = false;
	}

	
	public LWF.NGUIRenderer.BaseRenderer lwfRenderer { get { return mRenderer; } }
	public override Material material { get { return mRenderer!= null ? mRenderer.material : null; } }
	public override Shader shader { get { return mRenderer!= null ? mRenderer.shader : null; } }
	public override Texture mainTexture { get { return mRenderer!= null ? mRenderer.texture : null; } }

	new public Pivot pivot { get { return mRootLWF.pivot; } }
	new public int height { get { return mHeight; } }
	new public int width { get { return mWidth; } }
	new public Color color { get { return mRootLWF.color; } }
	
	void OnDestroy()
	{
		Reset(true);
	}

	public override void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mRenderer != null)
		{
			mRenderer.Fill(verts, uvs, cols);
		}
	}

	protected UILWFObject mRootLWF;
	protected LWF.Object mLWFObject;
	protected LWF.NGUIRenderer.BaseRenderer mRenderer;

}
