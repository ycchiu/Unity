using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;

#if UNITY_ANDROID

public class APKSignature
{
	private static AndroidJavaClass DetectAndroidJNI;
	public static bool IsValid
	{
		get
		{
			if( APKSignature.DetectAndroidJNI == null )
			{
				APKSignature.DetectAndroidJNI = new AndroidJavaClass("android.os.Build");
			}
			return APKSignature.DetectAndroidJNI.GetRawClass() != IntPtr.Zero;
		}
	}
		
	[DllImport( "apkprotect" )]
	private static extern bool APK_Check_Start( string jarPath );
	
	[DllImport( "apkprotect" )]
	private static extern bool APK_Check_Start_Salt( string jarPath, string salt );
	
	[DllImport( "apkprotect" )]
	private static extern bool APK_Check_Step( int maxBytesToRead, int maxFilesToExamine, int stepsBeforeLog );
	
	[DllImport( "apkprotect" )]
	private static extern string APK_Check_Complete();

	static public IEnumerator SyncGenerateSHA1SignatureFromAPK( string jarPath, EB.Action<string> callback )
	{	
		string signature = "";
		bool success = APK_Check_Start( jarPath );
		if( success == true )
		{
			bool complete = false;
			do
			{
				yield return 1;
				complete = APK_Check_Step( 1024 * 50, 20, 10 );
			}
			while( complete == false );
			signature = APK_Check_Complete();
		}
		
		callback( signature );
	}
	
	static public IEnumerator SyncGenerateHMACSignatureFromAPK( string jarPath, string salt, EB.Action<string> callback )
	{
		string signature = "";
		
		bool success = APK_Check_Start_Salt( jarPath, salt );
		if( success == true )
		{
			bool complete = false;
			do
			{
				yield return 1;
				complete = APK_Check_Step( 1024 * 50, 20, 10 );
			}
			while( complete == false );
			signature = APK_Check_Complete();
		}
		
		callback( signature );
	}
}

#endif