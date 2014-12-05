using UnityEngine;
using System.Collections;

public class FontLocalizer : CustomFontBase 
{
	[System.Serializable]
	public class Mapping
	{
		public EB.Language language = EB.Language.Unknown;
		public string fontPath 		= string.Empty;
	}
	
	public Mapping[] Fonts = new Mapping[0];
	public string _defaultFontPath;
	public int _defaultFontSize = 16;
	
#region CustomFontBase
	
	public override Material material
	{
		get
		{
			if (replacement != null) return replacement.material;
			SelectFont();
			if (_selectedBitmapFont != null) return _selectedBitmapFont.material;
			if (_selectedTrueTypeFont != null) return _selectedTrueTypeFont.material;
			return null;
		}
	}
	
	public override UIFont bitmapFont
	{
		get
		{
			if (replacement != null) return replacement.bitmapFont;
			SelectFont();
			return _selectedBitmapFont;
		}
	}
	
	public override bool isBitmap
	{
		get
		{
			if (replacement != null) return replacement.isBitmap;
			SelectFont();
			return _selectedBitmapFont != null;
		}
	}
	
	public override int bitmapFontSize 
	{
		get
		{
			if (replacement != null) return replacement.bitmapFontSize;
			return _selectedBitmapFontSize;
		}
	}
	
	public override Font trueTypeFont
	{
		get
		{
			if (replacement != null) return replacement.trueTypeFont;
			SelectFont();
			return _selectedTrueTypeFont;
		}
	}
	
	public override bool isTrueType
	{
		get
		{
			if (replacement != null) return replacement.isTrueType;
			SelectFont();
			return _selectedTrueTypeFont != null;
		}
	}
	
#endregion CustomFontBase
	
	private void SelectFont()
	{
		EB.Language language = Application.isPlaying ? EB.Localizer.Current : EB.Language.English;
		if (Application.isPlaying && (_selectedBitmapFont != null || _selectedTrueTypeFont != null) && language == _selectedLanguage)
		{
			//Debug.Log (gameObject.name + ": no change in language [" + _selectedLanguage.ToString () + "], not re-selecting font");
			return;
		}
		
		_selectedLanguage = language;
		
		Mapping fo = System.Array.Find<Mapping>(Fonts, delegate(Mapping obj) {
			return obj.language == _selectedLanguage;
		});
		
		if (fo != null)
		{
			LoadFont (fo.fontPath);
		}
		else
		{
			LoadFont (_defaultFontPath);
		}
	}
	
	private void LoadFont(string path)
	{
		Font ttf = Resources.Load (path, typeof(Font)) as Font;
		UIFont bmp = null;
		if (ttf == null)
		{
			GameObject assetGo = Resources.Load (path, typeof(GameObject)) as GameObject;
			if (assetGo != null)
			{
				bmp = assetGo.GetComponent<UIFont>();
			}
		}
		if (ttf != null || bmp != null)
		{
			_selectedTrueTypeFont = ttf;
			_selectedBitmapFont = bmp;
			_selectedBitmapFontSize = (_selectedBitmapFont != null) ? _selectedBitmapFont.defaultSize : _defaultFontSize;
			//EB.Debug.Log(string.Format ("{2} loaded {0} [{1}]", path, (_selectedTrueTypeFont != null) ? "ttf" : "uifont", gameObject.name));
		}
		else
		{
			EB.Debug.LogError(string.Format ("{1} FAILED to load font {0}", path, gameObject.name));
		}
	}

	private UIFont _selectedBitmapFont;
	private Font _selectedTrueTypeFont;
	private int _selectedBitmapFontSize = 16;
	private EB.Language _selectedLanguage = EB.Language.Unknown;
	
	[HideInInspector]
	public FontLocalizer replacement;
	
}
