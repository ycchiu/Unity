using UnityEngine;
using System.Collections;

namespace EB
{
	public static class TextureUtils 
	{	
		public static Texture2D GetFader( Color color ) 
		{
			Texture2D fader = new Texture2D(1,1);
			fader.SetPixel(0,0,color);
			fader.Apply();
			Object.DontDestroyOnLoad(fader);
			return fader;
		}
		
	    public static Texture2D FromRenderTexture( RenderTexture rt, bool mipMaps = false, bool readable = true )
	    {
	        var texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, mipMaps);
	        return FromRenderTexture(rt, texture, mipMaps, readable);
	    }
		
		public static Texture2D FromRenderTexture( RenderTexture rt, Texture2D texture, bool mipMaps = false, bool readable = true )
	    {
	        var prev = RenderTexture.active;
	        RenderTexture.active = rt;
	        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
	        RenderTexture.active = prev;
	
	        texture.Apply(mipMaps, !readable);
	        return texture;
	    }
		
	    public static byte[] ToPNG(RenderTexture rt)
	    {
	        var tmp = FromRenderTexture(rt);
	        var bytes = ToPNG(tmp);
	        Object.Destroy(tmp);
	        return bytes;
	    }
	
	    public static byte[] ToPNG( Texture2D texture )
	    {
	        return texture.EncodeToPNG();
	    }
	
	    public static byte[] ToJPG(RenderTexture rt, float quality)
	    {
	        var tmp = FromRenderTexture(rt);
	        var bytes = ToJPG(tmp,quality);
	        Object.Destroy(tmp);
	        return bytes;
	    }
	
	    public static byte[] ToJPG( Texture2D texture, float quality )
	    {
	        JPGEncoder encoder = new JPGEncoder(texture, quality);
	        return encoder.GetBytes();
	    }
	}	
}

