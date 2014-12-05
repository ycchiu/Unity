// EBG START
// Custom Text renderer for LWF integration with NGUI

using UnityEngine;

namespace LWF 
{
	namespace NGUIRenderer 
	{
		public enum Align
		{
			LEFT,
			RIGHT,
			CENTER
		}
		
		public enum VerticalAlign
		{
			TOP,
			BOTTOM,
			MIDDLE
		}

		public class TextContext
		{
			public Factory factory;
			public GameObject parent;
			public UnityEngine.Color color;

			protected string mName;
			protected float mSize;
			protected Align mAlign;
			protected VerticalAlign mVerticalAlign;
			protected float mWidth;
			protected float mHeight;
			protected bool mEmpty;

			protected string mFontName;
			protected string mText;
			protected UnityEngine.Color mTextColor;

			public TextContext(Factory f, GameObject p, Data data, int objectId)
			{
				factory = f;
				parent = p;
				
				Format.Text text = data.texts[objectId];
				Format.TextProperty textProperty =
					data.textProperties[text.textPropertyId];
				Format.Font fontProperty = data.fonts[textProperty.fontId];
				color = factory.ConvertColor(data.colors[text.colorId]);
				
				mFontName = data.strings[fontProperty.stringId];
				string fontPath = factory.fontPrefix + mFontName;
				float fontHeight = (float)textProperty.fontHeight;
				float width = (float)text.width;
				float height = (float)text.height;

				Align align;
				int a = textProperty.align & (int)(Format.TextProperty.Align.ALIGN_MASK);
				switch (a) {
				default:
				case (int)Format.TextProperty.Align.LEFT:
					align = Align.LEFT;   break;
				case (int)Format.TextProperty.Align.RIGHT:
					align = Align.RIGHT;  break;
				case (int)Format.TextProperty.Align.CENTER:
					align = Align.CENTER; break;
				}
				
				VerticalAlign valign;
				int va = textProperty.align & (int)Format.TextProperty.Align.VERTICAL_MASK;
				switch (va) {
				default:
					valign = VerticalAlign.TOP;
					break;
				case (int)Format.TextProperty.Align.VERTICAL_BOTTOM:
					valign = VerticalAlign.BOTTOM;
					break;
				case (int)Format.TextProperty.Align.VERTICAL_MIDDLE:
					valign = VerticalAlign.MIDDLE;
					break;
				}

				mName = fontPath;
				mSize = fontHeight;
				mAlign = align;
				mVerticalAlign = valign;

				mWidth = width;
				mHeight = height;
				mEmpty = true;
			}
			
			public void Destruct()
			{
				if (mName == null)
					return;

				mName = null;
			}

			public Material material
			{
				get
				{
					if (factory.fontAdapter != null)
					{
						return factory.fontAdapter.GetFontMaterial(mFontName);
					}
					return null;
				}
			}

			public void SetText(string text)
			{
				ProcessText(text, color);
			}

			public void Fill(Matrix4x4 matrix, UnityEngine.Color color, BetterList<UnityEngine.Vector3> verts, BetterList<UnityEngine.Vector2> uvs, BetterList<UnityEngine.Color32> cols)
			{
				UnityEngine.Color renderColor = new UnityEngine.Color(this.color.r * color.r, this.color.g * color.g, this.color.b * color.b, this.color.a * color.a);
				ProcessText(mText, renderColor);

				if (factory.fontAdapter != null)
				{
					NGUIText.fontSize = Mathf.RoundToInt(mSize);
					NGUIText.rectWidth = Mathf.RoundToInt(mWidth);
					NGUIText.tint = mTextColor;
					switch(mAlign)
					{
					case Align.LEFT:
						NGUIText.alignment = NGUIText.Alignment.Left;
						break;
					case Align.RIGHT:
						NGUIText.alignment = NGUIText.Alignment.Right;
						break;
					case Align.CENTER:
					default:
						NGUIText.alignment = NGUIText.Alignment.Center;
						break;
					}

					int offset = verts.size;
					factory.fontAdapter.PrintText(mFontName, mText, verts, uvs, cols);
					int stop = verts.size;
					for (int i = offset; i < stop; ++i)
					{
						verts[i] = matrix.MultiplyPoint(verts[i]);
					}
				}
			}

			protected void ProcessText(string text, UnityEngine.Color color)
			{
				if (string.IsNullOrEmpty(text))
				{
					mEmpty = true;
					return;
				}

				mEmpty = false;

				mText = text;
				mTextColor = color;
				if (factory.fontAdapter != null)
				{
					factory.fontAdapter.PreProcessText(mFontName, ref mText, ref mTextColor);
				}
			}
		}
		
		public class NGUITextRenderer : BaseRenderer
		{
			private TextContext m_context;
			private Matrix4x4 m_matrix;
			private Matrix4x4 m_renderMatrix;
			private UnityEngine.Color m_colorMult;
			#if LWF_USE_ADDITIONALCOLOR
			private UnityEngine.Color m_colorAdd;
			#endif
			#if UNITY_EDITOR
			private bool m_visible;
			#endif
			private bool m_shouldBeOnTop;
			private float m_zOffset;

			public override Material material {
				get {
					if (m_context != null) return m_context.material;
					return null;
				}
			}

			public override Shader shader {
				get {
					if (m_context != null && m_context.material != null) return m_context.material.shader;
					return null;
				}
			}

			public override Texture texture { get { return null; } }

			public NGUITextRenderer(LWF lwf, int objectId) : base(lwf)
			{
				Factory factory = lwf.rendererFactory as Factory;
				m_context = new TextContext(factory, factory.gameObject, lwf.data, objectId);
				m_matrix = new Matrix4x4();
				m_renderMatrix = new Matrix4x4();
				m_colorMult = new UnityEngine.Color();
				#if LWF_USE_ADDITIONALCOLOR
				m_colorAdd = new UnityEngine.Color();
				#endif

				NGUIRenderer.Factory n = lwf.rendererFactory as NGUIRenderer.Factory;
				if (n != null)
				{
					m_shouldBeOnTop = true;
					m_zOffset = Mathf.Abs(n.zRate);
				}
			}
			
			public override void Destruct()
			{
				if (m_context != null)
				{
					m_context.Destruct();
				}
				base.Destruct();
			}
			
			public override void SetText(string text)
			{
				if (m_context != null)
				{
					m_context.SetText(text);
				}
			}
			
			public override void Render(Matrix matrix, ColorTransform colorTransform,
			                            int renderingIndex, int renderingCount, bool visible)
			{
				#if UNITY_EDITOR
				m_visible = visible;
				#endif
				if (m_context == null || !visible)
					return;

				float scale = 1;
				Factory factory = m_context.factory;
				factory.ConvertMatrix(ref m_matrix, matrix, scale,
				                      m_shouldBeOnTop ? m_zOffset : renderingCount - renderingIndex);
				Factory.MultiplyMatrix(ref m_renderMatrix,
				                       m_context.parent.transform.localToWorldMatrix, m_matrix);
				
				#if LWF_USE_ADDITIONALCOLOR
				factory.ConvertColorTransform(
					ref m_colorMult, ref m_colorAdd, colorTransform);
				#else
				factory.ConvertColorTransform(ref m_colorMult, colorTransform);
				#endif
			}

			////////////////////////////////////////////////////////////
			#region NGUICommonRenderer implementation
			public override void Fill(BetterList<UnityEngine.Vector3> verts, BetterList<UnityEngine.Vector2> uvs, BetterList<UnityEngine.Color32> cols)
			{
				if (m_context == null)
					return;

			    #if UNITY_EDITOR
			    if (!m_visible)
					return;
				#endif

				m_context.Fill(m_matrix, m_colorMult, verts, uvs, cols);
			}
			#endregion
		}

	}	// namespace NGUIRenderer
}	// namespace LWF

// EBG END