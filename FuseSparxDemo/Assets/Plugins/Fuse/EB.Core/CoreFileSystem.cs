using System;
using System.IO;
using UnityEngine;

namespace EB
{
	/// <summary>
	/// Represents a free drive space checker
	/// </summary>
	public static class FileSystem
	{
#if UNITY_IPHONE && !UNITY_EDITOR
		[System.Runtime.InteropServices.DllImport("__Internal")]
		private static extern long EBFileSystem_GetAvailableBlocks(string path);
#endif

		/// <summary>
		/// Gives access to the available space on a drive that contains a certain location path
		/// </summary>
		/// <returns>The available free space for the application in bytes</returns>
		/// <summary>
		/// Gives access to the available space on a drive that contains a given path 
		/// </summary>
		/// <param name="PathOnDrive">A path on the drive</param>
		/// <returns>The available disk space on the drive that conatins the given path</returns>
		public static decimal GetAvailableSpaceInBytes(string PathOnDrive)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			int resultLength = 0;
			foreach (DriveInfo d in DriveInfo.GetDrives())
			{	
				//Selecting the nescessary drive, searching for the longest matching path section
				if (!PathOnDrive.StartsWith(d.Name) || d.Name.Length < resultLength)
				{
					continue;
				}
				
				//updating longest match
				resultLength = d.Name.Length;
				
				//Calling native Java function to bypass bugged mono functionality
				var statfs = new AndroidJavaObject("android.os.StatFs", d.Name);
				var availableBlocks = statfs.Call<int>("getAvailableBlocks");
				var blocksize = statfs.Call<int>("getBlockSize");
				return (decimal)availableBlocks * (decimal)blocksize;
			}
#elif UNITY_IPHONE && !UNITY_EDITOR
			return EBFileSystem_GetAvailableBlocks(Application.persistentDataPath);
#elif UNITY_WEBPLAYER && !UNITY_EDITOR		
			throw new Exception("EB.FileSystem.GetAvailableSpaceInBytes not implemented yet");
#elif UNITY_EDITOR
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			foreach (var d in allDrives) 
			{
				if (!PathOnDrive.StartsWith(d.Name))
				{
					continue;
				}
				return d.AvailableFreeSpace;
			}			
#else
			EB.Debug.LogError("Function not implemented! This class works specifically on Android platform. Implementations on other platforms are not available.");
#endif
			return Decimal.Zero;
		}
		
		/// <summary>
		/// Gives access to the available space on a drive given with it's DriveInfo
		/// </summary>
		/// <param name="drive">The info of the drive</param>
		/// <returns>The available disk space on the drive given in the drive parameter</returns>
		public static decimal GetAvailableSpaceInBytes(DriveInfo drive)
		{
			return GetAvailableSpaceInBytes(drive.Name);
		}
		
		/// <summary>
		/// Wrapper for the MONO DriveInfo.GetDrives() function
		/// </summary>
		/// <returns>An array of DriveInfo objects with the available drives</returns>
		public static DriveInfo[] GetDrives()
		{
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_WEBPLAYER
			return DriveInfo.GetDrives();
#else
			throw new Exception("EB.FileSystem.GetDrives not implemented yet");
#endif
		}
	}	
}
