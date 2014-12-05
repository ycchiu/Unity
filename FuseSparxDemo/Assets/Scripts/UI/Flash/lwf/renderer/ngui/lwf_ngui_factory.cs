// EBG START
// Custom NGUI Renderer plugin for LWF

using UnityEngine;

using TextureLoader = System.Func<string, UnityEngine.Texture2D>;
using TextureUnloader = System.Action<UnityEngine.Texture2D>;

namespace LWF
{
	namespace NGUIRenderer
	{
		public partial class Factory : UnityRenderer.Factory
		{
			public IFontAdapter fontAdapter;
			public ITextureAdapter textureAdapter;

			public Factory(Data data, GameObject gObj,
			               float zOff = 0, float zR = 1, int rQOff = 0, Camera cam = null,
			               string texturePrfx = "", string fontPrfx = "",
			               TextureLoader textureLdr = null,
			               TextureUnloader textureUnldr = null, 
			               IFontAdapter fontAdpt = null,
			               ITextureAdapter textureAdpt = null)
				: base(gObj, zOff, zR, rQOff,
				       cam, texturePrfx, fontPrfx, textureLdr, textureUnldr)
			{
				fontAdapter = fontAdpt;
				textureAdapter = textureAdpt;
				CreateBitmapContexts(data);
			}
			
			public override Renderer ConstructBitmap(LWF lwf,
			                                         int objectId, Bitmap bitmap)
			{
				return new BitmapRenderer(lwf, m_bitmapContexts[objectId]);
			}
			
			public override Renderer ConstructBitmapEx(LWF lwf,
			                                           int objectId, BitmapEx bitmapEx)
			{
				return new BitmapRenderer(lwf, m_bitmapExContexts[objectId]);
			}
			
			public override TextRenderer ConstructText(LWF lwf, int objectId, Text text)
			{
				return new NGUITextRenderer(lwf, objectId);
			}
		}

		public class BaseRenderer : TextRenderer
		{
			public BaseRenderer(LWF lwf) : base(lwf) {}
			public virtual UnityEngine.Material material { get { return null; } }
			public virtual UnityEngine.Shader shader { get { return null; } }
			public virtual UnityEngine.Texture texture { get { return null; } }
			public virtual void Fill(BetterList<UnityEngine.Vector3> verts, BetterList<UnityEngine.Vector2> uvs, BetterList<UnityEngine.Color32> cols) {}
		
			public System.Action<BaseRenderer, UnityEngine.Material> onRenderMaterialsChanged;
		}

	}	// namespace NGUIRenderer
}	// namespace LWF

// EBG END