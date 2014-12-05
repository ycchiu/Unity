using UnityEngine;
using System.Collections;

public class FontSelector : MonoBehaviour 
{
	[System.Serializable]
	public class FontOption
	{
		public EB.Language language = EB.Language.Unknown;
		public string fontPath 		= string.Empty;
	}
	
	public FontOption[] Fonts = new FontOption[0];
	public string _defaultFontPath;
	public int _defaultFontSize = 16;
		
	public UIFont selectedFont
	{
		get
		{
			if (replacement != null) return replacement.selectedFont;
			SelectFont();
			return _selectedFont;
		}
	}
	
	public int selectedFontSize 
	{
		get
		{
			if (replacement != null) return replacement.selectedFontSize;
			return _selectedFontSize;
		}
	}
	
	public Font selectedTrueTypeFont
	{
		get
		{
			if (replacement != null) return replacement.selectedTrueTypeFont;
			SelectFont();
			return _selectedDynamicFont;
		}
	}
	
	public Material material
	{
		get
		{
			if (replacement != null) return replacement.material;
			SelectFont();
			if (_selectedFont != null) return _selectedFont.material;
			if (_selectedDynamicFont != null) return _selectedDynamicFont.material;
			return null;
		}
	}
	
	public bool premultipliedAlpha
	{
		get
		{
			if (replacement != null) return replacement.premultipliedAlpha;
			SelectFont();
			if (_selectedFont != null) return _selectedFont.premultipliedAlphaShader;
			return false;
		}
	}
	
	public bool hasSymbols
	{
		get
		{
			if (replacement != null) return replacement.hasSymbols;
			SelectFont();
			return (_selectedFont != null) ? _selectedFont.hasSymbols : false;
		}
	}
	
	private void SelectFont()
	{
		EB.Language language = Application.isPlaying ? EB.Localizer.Current : EB.Language.English;
		if (Application.isPlaying && (_selectedFont != null || _selectedDynamicFont != null) && language == _selectedLanguage)
		{
			//Debug.Log (gameObject.name + ": no change in language [" + _selectedLanguage.ToString () + "], not re-selecting font");
			return;
		}
		
		_selectedLanguage = language;
		
		FontOption fo = System.Array.Find<FontOption>(Fonts, delegate(FontOption obj) {
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
			_selectedDynamicFont = ttf;
			_selectedFont = bmp;
			_selectedFontSize = (_selectedFont != null) ? _selectedFont.defaultSize : _defaultFontSize;
			//Debug.Log (string.Format ("{2} loaded {0} [{1}]", path, (_selectedDynamicFont != null) ? "ttf" : "uifont", gameObject.name));
		}
		else
		{
			Debug.LogError (string.Format ("{1} FAILED to load font {0}", path, gameObject.name));
		}
	}
	
	private UIFont _selectedFont;
	private Font _selectedDynamicFont;
	private int _selectedFontSize = 16;
	private EB.Language _selectedLanguage = EB.Language.Unknown;
	
	[HideInInspector]
	public FontSelector replacement;

}
