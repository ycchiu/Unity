using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ExtensionMethods 
{
	public static class RectExtensions
	{
		public static Rect ShiftBy( this Rect rect, float x, float y )
		{
			rect.x += x;
			rect.y += y;
			return rect;
		}
		
		public static Vector2 Position( this Rect rect )
		{
			return new Vector2( rect.x, rect.y );
		}
	
	    public static bool Intersect( this Rect rect, Rect candidate )
	    {
	    	bool noOverlap = ( rect.xMin > candidate.xMax ) ||
	    						( rect.xMax < candidate.xMin ) ||
	    						( rect.yMin > candidate.yMax ) ||
	    						( rect.yMax < candidate.yMin );
	    	return noOverlap == false;
	    }
	    
	    public static bool Contains( this Rect rect, Vector2 start, Vector2 end )
	    {
	    	bool contains = false;
	    	
	    	float left = ( start.x < end.x ) ? start.x : end.x;
	    	float right = ( start.x <= end.x ) ? end.x : start.x;
	    	float top = ( start.y < end.y ) ? start.y : end.y;
	    	float bottom = ( start.y <= end.y ) ? end.y : start.y;
	    	Rect lineBBox = Rect.MinMaxRect( left, top, right, bottom );
	    	
	    	bool bboxIntersect = rect.Intersect( lineBBox );
	    	if( bboxIntersect == true )
	    	{
	    		float slope = ( end.y - start.y ) / ( end.x - start.x );
	    		float offset = start.y - slope * start.x;
	    		
	    		float topLeft = slope * rect.xMin + offset - rect.yMin;
	    		float bottomLeft = slope * rect.xMin + offset - rect.yMax;
	    		float topRight = slope * rect.xMax + offset - rect.yMin;
	    		float bottomRight = slope * rect.xMax + offset - rect.yMax;
	    		
	    		bool allAboveOrBelow = ( ( topLeft > 0.0f ) && ( bottomLeft > 0.0f ) && ( topRight > 0.0f ) && ( bottomRight > 0.0f ) ) ||
	    								( ( topLeft < 0.0f ) && ( bottomLeft < 0.0f ) && ( topRight < 0.0f ) && ( bottomRight < 0.0f ) );
	    		
	    		contains = allAboveOrBelow == false;			
	    	}
	    	
	    	return contains;
	    }
	    
	    public static bool Contains( this Rect rect, Vector2 start, Vector2 startDir, Vector2 end, Vector3 endDir, int tesselationSegments )
	    {
	    	bool contains = false;
	    	
	    	List< KeyValuePair<Vector2, Vector2 > > segments = GUILineHelper.TesselateLine( start, startDir, end, endDir, 10 );
			foreach( KeyValuePair<Vector2, Vector2 > segment in segments )
			{
				if( rect.Contains( segment.Key, segment.Value ) == true )
				{
					contains = true;
					break;
				}
			}
	    	
	    	return contains;
	    }
	    
	    public static Rect Inflate( this Rect rect, float width, float height )
	    {
	    	rect.x -= width;
	    	rect.y -= height;
	    	rect.width += ( width * 2.0f );
	    	rect.height += ( height * 2.0f );
	    	return rect;
	    }
		
	public static Rect ScaleSizeBy(this UnityEngine.Rect rect, float scale)
    	{
    	    return rect.ScaleSizeBy(new Vector2(scale, scale), Vector2.zero);
    	}
	    	    
	public static Rect ScaleSizeBy(this UnityEngine.Rect rect, Vector2 scale)
    	{
    	    return rect.ScaleSizeBy(scale, Vector2.zero);
    	}
    	
	    public static Rect ScaleSizeBy(this UnityEngine.Rect rect, float scale, Vector2 pivotPoint)
    	{
    	    return rect.ScaleSizeBy(new Vector2(scale, scale), pivotPoint);
    	}    	
    	
	public static Rect ScaleSizeBy(this UnityEngine.Rect rect, Vector2 scale, Vector2 pivotPoint)
	{
	        Rect result = rect;
	        result.x -= pivotPoint.x;
	        result.y -= pivotPoint.y;
	        result.xMin *= scale.x;
	        result.xMax *= scale.x;
	        result.yMin *= scale.y;
	        result.yMax *= scale.y;
	        result.x += pivotPoint.x;
	        result.y += pivotPoint.y;
	        return result;
	    }
	    
		public static Vector2 TopLeft(this Rect rect)
	    {
	        return new Vector2(rect.xMin, rect.yMin);
	    }
	    
	    public static Rect Transform(this Rect rect, UnityEngine.Matrix4x4 mat)
	    {
	    	Rect result = rect;
	    	Vector3[] points = new Vector3[] { new Vector3(rect.xMin, rect.yMin, 0f), new Vector3(rect.xMax, rect.yMax, 0f) };
	    	for( int i = 0; i < 2; i++)
	    	{
				points[i] = mat.MultiplyPoint3x4(points[i]);
	    	}
	    	
	    	result.xMin = points[0].x;
	    	result.xMax = points[1].x;
	    	result.yMin = points[0].y;
	    	result.yMax = points[1].y;
	    	
	    	return result;
	    }
	}
}
