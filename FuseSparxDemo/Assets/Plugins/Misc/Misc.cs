using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Misc 
{		
			
	public static bool IsPhone()
	{
#if UNITY_IPHONE
		switch( iPhone.generation)
		{
		case iPhoneGeneration.iPhone:
		case iPhoneGeneration.iPhone3G:
		case iPhoneGeneration.iPhone3GS:
		case iPhoneGeneration.iPhone4:
		case iPhoneGeneration.iPhone4S:
		case iPhoneGeneration.iPhone5:
		case iPhoneGeneration.iPhone5C:
		case iPhoneGeneration.iPhone5S:
		case iPhoneGeneration.iPhoneUnknown:
		case iPhoneGeneration.iPodTouch1Gen:
		case iPhoneGeneration.iPodTouch2Gen:
		case iPhoneGeneration.iPodTouch3Gen:
		case iPhoneGeneration.iPodTouch4Gen:
		case iPhoneGeneration.iPodTouchUnknown:
			return true;
		}
#endif	
		return false;	
	}
	
	public static bool IsTablet()
	{
#if UNITY_IPHONE
		switch( iPhone.generation)
		{
		case iPhoneGeneration.iPad1Gen:
		case iPhoneGeneration.iPad2Gen:
		case iPhoneGeneration.iPad3Gen:
		case iPhoneGeneration.iPad4Gen:
		case iPhoneGeneration.iPad5Gen:
		case iPhoneGeneration.iPadMini1Gen:
		case iPhoneGeneration.iPadMini2Gen:
		case iPhoneGeneration.iPadUnknown:
			return true;
		}
#endif	
		return false;	
	}	

	public static bool HDAtlas()
	{
#if UNITY_IPHONE
		switch( iPhone.generation)
		{
		case iPhoneGeneration.iPhone4:
		case iPhoneGeneration.iPhone4S:
		case iPhoneGeneration.iPhone5:
		case iPhoneGeneration.iPodTouch4Gen:
		case iPhoneGeneration.iPodTouch5Gen:
		case iPhoneGeneration.iPad1Gen:
		case iPhoneGeneration.iPad2Gen:
		case iPhoneGeneration.iPadMini1Gen:
			return false;

		case iPhoneGeneration.iPad3Gen:
		case iPhoneGeneration.iPad4Gen:
		case iPhoneGeneration.iPadMini2Gen:
		case iPhoneGeneration.iPad5Gen:
		case iPhoneGeneration.iPadUnknown:
			return true;
		}
#elif UNITY_ANDROID
		// TODO: what metric to use for Android?
		if(Screen.dpi > 300f)
		{
			return true;
		}		
#endif	
		return false;			
	
	}

	public static bool IsRetina()
	{
#if UNITY_IPHONE
		switch( iPhone.generation)
		{
		case iPhoneGeneration.iPhone4:
		case iPhoneGeneration.iPhone4S:
		case iPhoneGeneration.iPhone5:
		case iPhoneGeneration.iPhone5C:
		case iPhoneGeneration.iPhone5S:
		case iPhoneGeneration.iPodTouch4Gen:
		case iPhoneGeneration.iPodTouch5Gen:
		case iPhoneGeneration.iPad3Gen:
		case iPhoneGeneration.iPad4Gen:
		case iPhoneGeneration.iPad5Gen:
		case iPhoneGeneration.iPadMini2Gen:
		case iPhoneGeneration.iPhoneUnknown:
		case iPhoneGeneration.iPadUnknown:
		case iPhoneGeneration.iPodTouchUnknown:
			return true;
		}
#elif UNITY_ANDROID
		if(Screen.dpi > 300f)
		{
			return true;
		}		
#endif	
		return false;			
	}
	
	public static bool IsSlow()
	{
#if UNITY_IPHONE && !UNITY_EDITOR
		switch( iPhone.generation)
		{
		case iPhoneGeneration.iPhone4:
		case iPhoneGeneration.iPodTouch4Gen:
 			return true;
		}
#elif EDITOR_PRETEND_SLOW
		return true;
#endif	
		return false;
	}
	
	public static float OSVersion()
	{
		float osVersion = -1f;
#if UNITY_IPHONE
		// System.Info.operatingSystem returns something like "iPhone OS 6.1.3"		
		string versionString = SystemInfo.operatingSystem.Replace("iPhone OS ", "");
		float.TryParse(versionString.Substring(0,1), out osVersion);
#endif
		return osVersion;
	}
	
	public static Mesh MakeQuadMesh(float xmin, float ymin, float xmax, float ymax)
	{	
		var mesh = new Mesh();
		mesh.vertices = new Vector3[] { new Vector3(xmin, ymin, 0), new Vector3(xmin, ymax,0), new Vector3(xmax, ymax, 0), new Vector3(xmax, ymin, 0), };
		mesh.uv = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0), };
		mesh.triangles = new int[]{ 0, 1, 3, 1, 2, 3 };
		
		return mesh;
	}
	
	public static Mesh MakeCircleMesh(float radius, int segs)
	{
		Mesh m = new Mesh();
		Vector3[] v = new Vector3[segs+1];
		Vector2[] uv = new Vector2[segs+1];
		int[] t = new int[segs*3];
			
		float delta = (360.0f/segs)*Mathf.Deg2Rad;
		float sina = Mathf.Sin(delta);
		float cosa = Mathf.Cos(delta);
		
		float x = 1.0f;
		float y = 0.0f;
		
		int i=0;
		for (;i<segs; ++i)
		{
			v[i] = new Vector3(x*radius, y*radius, 0.0f);
			
			uv[i] = new Vector2(x*radius + 0.5f, y*radius + 0.5f);
			float x_ = x*cosa - y*sina;
			y = x*sina + y*cosa;
			x = x_;
			
			int baseIdx = (i*3);
			t[baseIdx] = (i+1)%segs;
			t[baseIdx+1] = i%segs;
			t[baseIdx+2] = segs;
		}
	
		v[i] = Vector3.zero;
		uv[i] = new Vector2(0.5f, 0.5f);
		
		m.vertices = v;
		m.triangles = t;
		m.uv = uv;
		
		return m;
	}
	
	
	public static GameObject[] GetObjects(GameObject obj, string name)
    {
		List<GameObject> list = new List<GameObject>();
		
        if ( obj == null ) return list.ToArray();
		
        if (obj.name.Contains(name))
        {
			list.Add(obj);
        }

		foreach (UnityEngine.Transform t in obj.transform)
        {
			foreach( var tt in GetObjects(t.gameObject,name))
			{
				list.Add(tt);
			}
        }

        return list.ToArray();
    }
	
    public static GameObject GetObject(GameObject obj, string name)
    {
        return GetObject(obj, name, false);
    }

    public static GameObject GetObjectExactMatch(GameObject obj, string name)
    {
        return GetObject(obj, name, true);
    }

    public static GameObject GetObject(GameObject obj, string name, bool bExactMatch )
    {
		if ( obj == null ) return null;
		
        if (!bExactMatch)
        {
            if (obj.name.Contains(name))
            {
                return obj;
            }
        }
        else if (obj.name == name)
        {
            return obj;
        }

        foreach (UnityEngine.Transform t in obj.transform)
        {
            GameObject result = GetObject(t.gameObject, name, bExactMatch);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
	
	public static T FindOrAdd<T>(GameObject root) where T: Component
	{
		var t = root.GetComponent<T>();
		if (!t) {
			return root.AddComponent<T>();
		}
		return t;
	}
	
	public static T FindInTreeOrAdd<T>(GameObject root) where T: Component
	{
		var t = root.GetComponentInChildren<T>();
		if (!t) {
			return root.AddComponent<T>();
		}
		return t;
	}		
	
	public static string GetLocaleSuffix() 
    {
		switch(EB.Version.GetCountry())
		{
		case EB.Country.Argentina:
		case EB.Country.Bolivia:
		case EB.Country.Chile:
		case EB.Country.Colombia:
		case EB.Country.CostaRica:
		case EB.Country.DominicanRepublic:
		case EB.Country.Ecuador:
		case EB.Country.ElSalvador:
		case EB.Country.Guatemala:
		case EB.Country.Honduras:
		case EB.Country.Mexico:
		case EB.Country.Nicaragua:
		case EB.Country.Panama:
		case EB.Country.Paraguay:
		case EB.Country.Peru:
		case EB.Country.Uruguay:
		case EB.Country.Venezuela:
			return "SP_LATAM";
		
		case EB.Country.Italy:
			return "ITY";
			
		case EB.Country.Russia:
			return "RUS";
			
		case EB.Country.Spain:
			return "SP_SPAIN";
		
		case EB.Country.Japon:
			return "JAP";

		case EB.Country.Korea:
			return "KOR";
			
		case EB.Country.France:
			return "FR";
		
		case EB.Country.Germany:
			return "GER";
			
		case EB.Country.China:
			return "SIM_CHN";
			
		case EB.Country.Taiwan:
			return "TRAD_CHN";
			
		case EB.Country.Brazil:
			return "BRZ";
			
		case EB.Country.Portugal:
			return "POR";
			
		case EB.Country.HongKong:
			return "HK";
			
		default:
			return "ENG";
		}
		
	}
}

	

