using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class LevelUpNode
	{
		public LevelUpNode( Hashtable data )
		{
			this.NewLevel = EB.Dot.Integer( "newLevel", data, 0 );
			this.Category = EB.Dot.String( "category", data, string.Empty );
			this.Rewards = EB.Dot.List< RedeemerItem >( "prizes", data, null );
			
			if( this.Rewards == null )
			{
				this.Rewards = new List<RedeemerItem>();
			}
		}
		
		public List<RedeemerItem> Rewards { get; private set; }
		public int NewLevel { get; private set; }
		public string Category { get; private set; }
	}
	
	public class LevelUpComparer: IComparer<LevelUpNode>
	{
		public int Compare(LevelUpNode a, LevelUpNode b)
		{
			if (a.NewLevel > b.NewLevel)
			{
				return 1;
			}
			else if (a.NewLevel < b.NewLevel)
			{
				return -1;
			}
			else
			{
				return 0;
			}
		}
	}
	
	public class LevelRewardsType
	{
		public LevelRewardsType( string name, int levelNum, int nextThreshold, int prevThreshold )
		{
			this.Name = name;
			this.Level = levelNum;
			this.NextResourceThreshold = nextThreshold;
			this.PreviousResourceThreshold = prevThreshold;
			//EB.Debug.Log("LevelRewardsType name=" + name + " levelNum=" + levelNum + " nextThreshold=" + nextThreshold + " prevThreshold=" + prevThreshold);
		}
		
		public string Name { get; private set; }
		public int Level { get; private set; }
		public int NextResourceThreshold { get; private set; }
		public int PreviousResourceThreshold { get; private set; }
	}	

	public class LevelRewardsStatus
	{
		public LevelRewardsStatus( Hashtable data = null )
		{
			this.IsEnabled = false;
			LevelRewardsStatus.DefaultCategory = "xp";

			if( data != null )
			{
				this.Types = new List<LevelRewardsType>();
			
				Hashtable levelData = EB.Dot.Object( LevelRewardsStatus.DefaultCategory, data, null );
				//LevelRewardsManager.PrintHashTable("levelData", levelData);
				this.IsEnabled = true;
				this.Level = EB.Dot.Integer( "last_awarded_level", levelData, 0 );
				
				foreach( DictionaryEntry entry in data )
				{
					string name = entry.Key.ToString();
					levelData = EB.Dot.Object( name, data, null );
					int levelNum = EB.Dot.Integer("last_awarded_level", levelData, 0);
					int nextThreshold = EB.Dot.Integer("nextLevelXp", levelData, 0);
					int prevThreshold = EB.Dot.Integer("prevLevelXp", levelData, 0);
					
					this.Types.Add( new LevelRewardsType(name, levelNum, nextThreshold, prevThreshold) );
				}
			}
		}

		public int Level { get; private set; }
		public bool IsEnabled { get; private set; }
		static public string DefaultCategory { get; private set; }
		
		public List<LevelRewardsType> Types { get; private set; }
		
	}
	
	public class LevelMilestone
	{
		public LevelMilestone( Hashtable data = null )
		{	
			if( data != null )
			{
				this.tag = EB.Dot.String( "tag", data, string.Empty );
				this.category = EB.Dot.String( "category", data, string.Empty );
				this.level = EB.Dot.Integer( "level_num", data, 0 );

				Hashtable redeemerData = EB.Dot.Object("redeemer", data, null);
				if( redeemerData != null )
				{
					this.redeemer = new Sparx.RedeemerItem( redeemerData );
				}
			}
		}
		
		public string tag;
		public string category;
		public int level;
		public RedeemerItem redeemer;
	}
	
	public class LevelMilestoneStatus
	{
		public LevelMilestoneStatus( ArrayList data = null )
		{	
			this.milestones = new List<LevelMilestone>();
		
			if (data != null)
			{
				for (int i=0; i<data.Count; i++)
				{
					this.milestones.Add( new LevelMilestone( (Hashtable)data[i]) );
				}
			}
		}
		
		public List<LevelMilestone> milestones { get; private set; }
	}

	public class LevelRewardsManager : SubSystem, Updatable
	{
		LevelRewardsAPI _api = null;
		LevelRewardsStatus _status = new LevelRewardsStatus();
		LevelMilestoneStatus _milestones = new LevelMilestoneStatus();

		EB.SafeInt				_level;
		EB.SafeInt				_xpAmount;

		public delegate void LevelRewardsChangeDel(LevelRewardsStatus status);
		public LevelRewardsChangeDel OnLevelChange;
		
		public LevelRewardsStatus Status { get { return _status; }	}

		public int Level { get { return _level; } }
		public List<LevelUpNode> LevelUpQueue { get; private set; }
		
		void OnFetch( string err, Hashtable data )
		{
			if( string.IsNullOrEmpty( err ) == true )
			{
				Hashtable levelData = EB.Dot.Object( "levelrewards", data, null );
				if (levelData != null)
				{	
					OnLevelData( levelData );
				}
				
				ArrayList levelMilestoneData = EB.Dot.Array( "levelrewards_milestones", data, new ArrayList() );
				OnLevelMilestoneData( levelMilestoneData );
				
				/*for (int i=0; i<10; i++)
				{
					int level = GetMilestoneRedeemerLevel("ts", i);
					if (level >= 0)
					{
						EB.Debug.Log("++++++++++++++++++++++++TeamSize [" + i + "] unlocks at level[" + level + "]");
					}
				}
				

				int levelThor = GetMilestoneRedeemerLevel("bp", "thor_cm");
				if (levelThor >= 0)
				{
					EB.Debug.Log("++++++++++++++++++++++++Thor Common unlocks at level[" + levelThor + "]");
				}*/
			}
			else
			{
				EB.Debug.LogError( "Error Fetching Level Rewards: {0}", err );
			}
		}
		
		void OnLevelData( Hashtable data )
		{
			//PrintHashTable("levelrewards", data);
			this._status = new LevelRewardsStatus( data );
			
			_level = this._status.Level;
		}
		
		void OnLevelMilestoneData( ArrayList data )
		{
			this._milestones = new LevelMilestoneStatus( data );
		}
		
		void OnLevelUp( Hashtable data )
		{
			//PrintHashTable("OnLevelUp", data);
			QueueLevelUp( data );
			
			Hashtable levelData = EB.Dot.Object( "levelrewards", data, null );
			if (levelData != null)
			{	
				OnLevelData( levelData );
			}
			
			if (OnLevelChange != null) { OnLevelChange(_status); }
		}
		
		void QueueLevelUp( Hashtable data )
		{
			LevelUpNode node = new LevelUpNode( data );
			
			LevelUpQueue.Add(node);
			
			LevelUpComparer comp = new LevelUpComparer();
			LevelUpQueue.Sort(comp);
		}
		
		public LevelUpNode GetNextLevelUp(string typeName)
		{
			LevelUpNode levelUp = null;
			
			foreach(LevelUpNode node in LevelUpQueue)
			{
				if (node.Category == typeName)
				{
					levelUp = node;
					LevelUpQueue.Remove(node);
					break;
				}
			}
			
			return levelUp;
		}
		
		public int GetLevel(string typeName)
		{
			if (this._status.Types != null)
			{
				foreach (LevelRewardsType entry in this._status.Types) 
				{
					if (entry.Name == typeName)
					{
						return entry.Level;
					}
				}
			}
			return 0;
		}
		
		public int GetMilestoneRedeemerLevel(string redeemerType, int quanity, string category = null)
		{
			if (category == null)
			{
				category = LevelRewardsStatus.DefaultCategory;
			}
		
			if (this._milestones.milestones != null)
			{
				for (int i=0; i<this._milestones.milestones.Count; i++)
				{
					LevelMilestone m = this._milestones.milestones[i];
					if (m.redeemer != null)
					{
						if ((m.category == category) && (m.redeemer.Type == redeemerType) && (m.redeemer.Quantity == quanity))
						{
							return m.level;
						}
					}
				}
			}
			return -1;
		}
		
		public int GetMilestoneRedeemerLevel(string redeemerType, string dataType, int quanity, string category = null)
		{
			if (category == null)
			{
				category = LevelRewardsStatus.DefaultCategory;
			}
		
			if (this._milestones.milestones != null)
			{
				for (int i=0; i<this._milestones.milestones.Count; i++)
				{
					LevelMilestone m = this._milestones.milestones[i];
					if (m.redeemer != null)
					{
						if ((m.category == category) && (m.redeemer.Type == redeemerType) && (m.redeemer.Data == dataType) && (m.redeemer.Quantity == quanity))
						{
							return m.level;
						}
					}
				}
			}
			return -1;
		}
		
		public int GetMilestoneRedeemerLevel(string redeemerType, string dataType, string category = null)
		{
			if (category == null)
			{
				category = LevelRewardsStatus.DefaultCategory;
			}
		
			if (this._milestones.milestones != null)
			{
				for (int i=0; i<this._milestones.milestones.Count; i++)
				{
					LevelMilestone m = this._milestones.milestones[i];
					if (m.redeemer != null)
					{
						if ((m.category == category) && (m.redeemer.Type == redeemerType) && (m.redeemer.Data == dataType))
						{
							return m.level;
						}
					}
				}
			}
			return -1;
		}

		public static void PrintHashTable(string title, Hashtable data)
		{
			EB.Debug.Log("******[LevelRewardsManager] " + title + " data.Count=" + data.Count);
			foreach( DictionaryEntry entry in data)
			{
				EB.Debug.Log("      Entry: " + entry.Key.ToString() + " : " + entry.Value.ToString());
			}
		}

		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_api = new LevelRewardsAPI(Hub.ApiEndPoint);
			
			LevelUpQueue = new List<LevelUpNode>();
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var levelRewardsData = Dot.Object( "levelrewards", Hub.LoginManager.LoginData, null );
			if( levelRewardsData != null )
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
				case "level-up":
				{
					Hashtable data = payload as Hashtable;
					if( data == null )
					{
						data = JSON.Parse( payload as string ) as Hashtable;
					}
					
					if( data != null )
					{
						OnLevelUp( data );
					}
					break;
				}
				case "sync":
				{
					this._api.FetchStatus( OnFetch );
					break;
				}
				default:{
					break;
				}
			}
		}
		#endregion
	}
}

