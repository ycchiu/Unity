using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class Data
	{		
	    public Id userId 			{ get; private set; }
	    public Hashtable data 		{ get; private set; }
		public Hashtable fragments 	{ get; private set; }
		public bool fullFetch 		{ get; private set; } // do we have everything?
	
	    public Data(Id id)
	    {
	        userId = id;
			data = new Hashtable();
			fragments = new Hashtable();
			fullFetch = false;
	    }
		
		public void Load( string key )
		{
			var data = SecurePrefs.GetJSON(key);
			if ( data != null && data is Hashtable) 
			{
				StoreResult( (Hashtable)data, true);
			}
		}
		
		public void Save( string key )
		{
			// save fragments as it will be faster
			//var r = 
			SecurePrefs.SetJSON(key, fragments);
			//Debug.Log("Save Offline: \n" + r);
		}
	
	    public void StoreResult( Hashtable obj, bool full )
	    {
	        if (obj == null) return;
			
			if ( full)
			{
				fullFetch = true;
			}
			
			foreach (DictionaryEntry entry in obj)
			{
				var key = entry.Key.ToString();
				data[key] = entry.Value;
				fragments[key] = new JSON.Fragment(JSON.Stringify(entry.Value));
			}	
	    }	
		
		public Hashtable Delta( Hashtable obj )
		{			
			Hashtable diff = new Hashtable();
			foreach( DictionaryEntry entry in obj)
			{
				var src 	= fragments[entry.Key];
				var value	= new JSON.Fragment(JSON.Stringify(entry.Value));
				
				// compare 
				if (src == null || !Equal(src,value) )
				{
					diff.Add(entry.Key,value);
				}
				else
				{
					EB.Debug.Log("skipping save for key: " + entry.Key);
				}
			}
			return diff;
		}
		
		// todo: better comparision?
		private bool Equal(object o1, object o2)
		{
			return JSON.Stringify(o1)==JSON.Stringify(o2);
		}
	}
	
	public class DataManager : Manager
	{
		DataAPI _api;
		Dictionary<Id,Data> _data;
		
		public override void Initialize (Config config)
		{
			_api =  new DataAPI(Hub.ApiEndPoint);
			_data = new Dictionary<Id, Data>();
		}
		
		public Data GetData( Id userId )
		{
			Data result;
			if (!_data.TryGetValue(userId, out result))
			{
				result = new Data(userId);
				_data.Add(userId, result);
			}
			return result;
		}
		
		public void Load( Id userId, string key, Action<string,Data> callback, bool ignoreLoginData ) 
		{
			// offline support
			if (userId == Id.Offline)
			{
				callback(null, GetData(userId));
				return;
			}
			
			var data = GetData(userId);	
			if( ( userId == Hub.LoginManager.LocalUserId ) && ( ignoreLoginData == false ) )
			{
				// check for logindata
				var ds = Dot.Object("ds", Hub.LoginManager.LoginData, null);
				if ( ds != null )
				{
					data.StoreResult(ds, true);
					callback(null,data);
					return;
				}
			}
			
			_api.Load( userId, key, delegate(string error, Hashtable result){
				if (!string.IsNullOrEmpty(error))
				{
					callback(error,data);
					return;
				}
				
				bool fullFetch = string.IsNullOrEmpty(key);
				data.StoreResult(result,fullFetch);
				
				callback(null,data);
			});
		}
		
		public void Save( Id userId, Hashtable obj, Action<string> callback )
		{
			var data = GetData(userId);
			var toSave = data.Delta(obj);
			
			if (toSave.Count == 0)
			{
				EB.Debug.Log("Nothing to save");
				callback(null);
				return;
			}
			
			data.StoreResult(obj, false);
			
			if ( Hub.State != HubState.Idle && userId != Id.Offline )
			{
				_api.Save( userId, toSave, callback);
			}
			else
			{
				callback(string.Empty);	
			}
			
		}
	}
	
}
