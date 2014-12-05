using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.IAP
{	
	public interface ReceiptPersistance
	{
		void AddPendingPurchaseReceipt(string token, string sku);
		
		bool RemovePendingPurchaseReceipt(string token);
		
		Hashtable GetPendingPurchaseReceipts();
	}

	public class DefaultReceiptPersistance : ReceiptPersistance
	{
		const string kKey = "payment-queue";
		#region ReceiptPersistance implementation
		private void _Save(Hashtable ht)
		{
			EB.SecurePrefs.SetJSON(kKey, ht);
		}

		private Hashtable _Get()
		{
			var ht = EB.SecurePrefs.GetJSON(kKey) as Hashtable;
			if (ht == null)
			{
				ht = new Hashtable();
			}
			return ht;
		}

		public void AddPendingPurchaseReceipt (string token, string sku)
		{
			var current = GetPendingPurchaseReceipts();
			current[token] = sku;
			_Save(current);
		}

		public bool RemovePendingPurchaseReceipt (string token)
		{
			var current = GetPendingPurchaseReceipts();
			var result = false;
			if (current.ContainsKey(token))
			{
				result = true;
				current.Remove(token);
			}
			_Save(current);
			return result;
		}

		public Hashtable GetPendingPurchaseReceipts ()
		{
			return _Get();
		}

		#endregion


	}

	public class Config
	{
		// public key for Android
		public string PublicKey 								= string.Empty;
		public Action<Transaction> 			Verify 				= null; // a user defined verification callback
		public Action 			 			OnEnumerate 		= null;	
		public Action<string>				OnPurchaseFailed 	= null;
		public Action						OnPurchaseCanceled 	= null;
		public ReceiptPersistance 			ReceiptPersistance 	= new DefaultReceiptPersistance();
		public Action<Transaction>			OnCompleted			= null;	// moko: added an OnComplete() for web payment only
	}
	
	public enum ItemType
	{
		Consumable,
		NonConsumable,
		Subscription
	}
	
	public class Item
	{
		public string productId 		= string.Empty;
		public int payoutId				= 0;
		public float  cost 				= 0;
		public int cents				= 0;
		public string localizedCost 	= string.Empty;
		public bool   valid 			= false;
		public bool  show				= true;
		public ItemType type			= ItemType.Consumable;
		public int 	  value				= 0;
		public Hashtable metadata		= null;
		public string currencyCode		= string.Empty;
		public string localizedTitle	= string.Empty;
		public string longName			= string.Empty;
		public string localizedDesc		= string.Empty;
		public string developerPayload	= string.Empty;
		public int bonusCurrency 		= 0;
		public bool includesBonus		= true;
		public List<Sparx.RedeemerItem> redeemers = new List<EB.Sparx.RedeemerItem>();
		
		public Item( string trkId, Hashtable data )
		{
			cost = EB.Dot.Single("cost", data, 0);
			cents = UnityEngine.Mathf.RoundToInt(cost*100);
			value = EB.Dot.Integer("numOfIGC", data, 0);
			localizedCost = EB.Dot.String("coststring", data, string.Empty);
			productId = EB.Dot.String("thirdPartyId",data, string.Empty);
			type = EB.IAP.ItemType.Consumable;
			show = EB.Dot.Bool("show", data, false);
			payoutId = Dot.Integer("payoutid", data, 0);
			metadata = Dot.Object("metadata", data, new Hashtable() );
			bonusCurrency = EB.Dot.Integer("igcBonus", data, 0);
			localizedTitle = EB.Dot.String("longName", data, string.Empty);
			longName = EB.Dot.String("longName", data, string.Empty);
			localizedDesc = EB.Dot.String("description", data, string.Empty);
			includesBonus = EB.Dot.Bool("includesBonus", data, true );
			var redeemersData = EB.Dot.Array( "redeemers", data, new ArrayList() );
			foreach( object candidate in redeemersData )
			{
				Hashtable redeemer = candidate as Hashtable;
				if( redeemer != null )
				{
					var item = new Sparx.RedeemerItem( redeemer );
					if( item.IsValid == true )
					{
						redeemers.Add( item );
					}
				}
			}
			developerPayload = trkId + "@=>@" + EB.Dot.String("payoutid", data, string.Empty);
		}
		
		public bool ContainsRedeemer(Sparx.RedeemerItem item)
		{
			foreach(Sparx.RedeemerItem redeemer in redeemers)
			{
				if(redeemer.IsSameItem(item))
				{
					return true;
				}
			}
			
			return false;
		}
	}
	
	public class Transaction
	{
		public string transactionId  	= string.Empty;
		public string productId			= string.Empty;
		public string payload			= string.Empty;
		public string signature			= string.Empty;
		public string platform			= EB.Sparx.Device.MobilePlatform;
	}
	
	public class Manager
	{
		Internal.Provider _provider;
		
		public string ProviderName { get { return _provider.Name; }  }
		
		public Manager( Config config )
		{
			_provider = Internal.ProviderFactory.Create(config);
		}
		
		public void PurchaseItem( Item item ) 
		{
			_provider.PurchaseItem(item); 
		}
		
		public void Enumerate( List<Item> items )
		{
			_provider.Enumerate( items);
		}
		
		public void Complete( Transaction t )
		{
			_provider.Complete(t);
		}
	}
}

