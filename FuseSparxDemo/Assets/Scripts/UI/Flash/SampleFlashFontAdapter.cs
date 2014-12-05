using UnityEngine;
using System.Collections;

public class SampleFlashFontAdapter : MonoBehaviour, LWF.IFontAdapter
{
	[System.Serializable]
	public class FontMapping
	{
		public string FontName;
		public FontLocalizer FontAsset;
	}

	public FontMapping [] FontMapList;

	public static SampleFlashFontAdapter Instance { get { return sInstance; } }

	void Awake()
	{
		sInstance = this;
	}

	public Material GetFontMaterial(string lwfFontName)
	{
		FontLocalizer font = FontFor(lwfFontName);
		if (font != null)
		{
			return font.material;
		}
		return null;
	}
	
	public bool PrintText(string lwfFontName, string text, BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> colors)
	{
		FontLocalizer font = FontFor(lwfFontName);
		if (font != null)
		{
			if (font.isTrueType)
			{
				NGUIText.dynamicFont = font.trueTypeFont;
				NGUIText.Update();
				NGUIText.Print(text, verts, uvs, colors);
				return true;
			}
			else if (font.isBitmap)
			{
				NGUIText.bitmapFont = font.bitmapFont;
				NGUIText.Update();
				NGUIText.Print (text, verts, uvs, colors);
				return true;
			}
		}
		
		return false;
	}
	
	public void PreProcessText(string lwfFontName, ref string text, ref Color color)
	{
		string locId = text.StartsWith("ID_") ? text : string.Empty;
		
		if (!string.IsNullOrEmpty(locId) && EB.Localizer.ShowStatuses)
		{
			float alpha = color.a;
			color = EB.Localizer.GetColor(locId);
			color.a = alpha;
		}
		
		text = (!string.IsNullOrEmpty(locId)) ? EB.Localizer.GetString(locId) : text;
	}

	private FontLocalizer FontFor(string fontName)
	{
		FontMapping elem = null;
		for (int i = 0; i < FontMapList.Length; ++i)
		{
			if (FontMapList[i].FontName == fontName)
			{
				elem = FontMapList[i];
				break;
			}
		}
		
		if (elem == null)
		{
			Debug.LogError ("No mapping found for font name '" + fontName + "'");
		}
		return elem.FontAsset;
	}
	
	private static SampleFlashFontAdapter sInstance;
}

