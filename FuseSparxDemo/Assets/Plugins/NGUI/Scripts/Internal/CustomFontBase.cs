using UnityEngine;

public abstract class CustomFontBase : MonoBehaviour {

#region Properties

	// Common properties
	public abstract Material material { get; }

	// Bitmap font properties
	public abstract UIFont bitmapFont { get; }
	public abstract bool isBitmap { get; }
	public abstract int bitmapFontSize { get; }

	// TrueType font properties
	public abstract Font trueTypeFont { get; }
	public abstract bool isTrueType { get; }

#endregion

#region Methods

#endregion


}
