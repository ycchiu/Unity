using UnityEngine;

namespace LWF
{
	/// <summary>
	/// Client interface for NGUI font rendering.
	/// Client must implement this as font selection methods may vary.
	/// The LWFObject wrapper contains API to assign an IFontProvider object that will then be used globally via LWF.NGUIRenderer.Factory's fontProvider field.
	/// </summary>
	public interface IFontAdapter
	{
		/// <summary>
		/// Retrieve the material used to render strings using the given LWF font name.
		/// </summary>
		Material GetFontMaterial(string lwfFontName);

		/// <summary>
		/// Fills the specified NGUI BetterLists with vertex, uv, and colour information for the given text and LWF font name.
		/// </summary>
		bool PrintText(string lwfFontName, string text, BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> colors);

		/// <summary>
		/// Pre-processes the text, given the font name and the original colouring.
		/// This is useful for things like localization or string status display.
		/// </summary>
		/// <param name="lwfFontName">Lwf font name.</param>
		/// <param name="text">Text.</param>
		/// <param name="color">Color.</param>
		void PreProcessText(string lwfFontName, ref string text, ref UnityEngine.Color color);
	}

	public interface ITextureAdapter
	{
		void LoadTexture(string name, System.Action<Texture2D> callback);
		void UnloadTexture(string name, System.Action callback = null);
		void UnloadTexture(Texture2D texture, System.Action callback = null);

		string ProcessTextureName(string textureName);
		float GetPixelSize();
		Shader GetDefaultShader();

		/// <summary>
		/// Retrieves the texture root location.  This can either be empty, in which case textures will be loaded from 'Resources', or it
		/// can be some location under Resources (e.g. Bundles/UI/StreamingTextures/).
		/// </summary>
		/// <value>The texture root location.</value>
		string TextureRootLocation { get; }

		/// <summary>
		/// Retrieves where common UI atlases are stored by the game.  This is a path relative to the Resources directory, and can be digested
		/// by direct Resource.Load calls.
		/// </summary>
		/// <value>The atlas root location.</value>
		string AtlasRootLocation { get; } 

		bool IsHD();
	}
}
