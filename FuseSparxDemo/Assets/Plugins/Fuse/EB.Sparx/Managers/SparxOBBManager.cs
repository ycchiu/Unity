#if UNITY_ANDROID
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB.Sparx
{
	/// <summary>
	/// Content manager.
	/// Manages downloading the google OBB package
	/// </summary>
	public class ObbManager
	{	
		
		public string Error { get; private set; }
		public Action<string> ProgessCallback {get;set;}
		public Action<string, decimal> SpaceCallback { get; set; }
		public bool IsMounted {get; private set;}

		private decimal ExpectedObbSize = 0;
		
		string BasePath
		{
			get
			{
				return Path.Combine( Application.persistentDataPath, "Content");	
			}
		}
		
		public ObbManager(decimal expectedObbSize =0)
		{
			this.ExpectedObbSize = expectedObbSize;
		}
		
		public Coroutine Check()
		{
			return EB.Coroutines.Run(DoCheck());
		}
		
		IEnumerator DoCheck() 
		{
			Error = string.Empty;
			IsMounted = false;
			
			Notify( Localizer.GetString("ID_SPARX_CONTENT_CHECKING") ); 
			
			yield return 1;
			
			if (!GooglePlayDownloader.RunningOnAndroid())
			{
				yield break;
			}
			
			string expPath = GooglePlayDownloader.GetExpansionFilePath();
			if (string.IsNullOrEmpty(expPath))
			{
				Error = "ID_SPARX_OBB_ERROR";
				yield break;
			}			
		
			EB.Debug.Log("Ext Folder: " + expPath);
						
			string mainPath = GooglePlayDownloader.GetMainOBBPath(expPath);
			if (string.IsNullOrEmpty(mainPath))
			{
				// moko: verify we have enough disk-space for the OBB at the destinated OBB path
				var size = EB.FileSystem.GetAvailableSpaceInBytes(expPath);
				if (size > this.ExpectedObbSize)
				{
					EB.Debug.Log("ObbManager > disk space availbity: " + expPath + ", size: " + size + "/" + this.ExpectedObbSize);
				}
				else
				{
					EB.Debug.LogError("ObbManager > Not Enough disk space: " + expPath + ", size: " + size + "/" + this.ExpectedObbSize);					
					Error = "ID_SPARX_CONTENT_FAILED_EXTRACT";
                    yield break;				
                }

				UnityEngine.Debug.Log("Fetching main OBB ");
				GooglePlayDownloader.FetchOBB();
				yield return new WaitForSeconds(3.0f);				
			}
			
			mainPath = GooglePlayDownloader.GetMainOBBPath(expPath);
			EB.Debug.Log("Main OBB Path: " + mainPath);
			if (string.IsNullOrEmpty(mainPath))
			{
				UnityEngine.Debug.Log("Failed to download OBB");
				Error = "ID_SPARX_CONTENT_FAILED";
				yield break;
			}
			
			// cleanup old content folder
			try {
				if (Directory.Exists(BasePath))
				{
					Directory.Delete(BasePath, true);
				}
			}
			catch
			{
				
			}
			
			IsMounted = true;
			
			UnityEngine.Debug.Log("Main Path: " + mainPath);
			Loader.OverridePath( "jar:file://" + mainPath + "!/" );
		}
		
		void Notify( string txt )
		{
			if ( ProgessCallback != null)
			{
				ProgessCallback(txt);
			}
		}
		
		void NotifySpace( string path, decimal size )
		{
			if(SpaceCallback != null)
			{
				SpaceCallback(path, size);
			}
		}
		
	}
	
}
#endif
