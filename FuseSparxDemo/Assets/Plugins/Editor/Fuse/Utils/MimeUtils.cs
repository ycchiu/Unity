using UnityEngine;
using System.Collections;

public static class MimeUtils {

	public static  string GetMimeType( string extension )
	{
		string contentType = string.Empty;
		
		switch( extension )
		{
		case ".assetbundle":
		case ".unity3d":
			contentType = "application/vnd.unity";
			break;
		case ".ogg":
			contentType = "application/ogg";
			break;			
		case ".png":
			contentType = "image/png";
			break;
		case ".jpg":
			contentType = "image/jpeg";
			break;
		case ".json":
			contentType = "application/json";
			break;
		case ".txt":
			contentType = "text/plain";
			break;
		case ".plist":
			contentType = "application/x-plist";
			break;
		case ".apk":
			contentType = "application/vnd.android.package-archive";
			break;
		case ".gz":
			contentType = "application/x-gzip";
			break;
		}
		return contentType;
	}
}
