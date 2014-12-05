using UnityEngine;
using UnityEditor;
using System.Collections;

public class DrawingUtils 
{
	private static Texture2D _fader = null;
	public static Material _solidMaterial = null;
	private static GUIStyle _style = null;
	private static Rect _clip = new Rect(0,0,10000,10000);
	
	static DrawingUtils()
	{
		Init();
	}
	
	public static void Clip( Rect rc )
	{
		_clip = rc;
	}
	
	private static void GLVertex3( float x, float y, float z )
	{
		x = Mathf.Clamp(x, _clip.xMin,  _clip.xMax);
		y = Mathf.Clamp(y, _clip.yMin, _clip.yMax);
		GL.Vertex3(x,y,z);
	}
	
	private static void GLVertex( Vector3 v )
	{
		GLVertex3(v.x,v.y,v.z);
	}
	
	public static void Init()
	{
		if (_fader == null)
		{
			_fader = new Texture2D(1,1,TextureFormat.ARGB32,false);
			_fader.SetPixel(0,0,UnityEngine.Color.white);
			_fader.Apply();
			_fader.hideFlags = HideFlags.HideAndDontSave;
		}
				
		if ( _solidMaterial == null )
		{
            _solidMaterial = new Material("Shader \"Hidden/GUI/Solid\" {" +
           "SubShader { Pass {" +
           "   BindChannels { Bind \"Color\",color }" +
           "   Blend SrcAlpha OneMinusSrcAlpha" +
           "   ZWrite Off Cull Off Fog { Mode Off }" +
           "} } }");
            _solidMaterial.hideFlags = HideFlags.HideAndDontSave;
            _solidMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if ( _style == null )
		{
			_style = new GUIStyle();
			_style.wordWrap = true;
			_style.alignment = TextAnchor.UpperLeft;
			_style.normal.textColor = Color.white;
		}
	}
	
	public static void DiamondThing( Rect rc, float height, Color fill, Color outline, float alpha )
	{
		fill.a = alpha;
		outline.a = alpha;
		DiamondThing( rc, height, fill, outline );
	}
	
	public static void DiamondThing( Rect rc, float height, Color fill, Color outline )
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		var center = RectCenter(rc);
		const float z=  0.1f;
		
		_solidMaterial.SetPass(0);
		GL.Begin(GL.TRIANGLES);
		GL.Color(fill);
		
		var p1 = new Vector3(center.x, rc.yMin-height, z);
		var p2 = new Vector3(rc.xMin, rc.yMin, z);
		var p3 = new Vector3(rc.xMax, rc.yMin, z);
		
		var p4 = new Vector3(rc.xMax, rc.yMax, z);
		var p5 = new Vector3(rc.xMin, rc.yMax, z);
		var p6 = new Vector3(center.x, rc.yMax+height, z);
		
		
		GLVertex(p1); GLVertex(p2); GLVertex(p3);
		GLVertex(p3); GLVertex(p2); GLVertex(p5);
		GLVertex(p3); GLVertex(p5); GLVertex(p4);
		GLVertex(p4); GLVertex(p5); GLVertex(p6);

		GL.End();
		
		GL.Begin(GL.LINES);
		GL.Color(outline);
		
		GLVertex(p1); GLVertex(p2);
		GLVertex(p2); GLVertex(p5);
		GLVertex(p5); GLVertex(p6);
		GLVertex(p6); GLVertex(p4);
		GLVertex(p4); GLVertex(p3);
		GLVertex(p3); GLVertex(p1);
		
		
		GL.End();
		
	}
	
	public static void Fill( Rect rect, Color c ) 
	{
		Init();
				
		if ( Event.current.type != EventType.Repaint ) return;
		
		GUI.color = c;
		GUI.DrawTexture(rect, _fader);
		
	}
	
	public static void Texture( Rect rect, Texture2D texture )
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		//Quad( rect.xMin, rect.xMax, rect.yMin, rect.yMax ); 
		GUI.color = UnityEngine.Color.white;
		GUI.DrawTexture(rect,texture);
	}
	
	public static Rect Text( string text, Vector3 pt, TextAnchor anchor )
	{
		return Text(text, pt, anchor, Color.white);
	}
	
	public static Rect Text( string text, Vector3 pt, TextAnchor anchor, Color textColor, float alpha )
	{
		textColor.a = alpha;
		return Text( text, pt, anchor, textColor );
	}
	
	public static Rect Text( string text, Vector3 pt, TextAnchor anchor, Color textColor )
	{
		return Text(text, pt, anchor, textColor, true);
	}
		
	public static Rect Text( string text, Vector3 pt, TextAnchor anchor, Color textColor, bool shadow )
	{
		var content = new GUIContent(text);
		var rc = TextRect(text,pt,anchor);
		
		if ( shadow )
		{
			GUI.color = Color.black;
			GUI.Label( new Rect(rc.x+1,rc.y+1,rc.width,rc.height), content, _style ); 
		}
		
		GUI.color = textColor;
		GUI.Label( rc, content, _style ); 
		GUI.color = Color.white;
		
		return rc;
	}
	
	public static Vector2 TextSize( string text ) 
	{
		var content = new GUIContent(text);
		return _style.CalcSize(content);
	}
	
	public static Rect TextRect( string text, Vector3 pt, TextAnchor anchor  )
	{		
		var content = new GUIContent(text);
		var size = _style.CalcSize(content);
		var half = size*0.5f;
				
		switch( anchor )
		{
		case TextAnchor.UpperLeft:
				return new Rect( pt.x, pt.y, size.x, size.y );
		case TextAnchor.UpperCenter:
				return new Rect( pt.x-half.x, pt.y, size.x, size.y );
		case TextAnchor.UpperRight:
				return new Rect( pt.x-size.x, pt.y, size.x, size.y );
		case TextAnchor.MiddleCenter:
				return new Rect( pt.x-half.x, pt.y-half.y, size.x, size.y );
		case TextAnchor.MiddleLeft:
				return new Rect( pt.x, pt.y-half.y, size.x, size.y );
		case TextAnchor.MiddleRight:
				return new Rect( pt.x-size.x, pt.y-half.y, size.x, size.y );
		case TextAnchor.LowerLeft:
				return new Rect( pt.x, pt.y-size.y, size.x, size.y );
		case TextAnchor.LowerCenter:
				return new Rect( pt.x-half.x, pt.y-size.y, size.x, size.y );
		case TextAnchor.LowerRight:
				return new Rect( pt.x-size.x, pt.y-size.y, size.x, size.y );
		}
		
		return default(Rect);
	}
	
	public static Vector2 RectCenter( Rect r )
	{
		return new Vector2(r.xMin+r.xMax,r.yMin+r.yMax)*.5f;
	}
	
	public static Rect RectFromPtSize( Vector2 pt, Vector2 size )
	{
		var half = size*.5f;
		return new Rect( pt.x-half.x, pt.y-half.y, size.x, size.y ); 
	}
	
	public static Rect Union( Rect r1, Rect r2 )
	{
		var xMin = Mathf.Min(r1.xMin,r2.xMin);
		var xMax = Mathf.Max(r1.xMax,r2.xMax);
		
		var yMin = Mathf.Min(r1.yMin,r2.yMin);
		var yMax = Mathf.Max(r1.yMax,r2.yMax);
				
		return new Rect(xMin, yMin, xMax-xMin, yMax-yMin);
	}
	
	public static void Triangle( Vector3 p1, Vector3 p2, Vector3 p3, Color fill, float alpha )
	{
		fill.a = alpha;
		Triangle( p1, p2, p3, fill );
	}
	
	public static void Triangle( Vector3 p1, Vector3 p2, Vector3 p3, Color fill )
	{
		if ( Event.current.type != EventType.Repaint ) return;

		
		_solidMaterial.SetPass(0);
		GL.Begin(GL.TRIANGLES);
		GL.Color(fill);
		
		GLVertex(p1); 
		GLVertex(p2);
		GLVertex(p3);

		GL.End();
	}
	
	public static void Line( Vector3 p1, Vector3 p2, Color fill )
	{
		if ( Event.current.type != EventType.Repaint ) return;

		_solidMaterial.SetPass(0);
		GL.Begin(GL.LINES);
		GL.Color(fill);
		
		GLVertex(p1); 
		GLVertex(p2);

		GL.End();
	}
	
	public static void Quad( Rect r, Color fill, float alpha )
	{
		fill.a = alpha;
		Quad ( r, fill );
	}
	
	public static void Quad( Rect r, Color fill )
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		_solidMaterial.SetPass(0);
		GL.Begin(GL.QUADS);
		GL.Color(fill);
		
		const float z = 0.1f;
		GLVertex3(r.xMin, r.yMin, z); 
		GLVertex3(r.xMax, r.yMin, z); 
		GLVertex3(r.xMax, r.yMax, z); 
		GLVertex3(r.xMin, r.yMax, z); 

		GL.End();
	}
	
	public static void Quad( Rect r, Color fill, Color outline, float alpha )
	{
		fill.a = alpha;
		outline.a = alpha;
		Quad( r, fill, outline );
	}
	
	public static void Quad( Rect r, Color fill, Color outline )
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		Init();

		
		_solidMaterial.SetPass(0);
		GL.Begin(GL.QUADS);
		GL.Color(fill);
		
		const float z = 0.1f;
		GLVertex3(r.xMin, r.yMin, z); 
		GLVertex3(r.xMax, r.yMin, z); 
		GLVertex3(r.xMax, r.yMax, z); 
		GLVertex3(r.xMin, r.yMax, z); 

		GL.End();
		
		GL.Begin(GL.LINES);
		GL.Color(outline);
		
		GLVertex3(r.xMin, r.yMin, z); GLVertex3(r.xMax, r.yMin, z);
		GLVertex3(r.xMax, r.yMin, z); GLVertex3(r.xMax, r.yMax, z);
		GLVertex3(r.xMax, r.yMax, z); GLVertex3(r.xMin, r.yMax, z);
		GLVertex3(r.xMin, r.yMax, z); GLVertex3(r.xMin, r.yMin, z);
		
		GL.End();
	}
	
	public static void Circle( Vector3 pt, float radius, Color fill, Color outline, float alpha )
	{
		fill.a = alpha;
		outline.a = alpha;
		Circle( pt, radius, fill, outline );
	}
	
	public static void Circle( Vector3 pt, float radius, Color fill, Color outline )
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		Init();
		_solidMaterial.SetPass(0);
		
		const int segments = 100;
		float rad = 0;
		float radInc = Mathf.PI * 2.0f / (float)segments;
		Vector3 first = new Vector3( Mathf.Cos(rad), Mathf.Sin(rad),0)*radius + pt;
		Vector3 last;
		
		// draw the circle
		GL.Begin(GL.TRIANGLES);
		GL.Color(fill);
		
		rad = 0;		
		last = first;
		for ( int segment = 0; segment < segments; ++segment )
		{
			rad+=radInc;
			Vector3 next = new Vector3( Mathf.Cos(rad), Mathf.Sin(rad),0)*radius + pt;
			
			GLVertex(last); 
			GLVertex(pt);
			GLVertex(next);
			last = next;
		}
		
		GL.End();
		
		// now the lines
		GL.Begin(GL.LINES);
		GL.Color(outline);
		
		rad = 0;
		last = first;
		for ( int segment = 0; segment < segments; ++segment )
		{
			rad+=radInc;
			Vector3 next = new Vector3( Mathf.Cos(rad), Mathf.Sin(rad),0)*radius + pt;
			GLVertex(last); 
			GLVertex(next);
			last = next;
		}
		
		GL.End();
		
		
	}
	
}
