using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class PrizesConfig
	{
	}
	
	public class PrizesManager : SubSystem, Updatable
	{
		PrizesConfig Config = new PrizesConfig();
		PrizesAPI Api = null;
		string CheckHash = string.Empty;
		public List<PrizeBox> Prizes{ get; private set; }
		
		public void Refresh( EB.Action<bool> cb )
		{
			this.Api.Refresh( this.CheckHash, delegate( string err, List<PrizeBox> prizes, string updatedCheckHash ) {
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( false );
				}
				else
				{
					if( this.CheckHash == updatedCheckHash )
					{
						cb( false );
					}
					else
					{
						this.Prizes = prizes;
						this.CheckHash = updatedCheckHash;
						cb( true );
					}
				}
			});
		}
		
		public void ClaimBox( string prizeBoxID, EB.Action< List<PrizeBox> > cb ) {
			this.Api.ClaimBox( prizeBoxID, delegate( string err, List<PrizeBox> claimed ) {
				this.OnPrizesClaimed( claimed );
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( claimed );
				}
				else
				{
					cb( claimed );
				}
			});
		}
		
		public void ClaimPrize( PrizeBox prize, EB.Action<bool> cb )
		{
			this.Api.Claim( prize, delegate( string err, List<PrizeBox> claimed ) {
				this.OnPrizesClaimed( claimed );
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( false );
				}
				else
				{
					cb( claimed.Count == 1 );
				}
			});
		}
		
		public void ClaimAll( EB.Action<int> cb )
		{
			this.Api.ClaimAll( delegate( string err, List<PrizeBox> claimed ) {
				this.OnPrizesClaimed( claimed );
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( -1 );
				}
				else
				{
					cb( claimed.Count );
				}
			});
		}
		
		private void OnPrizesClaimed( List<PrizeBox> claimed )
		{
			foreach( PrizeBox box in claimed )
			{
				this.Prizes.RemoveAll( delegate( PrizeBox candidate ) { return candidate.Id == box.Id; } );
			}
			
			foreach( PrizeBox box in claimed )
			{
				this.Prizes.Add( box );
			}
		}
		
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			this.Config = config.PrizesConfig;
			this.Prizes = new List<PrizeBox>();
			this.Api = new PrizesAPI( Hub.ApiEndPoint );
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var prizesData = Dot.Object( "prizes", Hub.LoginManager.LoginData, null );
			if( prizesData != null )
			{
				this.Api.OnPrizesData( prizesData, delegate( string err, List<PrizeBox> prizes, string updatedCheckHash ) {
					this.Prizes = prizes;
					this.CheckHash = updatedCheckHash;
					State = SubSystemState.Connected;
				});
			}
			else
			{
				this.Refresh( delegate( bool updated ) {
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
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
			case "refresh":
			{
				this.Refresh( delegate( bool updated ) {
				});
				break;
			}
			default:
			{
				break;
			}
			}
		}
	}
}
