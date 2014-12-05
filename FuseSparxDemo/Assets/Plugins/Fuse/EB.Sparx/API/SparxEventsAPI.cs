using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class EventsEvent
	{
		public enum EventState
		{
			Running = 0,
			Finalizing,
			Pending,
			Ended,
			Unknown
		}
		
		
		
		public class ScoreReport : CompetitorRank
		{
			public class AwardedBox
			{
				public AwardedBox( Hashtable data = null )
				{
					this.Claimed = false;
					this.BoxID = string.Empty;
					this.Prizes = new List<string>();
					this.ClaimUrl = string.Empty;
					
					if( data != null )
					{
						this.Claimed = EB.Dot.Bool( "c", data, false );
						this.BoxID = EB.Dot.String( "b", data, string.Empty );
						ArrayList prizes = EB.Dot.Array( "p", data, null );
						if( prizes != null )
						{
							foreach( string prize in prizes )
							{
								this.Prizes.Add( prize );
							}
						}
						this.ClaimUrl = EB.Dot.String( "curl", data, string.Empty );
					}
				}
				
				public bool Claimed { get; private set; }
				public string BoxID { get; private set; }
				public List<string> Prizes { get; private set; }
				public string ClaimUrl { get; private set; }
			}
		
		
			public ScoreReport( Hashtable data = null )
				:
			base( data )
			{
				this.AwardedMilestones = new Dictionary<string, AwardedBox>();
				this.AwardedRanked = new Dictionary<string, AwardedBox>();
				
				if( data != null )
				{
					Hashtable awardedMilestones = EB.Dot.Object( "am", data, null );
					if( awardedMilestones != null )
					{
						foreach( string milestone in awardedMilestones.Keys )
						{
							AwardedBox award = new AwardedBox( awardedMilestones[ milestone ] as Hashtable );
							this.AwardedMilestones.Add( milestone, award );
						}
					}
					
					Hashtable awardedRanked = EB.Dot.Object( "ar", data, null );
					if( awardedRanked != null )
					{
						foreach( IDictionaryEnumerator pair in awardedRanked.Keys )
						{
							AwardedBox award = new AwardedBox( pair.Value as Hashtable );
							this.AwardedRanked.Add( pair.Key as string, award );
						}
					}
				}
			}
			
			public override string ToString()
			{
				string buffer = base.ToString();
				buffer += "AwardedMilestones:" + this.AwardedMilestones.Count + "\n";
				buffer += "AwardedRanked:" + this.AwardedRanked.Count + "\n";
				return buffer;
			}
			
			public bool Reported { get { return this.Joined > 0; } }
			public Dictionary< string, AwardedBox > AwardedMilestones { get; private set; }
			public Dictionary< string, AwardedBox > AwardedRanked { get; private set; }
		}
		
		public class Messaging
		{
			public Messaging( Hashtable data = null )
			{
				this.CDN = string.Empty;
				this.Name = string.Empty;
				this.ShortDescription = string.Empty;
				this.LongDescription = string.Empty;
				this.Banner = string.Empty;
				this.Data = data;
				
				if( data != null )
				{
					this.CDN = EB.Dot.String( "cdn", data, string.Empty );
					this.Name = EB.Dot.String( "name", data, string.Empty );
					this.ShortDescription = EB.Dot.String( "short", data, string.Empty );
					this.LongDescription = EB.Dot.String( "long", data, string.Empty );
					this.Banner = EB.Dot.String( "banner", data, string.Empty );
					if( string.IsNullOrEmpty( this.Banner ) == false )
					{
						this.Banner = this.CDN + "/" + this.Banner; 
					}
				}
			}
			
			public string GetMessageField( string field )
			{
				string value = string.Empty;
				if( this.Data != null )
				{
					value = EB.Dot.String( field, this.Data, string.Empty );
				}
				return value;
			}
			
			public string GetMessageFieldAsImage( string field )
			{
				string image = string.Empty;
				if( string.IsNullOrEmpty( this.CDN ) == false )
				{
					string value = EB.Dot.String( field, this.Data, string.Empty );
					if( string.IsNullOrEmpty( value ) == false )
					{
						image = this.CDN + "/" + value;
					}
				}
				return image;
			}
			
			public bool IsValid
			{ 
				get
				{
					return ( string.IsNullOrEmpty( this.Name ) == false ) && ( string.IsNullOrEmpty( this.LongDescription ) == false );
				}
			}
			
			public override string ToString()
			{
				return string.Format("Name:{0} Description:{1} Field Count:{2} Banner:{3}", this.Name, this.ShortDescription, this.Data.Keys.Count, this.Banner );
			}
			
			public string CDN { get; private set; }
			public string Name { get; private set; }
			public string ShortDescription { get; private set; }
			public string LongDescription { get; private set; }
			public string Banner { get; private set; }
			private Hashtable Data { get; set; }
		}
		
		public class MilestoneInfo
		{
			public class Milestone : EB.Dot.IDotListItem
			{
				public enum MilestoneStatus
				{
					NotAwarded,
					Awarded,
					Claimed
				}
				
				public Milestone( Hashtable data = null )
				{
					this.Id = string.Empty;
					this.Points = -1;
					this.Prizes = null;
					this.Status = MilestoneStatus.NotAwarded;
					this.ClaimUrl = string.Empty;
					
					if( data != null )
					{
						this.Id = EB.Dot.String( "pgrid", data, string.Empty );
						this.Points = EB.Dot.Integer( "points", data, -1 );
						this.FeaturedPrizes = EB.Dot.List<RedeemerItem>( "featured", data, null );
						this.Prizes = EB.Dot.List<RedeemerItem>( "prizes", data, null );
					}
					
					if( this.FeaturedPrizes == null )
					{
						this.FeaturedPrizes = new List<RedeemerItem>();
					}
					
					if( this.Prizes == null )
					{
						this.Prizes = new List<RedeemerItem>();
					}
				}
				
				public bool IsValid
				{
					get
					{
						return ( string.IsNullOrEmpty( this.Id ) == false ) && ( this.Points > 0 ) && ( this.Prizes.Count > 0 ); 
					}
				}
				
				public override string ToString()
				{
					string buffer = string.Empty;
					buffer += "Status:" + this.Status + " Points:" + this.Points + "-> Prizes-[";
					foreach( RedeemerItem p in this.Prizes )
					{
						buffer += p + ",";
					}
					buffer += "] Featured-[";
					foreach( RedeemerItem p in this.FeaturedPrizes )
					{
						buffer += p + ",";
					}
					buffer += "]";
					return buffer;
				}
				
				public void UpdateStatus( ScoreReport.AwardedBox award )
				{
					if( award.Claimed == true )
					{
						this.Status = MilestoneStatus.Claimed;
					}
					else
					{
						this.Status = MilestoneStatus.Awarded;
						this.ClaimUrl = award.ClaimUrl;
					}
				}
				
				public string Id { get; private set; }
				public int Points { get; private set; }
				public List<RedeemerItem> FeaturedPrizes { get; private set; }
				public List<RedeemerItem> Prizes { get; private set; }
				public MilestoneStatus Status { get; private set; }
				public string ClaimUrl { get; private set; }
			}
		
		
			public MilestoneInfo( Hashtable data = null )
			{
				this.Group = string.Empty;
				this.Milestones = null;
				if( data != null )
				{
					this.Group = EB.Dot.String( "group", data, string.Empty );
					this.Milestones = EB.Dot.List< Milestone >( "items", data, null );
				}
				if( this.Milestones == null )
				{
					this.Milestones = new List<Milestone>();
				}
			}
			
			public override string ToString()
			{
				string buffer = string.Empty;
				buffer += "Group:" + this.Group + " Count:" + this.Milestones.Count + "\n";
				foreach( Milestone m in this.Milestones )
				{
					buffer += "\t" + m;
				}
				return buffer;
			}
			
			public void UpdateStatus( Dictionary< string, ScoreReport.AwardedBox > awards )
			{
				foreach( Milestone milestone in this.Milestones )
				{
					ScoreReport.AwardedBox award = null;
					if( awards.TryGetValue( milestone.Id, out award ) == true )
					{
						milestone.UpdateStatus( award );
					}
				}
			}
			
			public string Group { get; private set; }
			public List<Milestone> Milestones { get; private set; }
		}
		
		public class RankedRangesInfo
		{
			public class RankedRange : EB.Dot.IDotListItem
			{
				public enum RankedRangeStatus
				{
					NotAwarded,
					Awarded,
					Claimed
				}
			
				public RankedRange( Hashtable data = null )
				{
					this.Id = string.Empty;
					this.Min = -1;
					this.Max = -1;
					this.Prizes = null;
					this.ClaimUrl = string.Empty;
					
					if( data != null )
					{
						this.Id = EB.Dot.String( "pgrid", data, string.Empty );
						this.Min = EB.Dot.Integer( "min", data, -1 );
						this.Max = EB.Dot.Integer( "max", data, -1 );
						this.FeaturedPrizes = EB.Dot.List<RedeemerItem>( "featured", data, null );
						this.Prizes = EB.Dot.List<RedeemerItem>( "prizes", data, null );
					}
					
					if( this.FeaturedPrizes == null )
					{
						this.FeaturedPrizes = new List<RedeemerItem>();
					}
					
					if( this.Prizes == null )
					{
						this.Prizes = new List<RedeemerItem>();
					}
				}
				
				public bool IsValid
				{
					get
					{
						return ( string.IsNullOrEmpty( this.Id ) == false ) && ( this.Min > 0 ) && ( this.Max > 0 ) && ( this.Max >= this.Min ) && ( this.Prizes.Count > 0 ); 
					}
				}
				
				public override string ToString()
				{
					string buffer = string.Empty;
					buffer += "Status:" + this.Status + " Min:" + this.Min + "Max:" + this.Max + "-> Prizes-[";
					foreach( RedeemerItem p in this.Prizes )
					{
						buffer += p + ",";
					}
					buffer += "] Featured-[";
					foreach( RedeemerItem p in this.FeaturedPrizes )
					{
						buffer += p + ",";
					}
					buffer += "]";
					return buffer;
				}
				
				public void UpdateStatus( ScoreReport.AwardedBox award )
				{
					if( award.Claimed == true )
					{
						this.Status = RankedRangeStatus.Claimed;
					}
					else
					{
						this.Status = RankedRangeStatus.Awarded;
						this.ClaimUrl = award.ClaimUrl;
					}
				}
				
				public string Id { get; private set; }
				public int Min { get; private set; }
				public int Max { get; private set; }
				public List<RedeemerItem> FeaturedPrizes { get; private set; }
				public List<RedeemerItem> Prizes { get; private set; }
				public RankedRangeStatus Status { get; private set; }
				public string ClaimUrl { get; private set; }
			}
			
			public RankedRangesInfo( Hashtable data = null )
			{
				this.Group = string.Empty;
				this.RankedRanges = null;
				if( data != null )
				{
					this.Group = EB.Dot.String( "group", data, string.Empty );
					this.RankedRanges = EB.Dot.List< RankedRange >( "items", data, null );
				}
				
				if( this.RankedRanges == null )
				{
					this.RankedRanges = new List<RankedRange>();
				}
			}
			
			public override string ToString()
			{
				string buffer = string.Empty;
				buffer += "Group:" + this.Group + " Count:" + this.RankedRanges.Count + "\n";
				foreach( RankedRange r in this.RankedRanges )
				{
					buffer += "\t" + r;
				}
				return buffer;
			}
			
			public void UpdateStatus( Dictionary< string, ScoreReport.AwardedBox > awards )
			{
				foreach( RankedRange rankedRange in this.RankedRanges )
				{
					ScoreReport.AwardedBox award = null;
					if( awards.TryGetValue( rankedRange.Id, out award ) == true )
					{
						rankedRange.UpdateStatus( award );
					}
				}
			}
			
			public string Group { get; private set; }
			public List<RankedRange> RankedRanges { get; private set; }
		}
		
		public class CompetitorRank : EB.Dot.IDotListItem
		{			
			public CompetitorRank( Hashtable data = null )
			{
				this.Source = data;
				this.UserID = EB.Sparx.Id.Null;
				this.Name = string.Empty;
				this.Score = 0;
				this.Rank = -1;
				this.BonusPool = 0;
				this.Joined = -1;
				this.LastUpdated = -1;
				
				if( data != null ) {
					this.UserID = new EB.Sparx.Id( EB.Dot.Find( "_id", data ) );
					this.Name = EB.Dot.String( "n", data, string.Empty );
					this.Score = EB.Dot.Integer( "s", data, 0 );
					this.Rank = EB.Dot.Integer( "r", data, -1 );
					this.BonusPool = EB.Dot.Integer( "xp", data, 0 );
					this.Joined = EB.Dot.Integer( "j", data, -1 );
					this.LastUpdated = EB.Dot.Integer( "u", data, -1 );
					this.FacebookID = EB.Dot.String( "fbid", data, string.Empty );
				}
			}
			
			public bool IsValid
			{
				get
				{
					return ( this.Source != null ) && ( this.UserID != EB.Sparx.Id.Null ) && ( string.IsNullOrEmpty( this.Name ) == false ) && ( this.Score >= 0 ) && ( this.Rank > 0 );
				}
			}
			
			public bool IsLocalPlayer
			{
				get
				{
					return Hub.Instance.LoginManager.LocalUserId == this.UserID;
				}
			}
			
			public override string ToString()
			{
				string buffer = string.Empty;
				buffer += "UserID:" + this.UserID + "\n";
				buffer += "Name:" + this.Name + "\n";
				buffer += "Score:" + this.Score + "\n";
				buffer += "Rank:" + this.Rank + "\n";
				buffer += "BonusPool:" + this.BonusPool + "\n";
				buffer += "Joined:" + this.Joined + "\n";
				buffer += "LastUpdated:" + this.LastUpdated + "\n";
				buffer += "FacebookID:" + this.FacebookID + "\n";
				return buffer;
			}
			
			public EB.Sparx.Id UserID { get; private set; }
			public string Name { get; private set; }
			public int Score { get; private set; }
			public int Rank { get; set; }
			public int BonusPool { get; private set; }
			public int Joined { get; private set; }
			public int LastUpdated { get; private set; }
			public string FacebookID { get; private set; }
			
			public Hashtable Source { get; private set; }
		}
		
		public EventsEvent( Hashtable data = null )
		{
			this.Id = string.Empty;
			this.ServerName = string.Empty;
			this.Type = string.Empty;
			this.Priority = 0;
			this.ShowScores = false;
			this.State = EventState.Unknown;
			this.Start = -1;
			this.End = -1;
			this.Final = -1;
			this.TopContenders = null;
			this.LocalContenders = null;
			
			Hashtable messagingData = null;
			Hashtable milestoneData = null;
			Hashtable rankedRangesData = null;
			Hashtable reportData = null;
			if( data != null )
			{
				this.Id = EB.Dot.String( "_id", data, string.Empty );
				this.ServerName = EB.Dot.String( "name", data, string.Empty );
				this.Type = EB.Dot.String( "eventtype", data, string.Empty );
				this.Priority = EB.Dot.Integer( "priority", data, 0 );
				this.ShowScores = EB.Dot.Bool( "showscores", data, false );
				this.State = EB.Dot.Enum< EventState >( "state", data, EventState.Unknown );
				this.Start = EB.Dot.Integer( "start", data, -1 );
				this.End = EB.Dot.Integer( "end", data, -1 );
				this.Final = EB.Dot.Integer( "final", data, -1 );
				messagingData = EB.Dot.Object( "messaging", data, null );
				milestoneData = EB.Dot.Object( "milestones", data, null );
				rankedRangesData = EB.Dot.Object( "rankedprizes", data, null );
				this.TopContenders = EB.Dot.List< CompetitorRank >( "top", data, null );
				this.LocalContenders = EB.Dot.List< CompetitorRank >( "local", data, null );
				reportData = EB.Dot.Object( "report", data, null );
			}
			
			this.Description = new Messaging( messagingData );
			this.Milestones = new MilestoneInfo( milestoneData );
			this.RankedRanges = new RankedRangesInfo( rankedRangesData );
			if( this.TopContenders == null )
			{
				this.TopContenders = new List<CompetitorRank>();
			}
			if( this.LocalContenders == null )
			{
				this.LocalContenders = new List<CompetitorRank>();
			}
			
			this.UpdateReport( reportData );
		}
		
		public void UpdateReport( Hashtable data )
		{
			int rank = ( this.Report != null ) ? this.Report.Rank : -1;
			this.Report = new ScoreReport( data );
			if( rank != -1 )
			{
				this.Report.Rank = rank;
			}
			this.Milestones.UpdateStatus( this.Report.AwardedMilestones );
			this.RankedRanges.UpdateStatus( this.Report.AwardedRanked );
		}
		
		public bool IsValid
		{ 
			get
			{
				return ( string.IsNullOrEmpty( this.Id ) == false ) && ( this.Start != -1 ) && ( this.End != -1 ) && ( this.Final != -1 ) && ( this.State != EventState.Unknown ) && ( this.Description.IsValid == true );
			}
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			buffer += "Id:" + this.Id + "\n";
			buffer += "ServerName:" + this.ServerName + "\n";
			buffer += "Type:" + this.Type + "\n";
			buffer += "Priority:" + this.Priority + "\n";
			buffer += "ShowScores:" + this.ShowScores + "\n";
			buffer += "State:" + this.State + "\n";
			buffer += "Start:" + this.Start + "\n";
			buffer += "End:" + this.End + "\n";
			buffer += "Final:" + this.Final + "\n";
			buffer += "Messaging:" + this.Description + "\n";
			buffer += "Milestones:" + this.Milestones + "\n";
			buffer += "RankedRanges:" + this.RankedRanges + "\n";
			buffer += "TopContenders:" + this.TopContenders.Count + "\n";
			buffer += "LocalContenders:" + this.LocalContenders.Count + "\n";
			buffer += "Report:\n" + this.Report + "\n";
			return buffer;
		}
		
		public string Id { get; private set; }
		public string ServerName { get; private set; }
		public string Type { get; private set; }
		public int Priority { get; private set; }
		public bool ShowScores { get; private set; }
		public EventState State { get; private set; }
		public int Start { get; private set; }
		public int End { get; private set; }
		public int Final { get; private set; }
		public Messaging Description { get; private set; }
		public MilestoneInfo Milestones { get; private set; }
		public RankedRangesInfo RankedRanges { get; private set; }
		public List<CompetitorRank> TopContenders { get; private set; }
		public List<CompetitorRank> LocalContenders { get; private set; }
		public ScoreReport Report { get; private set; }
	}

	public class EventsAPI
	{
		private readonly int EventsAPIVersion = 1;
	
		EventsConfig Config = null;
		EndPoint EndPoint = null;
		
		
		public EventsAPI( EventsConfig config, EndPoint endpoint )		
		{
			this.Config = config;
			this.EndPoint = endpoint;
		}
		
		public void Refresh( string checkHash, EB.Action< string, List<EventsEvent>, string > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Get("/events/refresh");
			request.AddData( "api", EventsAPIVersion );
			request.AddData( "hash", checkHash );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnEventsData( response.hashtable, delegate( string err, List<EventsEvent> events, string updatedCheck ) {
						cb( err, events, updatedCheck );
					});
				}
				else
				{
					List<EventsEvent> empty = new List<EventsEvent>();
					cb( response.error.ToString(), empty, checkHash );
				}
			});
		}
		
		public void ClaimAward( string awardUrl, EB.Action< string, bool > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Post( awardUrl );
			request.AddData( "api", EventsAPIVersion );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					cb( null, true );
				}
				else
				{
					cb( response.error.ToString(), false );
				}
			});
		}
		
		public void OnEventsData( Hashtable data, EB.Action< string, List<EventsEvent>, string > cb )
		{
			string err = null;
			List<EventsEvent> events = new List<EventsEvent>();
			string checkHash = string.Empty;
			
			string updatedCheck = EB.Dot.String( "check", data, null );
			if( ( updatedCheck != null ) && ( updatedCheck != checkHash ) )
			{
				ArrayList eventsData = EB.Dot.Array( "events", data, new ArrayList() );
				foreach( Hashtable eventData in eventsData )
				{
					string eventtype = EB.Dot.String( "eventtype", eventData, string.Empty );
					if( string.IsNullOrEmpty( eventtype ) == false )
					{
						System.Type type = typeof( EventsEvent );
						this.Config.EventTypeMapping.TryGetValue( eventtype, out type );
						if( type != null )
						{
							if( type.IsSubclassOf( typeof( EventsEvent ) ) == true )
							{
								EventsEvent evt = (EventsEvent)System.Activator.CreateInstance( type, eventData );
								
								if( evt.IsValid == true )
								{
									events.Add( evt );
									
									if( ( string.IsNullOrEmpty( evt.Description.CDN ) == false ) && ( string.IsNullOrEmpty( evt.Description.Banner ) == false ) )
									{
										EB.Cache.Precache( evt.Description.Banner );
									}
								}
							}
							else
							{
								EB.Debug.LogError( "EventsManager: {0} is using {1}, but it must be subclassed from EventsEvent", eventData, type );
							}
						}
						else
						{
							EB.Debug.LogError( "EventsManager: Unknown event type '{0}' for {1}", eventtype, eventData );
						}
					}
				}
			}
			
			if( cb != null )
			{
				cb( err, events, updatedCheck );
			}
		}
	}
}
