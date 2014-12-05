using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace EB
{
	public static class Version
	{
		public const string DefaultCountryCode = "US";
		public const Country DefaultCountry = Country.USA;
		public const string DefaultLocaleCode = "en";
		public const Language DefaultLanguage = Language.English;

		static public string BrowserAgent = string.Empty;
		
		private const string _defaultVersionString = "0.0.0.0";

#if UNITY_IPHONE && !UNITY_EDITOR
		static string _cachedUdid = string.Empty;
		static string _cachedLanguageCode = string.Empty;
		static string _cachedPreferredLanguageCode = string.Empty;
		static string _cachedCountryCode= string.Empty;
			
		[DllImport("__Internal")]
		static extern string _GetLanguageCode();
		
		[DllImport("__Internal")]
		static extern string _GetPreferredLanguageCode();
		
		[DllImport("__Internal")]
		static extern string _GetCountryCode();
		
		[DllImport("__Internal")]
		static extern string _GetOpenUDID();
		
		[DllImport("__Internal")]
		static extern string _GetUDID();
		
		[DllImport("__Internal")]
		static extern string _GetIFA();
		
		[DllImport("__Internal")]
		static extern string _GetMACAddress();
		
		[DllImport("__Internal")]
		static extern string _GetModel();
#endif
		
		public static string GetUDID()
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			if (string.IsNullOrEmpty(_cachedUdid))
			{
				_cachedUdid = _GetUDID();
			}
			return _cachedUdid;
#else
			return SystemInfo.deviceUniqueIdentifier;
#endif
		}

		public static Hashtable GetDeviceInfo()
		{
			Hashtable data = new Hashtable();			
			
			data["os"] = SystemInfo.operatingSystem;
#if UNITY_IPHONE && !UNITY_EDITOR
			var mac = _GetMACAddress();
			if(!string.IsNullOrEmpty(mac))
			{
				var bytes = Encoding.FromHexString(mac);
				data["mac"] = mac;
				data["odin1"] = Encoding.ToHexString(Digest.Sha1().Update(bytes).Final());
			}
			data["model"] = _GetModel();
			data["openudid"] = _GetOpenUDID();
			data["udid"] = _GetUDID();
			data["ifa"] = _GetIFA();
#elif UNITY_ANDROID && !UNITY_EDITOR
			data["mac"] = GetMACAddress(); 
			data["device_id"] = GetAndroidDeviceID();
			data["model"] = SystemInfo.deviceModel;
#elif UNITY_WEBPLAYER || UNITY_EDITOR
			data["mac"] = GetMACAddress();
			data["device_id"] = SystemInfo.deviceName;
			data["model"] = EB.Sparx.Device.DeviceModel;
			data["gpu"] = EB.Sparx.Device.DeviceGPU;
			data["caps"] = EB.Sparx.Device.DeviceCaps;
#endif

#if UNITY_WEBPLAYER
			if (!string.IsNullOrEmpty(EB.Version.BrowserAgent))		// moko: record the browser agent if exist
			{				
				data["agent"] = EB.Version.BrowserAgent;
			}
#endif
			return data;
		}
		
		static string _version = string.Empty;
		
		public static string GetVersion()		
		{
			if (string.IsNullOrEmpty(_version))
			{
				TextAsset versionAsset = EB.Assets.Load<TextAsset>("version");
				if(versionAsset != null && versionAsset.text.Length > 0)
				{
					_version = versionAsset.text.Replace("\n", "");
				}
				else
				{
					_version = _defaultVersionString;
					EB.Debug.LogError("Could not load version text file. Defaulting to " + _defaultVersionString);
				}
			}
			return _version;
		}

		static string _buildInfo = string.Empty;
		public static string GetBuildInfo()
		{
			if (string.IsNullOrEmpty(_buildInfo))
			{
				_buildInfo = EB.Assets.Load<TextAsset>("buildInfo").text;
			}
			return _buildInfo;
		}
		
		// returns iso local code (ie en_US);
		public static string GetLocale()
		{
			return GetLanguageCode() + "_" + GetCountryCode();
		}
		
		public static Language GetDefaultLanguageFromLanguageCode()
		{
			string language = GetPreferredLanguageCode();
			EB.Debug.Log("----------Language: " + language);
			if(Symbols.LanguageCode.ContainsKey(language))
				return Symbols.LanguageCode[language];
			else
				return Symbols.LanguageCode[DefaultLocaleCode];
		}
		
		public static Country GetCountry()
		{
			var country = EB.Version.GetCountryCode();
			return  GetCountryFromCountryCode(country);
		}

		public static bool IsNorthAmerica()
		{
			return (IsCountryUS() || GetCountry() == Country.Canada);
		}
		
		public static bool IsCountryUS()
		{
			string country = GetCountryCode();
			if(Symbols.CountryCode.ContainsKey(country) && Symbols.CountryCode[country] == Country.USA)
				return true;
			else
				return false;
		}
		
		public static Country GetCountryFromCountryCode( string country )
		{
			EB.Debug.Log("----------Country: " + country);
			if(Symbols.CountryCode.ContainsKey(country))
				return Symbols.CountryCode[country];
			else
				return Symbols.CountryCode[DefaultCountryCode];
		}
		
		public static string GetPreferredLanguageCode()
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			if (string.IsNullOrEmpty(_cachedPreferredLanguageCode))
			{
				_cachedPreferredLanguageCode = _GetPreferredLanguageCode();
			}
			return _cachedPreferredLanguageCode;
#elif (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_WEBPLAYER
			return GetLanguageCode();
#else
			return DefaultLocaleCode;
#endif
		}
		
		public static string GetLanguageCode()
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			if (string.IsNullOrEmpty(_cachedLanguageCode))
			{
				_cachedLanguageCode = _GetLanguageCode();
			}
			return _cachedLanguageCode;
#elif UNITY_ANDROID && !UNITY_EDITOR
			switch (Application.systemLanguage)
			{
			case SystemLanguage.English:
				return EB.Localizer.GetLanguageCode(EB.Language.English);
				break;
			case SystemLanguage.French:
				return EB.Localizer.GetLanguageCode(EB.Language.French);
				break;
			case SystemLanguage.Italian:
				return EB.Localizer.GetLanguageCode(EB.Language.Italian);
				break;
			case SystemLanguage.German:
				return EB.Localizer.GetLanguageCode(EB.Language.German);
				break;
			case SystemLanguage.Spanish:
				return EB.Localizer.GetLanguageCode(EB.Language.Spanish);
				break;
			case SystemLanguage.Portuguese:
				return EB.Localizer.GetLanguageCode(EB.Language.Portuguese);
				break;
			case SystemLanguage.Russian:
				return EB.Localizer.GetLanguageCode(EB.Language.Russian);
				break;
			case SystemLanguage.Korean:
				return EB.Localizer.GetLanguageCode(EB.Language.Korean);
				break;
			case SystemLanguage.Chinese:
				AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject playerActivityContext = actClass.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaObject resources = playerActivityContext.Call<AndroidJavaObject>("getResources");
				AndroidJavaObject configuration = resources.Call<AndroidJavaObject>("getConfiguration");
				AndroidJavaObject localeObj = configuration.Get<AndroidJavaObject>("locale");
				string locale = localeObj.Call<string>("toString");
				
				switch(locale)
				{
				case ("zh"):
				case ("zh_CN"):
				case ("zh_rCN"):
				case ("zh_Hans"):
				case ("zh_Hans_CN"):
				case ("zh_Hans_HK"):
				case ("zh_Hans_MO"):
				case ("zh_Hans_SG"):
				case ("zh_SG"):				//alias zh_Hans_SG
					return EB.Localizer.GetLanguageCode(EB.Language.ChineseSimplified);
					break;
					
				case ("zh_TW"):
				case ("zh_rTW"):
				case ("zh_Hant"):
				case ("zh_Hant_HK"):
				case ("zh_HK"): 			//alias zh_Hant_HK
				case ("zh_Hant_MO"):
				case ("zh_MO"): 			//alias zh_Hant_MO
				case ("zh_Hant_TW"):
					return EB.Localizer.GetLanguageCode(EB.Language.ChineseTraditional);
					break;
					
				default:
					return EB.Localizer.GetLanguageCode(EB.Language.ChineseSimplified);
				}
					
				break;
			case SystemLanguage.Japanese:
				return EB.Localizer.GetLanguageCode(EB.Language.Japanese);
				break;
			case SystemLanguage.Turkish:
				return EB.Localizer.GetLanguageCode(EB.Language.Turkish);
				break;
			default:
				return EB.Localizer.GetLanguageCode(EB.Language.English);
			}
#else
			return "en";
#endif
		}
		
		public static string GetCountryCode()
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			if (string.IsNullOrEmpty(_cachedCountryCode))
			{
				_cachedCountryCode = _GetCountryCode();
			}
			return _cachedCountryCode;
#elif UNITY_ANDROID && !UNITY_EDITOR
			//TODO: Need to get real location, not location based off language set.
			AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject playerActivityContext = actClass.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject resources = playerActivityContext.Call<AndroidJavaObject>("getResources");
			AndroidJavaObject configuration = resources.Call<AndroidJavaObject>("getConfiguration");
			AndroidJavaObject locale = configuration.Get<AndroidJavaObject>("locale");
			return locale.Call<string>("getCountry");
#else
			return DefaultCountryCode;
#endif
		}		
		
		public static int GetChangeList()
		{
			var version = GetVersion();
			try {
				var parts = version.Split('.');
				return int.Parse(parts[parts.Length-1]);  
			}
			catch {
				return 0;
			}
		}
		
		public static string GetAndroidDeviceID()
		{
			string deviceID = string.Empty;
			
		#if UNITY_ANDROID && !UNITY_EDITOR
			var deviceInfoClass = new AndroidJavaClass("com.explodingbarrel.android.UnityAndroidDeviceInfo");
			if( deviceInfoClass != null )
			{
				deviceID = deviceInfoClass.CallStatic<string>( "GetDeviceID" );
			}
		#endif
		
			return deviceID;
		}
		
		public static string GetMACAddress()
		{
			string macAddress = string.Empty;
			
#if UNITY_IPHONE && !UNITY_EDITOR
			macAddress = _GetMACAddress();
#elif UNITY_ANDROID && !UNITY_EDITOR
			var deviceInfoClass = new AndroidJavaClass("com.explodingbarrel.android.UnityAndroidDeviceInfo");
			if( deviceInfoClass != null )
			{
				macAddress = deviceInfoClass.CallStatic<string>( "WifiMacAddress" );
				macAddress = macAddress.Replace( ":", "" );
			}
#elif UNITY_WEBPLAYER || UNITY_EDITOR	// moko: use deviceUID as MAC address (it is same accordingly to unity doc)
			macAddress = SystemInfo.deviceUniqueIdentifier;
#endif
			return macAddress;
		}
		
		private static int _TimeZoneOffset = -1;
		public static int GetTimeZoneOffset()
		{
			if( _TimeZoneOffset == -1 )
			{
				_TimeZoneOffset = (int)System.TimeZone.CurrentTimeZone.GetUtcOffset( System.DateTime.Now ).TotalSeconds;
			}
			
			return _TimeZoneOffset;
		}
	}
}
