using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EBWorldPainterData : MonoBehaviour
{
	public Bounds worldBounds;
	public List<Point> points;
	public List<Region> regions;
	static int nextRegionID;
	
	public EBWorldPainterData()
	{
		nextRegionID = 0;
		points = new List<Point>();
		regions = new List<Region>();
	}

	public enum eDATA_LAYER
	{
		LightmapSize,
		RenderSetting
	}
	
	public enum eLIGHTMAP_SIZE
	{
		_0 = 0,
		_32 = 32,
		_64 = 64,
		_128 = 128,
		_256 = 256,
		_512 = 512,
		_1024 = 1024,
		_2048 = 2048
	}

	public enum eRENDER_SETTING
	{
		_0 = 0,
		_1 = 1,
		_2 = 2,
		_3 = 3
	}
	
	[System.Serializable]
	public class DataLayer
	{
		public int value;

		public DataLayer()
		{
			value = 0;
		}
	}
	
	[System.Serializable]
	public class Point
	{
		public Vector2 location;
		
		public Point()
		{
			location = Vector2.zero;
		}
		
		public Point(Vector2 l)
		{
			location = l;
		}
		
		public Point(float x, float y)
		{
			location = new Vector2(x, y);
		}
	}
	
	[System.Serializable]
	public class Region
	{
		public int id;
		public List<Point> points;
		public List<int> visibleRegions;
		public List<DataLayer> dataLayers;
		//used at track build time and runtime for culling; this is the bounds of the geometry inside
		public Bounds bounds;
		
		public Region()
		{
			id = nextRegionID++;
			points = new List<Point>();
			visibleRegions = new List<int>();
			dataLayers = new List<DataLayer>();
			for (int i = 0; i < System.Enum.GetNames(typeof(eDATA_LAYER)).Length; ++i)
			{
				dataLayers.Add(new DataLayer());
			}
			bounds = new Bounds();
		}
		
		public bool ContainsPoint(Point p)
		{
			return points.Contains(p);
		}
		
		public bool ContainsLine(Point p1, Point p2)
		{
			for(int i = 0; i < points.Count; ++i)
			{
				if ((points[i] == p1 && points[(i+1)%points.Count] == p2) || (points[i] == p2 && points[(i+1)%points.Count] == p1))
				{
					return true;
				}
			}
			return false;
		}
		
		public Point PointBefore(Point p)
		{
			if (!ContainsPoint(p))
				return null;
			if (points.IndexOf(p) == 0)
				return points[points.Count - 1];
			return points[points.IndexOf(p) - 1];
		}
		
		public Point PointAfter(Point p)
		{
			if (!ContainsPoint(p))
				return null;
			return points[(points.IndexOf(p)+1)%points.Count];
		}
		
		//'rotates' the points so that p1 starts and p2 ends, or p2 starts and p1 ends, whichever is first
		public bool MakeStartEnd(Point p1, Point p2)
		{
			if (!ContainsLine(p1, p2))
			{
				return false;
			}
			
			int endPoint = points.Count - 1;
			while(!((points[0] == p1 && points[endPoint] == p2) || (points[0] == p2 && points[endPoint] == p1)))
			{
				points.Add(points[0]);
				points.RemoveAt(0);
			}
			return true;
		}
		
		public bool InsertPoint(Point p, Point pBefore, Point pAfter)
		{
			//we can't insert a point more than once
			if (points.IndexOf(p) != -1)
			{
				return false;
			}
			
			for(int i = 0; i < points.Count; ++i)
			{
				if ((points[i] == pBefore && points[(i+1)%points.Count] == pAfter) || (points[i] == pAfter && points[(i+1)%points.Count] == pBefore))
				{
					points.Insert(i+1, p);
					return true;
				}
			}
			
			//we didn't find those two points to insert between
			return false;
		}
		
		public Region SplitAcrossPoints(Point p1, Point p2)
		{
			//can't split across the same point
			if (p1 == p2)
				return null;
			
			bool hasP1 = false;
			bool hasP2 = false;
			foreach(Point p in points)
			{
				if (p == p1)
					hasP1 = true;
				if (p == p2)
					hasP2 = true;
				if (hasP1 && hasP2)
					break;
			}
			
			//our region doesn't have both those points
			if (!hasP1 || !hasP2)
				return null;
			
			//order the points as they appear in the region
			if (points.IndexOf(p1) > points.IndexOf(p2))
			{
				Point temp = p1;
				p1 = p2;
				p2 = temp;
			}
		
			//make the new region, walking backwards and removing the points from the old region
			Region newRegion = new Region();
			
			newRegion.points.Add(p2);
			bool seenP2 = false;
			
			for(int i = points.Count - 1; i >= 0; --i)	
			{
				if(!seenP2)
				{
					if (points[i] == p2)
						seenP2 = true;
				}
				else if (points[i] == p1)
				{
					break;
				}
				else
				{	
					newRegion.points.Add(points[i]);
					points.RemoveAt(i);
				}
			}
				
			newRegion.points.Add(p1);
			
			return newRegion;
		}
		
		public bool SeesRegion(Region region)
		{
			return visibleRegions.Contains(region.id);
		}

		public void SetSeesRegion(Region region, bool sees)
		{
			if (sees && !visibleRegions.Contains(region.id))
			{
				visibleRegions.Add(region.id);
			}
			else if (!sees && visibleRegions.Contains(region.id))
			{
				visibleRegions.Remove(region.id);
			}
		}
		
		public void ToggleSeesRegion(Region region)
		{
			if (!visibleRegions.Contains(region.id))
			{
				visibleRegions.Add(region.id);
			}
			else
			{
				visibleRegions.Remove(region.id);
			}
		}

		public Point Center()
		{
			Vector2 avgPos = Vector2.zero;
			for(int i = 0; i < points.Count; ++i)
			{
				avgPos += points[i].location;
			}
			Vector2 center = avgPos / points.Count;
			return new Point(center);
		}
	}
	
	public void Clone(EBWorldPainterData toClone)
	{
		worldBounds = toClone.worldBounds;
		points = new List<Point>(toClone.points);
		regions = new List<Region>(toClone.regions);
	}
	
	//we work with references, not values, so we need to hook everything back up
	public void Unpack()
	{
		nextRegionID = regions.Count;
		
		foreach(Region region in regions)
		{
			for(int i = region.points.Count - 1; i >= 0; --i)
			{
				bool found = false;
				foreach(Point originalPoint in points)
				{
					if (region.points[i].location == originalPoint.location)
					{
						region.points[i] = originalPoint;
						found = true;
						break;
					}
				}
				if (!found)
				{
					Debug.LogWarning("Can't hook back up a region point!");
				}
			}
			if (region.dataLayers == null || (region.dataLayers.Count < System.Enum.GetNames(typeof(eDATA_LAYER)).Length))
			{
				List<DataLayer> oldDataLayers = region.dataLayers;
				region.dataLayers = new List<DataLayer>();
				for (int i = 0; i < region.dataLayers.Count; ++i)
				{
					region.dataLayers.Add(new DataLayer());
				}
				if (oldDataLayers != null)
				{
					for(int i = 0; i < oldDataLayers.Count; ++i)
					{
						region.dataLayers[i] = oldDataLayers[i];
					}
				}
			}
		}
	}
	
	public Point ClosestPoint(Point p)
	{
		int minPoint = -1;
		float minDistance = float.MaxValue;
		for(var i = 0; i < points.Count; ++i)
		{
			float distance = Vector2.Distance(p.location, points[i].location);
			if (distance < minDistance)
			{
				minPoint = i;
				minDistance = distance;
			}
		}
		return points[minPoint];
	}
	
	public Point[] ClosestLine(Point p)
	{
		Vector2 p0 = p.location;

		float minDistance = float.MaxValue;
		Point best1 = null;
		Point best2 = null;
		
		foreach(var region in regions)
		{
			for(int i = 0; i < region.points.Count; ++i)
			{
				Point point1 = region.points[i];
				Point point2 = region.points[(i+1) % region.points.Count];

				Vector2 p1 = point1.location;
				Vector2 p2 = point2.location;
			
				float lineToPointDistance = Mathf.Abs(((p2.x - p1.x) * (p1.y - p0.y)) - ((p1.x - p0.x) * (p2.y - p1.y)))/Mathf.Sqrt(((p2.x - p1.x)*(p2.x - p1.x)) + ((p2.y - p1.y)*(p2.y - p1.y)));
				float pointToPointDistance = Vector2.Distance(p1, p0) + Vector2.Distance(p2, p0);
				
				if (lineToPointDistance * pointToPointDistance < minDistance)
				{
					best1 = point1;
					best2 = point2;
					minDistance = lineToPointDistance * pointToPointDistance;
				}
			}
		}
		
		return new Point[] { best1, best2 };
	}
	
	public Bounds WorldBounds()
	{
		return worldBounds;
	}
	
	public void SetWorldBounds(Bounds wb)
	{ 
		worldBounds = wb;
		if (regions.Count == 0)
		{
			var r = new Region();
			Vector2 c1 = new Vector2(worldBounds.min.x, worldBounds.min.z);
			Vector2 c2 = new Vector2(worldBounds.min.x, worldBounds.max.z);
			Vector2 c3 = new Vector2(worldBounds.max.x, worldBounds.max.z);
			Vector2 c4 = new Vector2(worldBounds.max.x, worldBounds.min.z);
			points.Add(new Point(c1));
			points.Add(new Point(c2));
			points.Add(new Point(c3));
			points.Add(new Point(c4));
			r.points.Add(points[0]);
			r.points.Add(points[1]);
			r.points.Add(points[2]);
			r.points.Add(points[3]);
			regions.Add(r);
		}
	}
	
	public void SetWorldBounds(MeshRenderer[] renderers)
	{
		Bounds bounds = new Bounds();
		
		if (renderers.Length == 0)
		{
			return;
		}
		else
		{
			bounds = renderers[0].bounds;
		}
		
		for (var i = 1; i < renderers.Length; ++i)
		{
			bounds.Encapsulate(renderers[i].bounds);
		}
		
		SetWorldBounds(bounds);
	}

	public void SetRegionBounds(MeshRenderer[] renderers)
	{
		Dictionary<Region, bool> boundsSet = new Dictionary<Region, bool>();
		foreach(Region region in regions)
		{
			boundsSet[region] = false;
		}

		foreach(Renderer renderer in renderers)
		{
			Point p = new Point(renderer.bounds.center.x, renderer.bounds.center.z);
			Region[] r = RegionsPointIsInside(p);
			if (r.Length == 0)
			{
				r = new Region[] { ClosestRegionToPoint(p) };
			}
			Region region = r[0];
			if (!boundsSet[region])
			{
				region.bounds = renderer.bounds;
				boundsSet[region] = true;
			}
			else
			{
				region.bounds.Encapsulate(renderer.bounds);
			}
		}
	}
	
	public bool InsertPoint(Point pointToAdd, Point pBefore, Point pAfter)
	{
		points.Add(pointToAdd);
		bool addedToSomeRegion = false;
		
		foreach(EBWorldPainterData.Region region in regions)
		{
			addedToSomeRegion |= region.InsertPoint(pointToAdd, pBefore, pAfter);
		}
		
		return addedToSomeRegion;
	}

	public bool RemovePoint(Point p)
	{
		if (!points.Contains(p))
		{
			return false;
		}
		int count = 0;
		foreach(EBWorldPainterData.Region region in regions)
		{
			if (region.ContainsPoint(p) && (region.points.Count > 3))
			{
				++count;
			}
		}
		if (count != 1)
		{
			//point is shared betweeen regions; can't remove it
			return false;
		}

		//we can remove it
		points.Remove(p);
		foreach(EBWorldPainterData.Region region in regions)
		{
			if (region.ContainsPoint(p))
			{
				region.points.Remove(p);
				break;
			}
		}
		return true;
	}
	
	public bool SplitRegionAcrossPoints(Region region, Point p1, Point p2)
	{
		Region splitRegion = region.SplitAcrossPoints(p1, p2);
		if (splitRegion == null)
		{
			return false;
		}

		//these new regions are visible from each other
		splitRegion.SetSeesRegion(region, true);
		splitRegion.visibleRegions.AddRange(region.visibleRegions);
		region.SetSeesRegion(splitRegion, true);
		
		regions.Add(splitRegion);
		
		return true;
	}
	
	public bool RemoveLine(Point p1, Point p2)
	{
		var rs = RegionsContainingLine(p1, p2);
		if (rs.Length != 2)
		{
			//we only want to join two regions back together, so we know we don't leave any orphaned points or lines
			return false;
		}

		var region1 = rs[0];
		var region2 = rs[1];
		
		Point r1before = (region1.PointBefore(p1) == p2) ? region1.PointAfter(p1) : region1.PointBefore(p1);
		Point r1after = (region1.PointBefore(p2) == p1) ? region1.PointAfter(p2) : region1.PointBefore(p2);
		Point r2before = (region2.PointBefore(p1) == p2) ? region2.PointAfter(p1) : region2.PointBefore(p1);
		Point r2after = (region2.PointBefore(p2) == p1) ? region2.PointAfter(p2) : region2.PointBefore(p2);
		
		if ((r1before == r2before) || (r1before == r2after) || (r1after == r2before) || (r1after == r2after))
		{
			//we would orphan en edge
			return false;
		}
		
		region1.MakeStartEnd(p1, p2);
		region2.MakeStartEnd(p1, p2);
		
		if (region1.points[0] == region2.points[0])
		{
			region2.points.Reverse();
		}
		
		//take of the first point of each (as they are both duplicated in each list)
		region1.points.RemoveAt(0);
		region2.points.RemoveAt(0);
		
		//make the new region and remove the two old ones
		Region newRegion = new Region();
		newRegion.points = new List<Point>(region1.points);
		newRegion.points.AddRange(region2.points);
		
		//visible regions are the combination of these two regions
		newRegion.visibleRegions.AddRange(region1.visibleRegions);
		foreach(int regionID in region2.visibleRegions)
		{
			if (!newRegion.visibleRegions.Contains(regionID))
			{
				newRegion.visibleRegions.Add(regionID);
			}
		}
		
		regions.Remove(region1);
		regions.Remove(region2);

		//remove any regions that saw the regions we just combined, and add back this one as visible
		foreach(Region region in regions)
		{
			bool contained = false;
			if (region.SeesRegion(region1))
			{
				region.SetSeesRegion(region1, false);
				contained = true;
			}
			if (region.SeesRegion(region2))
			{
				region.SetSeesRegion(region2, false);
				contained = true;
			}
			if (contained)
			{
				region.SetSeesRegion(newRegion, true);
			}
		}

		regions.Add(newRegion);
		
		return true;
	}
	
	public Region[] RegionsContainingPoints(Point p1, Point p2)
	{
		List<Region> regionsMatching = new List<Region>();

		foreach(var region in regions)
		{
			if (region.ContainsPoint(p1) && region.ContainsPoint(p2))
			{
				regionsMatching.Add(region);
			}
		}
		
		return regionsMatching.ToArray();
	}
	
	public Region[] RegionsContainingLine(Point p1, Point p2)
	{
		List<Region> regionsMatching = new List<Region>();

		foreach(var region in regions)
		{
			if (region.ContainsLine(p1, p2))
			{
				regionsMatching.Add(region);
			}
		}
		
		return regionsMatching.ToArray();
	}
	
	public Region[] RegionsPointIsInside(Point p1)
	{
		Point p2 = new Point(-100000.0f, -100000.0f);
		
		List<Region> regionsMatching = new List<Region>();

		foreach(var region in regions)
		{
			int crosses = 0;
			for(int i = 0; i < region.points.Count; ++i)
			{
				Point p3 = region.points[i];
				Point p4 = region.points[(i+1)%region.points.Count];
				if (LinesCross(p1, p2, p3, p4))
				{
					crosses++;
				}
			}
			if (crosses % 2 == 1)
			{
				regionsMatching.Add(region);
			}
		}
		
		return regionsMatching.ToArray();
	}

	public Region ClosestRegionToPoint(Point p)
	{
		float closestDistance = float.MaxValue;
		Region closestRegion = null;
		
		foreach(var region in regions)
		{
			Point center = region.Center();
			float distance = Vector2.Distance(p.location, center.location);
			if (distance < closestDistance)
			{
				closestRegion = region;
				closestDistance = distance;
			}
		}

		return closestRegion;
	}
	
	// |a b|
	// |c d|
	private static float Determinant(float a, float b, float c, float d)
	{
		return (a * d) - (b * c);
	}
	
	private static float Cross(Vector2 a, Vector2 b)
	{
		return (a.x * b.y) - (a.y * b.x);
	}
	
	private static bool LinesCross(Point p1, Point p2, Point p3, Point p4)
	{
		var p = p1.location;
		var r = p2.location - p1.location;
		var q = p3.location;
		var s = p4.location - p3.location;
		
		float rxs = Cross(r, s);
		
		if (rxs == 0)
		{
			//parallel
			return false;
		}
		
		float t = Cross((q - p), s) / rxs;
		float u = Cross((q - p), r) / rxs;
		
		if (t > 1 || t < 0 || u > 1 || u < 0)
		{
			return false;
		}
		
		return true;
	}
}
