using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ObjectiveReport 
	{
		public ObjectiveReport(string category, string id, int increment)
		{
			this.Category = category;
			this.Id = id;
			this.Increment = increment;
		}
		public bool IsValid 
		{ 
			get 
			{
				return string.IsNullOrEmpty(this.Category) == false && string.IsNullOrEmpty(this.Id) == false && this.Increment > 0; 
			}
		}
		public string Category { get; private set; }
		public string Id { get; private set; }
		public int Increment { get; private set; }

		public void IncrementCount() { Increment++; }
	}

	public class ObjectiveItem
	{
		public ObjectiveItem( Hashtable data = null )
		{
			if( data != null )
			{
				this.Id = EB.Dot.String("_id", data, "");
				this.Category = EB.Dot.String("category", data, "");
				this.Description = EB.Dot.String("description", data, "");
				this.Name = EB.Dot.String("name", data, "");
				this.Data = EB.Dot.Array("data", data, new ArrayList());
				this.Current = EB.Dot.Integer("current", data, 0);
				this.Target = EB.Dot.Integer("target", data, 0);
				this.CompleteTime = EB.Dot.Integer("complete", data, 0);
				this.NextTime = EB.Dot.Integer("next", data, 0);
				this.ObjectiveType = EB.Dot.String("type", data, "rw");
				this.Rewarded = EB.Dot.Bool("rewarded", data, false);
				this.IsNew = EB.Dot.Bool("isNew", data, false);
				ArrayList rewards = EB.Dot.Array("rewards", data, null);
				List<RedeemerItem> redeemers = new List<RedeemerItem>();
				if(rewards != null) 
				{
					foreach (Hashtable reward in rewards)
					{
						if(reward != null) 
						{
							RedeemerItem redeemer = new RedeemerItem(reward);
							if(redeemer.IsValid)
							{
								redeemers.Add(redeemer);
							}
						}
					}

				}
				this.Rewards = redeemers.ToArray();
				this.IsNew = EB.Dot.Bool("first", data, false);
			}
			else
			{
				this.Id = string.Empty;
				this.Category = string.Empty;
				this.Description = string.Empty;
				this.Name = "";
				this.Data = new ArrayList();
				this.Current = 0;
				this.Target = 0;
				this.Rewards = new List<RedeemerItem>().ToArray();
				this.ObjectiveType = string.Empty;
				this.IsNew = false;
			}
		}

		public override string ToString()
		{
			return string.Format("Category:{0} Description:{1} Name:{2}", this.Category, this.Description, this.Name );
		}

		public int SecondsUntilNext { get{ return this.NextTime - EB.Time.Now; } }

		public string Id { get; private set; } 
		public string Category { get; private set; }
		public string Description { get; private set; }
		public string Name { get; private set; }
		public ArrayList Data { get; private set; }
		public int Current { get; private set; }
		public int Target { get; private set; }
		public RedeemerItem[] Rewards { get; private set; }
		public bool IsNew { get; set; }
		public int CompleteTime { get; private set; }
		public int NextTime { get; private set; }
		public string ObjectiveType { get; private set; }
		public bool Rewarded { get; private set; }

		public void IncrementProgress(int progress)
		{
			Current += progress;
			Current = Mathf.Min(Current, Target);
		}

		public bool IsComplete
		{ 
			get { return Current >= Target; } 
		}
		
		public bool IsValid
		{
			get
			{
				return this.Target > 0;
			}
		}
	}
	
	public class ObjectivesManager : SubSystem, Updatable
	{
		List<ObjectiveItem> objectives = new List<ObjectiveItem>();
		public event EB.Action<List<ObjectiveItem>> OnFetchedObjectivesComplete;

		public List<ObjectiveItem> Objectives{ get{ return objectives; } }

		ObjectivesAPI _api = null;
		public void Sync( Action<List<ObjectiveItem>> cb )
		{
			this._api.FetchObjectives( delegate( string error, Hashtable data ) {
				this.OnFetchedObjectives( error, data );
				cb( this.objectives );
			});
		}

		public void Report( List<ObjectiveReport> updates, Action<List<ObjectiveItem>> cb )
		{
			this._api.ReportObjectives(updates, delegate( string error, Hashtable data ) {
				this.OnFetchedObjectives( error, data );
				cb( this.objectives );
			});
		}

		public void ResetObjectives()
		{
			this._api.ResetObjectives(delegate( string error, Hashtable data ) {
				if(!string.IsNullOrEmpty(error))
				{
					EB.Debug.LogError("Could not reset objectives: {0}", error);
				}
				else
				{
					this.objectives.Clear();
				}
			});
		}
		
		private void OnFetchedObjectives( string error, Hashtable data )
		{
			this.objectives.Clear();
			if(string.IsNullOrEmpty(error) == false) 
			{
				EB.Debug.LogError("OnFetchedObjectives {0}",error);
			}
			if(data != null) 
			{
				foreach(Hashtable obj in data.Values) 
				{
					ObjectiveItem objectiveItem = new ObjectiveItem(obj);
					this.objectives.Add (objectiveItem);
				}
			}
			
			if( OnFetchedObjectivesComplete != null )
			{
				OnFetchedObjectivesComplete( this.objectives );
			}
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_api = new ObjectivesAPI(Hub.ApiEndPoint);
		}

		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var objectivesData = Dot.Object( "sobjs", Hub.LoginManager.LoginData, null );
			if( objectivesData != null )
			{
				this.OnFetchedObjectives( null, objectivesData );
				State = SubSystemState.Connected;
			}
			else
			{
				this.Sync( delegate( List<ObjectiveItem> objectives ) {
					State = SubSystemState.Connected;
				});
			}
		}

		public void Update ()
		{
		}

		public override void Disconnect (bool isLogout)
		{
		}
		#endregion
	
	}
}
