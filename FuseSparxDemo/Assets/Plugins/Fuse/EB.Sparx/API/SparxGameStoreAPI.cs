using System.Collections;
using System.Collections.Generic;


namespace EB.Sparx
{
	public class GameStoreItem
	{
		public GameStoreItem(Hashtable data = null)
		{
			if (data != null)
			{
				Enabled = EB.Dot.Bool("enabled", data, true);
				VersionID = EB.Dot.String("version_id", data, string.Empty);
				Name = EB.Dot.String("name", data, string.Empty);
				Category = EB.Dot.String("category", data, string.Empty);
				Title = EB.Dot.String("title", data, string.Empty);
				Description = EB.Dot.String("desc", data, string.Empty);
				Image = EB.Dot.String("image", data, string.Empty);
				ImageS3 = EB.Dot.Bool("images3", data, false);
				Redeemers = EB.Dot.List<RedeemerItem>("redeemers", data, new List<RedeemerItem>());
				Pricings = EB.Dot.List<GameStorePricing>("pricings", data, new List<GameStorePricing>());
			}
			else
			{
				Enabled = false;
				VersionID = string.Empty;
				Name = string.Empty;
				Category = string.Empty;
				Title = string.Empty;
				Description = string.Empty;
				Image = string.Empty;
				ImageS3 = false;
				Redeemers = null;
				Pricings = null;
			}
		}
		
		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(this.VersionID) && !string.IsNullOrEmpty(this.Name);
			}
		}

		public bool Enabled { get; private set; }						// Is this item enabled?
		public string VersionID { get; private set; }					// Version hash ID
		public string Name { get; private set; }						// Internal name (ID)
		public string Category { get; private set; }					// Category
		public string Title { get; private set; }						// Localized name
		public string Description { get; private set; }					// Localized description
		public string Image { get; private set; }						// URL or image ID
		public bool ImageS3 { get; private set; }						// Is the image in S3?
		public List<RedeemerItem> Redeemers { get; private set; }		// Content / Effect / Use
		public List<GameStorePricing> Pricings { get; private set; }	// Pricings (buying / selling options) 
	}
	
	
	public class GameStorePricing : EB.Dot.IDotListItem
	{
		public GameStorePricing(Hashtable data = null)
		{
			if (data != null)
			{
				this.VersionID = EB.Dot.String("version_id", data, string.Empty);
				this.SetID = EB.Dot.String("set_id", data, string.Empty);
				this.PricingID = EB.Dot.String("pricing_id", data, string.Empty);
				this.Bonus = EB.Dot.List<RedeemerItem>("bonus", data, null);
				this.Buy = EB.Dot.List<RedeemerItem>("buy", data, null);
				this.Sell = EB.Dot.List<RedeemerItem>("sell", data, null);
				this.Sale = EB.Dot.List<RedeemerItem>("sale", data, null);
			}
			else
			{
				this.VersionID = string.Empty;
				this.SetID = string.Empty;
				this.PricingID = string.Empty;
				this.Bonus = null;
				this.Buy = null;
				this.Sell = null;
				this.Sale = null;
			}
		}
		
		
		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(this.SetID) && !string.IsNullOrEmpty(this.PricingID);
			}
		}


		public Hashtable toHashTable()
		{
			Hashtable data = new Hashtable();
			data.Add( "version_id", this.VersionID );
			data.Add( "set_id", this.SetID );
			data.Add( "pricing_id", this.PricingID );
			return data;			
		}


		public string VersionID;								// Version hash ID
		public string SetID { get; private set; }				// Set ID
		public string PricingID { get; private set; }			// Pricing ID
		public List<RedeemerItem> Bonus { get; private set; }	// Bonus - will be redeemed when buying
		public List<RedeemerItem> Buy { get; private set; }		// Cost to buy the item
		public List<RedeemerItem> Sell { get; private set; }	// Sell price when selling the item
		public List<RedeemerItem> Sale { get; private set; }	// Sale cost to buy the item (empty if no sale)
	}
	
	
	public class GameStoreAPI
	{
		public static readonly int GameStoreAPIVersion = 2;
		
		private EndPoint _endpoint;
		
		
		public GameStoreAPI(EndPoint endpoint)
		{
			this._endpoint = endpoint;
		}

		public void BuyItem(GameStorePricing pricing, EB.Action<string, Hashtable> cb)
		{
			EB.Sparx.Request request = this._endpoint.Post("/gamestore/buy");
			request.AddData("api", GameStoreAPIVersion);
			request.AddData("version_id", pricing.VersionID);
			request.AddData("set_id", pricing.SetID);
			request.AddData("pricing_id", pricing.PricingID);
			request.AddData("nonce", EB.Sparx.Nonce.Generate());

			this._endpoint.Service(request, delegate(EB.Sparx.Response result) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf")
				{
					cb("nsf", null);
				}
				else
				{
					cb(result.localizedError, null);
				}
			});
		}
		
		
		public void SellItem(GameStorePricing pricing, EB.Action<string, Hashtable> cb)
		{
			EB.Sparx.Request request = this._endpoint.Post("/gamestore/sell");
			request.AddData("api", GameStoreAPIVersion);
			request.AddData("version_id", pricing.VersionID);
			request.AddData("set_id", pricing.SetID);
			request.AddData("pricing_id", pricing.PricingID);
			request.AddData("nonce", EB.Sparx.Nonce.Generate());

			this._endpoint.Service(request, delegate(EB.Sparx.Response result) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf")
				{
					cb("nsf", null);
				}
				else
				{
					cb(result.localizedError, null);
				}
			});
		}
		
		public void SellItems(GameStorePricing[] pricings, EB.Action<string, Hashtable> cb)
		{
			EB.Sparx.Request request = this._endpoint.Post("/gamestore/sell");
			request.AddData("api", GameStoreAPIVersion);
			request.AddData("nonce", EB.Sparx.Nonce.Generate());

			ArrayList hashList = new ArrayList();
			foreach(GameStorePricing pricing in pricings)
			{
				hashList.Add(pricing.toHashTable());
			}			
		
			request.AddData ("items", hashList);
			
			this._endpoint.Service(request, delegate(EB.Sparx.Response result) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf")
				{
					cb("nsf", null);
				}
				else
				{
					cb(result.localizedError, null);
				}
			});
		}
		
		public void UseItem(GameStoreItem item, Hashtable context, EB.Action<string, Hashtable> cb)
		{
			EB.Sparx.Request request = this._endpoint.Post("/gamestore/use");
			request.AddData("api", GameStoreAPIVersion);
			request.AddData("version_id", item.VersionID);
			request.AddData("item_name", item.Name);
			request.AddData("nonce", EB.Sparx.Nonce.Generate());

			if (context != null)
			{
				request.AddData("context", context);
			}
			
			this._endpoint.Service(request, delegate(EB.Sparx.Response result) {
				if (result.sucessful)
				{
					cb(null, result.hashtable);
				}
				else if (result.error != null && result.error.ToString() == "nsf")
				{
					cb("nsf", null);
				}
				else
				{
					cb(result.localizedError, null);
				}
			});
		}
	}
}
