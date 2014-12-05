#if !UNITY_WEBPLAYER || UNITY_EDITOR
#define USE_CACHE
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USE_CACHE
using System.IO;
#endif

namespace EB
{
	// a really simple cache
	// makes zero attempts to purge any data right now
	public static class Cache
	{
#if USE_CACHE
		private static List< WWW > _ActivelyLoading = new List<WWW>();
		private static List< WWW > _DisposeOnRelease = new List<WWW>();
		static string CacheFolder
		{
			get
			{
				return Path.Combine(Application.temporaryCachePath,"asset_cache");
			}
		}

		private static IEnumerator SaveToCache( string url, WWW www )
		{
			while(www.isDone == false)
			{
				yield return 1;
			}

			byte[] bytes = null;

			try 
			{
				if (string.IsNullOrEmpty(www.error))
				{
					bytes = www.bytes;
				}
			}
			catch 
			{

			}

			if (bytes != null) 
			{
				SaveToCache( url, bytes );
			}
			
			_ActivelyLoading.Remove(www);
			bool dispose = _DisposeOnRelease.Remove( www );
			if( dispose == true )
			{
				www.Dispose();
			}
		}

		private static void SaveToCache( string url, byte[] bytes )
		{
			try 
			{
				if (Directory.Exists(CacheFolder)==false) 
				{
					Directory.CreateDirectory(CacheFolder);
				}

				var filename = GetCacheFile(url);

#if UNITY_WEBPLAYER	// TODO-moko: disable for web player. need to confirm this
				EB.Debug.LogWarning("File.IO is not implemented for webplayer:" + filename);
#else
				File.WriteAllBytes(filename,  bytes);
#endif
				EB.Debug.Log("Cached asset: {0}->{1}", url, filename);
			}
			catch (System.Exception ex)
			{
				EB.Debug.LogError("Failed to write to cache folder! url:{0}, ex:{1}", url, ex);
			}
		}

		private static string GetCacheFile( string url ) 
		{
			var ext = Path.GetExtension(url);
			var name = Hash.StringHash(url);
			return Path.Combine( CacheFolder, name+ext); 
		}

		public static WWW LoadFromCacheOrDownload(string url, out bool disposable)
		{
			disposable = true;

			if (string.IsNullOrEmpty(url))
			{
				return null;
			}

			if( url.StartsWith("jar:") == true )
			{
				return new WWW(url); 
			}
			
			var uri = new Uri();
			if (!uri.Parse(url))
			{
				EB.Debug.LogError( "LoadFromCacheOrDownload Parse Failed {0}", url );
				return null;
			}

			if (uri.Scheme == "file")
			{
				return new WWW(url);
			}

			// assume network
			var fileName = GetCacheFile(url);
			if ( File.Exists(fileName) )
			{
				EB.Debug.Log( "Loading {0} from Cache {1}", url, fileName );
				var cachedPath = "file://"+fileName;
				return new WWW(cachedPath);
			}

			// load it and save it to the cache
			var www = new WWW(url);
			disposable = false;
			EB.Debug.Log( "Loading to Cache {0}", url );
			_ActivelyLoading.Add(www);
			Coroutines.Run(SaveToCache(url,www));
			return www;
		}
		
		public static void DisposeOnComplete(WWW www)
		{
			if( www != null )
			{
				if( _ActivelyLoading.Contains( www ) == false )
				{
					EB.Debug.Log( "Cache Not Using Dispose" );
					www.Dispose();
				}
				else
				{
					EB.Debug.Log( "Cache Is Loading Defer Dispose" );
					_DisposeOnRelease.Add( www );
				}
			}
		}
#else
		public static WWW LoadFromCacheOrDownload(string url, out bool disposable)
		{
			disposable = true;
			return new WWW(url);
		}
		
		public static void DisposeOnComplete(WWW www)
		{
		}
#endif

		public static void Precache(string url)
		{
			bool disposable = false;
			EB.Debug.Log( "Precaching {0}", url );
			LoadFromCacheOrDownload(url, out disposable);
		}
		private static IEnumerator DoPreCache(string url, Action<string> cb)
		{
			bool disposable = false;
			var www = LoadFromCacheOrDownload(url, out disposable);
			yield return www;
			yield return 1;
#if USE_CACHE	
			var fileName = GetCacheFile(url);
			if ( File.Exists(fileName) )
			{
				
				var cachedPath = "file://"+fileName;
				cb(cachedPath);
			}
			else
#endif
			{
				cb(url);
			}
			
		}
		
		public static Coroutine Precache(string url, Action<string> cb)
		{
			return Coroutines.Run( DoPreCache(url,cb) );
		}
	}
	
}
