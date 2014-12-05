using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class LoginRewardsTile
	{
		public enum TileType
		{
			Empty,
			Prize,
			Mystery,
			Advance	
		};
	
		public LoginRewardsTile( int x, int y, Hashtable data = null )
		{
			this.X = x;
			this.Y = y;
			this.Type = TileType.Empty;
			this.Data = string.Empty;
			this.Prizes = new List<RedeemerItem>();
			
			if( data != null )
			{
				this.Type = EB.Dot.Enum< TileType >( "type", data, TileType.Empty );
				this.Data = EB.Dot.String( "data", data, string.Empty );
				ArrayList prizes = EB.Dot.Array( "prizes", data, null );
				if( prizes != null )
				{
					foreach( object candidate in prizes )
					{
						Hashtable prize = candidate as Hashtable;
						if( prize != null )
						{
							this.Prizes.Add( new RedeemerItem( prize ) );
						}
					}
				}
			}
		}
		
		public bool IsValid
		{
			get
			{
				return this.Type != TileType.Empty;
			}
		}
		
		public override string ToString()
		{
			return string.Format("({0},{1}) : {2} -> {3}", this.X, this.Y, this.Type, ( this.Prizes.Count > 0 ? string.Join( ",", this.Prizes.ConvertAll( p => p.ToString() ).ToArray() ) : this.Data ) );
		}
		
		
		public int X { get; private set; }
		public int Y { get; private set; }
		public TileType Type { get; private set; }
		public string Data { get; private set; }
		public List<RedeemerItem> Prizes { get; private set; }
	}
	
	public class LoginRewardsTileClaim
	{
		public LoginRewardsTileClaim( Hashtable data = null )
		{
			this.X = 0;
			this.Y = 0;
			this.Prizes = new List<RedeemerItem>();
			if( data != null )
			{
				this.X = EB.Dot.Integer( "x", data, 0 );
				this.Y = EB.Dot.Integer( "y", data, 0 );
				
				ArrayList prizes = EB.Dot.Array( "prizes", data, null );
				if( prizes != null )
				{
					foreach( object candidate in prizes )
					{
						Hashtable prize = candidate as Hashtable;
						if( prize != null )
						{
							this.Prizes.Add( new RedeemerItem( prize ) );
						}
					}
				}
			}
		}
		
		public bool IsValid( LoginRewardsBoard board )
		{
			return ( this.X >= 0 ) && ( this.X < board.MaxRows ) && ( this.Y >= 0 ) && ( this.Y < board.MaxColumns );
		}
		
		public override string ToString()
		{
			return string.Format("({0},{1})", this.X, this.Y );
		}
		
		public int X { get; private set; }
		public int Y { get; private set; }
		public List<RedeemerItem> Prizes { get; private set; }
	}
	
	public class LoginRewardsBoard
	{
		public LoginRewardsBoard( Hashtable data = null )
		{
			this.MaxRows = 0;
			this.MaxColumns = 0;
			this.Tiles = new List<LoginRewardsTile>();
			if( data != null )
			{
				this.MaxRows = EB.Dot.Integer( "rows", data, 0 );
				this.MaxColumns = EB.Dot.Integer( "columns", data, 0 );
				
				Hashtable items = EB.Dot.Object("items", data, null);
				if( items != null )
				{
					for( int x = 0; x < this.MaxRows; ++x )
					{
						Hashtable rowData = EB.Dot.Object( x.ToString(), items, null );
						if( rowData != null )
						{
							for( int y = 0; y < this.MaxColumns; ++y )
							{
								Hashtable tileData = EB.Dot.Object( y.ToString(), rowData, null );
								if( tileData != null )
								{
									LoginRewardsTile tile = new LoginRewardsTile( x, y, tileData );
									if( tile.IsValid == true )
									{
										this.Tiles.Add( tile );
									} 
								}
							}
						}
					}
				}				
			}
		}
		
		public int MaxRows { get; private set; }
		public int MaxColumns { get; private set; }
		public List< LoginRewardsTile > Tiles;
	}
	
	public class LoginRewardsState
	{
		public LoginRewardsState( Hashtable data = null )
		{
			this.Consecutive = 0;
			this.Nonconsecutive = 0;
			if( data != null )
			{
				this.Consecutive = EB.Dot.Integer( "consecutive", data, 0 );
				this.Nonconsecutive = EB.Dot.Integer( "nonconsecutive", data, 0 );
			}
		}
		
		public int Consecutive { get; private set; }
		public int Nonconsecutive { get; private set; }
	}
	
	public class LoginRewardsStatus
	{
		public LoginRewardsStatus( Hashtable data = null )
		{
			this.ChasePrizes = new List<RedeemerItem>();
			this.NewlyClaimed = new List<LoginRewardsTileClaim>();
			this.Claimed = new List<LoginRewardsTileClaim>();
			this.IsNew = false;
			this.IsEnabled = false;
			this.IsChasePrizeNewlyClaimed = false;
			this.IsChasePrizeClaimed = false;
			if( data != null )
			{
				this.IsEnabled = true;
				Hashtable login = EB.Dot.Object( "login", data, null );
				this.Current = new LoginRewardsState( login );
				this.Old = new LoginRewardsState( EB.Dot.Object( "old", data, null ) );
				this.Offset = new LoginRewardsState( EB.Dot.Object( "offset", data, null ) );
				this.Board = new LoginRewardsBoard( EB.Dot.Object( "source", data, null ) );
				this.IsNew = EB.Dot.Bool("isnew", data, false);
				this.IsChasePrizeNewlyClaimed = EB.Dot.Bool("chasenewlyclaimed", data, false);
				this.IsChasePrizeClaimed = EB.Dot.Bool("chaseclaimed", data, false);
				
				ArrayList newlyclaimed = EB.Dot.Array( "newlyclaimed", data, null );
				if( newlyclaimed != null )
				{
					foreach( object candidate in newlyclaimed )
					{
						Hashtable claimData = candidate as Hashtable;
						if( claimData != null )
						{
							LoginRewardsTileClaim claim = new LoginRewardsTileClaim( claimData );
							if( claim.IsValid( this.Board ) == true )
							{
								this.NewlyClaimed.Add( claim );
							}
						}
					}
				}
				
				ArrayList claimed = EB.Dot.Array( "claimed", data, null );
				if( claimed != null )
				{
					foreach( object candidate in claimed )
					{
						Hashtable claimData = candidate as Hashtable;
						if( claimData != null )
						{
							LoginRewardsTileClaim claim = new LoginRewardsTileClaim( claimData );
							if( claim.IsValid( this.Board ) == true )
							{
								this.Claimed.Add( claim );
							}
						}
					}
				}
				
				ArrayList chasePrizes = EB.Dot.Array( "chase", data, null );
				if( chasePrizes != null )
				{
					foreach( object candidate in chasePrizes )
					{
						Hashtable chase = candidate as Hashtable;
						if( chase != null )
						{
							this.ChasePrizes.Add( new RedeemerItem( chase ) );
						}
					}
				}
			}
			else
			{
				this.Current = new LoginRewardsState( null );
				this.Old = new LoginRewardsState( null );
				this.Offset = new LoginRewardsState( null );
				this.Board = new LoginRewardsBoard( null );
			}
		}

		public void SetChasePrizeClaimed()
		{
			IsChasePrizeNewlyClaimed = false;
		}
		
		public LoginRewardsState Current { get; private set; }
		public LoginRewardsState Old { get; private set; }
		public LoginRewardsState Offset { get; private set; }
		public List<RedeemerItem> ChasePrizes { get; private set; }
		public bool IsChasePrizeNewlyClaimed { get; private set; }
		public bool IsChasePrizeClaimed { get; private set; }
		public List<LoginRewardsTileClaim> NewlyClaimed { get; private set; }
		public List<LoginRewardsTileClaim> Claimed { get; private set; }
		public LoginRewardsBoard Board { get; private set; }
		public bool IsNew { get; private set; }
		public bool IsEnabled { get; private set; }
	}
	
	public class LoginRewardsManager : SubSystem, Updatable
	{
		LoginRewardsAPI _api = null;
		LoginRewardsStatus _status = new LoginRewardsStatus();
		
		public LoginRewardsStatus Status 
		{
			get
			{
				return this._status;
			}
		}
		
		public List<LoginRewardsTile> Tiles
		{
			get
			{
				List<LoginRewardsTile> tiles = null;
				
				if( ( this._status != null ) && ( this._status.Board != null ) )
				{
					tiles = this._status.Board.Tiles;
				}
				else
				{
					tiles = new List<LoginRewardsTile>();
				}
				
				return tiles;
			}
		}
		
		public List<LoginRewardsTile> GetTilesForRow( int row )
		{
			List<LoginRewardsTile> tiles = new List<LoginRewardsTile>();
			if( ( this._status != null ) && ( this._status.Board != null ) )
			{
				foreach( LoginRewardsTile candidate in this._status.Board.Tiles )
				{
					if( candidate.X == row )
					{
						tiles.Add( candidate );
					}
				}
			}
			tiles.Sort(	delegate( LoginRewardsTile lhs, LoginRewardsTile rhs ) { return	lhs.Y - rhs.Y; } );
			return tiles;
		}
		
		public bool IsTileClaimed( int x, int y, out bool isNewlyClaimed )
		{
			bool claimed = false;
			isNewlyClaimed = false;
			if( this._status != null )
			{
				foreach( LoginRewardsTileClaim claim in this._status.Claimed )
				{
					if( ( claim.X == x ) && ( claim.Y == y ) )
					{
						claimed = true;
						foreach( LoginRewardsTileClaim newlyClaimed in this._status.NewlyClaimed )
						{
							if( ( newlyClaimed.X == x ) && ( newlyClaimed.Y == y ) )
							{
								isNewlyClaimed = true;
								break;
							}
						}
						break;
					}
				}
			}
			return claimed;
		}
		
		public void ClaimPrizes()
		{
			if( this._status != null )
			{
				this._status.NewlyClaimed.Clear();
				this._status.SetChasePrizeClaimed();
			}
		}
		
		public void PickReward( int x, int y, Action<string, LoginRewardsTileClaim> onComplete )
		{
			this._api.ClaimReward( x, y, delegate( string error, Hashtable data ){
				if( data != null )
				{
					LoginRewardsTileClaim claim = new LoginRewardsTileClaim( EB.Dot.Object( "tile", data, null ) );
					onComplete( null, claim );
				}
				else
				{
					EB.Debug.LogError("Server responded with error to LoginRewards ClaimReward: '{0}'", error );
					onComplete( error, null );
				}
			} );
		}
		
		void OnFetch( string err, Hashtable data )
		{
			if( string.IsNullOrEmpty( err ) == true )
			{
				this._status = new LoginRewardsStatus( data );
			}
			else
			{
				EB.Debug.LogError( "Error Fetching Login Rewards: {0}", err );
			}
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_api = new LoginRewardsAPI(Hub.ApiEndPoint);
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var loginRewardsData = Dot.Object( "loginrewards", Hub.LoginManager.LoginData, null );
			if( loginRewardsData != null )
			{
				this.OnFetch( null, loginRewardsData );
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
				case "admin-update":
				{
					Hashtable data = payload as Hashtable;
					if( data == null )
					{
						data = JSON.Parse( payload as string ) as Hashtable;
					}
					
					if( data != null )
					{
						this.OnFetch( null, data );
					}
					break;
				}
				case "sync":
				{
					this._api.FetchLoginRewardsStatus( OnFetch );
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

