//#define TEXTUREPOOL_SPEW
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TexturePoolManager : MonoBehaviour 
{
	static TexturePoolManager _this;
	public static TexturePoolManager Instance { get { return _this; } } 

	class TextureInfo
	{
		public string textureName = null;
		public Texture2D texture = null;	
		public float time = 0;
		public List<EB.Action<Texture2D>>	callbacks = new List<EB.Action<Texture2D>>();
		public int refCount = 0;
		
		public int size
		{
			get
			{
				if ( texture != null )
				{
					return texture.width*texture.height;
				}
				return 0;
			}
		}
		
		public void DoCallbacks()
		{
			while (callbacks.Count>0)
			{
				var cb = callbacks[callbacks.Count-1];
				callbacks.RemoveAt(callbacks.Count-1);
				if ( cb != null )
				{
					cb(texture);
				}
			}
		}
	}
	
#if UNITY_IPHONE
	const int 			kMaxConcurrentRequests  = 4;
	const int 			kPoolSize  	 			= 16;
	const int 			kGCAfterEvictions		= 4;
#else
	const int 			kMaxConcurrentRequests 	= 4;
	const int 			kPoolSize  	 			= 48;
	const int 			kGCAfterEvictions		= 16;
#endif
	
	int					_concurrentRequests 	= 0;
	int					_evictions				= 0;
	
	List<TextureInfo> 	_all = new List<TextureInfo>();
	EB.Collections.Queue<string> 	_loads = new EB.Collections.Queue<string>();

	void Awake() 
	{
		_this = this;
	}

	public override string ToString()
	{
		string msg = "";
		foreach (TextureInfo info in _all)
		{
			msg += string.Format("{0} x{1} ({2})\n", info.textureName, info.refCount, info.size);
		}
		return msg;
	}
	
	void Start()
	{
		StartCoroutine(UpdateTexturePoolManager());
	}

	private IEnumerator UpdateTexturePoolManager ()
	{
		while(true)
		{
			if (_loads.Count>0)
			{
				if (_concurrentRequests < kMaxConcurrentRequests)
				{				
					var textureName = _loads.Dequeue();
					var info = GetTextureInfo(textureName);
					if ( info != null && info.texture == null )
					{
						_concurrentRequests++;
						StartCoroutine(ProcessNextTexture(info));			
					}
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}
	
	private TextureInfo GetTextureInfo( string textureName )
	{
		foreach( var info in _all )
		{
			if ( info.textureName == textureName )
			{
				return info;
			}
		}
		return null;
	}
	
	private void Print()
	{
#if TEXTUREPOOL_SPEW
		foreach( var info in _all) 
		{
			EB.Debug.Log("{0}:{1},{2},{3}", info.textureName, info.size, info.time, info.refCount); 
		}
#endif					
	}
	
	private void Evict(string nextTextureToLoad = "")
	{
		var dead = new List<TextureInfo>();
		foreach( var info in _all) 
		{
			if ( info.refCount <= 0 && info.textureName != nextTextureToLoad )
			{
				dead.Add(info);
			}
		}
		
		if ( dead.Count > kPoolSize )
		{
			// sort by size, then time
			dead.Sort( delegate(TextureInfo ti1, TextureInfo ti2 ){
				if ( ti1.size == ti2.size )
				{
					if ( ti1.time > ti2.time )
					{
						return 1;
					}
					else if ( ti1.time < ti2.time )
					{
						return -1;
					}
					return 0;
				}
				return ti2.size - ti1.size;
			});
			
			Print();
		
			int max = dead.Count - kPoolSize;
			for ( int i = 0; i < dead.Count && i < max; ++i )
			{
				var info = dead[i];
				// evict
				// Resources.UnloadAsset(info.texture);
				Destroy(info.texture);
				_all.Remove(info);
				++_evictions;
			}
		}
		
		if ( _evictions >= kGCAfterEvictions )
		{
			_evictions = 0;
			EB.Assets.UnloadUnusedAssets();
		}
		
	}
	
	public static string GetUrl(string texture)
	{
		if ( texture.StartsWith("http://") || texture.StartsWith("https://") || texture.StartsWith("file://") )
		{
			return texture;
		}
		return EB.Assets.FindDiskAsset(texture);
	}
	
	IEnumerator ProcessNextTexture( TextureInfo info )
	{	
		string url = GetUrl(info.textureName);
		
#if TEXTUREPOOL_SPEW
		EB.Debug.Log("TexturePoolManager - LOADING " + url + " for texture info " + info.textureName);
#endif
		
		// evict any old textures before moving on
		Evict();
		
		WWW textureLoader = null;
		bool disposable = true;
		
		if (EB.Assets.CurrentBundleMode == EB.Assets.DevBundleType.BundleServer)
		{
			EB.Debug.Log(string.Format("Url for {0} is {1}", info.textureName, url));

			EB.Loader.CachedLoadHandler loader = new EB.Loader.CachedLoadHandler(url, true);
			yield return loader.Load();
			
			WWW result = loader.GetWWW();
			if (result != null && string.IsNullOrEmpty(result.error)) 
			{
				EB.Debug.Log ("RETURNING CachedLoadHandler.WWW from LoadFromCacheOrDownload");
				textureLoader = result;
			}
		}
		if (textureLoader == null)
		{
			for( int i = 0; i < 5; ++i )
			{	
				textureLoader = EB.Cache.LoadFromCacheOrDownload(url, out disposable);
				if (textureLoader == null ) 
				{
					break;
				}

				yield return textureLoader;
				
				if ( string.IsNullOrEmpty(textureLoader.error) )
				{
					break;
				}
				else if (url.StartsWith("file://"))
				{
					// file error are going to make a difference with a retry
					break;
				}
				
				EB.Debug.LogWarning("COULD NOT FIND TEXTURE: " + url);
				yield return new WaitForSeconds(0.5f*i);
			}
		}
		
		if ( Application.isEditor )
		{
			yield return new WaitForSeconds(0.15f);
		}
			
		if ( textureLoader != null && string.IsNullOrEmpty(textureLoader.error)  )
		{
			info.texture = textureLoader.textureNonReadable;
			info.texture.wrapMode = TextureWrapMode.Clamp;
			info.texture.name = info.textureName;
			info.DoCallbacks();
		
#if TEXTUREPOOL_SPEW	
			EB.Debug.LogWarning("ON-DEMAND LOAD FROM {0} THERE ARE NOW {1} IN THE POOL", url, _all.Count);
#endif
		}
		else
		{
			info.DoCallbacks();
		}
		
		if( textureLoader != null ) 
		{
			if( disposable == true )
			{
				textureLoader.Dispose();
			}
			else
			{
				EB.Cache.DisposeOnComplete( textureLoader );
			}
		}

		--_concurrentRequests;
	}
	
	public void ClearPool(bool forceGarbageCollect = false)
	{
#if TEXTUREPOOL_SPEW
		EB.Debug.Log("Clearing pool!");
#endif
		foreach( var info in _all )
		{
			Destroy(info.texture);
		}		
		_all.Clear();
		_evictions = 0;
		
		if (forceGarbageCollect && EB.Assets.RequiresUnloadAssets() )
		{
			EB.Assets.UnloadUnusedAssets();
		}
	}
	
	public void ReleaseTexture( string textureName )
	{
		if (string.IsNullOrEmpty(textureName))
		{
			return;
		}
		
		var info = GetTextureInfo(textureName);
		if ( info != null )
		{
			info.refCount--;
#if TEXTUREPOOL_SPEW			
			EB.Debug.Log("Dec refcount for textue: {0}, ref count: {1}", textureName, info.refCount );
#endif
		}
	}

    public void LoadTexture(string textureName, MonoBehaviour obj, EB.Action<Texture2D> cb )
	{
        LoadTexture(textureName, EB.SafeAction.Wrap(obj,cb) ); 
	}
	
	private void LoadTexture(string textureName, EB.Action<Texture2D> cb)
	{
#if TEXTUREPOOL_SPEW
		EB.Debug.Log("Trying to load texture " + textureName);
#endif
		if (string.IsNullOrEmpty(textureName))
		{
			EB.Debug.LogWarning("Attempt to load empty texture.");
			return;
		}
		
		var info = GetTextureInfo(textureName);
		if ( info != null )
		{
			info.refCount++; // inc the refcount
			info.time = Time.realtimeSinceStartup;
			
			if ( info.texture != null )
			{
				if ( cb != null )
				{
					cb(info.texture);
				}
			}
			else
			{
				info.callbacks.Add(cb);
				_loads.Add(textureName);
			}
			return;
		}

		// create new info
		info = new TextureInfo();
		info.textureName = textureName;
		info.refCount = 1;
		info.time = Time.realtimeSinceStartup;
		info.callbacks.Add(cb);
		_all.Add(info);
		_loads.Add(textureName);
	}
	
}
