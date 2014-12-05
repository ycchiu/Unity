using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public interface ISaveLoadInterface
	{
	    string Key 						{ get; }
		bool	PostLoadSaveRequired 	{ get; }
		
	    IEnumerator Save( Id userId, Hashtable data);
	    IEnumerator Load( Id userId, Hashtable data);
	    IEnumerator PostLoad(Id userId);
	}

	public class SaveLoadManagerConfig
	{
		public float SaveDelay = 0.0f;
	}
		
	public class SaveLoadManager : SubSystem, Updatable
	{
	    public static int Version = 1;	
	    private List<ISaveLoadInterface> _saveMap = new List<ISaveLoadInterface>();
		private SaveLoadManagerConfig _config;
	 
	    private bool _doLoad = false;
	    private bool _ignoreLoginData = false;
	    private Id _loadId = Id.Null;
	    private Id _loadedId = null;
		private float _saveDelay = 0.0f;
	
	    public bool IsLoading = false;
	    public bool IsSaving = false;
	    public bool IsError = false;
	
	    public static bool IsReady = false;

		public bool UpdateOffline { get { return true;} }
			
	    public Id LoadedId
	    {
	        get
	        {
	            if (_loadedId != null)
	                return _loadedId;
	            return Hub.LoginManager.LocalUserId;
	        }
	    }
		
	    const string kVersionKey = "version";
		
		private class SaveInfo
		{
			public int id = 0;
			public List<ISaveLoadInterface> interfaces = new List<ISaveLoadInterface>();
			
			public void Add( ISaveLoadInterface[] other ) 
			{
				foreach( ISaveLoadInterface i in other )
				{
					if ( this.interfaces.Contains(i) == false )
					{
						this.interfaces.Add(i);
					}
				}
			}
			
			public override string ToString ()
			{
				var r = "";
				for( int i = 0; i < interfaces.Count; ++i )
				{
					r += interfaces[i].Key + ",";
				}
				return r;
			}
		}
		
		private Collections.Queue<SaveInfo> _saves = new Collections.Queue<SaveInfo>();
		private int 	_nextSaveId = 1;
		private int 	_doneSaveId = 0;
		
	    public void Register( ISaveLoadInterface obj )
	    {
	        Unregister(obj);
	        _saveMap.Add(obj); 
	    }
		
		public void Unregister( ISaveLoadInterface obj )
		{
			_saveMap.Remove(obj);
		}
	
		// Update is called once per frame
		public void Update()
	    {
	        if (IsSaving || IsLoading)
	        {
	            return;
	        }
			
			if ( _saves.Count > 0 )
			{
				_saveDelay -= Time.deltaTime;

				if (_saveDelay>0)
				{
					return;
				}

				var localId = Hub.LoginManager.LocalUserId;
				if (localId == _loadedId)
				{
					StartCoroutine( DoSave( _saves.Dequeue() ) );
					return;
				}
				else
				{
					EB.Debug.LogError("Cant save because we havent loaded yet!");
				}
			}
	
	        if (_doLoad)
	        {
	            _doLoad = false;
	            StartCoroutine(DoLoad( _loadId ?? Hub.LoginManager.LocalUserId));
	            return;
	        }
	
		}	

	    public Coroutine Save( params ISaveLoadInterface[] saveLoad )
	    {
	        // only allow saving of local profile
	        return _SaveInternal(saveLoad); 
	    }
			
	    public void Load(Id userId, bool ignoreLoginData = false)
	    {
	        _doLoad = true;
	        _ignoreLoginData = ignoreLoginData;
	        _loadId = userId;
	    }
	
		public override void Async (string message, object options)
		{
			switch(message.ToLower())
			{
			case "save":
			{
				this.Save(null);
				break;
			}
			case "load":
			{
				this.Load(Hub.LoginManager.LocalUserId, true);
				break;
			}
			default:
			{
				EB.Debug.LogError("SaveLoadManager > Unknown Async Message: " + message + " -> " + options);
				break;
			}
			}
		}

	    public Coroutine LoadSingle(Id userId, ISaveLoadInterface loader)
	    {
	        return StartCoroutine(DoLoadSingle(userId, loader));
	    }
		
		public Coroutine _SaveInternal(ISaveLoadInterface[] saveLoad)
	    {
			if ( saveLoad == null || saveLoad.Length == 0 )
			{
	            EB.Debug.Log("Full Save Requested");
				saveLoad = _saveMap.ToArray();
			}
			
			SaveInfo info = null;
			if ( _saves.Count > 0 )
			{
				info = _saves.Peek();
			}
			else
			{
				info = new SaveInfo();
				info.id = _nextSaveId++;
				_saves.Enqueue(info);
			}

			// set the save delay
			_saveDelay = _config.SaveDelay;
			
			info.Add(saveLoad);
	        return Wait( info.id );
	    }
		
		public Coroutine Wait( int saveId )
		{
			return StartCoroutine( DoWait(saveId) );
		}
		
		private WaitForFixedUpdate _waiter = new WaitForFixedUpdate();
		private IEnumerator DoWait( int saveId )
		{
			while (_doneSaveId < saveId )
			{
				yield return _waiter;
			}
		}
	
	    private IEnumerator DoLoadSingle(Id userId, ISaveLoadInterface loader)
	    {
			bool complete = false;	
			bool error = false;
			Hub.DataManager.Load( userId, loader.Key, delegate(string err, Data result) {
				complete = true;
				if (!string.IsNullOrEmpty(err))
				{
					error = true;
					Hub.FatalError(err ?? "Failed to load data");
					return;
				}
			}, _ignoreLoginData);
			
			while (!complete) 
			{
				yield return 1;
			}

			if (error) {
				yield break;
			}
			
			var data = Hub.DataManager.GetData( userId );
	        object obj = data.data[loader.Key];
	
	        Hashtable value = obj as Hashtable;
			if ( value == null )
			{
				value = new Hashtable();
			}
			
			yield return StartCoroutine(loader.Load(userId, value));
			yield return StartCoroutine(loader.PostLoad(userId));
			_ignoreLoginData = false;
		}
	
	    IEnumerator DoSave( SaveInfo info )
	    {
	        IsSaving = true;

			EB.Debug.Log("Do Save: " + info );
			
			var userId = Hub.LoginManager.LocalUserId;
	
	        Hashtable master = new Hashtable();
			for ( int i = 0; i < info.interfaces.Count; ++i )
	        {
				ISaveLoadInterface saveLoad = info.interfaces[i];
				
	            Hashtable data = new Hashtable();
	            yield return StartCoroutine(saveLoad.Save(userId,data));
	            master[saveLoad.Key] = data;
	        }
			
			master[kVersionKey] = Version.ToString();	
			master["platform"] = Application.platform.ToString();		
	        
			var complete = false;
			
			Hub.DataManager.Save( userId, master, delegate(string error) {
			
				if (!string.IsNullOrEmpty(error))
				{
					Hub.FatalError(error);
				}
				complete = true;
			});
			
			while (!complete) 
			{
				yield return 1;
			}
				
	        IsSaving = false;
			_doneSaveId++;
	    }
	
	    public Coroutine DebugClear()
	    {
	        return StartCoroutine(DoDebugClear());
	    }
	
	    private IEnumerator DoDebugClear()
	    {		
	        Hashtable tmp = new Hashtable();
			foreach ( var saveload in _saveMap )
			{
				tmp[saveload.Key] = new Hashtable();
			}
	        tmp[kVersionKey] = 0;
			
			bool complete = false;
			Hub.DataManager.Save( Hub.LoginManager.LocalUserId, tmp, delegate(string obj) {
				complete = true;
			});
			
			while (!complete) yield return 1;
			
	#if UNITY_IPHONE
	        Application.Quit();
	#endif
	    }
	
	    private int GetVersion(IDictionary data)
	    {
	        if (data == null) return 0;
	
	        object value = data[kVersionKey];
	        if (value != null)
	        {
	            int version = 0;
	            if ( int.TryParse(value.ToString(), out version ) )
	            {
	                return version;
	            }
	        }
	        return Version; // assume empyt if there's no version
	    }
			
	    IEnumerator DoLoad(Id userId)
	    {
	        IsLoading = true;
		
			var complete = false;
			Hub.DataManager.Load( userId, string.Empty, delegate(string err, Data res) {
				if (!string.IsNullOrEmpty(err))
				{
					Hub.FatalError(err);
				}
				
				complete = true;	
			},_ignoreLoginData); 
			
			while (!complete)
			{
				yield return 1;
			}
			
	        _loadedId = userId;
	
	        IDictionary master = Hub.DataManager.GetData(userId).data;		
			
	        if (GetVersion(master) != Version)
	        {
	            EB.Debug.Log("Version Mismatch, defaulting data!!");
	            master = new Hashtable();
	        }
	        
	        // foreach (ISaveLoadInterface saveLoad in _saveMap)
			for ( int i = 0; i < _saveMap.Count; ++i )
	        {
				ISaveLoadInterface saveLoad = _saveMap[i];
	            object obj = master[saveLoad.Key];
	
	            Hashtable data = obj as Hashtable;
	
	            if (data == null)
	            {
	                data = new Hashtable();
	            }          
	            yield return StartCoroutine(saveLoad.Load(userId, data));
	        }
			
			// post load
	        for ( int i = 0; i < _saveMap.Count; ++i )
	        {
				ISaveLoadInterface saveLoad = _saveMap[i];
				yield return StartCoroutine(saveLoad.PostLoad(userId));
	        }
			
			if ( userId == Hub.LoginManager.LocalUserId )
			{
				Hashtable saveData = new Hashtable();
				for ( int i = 0; i < _saveMap.Count; ++i )
	        	{
					ISaveLoadInterface saveLoad = _saveMap[i];
					if ( saveLoad.PostLoadSaveRequired )
					{
						EB.Debug.LogWarning("PostLoadSave: " + saveLoad.Key);
						Hashtable data = new Hashtable();
						yield return StartCoroutine(saveLoad.Save(userId,data) );
						saveData[saveLoad.Key] = data;
					}
	        	}
				
				if ( saveData.Count > 0 )
				{
					EB.Debug.Log("Handling post load save");
					complete = false;
					Hub.DataManager.Save( userId, saveData, delegate(string error){
						complete = true;
					});
			        while (!complete)
					{
						yield return 1;
					}
				}
				
				State = SubSystemState.Connected;
			}
		
	        IsLoading = false;
	        IsReady = true;
			_ignoreLoginData = false;
	    }
		
		public override void Connect ()
		{
			State = SubSystemState.Connecting; 
			Load( Hub.LoginManager.LocalUserId );
		}

		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			_config = config.SaveLoadConfig;
		}
		#endregion

		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Disconnect (bool isLogout)
		{
			State = SubSystemState.Disconnected;
			_loadedId = Id.Null;
			_saves.Clear();
		}
		#endregion
	}
}


