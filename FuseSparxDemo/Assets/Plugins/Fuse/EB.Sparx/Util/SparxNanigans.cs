using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace EB.Sparx
{
	public class Nanigans
	{
		#if UNITY_IPHONE && !UNITY_EDITOR
		static bool _init = false;

		[DllImport ("__Internal")]
		static extern void _Nanigans_Init(string fbId);
		
		[DllImport ("__Internal")]
		static extern void _Nanigans_TrackInstall(string uid);
		
		[DllImport ("__Internal")]
		static extern void _Nanigans_TrackVisit(string uid);
		#endif
		
		public static void Init(string fbId)
		{
			EB.Debug.Log("Nanigans Init: " + fbId);
			#if UNITY_IPHONE && !UNITY_EDITOR
			if (!_init)
			{
				_init = true;
				_Nanigans_Init(fbId);
			}
			#endif
		}

		public static void Install( string uid )
		{
			EB.Debug.Log("Nanigans Install: " + uid);
			#if UNITY_IPHONE && !UNITY_EDITOR
			if (_init)
			{
				_Nanigans_TrackInstall(uid);
			}
			#endif
		}

		public static void Visit( string uid )
		{
			EB.Debug.Log("Nanigans Visit: " + uid);
			#if UNITY_IPHONE && !UNITY_EDITOR
			if (_init)
			{
				_Nanigans_TrackVisit(uid);
			}
			#endif
		}

	}

}


