using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public static class Device
	{
		public static string UniqueIdentifier
		{
			get
			{
				var udid = PlayerPrefs.GetString("udid", string.Empty);
				if ( string.IsNullOrEmpty(udid) )
				{
					udid = Version.GetUDID();
					if ( string.IsNullOrEmpty(udid) )
					{
						udid = NewUniqueIdentifier();
					}
				}

#if UNITY_EDITOR
				udid += "-EDITOR";
#endif

				return udid;
			}
			set
			{
				PlayerPrefs.SetString("udid", value );
				PlayerPrefs.Save();
				EB.Debug.Log("Setting udid: " + value);
			}
		}

		public static string NewUniqueIdentifier()
		{
			var udid = System.Guid.NewGuid().ToString();
			PlayerPrefs.SetString("udid", udid);
			PlayerPrefs.Save();
			return udid;
		}

		public static string Platform
		{
			get
			{
#if UNITY_EDITOR
				return "editor";
#elif UNITY_IPHONE
				return "iphone";
#elif UNITY_ANDROID
				return "android";
#elif UNITY_FLASH
				return "flash";
#elif UNITY_WEBPLAYER
				return "web";
#else
				return "unknown";
#endif
			}
		}

		public static string DeviceModel
		{
			get
			{
#if UNITY_EDITOR
				return "editor";
#elif UNITY_IPHONE
				return iPhone.generation.ToString();
#else
				return SystemInfo.deviceModel;
#endif
			}
		}

		public static string DeviceGPU
		{
			get
			{
#if UNITY_EDITOR
				return "editor";
#else
				return SystemInfo.graphicsDeviceName;
#endif
			}
		}

		public static string DeviceCaps
		{
			get
			{
#if UNITY_EDITOR
				return "editor";
#else
				return SystemInfo.graphicsDeviceVersion;
#endif
			}
		}

		public static string MobilePlatform
		{
			get
			{
#if UNITY_IPHONE
				return "iphone";
#elif UNITY_WEBPLAYER
				return "facebook";
#else
//				EB.Debug.Log (System.String.Format("SystemInfo Model: {0}", SystemInfo.deviceModel));
				if (SystemInfo.deviceModel.ToLower().IndexOf("amazon") != -1)
				{
					return "amazonapp";
				}
				return "android";
#endif
			}
		}
	}
}


