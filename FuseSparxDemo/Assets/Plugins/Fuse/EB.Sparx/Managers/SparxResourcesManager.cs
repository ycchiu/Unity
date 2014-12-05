using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ResourcesType
	{
		public ResourcesType( string name, Hashtable resData = null )
		{
			this.Name = name;

			if (resData != null) 
			{
				this.Amount = EB.Dot.Integer("v", resData, 0);
				this.Max = EB.Dot.Integer("max", resData, 0);
				this.NextGrowthAmount = EB.Dot.Integer("nextGrowthAmount", resData, 0);
				this.NextGrowthTime = EB.Dot.Integer("nextGrowthTime", resData, 0);
				//EB.Debug.Log("++++++++++++++++++++++++++++++++++++++++++Initial Data: " + this.ToString());
			}
		}
		
		public string ToString()
		{
			string data = "[" + this.Name + "] Amount=" + this.Amount + " Max=" + this.Max + " NextGrowthTime=" + this.GetNextGrowthTime_Local().ToString() + " NextGrowthAmount=" +  this.NextGrowthAmount;
			return data;
		}
		
		public System.DateTime GetNextGrowthTime_UTC()
		{
			return EB.Time.FromPosixTime(this.NextGrowthTime);
		}
		
		public System.DateTime GetNextGrowthTime_Local()
		{
			return EB.Time.FromPosixTime(this.NextGrowthTime + Version.GetTimeZoneOffset());
		}

		public void Internal_SetAmount(int amount) { this.Amount = amount; }
		public void Internal_SetMax(int max) { this.Max = max; }
		public void Internal_SetGrowth(int nextAmount, int nextTime) { this.NextGrowthAmount = nextAmount; this.NextGrowthTime = nextTime; }

		public string Name { get; private set; }
		public int Amount { get; private set; }
		public int Max { get; private set; }
		public int NextGrowthAmount { get; private set; }
		public int NextGrowthTime { get; private set; }
	}

	public class ResourcesStatus
	{
		public ResourcesStatus( Hashtable data = null )
		{
			this.Types = new List<ResourcesType>();

			if( data != null )
			{
				//ResourcesManager.PrintHashTable("ResourcesStatus", data);

				foreach( DictionaryEntry entry in data )
				{
					string name = entry.Key.ToString();
					Hashtable resData = EB.Dot.Object(name, data, null);

					this.Types.Add( new ResourcesType(name, resData) );
				}
			}
		}

		public List<ResourcesType> Types { get; private set; }
	}

	public class ResourcesManager : SubSystem, Updatable
	{
		ResourcesAPI _api = null;
		ResourcesStatus _status = new ResourcesStatus();

		public delegate void ResourceChangeDel(ResourcesStatus status);
		public ResourceChangeDel OnResourceChange;
		
		public ResourcesStatus Status { get { return _status; }	}

		public ResourcesType GetResource(string typeName)
		{
			foreach (ResourcesType entry in this._status.Types) 
			{
				if (entry.Name == typeName)
				{
					return entry;
				}
			}
			return null;
		}

		public void Fetch()
		{
			this._api.FetchStatus(OnFetch);
		}

		public int GetAmount(string typeName)
		{
			ResourcesType type = GetResource(typeName);
			if (type != null) 
				return type.Amount;
			else 
				return 0;
		}
		
		public void DebugAddResource( string typeName, int amount, EB.Action<int> cb )
		{
			this._api.DebugAddResource( typeName, amount, delegate( string err, Hashtable data ) {
				if( ( string.IsNullOrEmpty( err ) == true ) && ( data != null ) )
				{
					Hashtable resourceCategories = EB.Dot.Object( "res", data, null );
					if (resourceCategories != null)
					{
						OnResourceData( resourceCategories );
					}
				}
				
				if( cb != null )
				{
					cb( GetAmount( typeName ) );
				}
			});
		}
		
		void OnFetch( string err, Hashtable data )
		{
			if( string.IsNullOrEmpty( err ) == true )
			{
				Hashtable resourceCategories = EB.Dot.Object( "res", data, null );
				if (resourceCategories != null)
				{
					OnResourceData( resourceCategories );
				}
			}
			else
			{
				EB.Debug.LogError( "Error Fetching Resouces: {0}", err );
			}
		}
		
		void OnResourceData( Hashtable data )
		{
			this._status = new ResourcesStatus( data );
			if (OnResourceChange != null) { OnResourceChange(_status); }
		}

		public static void PrintHashTable(string title, Hashtable data)
		{
			EB.Debug.Log("******[ResourcesManager] " + title + " data.Count=" + data.Count);
			foreach( DictionaryEntry entry in data)
			{
				EB.Debug.Log("      Entry: " + entry.Key.ToString() + " : " + entry.Value.ToString());
			}
		}

		
		void OnUpdate(object update)
		{
			var name = Dot.String("resource", update, string.Empty);
			var balance = Dot.Integer("balance", update, -1);
			var max = Dot.Integer("max", update, -1);
			var nextGrowthAmount = Dot.Integer("nextGrowthAmount", update, -1);
			var nextGrowthTime = Dot.Integer("nextGrowthTime", update, -1);
			
			if (string.IsNullOrEmpty(name))
			{
				EB.Debug.LogError("Invalid resource name ({0})", name);
				return;
			}

			foreach(ResourcesType resource in _status.Types)
			{
				if (resource.Name == name)
				{
					if (balance >= 0)
					{
						resource.Internal_SetAmount(balance);
					}

					if (max >= 0)
					{
						resource.Internal_SetMax(max);
					}
					
					if (nextGrowthTime >= 0)
					{
						resource.Internal_SetGrowth(nextGrowthAmount, nextGrowthTime);
					}

					// Get out of loop, we found what we were looking out
					break;
				}
			}
		}


		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_api = new ResourcesAPI(Hub.ApiEndPoint);
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var resourcesData = Dot.Object( "res", Hub.LoginManager.LoginData, null );
			if( resourcesData != null )
			{
				this.OnFetch( null, Hub.LoginManager.LoginData );
			}

			State = SubSystemState.Connected;
		}
		
		public void Update ()
		{
		}
		
		public override void Disconnect (bool isLogout)
		{
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
			
					if (OnResourceChange != null)
					{
						OnResourceChange(_status);
					}
				}
				break;

			case "sync":
				{
					this._api.FetchStatus( OnFetch );
				}
				break;
			}
		}
		#endregion
	}
}

