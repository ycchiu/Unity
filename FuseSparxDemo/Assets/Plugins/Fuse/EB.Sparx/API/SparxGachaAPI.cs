using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class GachaTokenCount : EB.Dot.IDotListItem
	{
		public GachaTokenCount( Hashtable data = null )
		{
			this.Name = string.Empty;
			this.Count = 0;

			if( data != null )
			{
				this.Name = EB.Dot.String( "token", data, string.Empty );
				this.Count = EB.Dot.Integer( "count", data, 0 );
			}
		}
		
		public bool IsValid
		{ 
			get
			{
				return ( string.IsNullOrEmpty( this.Name ) == false );
			}
		}
		
		public override string ToString()
		{
			return string.Format("Name:{0} Count:{1}", this.Name, this.Count );
		}
		
		public string Name { get; private set; }
		public int Count { get; private set; }
	}
	
	public class GachaFreeTimeChange : EB.Dot.IDotListItem
	{
		public GachaFreeTimeChange( Hashtable data = null )
		{
			this.Group = string.Empty;
			this.Box = string.Empty;
			this.FreeTime = -1;
			
			if( data != null )
			{
				this.Group = EB.Dot.String( "group", data, string.Empty );
				this.Box = EB.Dot.String( "box", data, string.Empty );
				this.FreeTime = EB.Dot.Integer( "free", data, 0 );
			}
		}
		
		public bool IsValid
		{ 
			get
			{
				return ( string.IsNullOrEmpty( this.Group ) == false ) && ( string.IsNullOrEmpty( this.Box ) == false );
			}
		}
		
		public override string ToString()
		{
			return string.Format("Group:{0} Box:{1} FreeTime:{2}", this.Group, this.Box, this.FreeTime );
		}
		
		public string Group { get; private set; }
		public string Box { get; private set; }
		public int FreeTime { get; private set; }
	}
	
	public class GachaPickResult
	{
		public GachaPickResult( Hashtable data = null )
		{
			this.SoftCurrentToPay = 0;
			this.XpToGive = 0;
			this.Spins = 1;
			this.Items = null;
			
			if( data != null )
			{
				this.SoftCurrentToPay = EB.Dot.Integer("softToPay", data, 0);
				this.XpToGive = EB.Dot.Integer("xpToGive", data, 0);
				this.Spins = EB.Dot.Integer( "spins", data, 1 );
				this.Items = EB.Dot.List<RedeemerItem>( "items", data, null );
			}
			
			if( this.Items == null )
			{
				this.Items = new List<RedeemerItem>();
			}
		}
		
		public override string ToString()
		{
			return string.Format("SoftCurrentToPay:{0} XpToGive:{1} Items #:{2}", this.SoftCurrentToPay, this.XpToGive, this.Items.Count );
		}
		
		public int SoftCurrentToPay { get; private set; }
		public int XpToGive { get; private set; }
		public int Spins { get; private set; }
		public List<RedeemerItem> Items { get; private set; }
	}
	
	public class GachaBoxSpendInfo
	{
		public GachaBoxSpendInfo( Hashtable data = null )
		{
			this.Cost = 0;
			this.Sale = 0;
			this.Xp = 0;
			
			if( data != null )
			{
				this.Cost = EB.Dot.Integer( "cost", data, 0 );
				this.Sale = EB.Dot.Integer( "sale", data, 0 );
				this.Xp = EB.Dot.Integer( "xp", data, 0 );
			}
		}
		
		public override string ToString()
		{
			return string.Format("OnSale:{0} Cost:{1} Sale:{2} Xp:{3}", this.IsOnSale, this.Cost, this.Sale, this.Xp );
		}
		
		public bool IsOnSale
		{
			get
			{
				return ( this.Sale > 0 ) && ( this.Sale < this.Cost ); 
			}
		}
		
		public int Cost { get; private set; }
		public int Sale { get; private set; }
		public int Xp { get; private set; }
	}
	
	public class GachaBox
	{
		public enum Free
		{
			NoFree,
			Claimable,
			Wait
		};
	
		public GachaBox( GachaSet set, int index, Hashtable data = null )
		{
			this.Set = ( set != null ) ? set : new GachaSet( "null" );
			this.Index = index;
			
			this.CDN = string.Empty;
			this.Name = string.Empty;
			this.Group = string.Empty;
			this.PickSet = string.Empty;
			this.Version = string.Empty;
			this.Description = string.Empty;
			this.Token = string.Empty;
			this.EndTime = -1;
			this.FreeTime = -1;
			this.Multiplier = 0;
			this.MultiplierText = string.Empty;
			this.SoftCurrency = null;
			this.HardCurrency = null;
			this.Tokens = null;
			this.PossiblePrizes = null;
			this.FeaturedPrizes = null;
			this.BannerText = string.Empty;
			this.BannerColor = Color.white;
			this.Tint = Color.white;
			this.DisplayName = string.Empty;
			this.BackgroundOnCDN = false;
			this.BackgroundImage = string.Empty;
			this.OpenOnCDN = false;
			this.OpenImage = string.Empty;
			this.ClosedOnCDN = false;
			this.ClosedImage = string.Empty;
			this.TokenImageOnCDN = false;
			this.TokenImage = string.Empty;
			
			if( data != null )
			{
				this.CDN = EB.Dot.String( "cdn", data, string.Empty );
				this.Name = EB.Dot.String( "name", data, string.Empty );
				this.Group = EB.Dot.String( "group", data, string.Empty );
				this.PickSet = EB.Dot.String( "set", data, string.Empty );
				this.Version = EB.Dot.String( "version", data, string.Empty );
				this.Description = EB.Dot.String( "desc", data, string.Empty );
				this.Token = EB.Dot.String( "token", data, string.Empty );
				this.EndTime = EB.Dot.Integer( "end", data, -1 );
				this.FreeTime = EB.Dot.Integer( "free", data, -1 );
				this.Multiplier = EB.Dot.Integer( "multiplier", data, 0 );
				this.MultiplierText = EB.Dot.String( "multtxt", data, string.Empty );
				this.SoftCurrency = new GachaBoxSpendInfo( EB.Dot.Object( "sc", data, null ) );
				this.HardCurrency = new GachaBoxSpendInfo( EB.Dot.Object( "hc", data, null ) );
				this.Tokens = new GachaBoxSpendInfo( EB.Dot.Object( "tokenc", data, null ) );
				this.PossiblePrizes = EB.Dot.List<RedeemerItem>( "possiblePrizes", data, null );
				this.FeaturedPrizes = EB.Dot.List<RedeemerItem>( "featured", data, null );
				this.BannerText = EB.Dot.String( "banner", data, string.Empty );
				this.BannerColor = EB.Dot.Colour( "bannercolor", data, Color.white );
				this.Tint = EB.Dot.Colour( "tint", data, Color.white );
				this.DisplayName = EB.Dot.String( "displayname", data, string.Empty );
				this.BackgroundOnCDN = EB.Dot.Bool( "bgs3", data, false );
				this.BackgroundImage = EB.Dot.String( "bg", data, string.Empty );
				if( this.BackgroundOnCDN == true )
				{
					this.BackgroundImage = this.CDN + "/" + this.BackgroundImage;
				}
				this.OpenOnCDN = EB.Dot.Bool( "opens3", data, false );
				this.OpenImage = EB.Dot.String( "openimg", data, string.Empty );
				if( this.OpenOnCDN == true )
				{
					this.OpenImage = this.CDN + "/" + this.OpenImage;
				}
				this.ClosedOnCDN = EB.Dot.Bool( "closeds3", data, false );
				this.ClosedImage = EB.Dot.String( "closedimg", data, string.Empty );
				if( this.ClosedOnCDN == true )
				{
					this.ClosedImage = this.CDN + "/" + this.ClosedImage;
				}
				
				this.TokenImageOnCDN = EB.Dot.Bool( "tokenimgs3", data, false );
				this.TokenImage = EB.Dot.String( "tokenimg", data, string.Empty );
				if( this.TokenImageOnCDN == true )
				{
					this.TokenImage = this.CDN + "/" + this.TokenImage;
				}
			}
			
			if( string.IsNullOrEmpty( this.PickSet ) == true )
			{
				this.PickSet = this.Set.Name;
			}
			
			if( string.IsNullOrEmpty( this.Version ) == true )
			{
				this.Version = this.Set.Version;
			}
			
			if( string.IsNullOrEmpty( this.Group ) == true )
			{
				this.Group = this.Set.Group;
			}
			
			if( this.SoftCurrency == null )
			{
				this.SoftCurrency = new GachaBoxSpendInfo();
			}
			
			if( this.HardCurrency == null )
			{
				this.HardCurrency = new GachaBoxSpendInfo();
			}
			
			if( this.Tokens == null )
			{
				this.Tokens = new GachaBoxSpendInfo();
			}
			
			if( this.PossiblePrizes == null )
			{
				this.PossiblePrizes = new List<RedeemerItem>();
			}
			
			if( this.FeaturedPrizes == null )
			{
				this.FeaturedPrizes = new List<RedeemerItem>();
			}
		}
		
		public bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Name ) == false ) && ( ( this.SoftCurrency.Cost != 0 ) || ( this.HardCurrency.Cost != 0 ) || ( this.Tokens.Cost != 0 ) );
			}
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			buffer += "\t\t\t\tName:" + this.Name + "\n";
			buffer += "\t\t\t\tIsValid:" + this.IsValid + "\n";
			buffer += "\t\t\t\tGroup:" + this.Group + "\n";
			buffer += "\t\t\t\tSet:" + this.PickSet + "\n";
			buffer += "\t\t\t\tVersion:" + this.Version + "\n";
			buffer += "\t\t\t\tIndex:" + this.Index + "\n";
			buffer += "\t\t\t\tDescription:" + this.Description + "\n";
			buffer += "\t\t\t\tToken:" + this.Token + "\n";
			buffer += "\t\t\t\tEndTime:" + this.EndTime + "\n";
			buffer += "\t\t\t\tFree:" + this.FreeState + "(" + this.FreeTime + ")\n";
			buffer += "\t\t\t\tMultiplier:" + this.Multiplier + "\n";
			buffer += "\t\t\t\tMultiplierText:" + this.MultiplierText + "\n";
			buffer += "\t\t\t\tSoftCurrency:" + this.SoftCurrency + "\n";
			buffer += "\t\t\t\tHardCurrency:" + this.HardCurrency + "\n";
			buffer += "\t\t\t\tTokens:" + this.Tokens + "\n";
			buffer += "\t\t\t\tPossiblePrizes:" + this.PossiblePrizes.Count + "\n";
			buffer += "\t\t\t\tFeaturedPrizes:" + this.FeaturedPrizes.Count + "\n";
			buffer += "\t\t\t\tBanner:" + this.Banner + "\n";
			buffer += "\t\t\t\tBannerText:" + this.BannerText + "\n";
			buffer += "\t\t\t\tBannerColor:" + this.BannerColor + "\n";
			buffer += "\t\t\t\tTint:" + this.Tint + "\n";
			buffer += "\t\t\t\tDisplayName:" + this.DisplayName + "\n";
			buffer += "\t\t\t\tBackgroundOnCDN:" + this.BackgroundOnCDN + "\n";
			buffer += "\t\t\t\tBackgroundImage:" + this.BackgroundImage + "\n";
			buffer += "\t\t\t\tOpenOnCDN:" + this.OpenOnCDN + "\n";
			buffer += "\t\t\t\tOpenImage:" + this.OpenImage + "\n";
			buffer += "\t\t\t\tClosedOnCDN:" + this.ClosedOnCDN + "\n";
			buffer += "\t\t\t\tClosedImage:" + this.ClosedImage + "\n";
			buffer += "\t\t\t\tTokenImageOnCDN:" + this.TokenImageOnCDN + "\n";
			buffer += "\t\t\t\tTokenImage:" + this.TokenImage + "\n";
			
			return buffer;
		}
		
		public GachaSet Set { get; private set; }
		public int Index { get; private set; }
		
		public string CDN { get; private set; }
		public string Name { get; private set; }
		public string Group { get; private set; }
		public string PickSet { get; private set; }
		public string Version { get; private set; }
		public string Description { get; private set; }
		public string Token { get; private set; }
		public int EndTime { get; private set; }
		public int FreeTime { get; set; }
		public Free FreeState
		{ 
			get
			{
				Free state = Free.NoFree;
				if( ( FreeTime == 0 ) || ( ( this.FreeTime > 0 ) && ( Time.Now > this.FreeTime ) ) )
				{
					state = Free.Claimable;
				}
				else if( FreeTime > 0 )
				{
					state = Free.Wait;
				}
				return state;
			}
		}
		public int Multiplier { get; private set; }
		public string MultiplierText { get; private set; }
		public GachaBoxSpendInfo SoftCurrency { get; private set; }
		public GachaBoxSpendInfo HardCurrency { get; private set; }
		public GachaBoxSpendInfo Tokens { get; private set; }
		public List<RedeemerItem> PossiblePrizes { get; private set; }
		public List<RedeemerItem> FeaturedPrizes { get; private set; }
		public bool Banner
		{
			get
			{
				return string.IsNullOrEmpty( this.BannerText ) == false;
			}
		}
		public string BannerText { get; private set; }
		public Color BannerColor { get; private set; }
		public Color Tint { get; private set; }
		public string DisplayName { get; private set; }
		public bool BackgroundOnCDN { get; private set; }
		public string BackgroundImage { get; private set; }
		public bool OpenOnCDN { get; private set; }
		public string OpenImage { get; private set; }
		public bool ClosedOnCDN { get; private set; }
		public string ClosedImage { get; private set; }
		public bool TokenImageOnCDN { get; private set; }
		public string TokenImage { get; private set; }
	}
	
	public class GachaSet : EB.Dot.IDotListItem
	{
		public GachaSet( string group, Hashtable data = null )
		{
			this.Version = string.Empty;
			this.Name = string.Empty;
			this.Group = group;
			this.AttractImages = new List<string>();
			this.PossiblePrizes = null;
			this.Boxes = new List<GachaBox>();
			
			if( data != null )
			{
				this.Version = EB.Dot.String( "version", data, string.Empty );
				this.Name = EB.Dot.String( "name", data, string.Empty );
				ArrayList attractsList = EB.Dot.Array( "attracts", data, null );
				if( attractsList != null )
				{
					foreach( object candidate in attractsList )
					{
						if( candidate is string )
						{
							this.AttractImages.Add( candidate as string );
						}
					}
					this.AttractImages.Reverse();
				}
				this.PossiblePrizes = EB.Dot.List<RedeemerItem>( "possiblePrizes", data, null );
				ArrayList boxesList = EB.Dot.Array( "boxes", data, null );
				if( boxesList != null )
				{
					for( int i = 0; i < boxesList.Count; ++i )
					{
						GachaBox box = new GachaBox( this, i, boxesList[ i ] as Hashtable );
						if( box.IsValid == true )
						{
							this.Boxes.Add( box );
						}
					}
				}
			}
			
			if( this.PossiblePrizes == null )
			{
				this.PossiblePrizes = new List<RedeemerItem>();
			}
		}
		
		public bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Name ) == false ) && ( string.IsNullOrEmpty( this.Version ) == false );
			}
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			
			buffer += "Name:" + this.Name + "\n";
			buffer += "\t\t\tIsValid:" + this.IsValid + "\n";
			buffer += "\t\t\tGroup:" + this.Group + "\n";
			buffer += "\t\t\tVersion:" + this.Version + "\n";
			buffer += "\t\t\tBoxes:" + this.Boxes.Count + "\n";
			foreach( GachaBox box in this.Boxes )
			{
				buffer += box.ToString();	
			}
			buffer += "\t\t\tAttracts:" + this.AttractImages.Count + "\n";
			buffer += "\t\t\tPossiblePrizes:" + this.PossiblePrizes.Count + "\n";
			
			return buffer;
		}
		
		public string Name { get; private set; }
		public string Group { get; private set; }
		public string Version { get; private set; }
		public List<string> AttractImages { get; private set; }
		public List<RedeemerItem> PossiblePrizes { get; private set; }
		public List<GachaBox> Boxes { get; private set; }
	}
	
	public class GachaGroup : EB.Dot.IDotListItem
	{
		public GachaGroup( Hashtable data = null )
		{
			this.Name = string.Empty;
			Hashtable setData = null;	
			if( data != null )
			{
				this.Name = EB.Dot.String( "group", data, string.Empty );
				setData = EB.Dot.Object( "set", data, null );
			}
			
			this.Set = new GachaSet( this.Name, setData );
		}
		
		public bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Name ) == false ) && ( this.Set.IsValid == true );
			}
		}
		
		public override string ToString()
		{
			return string.Format("Name:{0}\nSet:{1}", this.Name, this.Set );
		}
		
		public string Name { get; private set; }
		public GachaSet Set { get; private set; }
	}
	

	public class GachaAPI
	{
		public static readonly int GachaAPIVersion = 7;
		EndPoint _api;
		
		public GachaAPI( EndPoint api )		
		{
			_api = api;
		}
				
		public void PickFromBox( string group, string version, string set, string box, string payment, int spins, Action<string,Hashtable> callback )
		{
			EB.Sparx.Request request = this._api.Post("/gacha/pick");
			request.AddData("group", group );
			request.AddData("api", GachaAPIVersion );
			
			request.AddData("version", version );
			request.AddData("set", set );
			request.AddData("box", box);
			request.AddData("payment", payment);
			request.AddData("spins", spins );
			request.AddData("nonce", EB.Sparx.Nonce.Generate() );
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					callback(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "PickFromBox Error: {0}", result.localizedError );
					callback(result.localizedError, null);
				}
			});
		}
		
		public void ClaimFreeBox( string group, string version, string set, string box, Action<string,Hashtable> callback )
		{
			EB.Sparx.Request request = this._api.Post("/gacha/claimfree");
			request.AddData("group", group );
			request.AddData("api", GachaAPIVersion );
			request.AddData("version", version );
			request.AddData("set", set );
			request.AddData("box", box);
			request.AddData("nonce", EB.Sparx.Nonce.Generate() );
			this._api.Service( request, delegate( Response result ) {
				if (result.sucessful)
				{
					callback(null, result.hashtable);
				}
				else
				{
					EB.Debug.Log( "ClaimFreeBox Error: {0}", result.localizedError );
					callback(result.localizedError, null);
				}
			});
		}
		
		
		public void OnGachaData( Hashtable data, EB.Action< List<GachaGroup>, List<GachaTokenCount>, int > cb )
		{
			List<GachaGroup> groups = EB.Dot.List< GachaGroup >( "groups", data, null );
			List<GachaTokenCount> tokens = EB.Dot.List< GachaTokenCount >( "tokens", data, null );
			int maxspins = EB.Dot.Integer( "maxspins", data, 1 );
			
			cb( groups, tokens, maxspins );
		}
	}
}
