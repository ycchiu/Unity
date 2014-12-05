using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class PrizeBox : EB.Dot.IDotListItem
	{
		public PrizeBox( Hashtable data = null )
		{
			this.Id = string.Empty;
			this.Items = null;
			this.Received = -1;
			this.Expiry = -1;
			this.Claimed = -1;
			this.From = string.Empty;
			this.Reason = string.Empty;
			
			if( data != null )
			{
				this.Id = EB.Dot.String( "_id", data, string.Empty );
				this.Items = EB.Dot.List< RedeemerItem >( "items", data, null );
				this.Received = EB.Dot.Integer( "ts", data, -1 );
				this.Expiry = EB.Dot.Integer( "expiry", data, -1 );
				this.Claimed = EB.Dot.Integer( "claimed", data, -1 );
				this.From = EB.Dot.String( "from", data, string.Empty );
				this.Reason = EB.Dot.String( "reason", data, string.Empty );
			}
			
			if( this.Items == null )
			{
				this.Items = new List<RedeemerItem>();
			}
		}
		
		public bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Id ) == false ) && ( this.Items.Count > 0 );
			}
		}
		
		public override string ToString()
		{
			List<string> items = new List<string>();
			foreach( RedeemerItem item in this.Items )
			{
				items.Add( item.ToString() );
			}
			
			return string.Format("Id:{0} Received:{1} Expiry:{2} Items:{3}", this.Id, this.Received, this.Expiry, string.Join( "\n", items.ToArray() ) );
		}
		
		public string Id { get; private set; }
		public List<RedeemerItem> Items { get; private set; }
		public int Received { get; private set; }
		public int Expiry { get; private set; }
		public int Claimed { get; private set; }
		public string From { get; private set; }
		public string Reason { get; private set; }
	}


	public class PrizesAPI
	{
		private readonly int PrizesAPIVersion = 1;
	
		EndPoint EndPoint;
		
		public PrizesAPI( EndPoint endpoint )		
		{
			this.EndPoint = endpoint;
		}
		
		public void Refresh( string checkHash, EB.Action< string, List<PrizeBox>, string > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Get("/prizes/refresh");
			request.AddData( "api", PrizesAPIVersion );
			request.AddData( "hash", checkHash );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnPrizesData( response.hashtable, delegate( string err, List<PrizeBox> prizes, string updatedCheck ) {
						cb( err, prizes, updatedCheck );
					});
				}
				else
				{
					List<PrizeBox> empty = new List<PrizeBox>();
					cb( response.error.ToString(), empty, checkHash );
				}
			});
		}
		
		public void Claim( PrizeBox box, EB.Action< string, List<PrizeBox> > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Post("/prizes/claim");
			request.AddData( "api", PrizesAPIVersion );
			request.AddData( "id", box.Id );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnPrizesClaimed( response.hashtable, delegate( string err, List<PrizeBox> claimed ) {
						cb( err, claimed );
					});
				}
				else
				{
					cb( response.error.ToString(), new List<PrizeBox>() );
				}
			});
		}

		public void ClaimBox( string bid, EB.Action< string, List<PrizeBox> > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Post("/prizes/claimbox/"+bid);
			request.AddData( "api", PrizesAPIVersion );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnPrizesClaimed( response.hashtable, delegate( string err, List<PrizeBox> claimed ) {
						cb( err, claimed );
					});
				}
				else
				{
					cb( response.error.ToString(), new List<PrizeBox>() );
				}
			});
		}
		
		public void ClaimAll( EB.Action< string, List<PrizeBox> > cb )
		{
			EB.Sparx.Request request = this.EndPoint.Post("/prizes/claimall");
			request.AddData( "api", PrizesAPIVersion );
			this.EndPoint.Service( request, delegate(EB.Sparx.Response response) {
				if( response.sucessful == true )
				{
					this.OnPrizesClaimed( response.hashtable, delegate( string err, List<PrizeBox> claimed ) {
						cb( err, claimed );
					});
				}
				else
				{
					cb( response.error.ToString(), new List<PrizeBox>() );
				}
			});
		}
		
		private void OnPrizesClaimed( Hashtable data, EB.Action< string, List<PrizeBox> > cb )
		{
			List<PrizeBox> claimed = new List<PrizeBox>();
			if( data != null )
			{
				List<PrizeBox> candidates = EB.Dot.List< PrizeBox >( "claimed", data, null );
				if( candidates != null )
				{
					foreach( PrizeBox box in candidates )
					{
						if( box.IsValid == true )
						{
							claimed.Add( box );
						}
					}
				}
			}
			cb( null, claimed );
		}
		
		public void OnPrizesData( Hashtable data, EB.Action< string, List<PrizeBox>, string > cb )
		{
			string err = null;
			List<PrizeBox> prizes = new List<PrizeBox>();
			string checkHash = string.Empty;
			
			string updatedCheck = EB.Dot.String( "check", data, null );
			if( ( updatedCheck != null ) && ( updatedCheck != checkHash ) )
			{
				ArrayList prizesData = EB.Dot.Array( "prizes", data, new ArrayList() );
				foreach( Hashtable prizeData in prizesData )
				{
					PrizeBox prize = new PrizeBox( prizeData );
					if( prize.IsValid == true )
					{
						prizes.Add( prize );
					}
				}
			}
			
			if( cb != null )
			{
				cb( err, prizes, updatedCheck );
			}
		}
	}
}
