using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public static class Symbols
	{
		public const string Infinity = "\u221E";
		public const string Degrees = "\u00B0";
		public const string LocIdPrefix = "ID_";

		public static Dictionary<string, Language> LanguageCode = new Dictionary<string, Language>
		{
			{"en", Language.English },
			{"fr", Language.French },
			{"fr-CA", Language.French },
			{"fr-FR", Language.French },
			{"fr-CH", Language.French },
			{"fr-BE", Language.French },
			{"it", Language.Italian },
			{"it-IT", Language.Italian },
			{"it-CH", Language.Italian },
			{"de", Language.German },
			{"de-DE", Language.German },
			{"de-CH", Language.German },
			{"de-AT", Language.German },
			{"de-LI", Language.German },
			{"es", Language.Spanish },
			{"es-SP", Language.Spanish },
			{"es-MX", Language.Spanish },
			{"es-US", Language.Spanish },
			{"pt", Language.Portuguese },
			{"pt-BR", Language.Portuguese },
			{"pt-PT", Language.Portuguese },
			{"ru", Language.Russian },
			{"ko", Language.Korean },
			{"zh-Hans", Language.ChineseSimplified },
			{"zh-Hant", Language.ChineseTraditional },
			{"ja", Language.Japanese },
			{"tr", Language.Turkish },
		};

		// moko: list of proper facebook locale (https://www.facebook.com/translations/FacebookLocales.xml)
		public static Dictionary<string, Language> FacebookLocaleCode = new Dictionary<string, Language>
		{
#region Facebook_Language_List
			{"de_DE", Language.German},		/* German */
			{"en_GB", Language.English},	/* English (UK) */
			{"en_PI", Language.English},	/* English (Pirate) */
			{"en_UD", Language.English},	/* English (Upside Down) */
			{"en_US", Language.English},	/* English (US) */
			{"es_ES", Language.Spanish},	/* Spanish (Spain) */
			{"es_LA", Language.Spanish},	/* Spanish */
			{"fr_CA", Language.French},		/* French (Canada) */
			{"fr_FR", Language.French},		/* French (France) */
			{"it_IT", Language.Italian},	/* Italian */
			{"ja_JP", Language.Japanese},	/* Japanese */
			{"ko_KR", Language.Korean},		/* Korean */
			{"pt_BR", Language.Portuguese},	/* Portuguese (Brazil) */
			{"pt_PT", Language.Portuguese},	/* Portuguese (Portugal) */
			{"ru_RU", Language.Russian},	/* Russian */
			{"tr_TR", Language.Turkish},	/* Turkish */
			{"zh_CN", Language.ChineseSimplified},	/* Simplified Chinese (China) */
			{"zh_HK", Language.ChineseTraditional},	/* Traditional Chinese (Hong Kong) */
			{"zh_TW", Language.ChineseTraditional}	/* Traditional Chinese (Taiwan) */
#endregion
		};

		public static Dictionary<string, Country> CountryCode = new Dictionary<string, Country>
		{
			{"AR", Country.Argentina },
			{"BO", Country.Bolivia },
			{"BR", Country.Brazil },
			{"CA", Country.Canada },
			{"CL", Country.Chile },
			{"CN", Country.China },
			{"CO", Country.Colombia },
			{"CR", Country.CostaRica},
			{"DO", Country.DominicanRepublic },
			{"EC", Country.Ecuador },
			{"SV", Country.ElSalvador },
			{"FR", Country.France },
			{"DE", Country.Germany },
			{"GT", Country.Guatemala },
			{"HN", Country.Honduras },
			{"HK", Country.HongKong },
			{"IT", Country.Italy },
			{"JP", Country.Japon },
			{"KR", Country.Korea },
			{"MX", Country.Mexico },
			{"NI", Country.Nicaragua },
			{"PA", Country.Panama },
			{"PY", Country.Paraguay },
			{"PT", Country.Portugal },
			{"PE", Country.Peru },
			{"RU", Country.Russia },
			{"ES", Country.Spain },
			{"TW", Country.Taiwan },
			{"TR", Country.Turkey },
			{"UY", Country.Uruguay },
			{"US", Country.USA },
			{"VE", Country.Venezuela }
		};
		
		//TODO (THIS NEEDS TO BE DELETED)
		public static Dictionary<string, Language> OldLanguageCode = new Dictionary<string, Language>
		{
			{"en", Language.English },
			{"fr", Language.French },
			{"it", Language.Italian },
			{"de", Language.German },
			{"es", Language.Spanish },
			{"pt", Language.Portuguese },
			{"ru", Language.Russian },
			{"ko", Language.Korean },
			{"zh-CN", Language.ChineseSimplified },
			{"zh-TW", Language.ChineseTraditional },
			{"ja", Language.Japanese },
			{"tr", Language.Turkish },
		};
	}

	public enum Language
	{
		Unknown,

		English,
		French,
		Italian,
		German,
		Spanish,
		Portuguese,
		Russian,
		Korean,
		ChineseSimplified,
		ChineseTraditional,
		Japanese,
		Turkish,
	}

	public enum Country
	{
		Unknown,

		Argentina,
		Bolivia,
		Brazil,
		Canada,
		Chile,
		China,
		Colombia,
		CostaRica,
		DominicanRepublic,
		Ecuador,
		ElSalvador,
		France,
		Germany,
		Guatemala,
		Honduras,
		HongKong,
		Italy,
		Japon,
		Korea,
		Mexico,
		Nicaragua,
		Panama,
		Paraguay,
		Peru,
		Portugal,
		Russia,
		Spain,
		Taiwan,
		Turkey,
		Uruguay,
		USA,
		Venezuela
	}

	public enum LocStatus
	{
		Missing,
		Source,
		Placeholder,
		For_Translation,
		Translated,
	}

	public class LocFile
	{
		Dictionary<string,string> _strings;
		Dictionary<string,LocStatus> _status;

		public LocFile()
		{
			_strings = new Dictionary<string,string>();
			_status = new Dictionary<string, LocStatus>();
		}

		public LocFile( Dictionary<string,string> values )
		{
			_strings = values;
			_status = new Dictionary<string, LocStatus>();
		}

		public LocFile( Dictionary<string,string> values, Dictionary<string,LocStatus> status )
		{
			_strings = values;
			_status = status;
		}

		public string NextId( string prefix )
		{
			int i = 1;
			while(true)
			{
				var id = string.Format("ID_{0}_{1}", prefix, i).ToUpper();
				if (_strings.ContainsKey(id)==false)
				{
					return id;
				}
				++i;
			}
		}

		public void Add( string id, string value )
		{
			_strings[id] = value;
		}

		public LocStatus GetStatus( string id )
		{
			LocStatus s;
			if (_status.TryGetValue(id, out s))
			{
				return s;
			}

			return LocStatus.Missing;
		}

		public bool Get( string id, out string result )
		{
			if (!_strings.TryGetValue(id, out result))
			{
				result = string.Empty;
				return false;
			}
			return true;
		}

#if UNITY_EDITOR
		public string Write()
		{
			List<string> ids = new List<string>(_strings.Keys);
			ids.Sort();

			var sb = new System.Text.StringBuilder();
			foreach( var id in ids )
			{
				sb.AppendLine( id+","+ _strings[id].Replace("\n", "\\n"));
			}

			return sb.ToString();
		}

		public string WriteTSV()
		{
			List<string> ids = new List<string>(_strings.Keys);
			ids.Sort();

			var sb = new System.Text.StringBuilder();
			foreach( var id in ids )
			{
				sb.AppendLine( id+"\t"+ _strings[id].Replace("\n", "\\n").Replace("\t",""));
			}

			return sb.ToString();
		}
#endif

		public Hashtable Read( string data )
		{
			Hashtable result = new Hashtable();
			// convert to unix line feeds
	        data = data.Replace("\r\n", "\n");

	        string[] lines = data.Split('\n');
	        foreach (string line in lines)
	        {
				if (line.Length > 0 && line[0] == '#')
				{
					continue;
				}

	            int comma = line.IndexOf(',');
	            if (comma > 0)
	            {
	                string id = line.Substring(0, comma);
	                string value = line.Substring(comma + 1);

	                if ( id.StartsWith("ID_" ))
	                {
						var idParts = id.Split('|');
						id = idParts[0];

						if (idParts.Length>1)
						{
							_status[id] = (LocStatus)System.Enum.Parse(typeof(LocStatus), idParts[1], true);
						}
						else
						{
							_status[id] = LocStatus.Source;
						}

	                    // convert line feeds
	                    value = value.Replace("\\n", "\n").Replace("\\t","\t");

						Add(id, value);

						result[id] = value;
	                }
	            }
	        }
			return result;
		}
	}

	public static class Localizer
	{
		class FormatProvider : System.IFormatProvider, System.ICustomFormatter
		{
			#region IFormatProvider implementation
			public object GetFormat (System.Type formatType)
			{
				return this;
			}
			#endregion

			#region ICustomFormatter implementation
			public string Format (string format, object arg, System.IFormatProvider formatProvider)
			{
				var result = string.Empty;
				if ( arg != null )
				{
					result = arg.ToString();
				}
				else
				{
					result = "null";
				}

				if ( result.StartsWith(Symbols.LocIdPrefix) )
				{
					result = GetString(result);
				}
				return result;
			}
			#endregion
		}

		public static bool ShowStatuses 	= false;
		public static bool ShowStringIds 	= false;
		
	    private static Dictionary<string, string> _strings = new Dictionary<string, string>();
		public static Dictionary<string,LocStatus> _status = new Dictionary<string, LocStatus>();

		private static FormatProvider _provider = new FormatProvider();

		public static Dictionary<string, string> Strings { get { return _strings; }}

		public static Language Current { get; private set; }

		public static void Clear()
		{
			_status.Clear();
			_strings.Clear();
		}

		public static IDictionary LoadAllFromResources( Language locale, bool loadCommon )
		{
			var l = GetSparxLanguageCode(locale);
			Current = locale;

			if (loadCommon)
			{
				foreach( TextAsset asset in EB.Assets.LoadAll("Languages/common", typeof(TextAsset) ) )
				{
					var text = Encoding.GetString(asset.bytes);
					LoadStringsInternal(text);
					Assets.Unload(asset);
				}
			}


			foreach( TextAsset asset in EB.Assets.LoadAll("Languages/"+l, typeof(TextAsset) ) )
			{
				var text = Encoding.GetString(asset.bytes);
				LoadStringsInternal(text);
				Assets.Unload(asset);
			}
			return _strings;
		}

		public static void Dump()
		{
			foreach( var key in _strings.Keys )
			{
				EB.Debug.Log("String (" + key + ") " + Encoding.ToHexString( Encoding.GetBytes(key) ) + " " + _strings[key] );
			}
		}

		public static string GetLanguageCode( Language locale )
		{
			foreach( KeyValuePair<string, Language> lcode in Symbols.LanguageCode )
    		{
				if(lcode.Value == locale)
					return lcode.Key;
			}

			return locale.ToString().ToLower();
		}
		
		public static string GetSparxLanguageCode( Language locale )
		{
			foreach( KeyValuePair<string, Language> lcode in Symbols.OldLanguageCode )
    		{
				if(lcode.Value == locale)
					return lcode.Key;
			}

			return locale.ToString().ToLower();
		}

		// moko: function to convert facebook locale code into EB.Language code
		public static Language GetLanguageFromFBLocale(string fbLocale)
		{
			var lang = EB.Version.DefaultLanguage;
			if (!Symbols.FacebookLocaleCode.TryGetValue(fbLocale, out lang))
				return EB.Version.DefaultLanguage;
			return lang;
		}

		// moko: function to convert facebook locale code into EB.Country code
		public static Country GetCountryFromFBLocale(string fbLocale)
		{
			var country = EB.Version.DefaultCountry;
			if (!Symbols.CountryCode.TryGetValue(fbLocale, out country))
				return EB.Version.DefaultCountry;
			return country;
		}

	    public static Hashtable LoadStrings( Language locale, string database )
	    {
			Current = locale;
			var l = GetSparxLanguageCode(locale);
			var path = "Languages/"+l+"/"+database;
	        TextAsset asset = (TextAsset)Assets.Load(path, typeof(TextAsset));
	        if (asset != null)
	        {
				var text = Encoding.GetString(asset.bytes);
	            var result = LoadStringsInternal(text);
				EB.Debug.Log("Localizer loaded " + result.Count + " strings for database " + database);
				Assets.Unload(asset);
				return result;
	        }
	        else
	        {
	            EB.Debug.LogWarning("Failed to load local: " + locale + " for database " + database);
	        }

			return null;
	    }

		public static bool HasString(string id)
		{
			lock(_strings)
			{
				return _strings.ContainsKey(id);
			}
		}

	    public static bool GetString(string id, out string value)
	    {
			if (string.IsNullOrEmpty(id))
			{
				EB.Debug.LogError("empty or null string id");
				value = "MS_NULL";
				return false;
			}

			if (ShowStringIds)
			{
				value = id;
				return true;
			}

			lock(_strings)
			{
				if (_strings.TryGetValue(id, out value))
		        {
		            return true;
		        }
			}
			//Dump();
			EB.Debug.LogWarning("Missing String (" + id + ") ");
	        value = "MS_" + id;
	        return false;
	    }

	    public static string GetString(string id)
	    {
	        string value;
	        GetString(id, out value);
	        return value;
	    }

		public static LocStatus GetStatus( string id )
		{
			LocStatus s;
			lock(_strings)
			{
				if (_status.TryGetValue(id, out s))
				{
					return s;
				}
			}
			return LocStatus.Missing;
		}

		public static UnityEngine.Color GetColor( string id )
		{
			var status = GetStatus(id);
			//Debug.LogError("GetColor: " + id + " " + status);
			switch(status)
			{
			case LocStatus.Translated:
				return Color.blue;
			case LocStatus.Placeholder:
				return Color.yellow;
			case LocStatus.For_Translation:
				return Color.yellow;
			case LocStatus.Missing:
				return Color.red;
			}
			return Color.white;
		}

	    public static string Format(string id, params object[] args)
	    {
			if (ShowStringIds)
			{
				return GetString(id);
			}
	        return string.Format(_provider, GetString(id), args);
	    }

	    private static Hashtable LoadStringsInternal(string data)
	    {
			var file = new LocFile(_strings, _status);
			return file.Read(data);
	    }

		public static string FormatTimeHundredthsSeconds(int totalHundredthsSeconds)
		{
			int minutes, seconds, hundredths;
			hundredths = totalHundredthsSeconds;
			seconds = Mathf.FloorToInt(hundredths/100);
			hundredths -= hundredths * 100;
			minutes = Mathf.FloorToInt(seconds/60);
			
			string timeString = (minutes < 10 && minutes >=0 ? "0" : "") + (minutes > 0 ? minutes.ToString() + "+" : "") 
					+ ((seconds < 10 && seconds >=0 && minutes > 0) ? "0" : "") + seconds.ToString() 
					+ ":" + (totalHundredthsSeconds % 100 < 10 ? "0" :"" + totalHundredthsSeconds %100).ToString();
			return timeString;
		}
		
		public static string FormatTime(int totalSeconds, bool showSeconds)
		{
			int minutes, hours, seconds;

			seconds = totalSeconds;
			hours = Mathf.FloorToInt(seconds / 3600);
			seconds -= hours * 3600;
			minutes = Mathf.FloorToInt(seconds / 60);

			string timeString = (hours > 0 ? hours.ToString() + ":" : "") + ((minutes < 10 && minutes>=0 && hours > 0) ? "0" : "") + minutes.ToString();
			if(showSeconds)
			{
				timeString += ":" + ((totalSeconds % 60 < 10) ? "0" : "") + (totalSeconds % 60).ToString();
			}
			return timeString;
		}

		private static System.Globalization.CultureInfo _culture = null;

		public static string FormatNumber( float value, bool removeDecimal )
		{
			if (_culture == null)
            {
                _culture = new System.Globalization.CultureInfo("en-US");
            }

            string str = string.Format(_culture, "{0:N}", value);
			if (removeDecimal)
			{
            	int dec = str.IndexOf('.');
            	if ( dec >= 0 ) return str.Substring(0, dec);
			}
            return str;
		}

		public static string FormatInteger(int value, bool separators = true)
		{
			if (_culture == null)
			{
				_culture = new System.Globalization.CultureInfo("en-US");
			}

			string str = separators ? value.ToString("N0", _culture) : value.ToString();
			return str;
		}

		const int SecondsInMin = 60;
		const int SecondsInHour = SecondsInMin * 60;
		const int SecondsInDay = SecondsInHour * 24;
		public static string FormatDuration(int seconds, bool showSeconds )
        {
            string duration = "";

            showSeconds = showSeconds || (seconds < SecondsInMin);

            if (seconds >= SecondsInDay)
            {
                int days = seconds / SecondsInDay;

                if (days > 1)
                {
                    duration += string.Format("{0} days ", days);
                }
                else
                {
                    duration += "1 day ";
                }

                seconds = seconds % SecondsInDay;
            }

            if (seconds >= SecondsInHour)
            {
                int hours = seconds / SecondsInHour;

                if (hours > 1)
                {
                    duration += string.Format("{0} hours ", hours);
                }
                else
                {
                    duration += "1 hour ";
                }

                seconds = seconds % SecondsInHour;
            }

            if (seconds >= SecondsInMin)
            {
                int mins = seconds / SecondsInMin;

                if (mins > 1)
                {
                    duration += string.Format("{0} minutes ", mins);
                }
                else
                {
                    duration += "1 minute ";
                }

                seconds = seconds % SecondsInMin;
            }

            if (seconds > 0 && showSeconds)
            {
                if (seconds > 1)
                {
                    duration += string.Format("{0} seconds ", seconds);
                }
                else
                {
                    duration += "1 second";
                }
            }

            return duration.Trim();
        }
	}
}

