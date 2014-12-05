using UnityEngine;
using System;
using System.Collections.Generic;

public class GUILineHelper
{
    protected static bool clippingEnabled;
    protected static Rect clippingBounds;
    public static Material lineMaterial;

    /* @ Credit: "http://cs-people.bu.edu/jalon/cs480/Oct11Lab/clip.c" */
    protected static bool clip_test(float p, float q, ref float u1, ref float u2)
    {
        float r;
        bool retval = true;
        if (p < 0.0)
        {
            r = q / p;
            if (r > u2)
                retval = false;
            else if (r > u1)
                u1 = r;
        }
        else if (p > 0.0)
        {
            r = q / p;
            if (r < u1)
                retval = false;
            else if (r < u2)
                u2 = r;
        }
        else
            if (q < 0.0)
                retval = false;

        return retval;
    }

    protected static bool segment_rect_intersection(Rect bounds, ref Vector3 p1, ref Vector3 p2)
    {
        float u1 = 0.0f, u2 = 1.0f, dx = p2.x - p1.x, dy;
        if (clip_test(-dx, p1.x - bounds.xMin, ref u1, ref u2))
            if (clip_test(dx, bounds.xMax - p1.x, ref u1, ref u2))
            {
                dy = p2.y - p1.y;
                if (clip_test(-dy, p1.y - bounds.yMin, ref u1, ref u2))
                    if (clip_test(dy, bounds.yMax - p1.y, ref u1, ref u2))
                    {
                        if (u2 < 1.0)
                        {
                            p2.x = p1.x + u2 * dx;
                            p2.y = p1.y + u2 * dy;
                        }
                        if (u1 > 0.0)
                        {
                            p1.x += u1 * dx;
                            p1.y += u1 * dy;
                        }
                        return true;
                    }
            }
        return false;
    }
	
	public static void DrawArrowhead( Vector2 pt, Vector2 dir, Color color )
	{					
        if (clippingEnabled)
		{
			if (!clippingBounds.Contains(pt))
				return;			
		}
		
		Material material = GUILineHelper.lineMaterial;
		
		material.SetPass(0);
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
		
		float length = 10.0f;
		
		Vector2 perp = new Vector2(dir.y, -dir.x );
		Vector2 p1 = pt - (dir * length) - (perp * length);
		Vector2 p2 = pt;
		Vector2 p3 = pt - (dir * length) + (perp * length);
		
		GL.Vertex3( p1.x, p1.y, 0 ); 
		GL.Vertex3( p2.x, p2.y, 0 );
		GL.Vertex3( p3.x, p3.y, 0 );
		
        GL.End();
	}

	
    public static void BeginGroup(Rect position)
    {
        clippingEnabled = true;
        clippingBounds = position;
//        GUI.BeginGroup( new Rect(0,0,position.width,position.height));
    }

    public static void EndGroup()
    {
//        GUI.EndGroup();
        clippingBounds = new Rect(0, 0, Screen.width, Screen.height);
        clippingEnabled = false;
    }
	
	// Simple line ( 4 lines ) to do a mid-point
	public static void DrawSnapLine( Vector2 pointA, Vector2 pointB, Color color )
	{	
		Vector2 midpoint= pointB-pointA;		
		
		bool bOutputOnRight = false;
		
		if (pointA.x-pointB.x<0.0f)
		{
			bOutputOnRight = true;
		}
		else
		{
			midpoint = pointB-pointA;
		}

		//
		Vector2 connection1 = pointA;
		
		if (bOutputOnRight)
			connection1.x = pointA.x + (midpoint.x/2);
		else
			connection1.x = pointA.x - (midpoint.x/2);
		
		Vector2 connection2 = connection1;
		connection2.y = pointA.y + (midpoint.y);

		DrawLine( pointA, connection1, color);
		DrawLine( connection1, connection2,color);
					
		// 		
		if (connection2.x>pointB.x)
			DrawLine( connection2, pointB,color);
		else
			DrawLine( connection2, pointB,color);
		
		Vector2 dir = new Vector2(connection2.x,connection2.y) - new Vector2(connection1.x,connection1.y);		
		dir.Normalize();
		
		Vector2 finalPos = connection1 + ((connection2-connection1)/2);
		
		DrawArrowhead( finalPos, dir, color );
	}
	
	public static void DrawRect( Rect r, Color col )
	{
		//SR TODO Stuff a rect draw into the LineHelper clas
		Vector2 itemStartLine = new Vector2(r.x, r.y);
		Vector2 itemEndLine = new Vector2(r.x+r.width,r.y);
		GUILineHelper.DrawLine(itemStartLine,itemEndLine, col );//
			
		itemStartLine = new Vector2(r.x+r.width, r.y);
		itemEndLine = new Vector2(r.x+r.width,r.y+r.height);
		GUILineHelper.DrawLine(itemStartLine,itemEndLine, col );//

		itemStartLine = new Vector2(r.x+r.width, r.y+r.height);
		itemEndLine = new Vector2(r.x,r.y+r.height);
		GUILineHelper.DrawLine(itemStartLine,itemEndLine, col );//

		itemStartLine = new Vector2(r.x, r.y+r.height);
		itemEndLine = new Vector2(r.x,r.y);
		GUILineHelper.DrawLine(itemStartLine,itemEndLine, col );//
	}
	
    public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
    {
		Vector3 posA = new Vector3(pointA.x, pointA.y, 0.0f);
		// = GUI.matrix.MultiplyVector(posA);
		
		Vector3 posB = new Vector3(pointB.x, pointB.y, 0.0f);
		//posB = GUI.matrix.MultiplyVector(posB);
			
        if (clippingEnabled)
            if (!segment_rect_intersection(clippingBounds, ref posA, ref posB))
                return;

        if (!lineMaterial)
        {
            /* Credit:  */
            lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
           "SubShader { Pass {" +
           "   BindChannels { Bind \"Color\",color }" +
           "   Blend SrcAlpha OneMinusSrcAlpha" +
           "   ZWrite Off Cull Off Fog { Mode Off }" +
           "} } }");
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }
		
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex(posA);
        GL.Vertex(posB);
        GL.End();
    }
	
	public static Vector2 CubicInterp( Vector2 p0, Vector2 t0, Vector2 p1, Vector2 t1, float a )
	{
		float a2 = a  * a;
		float a3 = a2 * a;

		return (((2*a3)-(3*a2)+1) * p0) + ((a3-(2*a2)+a) * t0) + ((a3-a2) * t1) + (((-2*a3)+(3*a2)) * p1);
	}
	
	public static void DrawCurve( Vector2 start, Vector2 startDir, Vector2 end, Vector3 endDir, int segments, Color c ) 
	{
		Vector2 last = start;
		for(int i = 0; i < segments; ++i)
		{
			float a = ((float)i+1.0f)/(float)segments;
			Vector2 next = CubicInterp(start, startDir, end, endDir, a);
			
			GUILineHelper.DrawLine(last, next, c );
			//Drawing.DrawLine( last, next, c, 2.0f, true ); 

			// If this is the last section, use its direction to draw the arrowhead.
			if( (i == segments-1) )
			{
				Vector2 dir = (next - last);
				dir.Normalize();
				DrawArrowhead( last, dir, c );
			}
			
			last = next;
		}
	}
	
	public static List< KeyValuePair< Vector2, Vector2 > > TesselateLine( Vector2 start, Vector2 startDir, Vector2 end, Vector3 endDir, int segments )
	{
		List< KeyValuePair< Vector2, Vector2 > > lines = new List< KeyValuePair< Vector2, Vector2 > >();
	
		Vector2 delta = (end-start);
		delta.x = Mathf.Abs(delta.x) * 2;
		delta.y = Mathf.Abs(delta.y);
		
		startDir = new Vector2( startDir.x * delta.x, startDir.y * delta.y);
		endDir = new Vector2( endDir.x * delta.x, endDir.y * delta.y);
		
		Vector2 last = start;
		for(int i = 0; i < segments; ++i)
		{
			float a = ((float)i+1.0f)/(float)segments;
			Vector2 next = CubicInterp(start, startDir, end, endDir, a);
			
			KeyValuePair< Vector2, Vector2 > line = new KeyValuePair< Vector2, Vector2 >( last, next );
			lines.Add( line );
			
			last = next;
		}
		
		return lines;
	}
	
	public static void MidPoint( Vector2 start, Vector2 end, Color color ) 
	{
		Vector2 delta = (end-start);
		delta.y = 0;
		
		float tension = Mathf.Abs(delta.x);
		DrawCurve(start, tension*Vector2.right, end, tension*Vector2.right, 100, color );
	}
	
	public static void MidPoint( Vector2 start, Vector2 startDir, Vector2 end, Vector3 endDir, Color color ) 
	{
		Vector2 delta = (end-start);
		delta.x = Mathf.Abs(delta.x) * 2;
		delta.y = Mathf.Abs(delta.y);
		
		startDir = new Vector2( startDir.x * delta.x, startDir.y * delta.y);
		endDir = new Vector2( endDir.x * delta.x, endDir.y * delta.y);
		
		DrawCurve(start, startDir, end, endDir, 100, color );
	}
	
};
