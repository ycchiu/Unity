using UnityEngine;
using System.Collections;

namespace EB
{
	public static class Options
	{
		public static event EB.Action SettingsChanged;
		
		public static int defaultUnit = 0;
		
		// TODO: Should probably move game-specific settings out of here...
		public static float SFX { get { return GetOption("sfx", 1.0f); } set { SetOption("sfx", value); } }
		public static float Music { get { return GetOption("music", 1.0f); } set { SetOption("music", value); } }
		public static bool UseImperial { get { return GetOption("units", defaultUnit) == 1; } set { SetOption("units", value ? 1 : 0); } }
		
		public static int GetOption( string name, int def )
		{
			return EB.SecurePrefs.GetInt(name, def);
		}
		
		public static void SetOption( string name, int value )
		{
			EB.SecurePrefs.SetInt(name, value);
			EB.SecurePrefs.Save();
			if ( SettingsChanged != null)
			{
				SettingsChanged();
			}
		}
		
		public static float GetOption( string name, float def )
		{
			return EB.SecurePrefs.GetFloat(name, def);
		}
		
		public static void SetOption( string name, float value )
		{
			EB.SecurePrefs.SetFloat(name, value);
			EB.SecurePrefs.Save();
			if ( SettingsChanged != null)
			{
				SettingsChanged();
			}
		}
		
		
	}

}
