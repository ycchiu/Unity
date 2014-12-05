using System.Collections;
using System.Collections.Generic;


namespace EB.Sparx
{
	public class GameStoreConfig
	{
	}


	abstract public class IGameStoreItemFactory
	{
		public abstract GameStoreItem CreateInstance(Hashtable data);
	}


	// Default factory build GameStoreItem objects.
	// Register your own factories to build subclasses of GameStoreItem.
	public class DefaultGameStoreItemFactory : IGameStoreItemFactory
	{
		public override GameStoreItem CreateInstance(Hashtable data)
		{
			return new GameStoreItem(data);
		}
	}
	

	public class GameStoreManager : AutoRefreshingManager
	{
		private GameStoreConfig _config = new GameStoreConfig();
		private GameStoreAPI _api = null;

		// Caching mechanism for items
		private List<GameStoreItem> _items = new List<GameStoreItem>();

		// This maps item categories to registered factories
		private Dictionary<string, IGameStoreItemFactory> _itemFactories = new Dictionary<string, IGameStoreItemFactory>();

		// Default factory used if an item's category isn't found in _itemFactories
		private IGameStoreItemFactory _defaultFactory = new DefaultGameStoreItemFactory();
		
		public GameStoreManager()
			:
		base( "gamestore", GameStoreAPI.GameStoreAPIVersion )
		{
		}


		// Register a GameStoreItem factory. The idea is that you register per-category
		// factories that build GameStoreItem subclasses.
		void RegisterFactory(string itemCategory, IGameStoreItemFactory factory)
		{
			_itemFactories.Add(itemCategory, factory);
		}

		
		// Return all existing items from the game store.
		// This list will be empty until someone calls Refresh() first.
		public List<GameStoreItem> GetItems()
		{
			List<GameStoreItem> items = new List<GameStoreItem>();
			foreach (GameStoreItem item in this._items)
			{
				items.Add(item);
			}
			return items;
		}

		public GameStoreItem GetItem( string itemID )
		{
			foreach (GameStoreItem item in this._items)
			{
				if( item.Name == itemID )
				{
					return item;
				}
			}
			return null;
		}
		
		public void BuyItem(GameStorePricing pricing, EB.Action<string, List<RedeemerItem>> cb)
		{
			this._api.BuyItem(pricing, delegate(string err, Hashtable data) {
				cb(err, ParseResults(data));
			});
		}


		public void SellItem(GameStorePricing pricing, EB.Action<string, List<RedeemerItem>> cb)
		{
			this._api.SellItem(pricing, delegate(string err, Hashtable data) {
				cb(err, ParseResults(data));
			});
		}
		
		public void SellItems(GameStorePricing[] pricings, EB.Action<string, List<RedeemerItem>> cb)
		{
			this._api.SellItems (pricings,  delegate(string err, Hashtable data) {
				cb(err, ParseResults(data));
			});
		}
		
	
		// "context" is arbitrary data to be used by redeemers.
		public void UseItem(GameStoreItem item, Hashtable context, EB.Action<string, List<RedeemerItem>> cb)
		{
			this._api.UseItem(item, context, delegate(string err, Hashtable data) {
				cb(err, ParseResults(data));
			});
		}
		
		public override void OnData( Hashtable data, Action< bool > cb )
		{
			bool success = this.OnGameStoreData( data );
			cb( success );
		}


		// Process the received game store data
		private bool OnGameStoreData(Hashtable data)
		{
			// Check if we received new data
			string defaultCDN = EB.Dot.String("cdn", data, null);

			// Parse received data
			ArrayList itemsData = EB.Dot.Array("items", data, new ArrayList());
			List<GameStoreItem> items = new List<GameStoreItem>();
			foreach (Hashtable itemData in itemsData)
			{
				// Check if the item overrides the CDN
				string cdn = EB.Dot.String("cdn", itemData, defaultCDN);

				// Fix 'image' to include the CDN when needed
				if (cdn != null && EB.Dot.Bool("images3", itemData, false))
				{
					string image = EB.Dot.String("image", itemData, null);
					if (!string.IsNullOrEmpty(image))
					{
						itemData["image"] = cdn + "/" + image;
					}
				}


				// Determine which factory to use
				string category = EB.Dot.String("category", itemData, string.Empty);

				IGameStoreItemFactory factory;
				if (!this._itemFactories.TryGetValue(category, out factory))
				{
					factory = this._defaultFactory;
				}


				// Build and validate the item
				GameStoreItem item = factory.CreateInstance(itemData);

				if (item.IsValid)
				{
					// Update each pricing with the version hash ID
					foreach(GameStorePricing pricing in item.Pricings)
					{
						pricing.VersionID = item.VersionID;
					}
					
					items.Add(item);
				}
			}

			// Store result
			this._items = items;
			return true;
		}


		private List<RedeemerItem> ParseResults(Hashtable data) {
			List<RedeemerItem> results = EB.Dot.List<RedeemerItem>("redeemers", data, new List<RedeemerItem>());
			return results;
		}


		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize(Config config)
		{
			base.Initialize( config );
			
			this._config = config.GameStoreConfig;
			this._api = new GameStoreAPI(Hub.ApiEndPoint);
		}		
		
		public override void Disconnect(bool isLogout)
		{
		}
		#endregion
	}
}
