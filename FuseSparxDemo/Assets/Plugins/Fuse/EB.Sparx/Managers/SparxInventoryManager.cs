using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace EB.Sparx
{
	public class InventoryConfig
	{
		public InventoryListener Listener = new DefaultInventoryListener();
		public bool Enabled = true;
		public Hashtable StartingInventory = new Hashtable();
	}
	
	public class InventoryManager : SubSystem
	{
		InventoryConfig _config;
		InventoryAPI _api;
		
		Hashtable _data;
		Hashtable _used;
		
		Hashtable _overrides;
		
		Hashtable _inventory { get { return _overrides.Count > 0 || ForceOverride ? _overrides : _data; } }
		
		public bool ForceOverride;
		
		public bool IsOverriden { get { return _overrides.Count > 0 || ForceOverride; } }
		public int	OverrideCount { get { return _overrides.Count; } } 
		
		public bool EverythingFree;
		
		List<string> _overrideKeys; // want this in order
		public string GetOverrideItem(int index) 
		{ 
			if(index < _overrideKeys.Count) 
			{ 
				return _overrideKeys[index];
			}

			EB.Debug.LogWarning("Warning: index out of range, returning empty string");
			return string.Empty;
		}
		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			EB.Debug.LogWarning("Inventory initialize");
			_config = config.InventoryConfig;
			
			if (_config.Listener == null )
			{
				throw new System.ArgumentNullException("Must provide a valid inventory listener");
			}
			_api = new InventoryAPI( Hub.ApiEndPoint );
			
			_used = new Hashtable();
			_data = new Hashtable();
			_overrides = new Hashtable();
			_overrideKeys = new List<string>();
			ForceOverride = false;
			EverythingFree = false;
		}
		
		public override void Connect ()
		{
			EB.Debug.LogWarning("Inventory Connect");
			State = SubSystemState.Connecting;
			
			var data = Dot.Object("inventory", Hub.LoginManager.LoginData, null);
			if ( data != null)
			{
				OnSync(0, null, data);
			}
			else
			{
				Sync();
			}
			
			
		}

		public override void Disconnect (bool isLogout)
		{
			
		}
		#endregion
		
		
		public void Override( Hashtable items )
		{
			_overrides = SafeNames(items);
		}
		
		public void ClearOverride()
		{
			_overrides.Clear();
			_overrideKeys.Clear();
			ForceOverride = false;
			EverythingFree = false; // this "should" be turned off by the sequence, but the sequence is bypassed by quitting
		}
		
		public void AddOverride( string name, int count )
		{
			name = SafeName(name);
			_overrides[name] = count;
			AddOverrideKey(name);
		}
		
		public void AddOverrideKey(string key)
		{
			_overrideKeys.Add(key);
		}
		
		public void ClearOverrideKeys()
		{
			_overrideKeys.Clear();
		}
		
		//ONLY USE IN SP!!!!
		public void SinglePlayerClearUsedInventory()
		{
			_used.Clear();
		}
		
		public bool IsEmpty()
		{
			if(EverythingFree)
			{
				return false;
			}
			foreach(string key in _inventory.Keys)
			{
				if(GetCount(key) > 0)
				{
					return false;
				}
			}
			return true;
		}
		
		public int GetCount( string item ) 
		{		
			item = SafeName(item);
			var count = Dot.Integer( item, _inventory, 0 ); 
			var used  = Dot.Integer( item, _used, 0 );
			return Mathf.Max(0, count - used);
		}
		
		public int AddItem( string item, int quantity )
		{
			var items = new Hashtable(){ { item, quantity } };
			return AddItems( items );
		}
		
		public int UseItem( string item, int quantity )
		{
			var items = new Hashtable(){ { item, quantity } };
			return UseItems( items );
		}
				
		public int UseItems( Hashtable items )
		{
			items = SafeNames(items);
			return _api.Use( items, OnSync );
		}		
		
		public bool UseInGame( string item, int quantity )
		{
			item = SafeName(item);
			if ( GetCount(item) >= quantity )
			{
//				EB.Debug.LogWarning("Using " + item);
				int used = Dot.Integer( item, _used, 0 );
				used 	+= quantity;
				_used[item] = used;
				
				if ( Hub.GameManager != null && Hub.GameManager.Game != null )
				{
					Hub.GameManager.Game.SendGameCommand( Network.HostId, "use", new Hashtable{ { item,quantity } } ); 
				}
				return true;
			}
			return false;
		}
		
		public int AddItems( Hashtable items )
		{
			items = SafeNames(items);
			return _api.Add( items, OnAdd ); 
		}
		
		public int PurchaseItems( Hashtable items, int cost )
		{
			items = SafeNames(items);
			if ( cost > 0 )
			{
				return _api.Purchase( items, cost, OnPurchase ); 
			}
			else
			{
				return _api.Add(items, OnAdd);
			}
		}
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
			case "update":
				{
					ArrayList updates = payload as ArrayList;
					if (updates != null)
					{
						foreach(object update in updates)
						{
							OnUpdate(update);
						}
					}
					else
					{
						OnUpdate(payload);
					}
					_config.Listener.OnInventoryUpdated();
				}
				break;

			case "sync":
				{
					EB.Debug.Log("recieved sync notification");
					Sync();
				}
				break;
			}
		}

		void OnUpdate(object update)
		{
			var item = Dot.String("item", update, string.Empty);
			var quantity = Dot.Integer("quantity", update, -1);

			if (string.IsNullOrEmpty(item))
			{
				EB.Debug.LogError("Invalid item name ({0})", item);
				return;
			}

			if (quantity < 0)
			{
				EB.Debug.LogError("Invalid quantity ({0}) for item {1}", quantity, item);
				return;
			}

			this._data[SafeName(item)] = quantity;
		}

		public int Sync()
		{
			EB.Debug.Log("SparxInventoryManager > Sync");
			return _api.Sync(OnSync);	
		}
		
		public Coroutine Wait( int requestId)
		{
			return _api.Wait(requestId);
		}
		
		void OnPurchase(int transId, string err, Hashtable data)
		{
			if ( err == "nsf" )
			{
				_config.Listener.OnInventoryPurchaseFailed(transId);
				return;
			}
			else if (!string.IsNullOrEmpty(err))
			{
				State = SubSystemState.Error;
				return;
			}
			
			foreach( DictionaryEntry entry in data )
			{
				_inventory[SafeName(entry.Key.ToString())] = entry.Value;
			}
			_config.Listener.OnInventoryItemsAdded(transId, data);
		}
		
		void OnAdd(int transId, string err, Hashtable data)
		{
			if (!string.IsNullOrEmpty(err))
			{
				State = SubSystemState.Error;
				return;
			}
			
			foreach( DictionaryEntry entry in data )
			{
				_inventory[SafeName(entry.Key.ToString())] = entry.Value;
			}
			_config.Listener.OnInventoryItemsAdded(transId, data);
		}
		
		void OnSync(int transId, string err, Hashtable data)
		{
			if (!string.IsNullOrEmpty(err))
			{
				if (err == "nsf")
				{
					return;
				}
				FatalError(err);
				return;
			}
			EB.Debug.LogWarning("SparxInventoryManager > OnSync");
			State = SubSystemState.Connected;
			
			if (data == null)
			{
				EB.Debug.LogWarning("SparxInventoryManager > data is null");
				// setup starting inventory
				_data = new Hashtable();
				
				if (_config.StartingInventory.Count>0)
				{
					EB.Debug.Log("SparxInventoryManager > OnSync > initializing inventory");
					AddItems(_config.StartingInventory);
				}
			}
			else
			{
				string s = ("SparxInventoryManager > OnSync > data.Count is " + data.Count);	// moko: change how debug spews being spill (minimize the # of lines ;-P)
				foreach( DictionaryEntry entry in data)
				{
					s += ("\nSparxInventoryManager > OnSync > Entry: " + entry.Key.ToString() + " : " + entry.Value.ToString());
				}
				EB.Debug.Log(s);
				_data = SafeNames(data);
			}
			
			// only clear used if we aren't in a game
			if ( Hub.GameManager != null && Hub.GameManager.Game == null)
			{
				_used.Clear(); // cleared used
			}

			_config.Listener.OnInventorySynced(transId);
		}
		
		// maybe move this to a utility class
		public string SafeName( string src )
		{
			return StringUtil.SafeKey(src);
		}
		
		Hashtable SafeNames( Hashtable src)
		{
			Hashtable data = new Hashtable();
			foreach( DictionaryEntry entry in src )
			{
				data[SafeName(entry.Key.ToString())] = entry.Value;
			}
			return data;
		}
		
		
	}
	
}