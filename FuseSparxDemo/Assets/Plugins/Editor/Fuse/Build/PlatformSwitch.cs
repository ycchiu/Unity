using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class PlatformSwitch : EditorWindow
{

	private BuildTarget currentPlatform;
	private static PlatformSwitch window;
	private bool switching;

	private const string FilePath = "/../Temp/SwitchPlat_Callback.txt";

	private string EnvVariable
	{
		get
		{
			try
			{
				return File.ReadAllText(Application.dataPath + FilePath);
			}
			catch (Exception e)
			{
				EB.Debug.LogError("Could not read file: " + e.ToString());
			}
			return null;
		}
		set
		{
			try
			{
				File.WriteAllText(Application.dataPath + FilePath, value);
			}
			catch (Exception e)
			{
				EB.Debug.LogError("Could not write file: " + e.ToString());
			}
		}
	}

	public PlatformSwitch()
	{
		switching = false;
		currentPlatform = GetPlatform();
	}

	public static bool Change(BuildTarget platform, string menuItemPath)
	{
		if (platform == GetPlatform())
		{
			EB.Debug.Log("No need to switch platform because it is already " + platform);
			return true;
		}

		window = (PlatformSwitch)EditorWindow.GetWindow(typeof(PlatformSwitch));
		window.DoSwitch(platform, menuItemPath);
		return false;
	}

	private void DoSwitch(BuildTarget platform, string menuItemPath)
	{
		if (switching)
		{
			EB.Debug.LogError("Platform is already being switched!");
			return;
		}
		switching = true;
		currentPlatform = platform;
		EnvVariable = menuItemPath;

		EditorUserBuildSettings.SwitchActiveBuildTarget(platform);

		if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
			throw new System.Exception("Build failed. You tried to build for " + platform + " using the " + GetPlatform()
									   + " platform. The platform was changed, but you need to run the command again.");
	}

	void OnGUI()
	{
		GUILayout.Label("Changing platform to " + currentPlatform + "...\nPlease don't close this window.", EditorStyles.boldLabel);
	}

	void Update()
	{
		if (currentPlatform == GetPlatform())
		{ //Second pass
			string menuItem = EnvVariable;
			switching = false;
			EnvVariable = "";
			Close();

			if (!String.IsNullOrEmpty(menuItem))
				EditorApplication.ExecuteMenuItem(menuItem);
			else
				EB.Debug.LogError("Problem reading file (value is null)");
		}
	}

	public static void AssertPlatform(BuildTarget expected)
	{
		BuildSettings.Target = expected;
		BuildTarget currentPlatform = GetPlatform();
		if (currentPlatform != expected)
			throw new System.Exception("Unexpected platform. You are using " + currentPlatform + ". It was expected " + expected);
	}

	public static BuildTarget GetPlatform()
	{
#if UNITY_ANDROID
		return BuildTarget.Android;
#elif UNITY_IPHONE
		return BuildTarget.iPhone;
#elif UNITY_WEBPLAYER
		return BuildTarget.WebPlayer;
#elif UNITY_STANDALONE_WIN
		return BuildTarget.StandaloneWindows;
#else
		throw new System.Exception("Unexpected platform");
#endif
	}

}