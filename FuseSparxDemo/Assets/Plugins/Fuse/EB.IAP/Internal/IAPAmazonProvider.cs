using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID	
namespace EB.IAP.Internal
{
	
	public class AmazonProvider : MonoBehaviour, Internal.Provider
	{
		private static Config _config;
		
//		// our plugin class
		private AndroidJavaClass _class;
	
		private List<Item> 	_items;
		private string 		_userId = null;
				
		public static AmazonProvider Create( Config config)
		{
			_config 	= config;
			var go 		= new GameObject("iap_plugin_kindle" );
			go.AddComponent<AmazonIAPManager>();
			return go.AddComponent<IAPAmazonProvider>();
		}
		private int 			kMaxRetryCount = 3;
		
		void Awake()	
		{
			EB.Debug.Log ("AmazonProvider.Awake : DontDestroyOnLoad");
			DontDestroyOnLoad(this);
		}
		
		private IEnumerator RetryPendingPurchases()
		{
			EB.Debug.Log("Retrying Amazon pending PURCHASES");
			int retryCount = 0;
			float waitSeconds = 1.0f;
			while (retryCount < kMaxRetryCount)
			{
				if (_config.ReceiptPersistance == null)
				{
					EB.Debug.Log("NO RECEIPT PERSISTANCE");
					break;
				}
				Hashtable pendingPurchases = _config.ReceiptPersistance.GetPendingPurchaseReceipts();
				if (pendingPurchases.Count <= 0) 
				{
					EB.Debug.Log("No Pending Purchases! Done!");
					break;
				}
				Hashtable pendingPurchasesCopy = new Hashtable(pendingPurchases);
				for (IDictionaryEnumerator iter = pendingPurchasesCopy.GetEnumerator(); iter.MoveNext();)
				{
					string receiptString = (string)iter.Key;
					string sku = (string)iter.Value;
					
					EB.Debug.Log("Retrying Amazon pending purchase token "+receiptString+" - "+sku);
					
					Hashtable ht = new Hashtable();
					ht["token"] = receiptString;
					ht["sku"] = sku;
					//TODO: not sure if we actually need to store this info below, I think we only need to store consumables because their receipts don't get stored on the server.
					ht["type"] = "CONSUMABLE";
					
					AmazonReceipt amazonReceipt = new AmazonReceipt(ht);
					purchaseSuccessfulEvent(amazonReceipt);			         	
				}
				yield return new WaitForSeconds(waitSeconds);
				waitSeconds *= 2.0f;
				retryCount++;
			}
			EB.Debug.Log("Retrying Amazon pending PURCHASES DONE");
		}
		
		private IEnumerator RequestAmazonUserId()
		{
			EB.Debug.Log("Getting Amazon User ID");
			int retryCount = 0;
			float waitSeconds = 1.0f;
			while (retryCount < kMaxRetryCount && System.String.IsNullOrEmpty(_userId))
			{
				AmazonIAP.initiateGetUserIdRequest();
				yield return new WaitForSeconds(waitSeconds);
				waitSeconds *= 2.0f;
				retryCount++;
			}
			if (!System.String.IsNullOrEmpty(_userId))
			{
				Coroutines.Run(RetryPendingPurchases());
			}
			EB.Debug.Log("RequestAmazonUserId Finished - Result: {0}", (System.String.IsNullOrEmpty(_userId) ? "failed." : _userId));
		}
				
		#region Provider implementation
		public void PurchaseItem( Item item ) 
		{
//			var typeString = "inapp";
//			if (item.type == ItemType.Subscription)
//			{
//				typeString = "subs";
//			}
//			
//			EB.Debug.Log("PurchaseItem: " + item.productId);
			
			//TODO: if user doesn't have userid yet warning / retry 
			
			AmazonIAP.initiatePurchaseRequest( item.productId );
		}
		
		public void Enumerate (List<Item> items)
		{
			_items = items;
			
			List<string> productIds = ArrayUtils.Map<Item,string>( items, delegate(Item item){ 
				return item.productId; 
			} );
			
			string[] skus = productIds.ToArray();
			
			AmazonIAP.initiateItemDataRequest( skus );
		}
				
		public void Complete (Transaction transaction)
		{
			EB.Debug.Log ("AmazonProvider.Complete.");
			
			if (_config.ReceiptPersistance != null)
			{
				_config.ReceiptPersistance.RemovePendingPurchaseReceipt(transaction.transactionId);	// save the token in case it doesn't get verified correctly, so we can retry later.
				EB.Debug.Log("+++++++++++++ REMOVAL SUCCESSFUL");
			}
			else
			{
				EB.Debug.Log("++++++++ NO RECEIPT PERSISTANCE!!");
			}			
		}
	
		#endregion
		
		#region Amazon callbacks
		void OnEnable()
		{
			EB.Debug.Log("AmazonIAPOnEnable");
			// Listen to all events for illustration purposes
			AmazonIAPManager.itemDataRequestFailedEvent += itemDataRequestFailedEvent;
			AmazonIAPManager.itemDataRequestFinishedEvent += itemDataRequestFinishedEvent;
			AmazonIAPManager.purchaseFailedEvent += purchaseFailedEvent;
			AmazonIAPManager.purchaseSuccessfulEvent += purchaseSuccessfulEvent;
			AmazonIAPManager.purchaseUpdatesRequestFailedEvent += purchaseUpdatesRequestFailedEvent;
			AmazonIAPManager.purchaseUpdatesRequestSuccessfulEvent += purchaseUpdatesRequestSuccessfulEvent;
			AmazonIAPManager.onSdkAvailableEvent += onSdkAvailableEvent;
			AmazonIAPManager.onGetUserIdResponseEvent += onGetUserIdResponseEvent;
		}
	
	
		void OnDisable()
		{
			EB.Debug.Log("AmazonIAPOnDisable");
			// Remove all event handlers
			AmazonIAPManager.itemDataRequestFailedEvent -= itemDataRequestFailedEvent;
			AmazonIAPManager.itemDataRequestFinishedEvent -= itemDataRequestFinishedEvent;
			AmazonIAPManager.purchaseFailedEvent -= purchaseFailedEvent;
			AmazonIAPManager.purchaseSuccessfulEvent -= purchaseSuccessfulEvent;
			AmazonIAPManager.purchaseUpdatesRequestFailedEvent -= purchaseUpdatesRequestFailedEvent;
			AmazonIAPManager.purchaseUpdatesRequestSuccessfulEvent -= purchaseUpdatesRequestSuccessfulEvent;
			AmazonIAPManager.onSdkAvailableEvent -= onSdkAvailableEvent;
			AmazonIAPManager.onGetUserIdResponseEvent -= onGetUserIdResponseEvent;
		}
	
		void itemDataRequestFailedEvent()
		{
			EB.Debug.Log( "itemDataRequestFailedEvent" );
			if (_items != null)
			{
				foreach( Item item in _items)
				{
					item.valid = false;
				}
				
				if (_config.OnEnumerate != null)
				{
					_config.OnEnumerate();
				}
			}
		}
	
	
		void itemDataRequestFinishedEvent( List<string> unavailableSkus, List<AmazonItem> availableItems )
		{
			EB.Debug.Log( "itemDataRequestFinishedEvent. unavailable skus: " + unavailableSkus.Count + ", available items: " + availableItems.Count );
			if (_items != null && availableItems != null)
			{	
				EB.Debug.Log("AmazonItems:");
				foreach( AmazonItem item in availableItems )
				{
					EB.Debug.Log(item.ToString());
				}
					
				foreach( Item item in _items )
				{
					item.valid = false;

					foreach( AmazonItem available in availableItems )
					{
						if ( available.sku == item.productId )
						{
							item.valid = true;
							item.localizedCost = available.price;
							item.localizedTitle = available.title;
							item.localizedDesc = available.description;

							//TODO: amazon doesn't seem to provide a currency code, or cents value
							//item.currencyCode = "";
							
							EB.Debug.Log("parsing cents: {0}", available.price);
							float costValue = 0.0f;
							string price = available.price;

							//TODO: need to make sure this works for all currency
							bool result = System.Single.TryParse(price, out costValue);
							if (result)
							{
								item.cents = (int)(costValue * 100);
								EB.Debug.Log ("cents: {0}", item.cents);
								break;
							}
							else
							{
								item.cents = (int)item.cost*100;
							}
							
							
							EB.Debug.Log("Cost {0}", item.cost);
							EB.Debug.Log("item currency code {0}", item.currencyCode);
							
							//TODO: Are there other item members that need filling here?

							break;
						}
					}
				}
				
				if (_config.OnEnumerate != null)
				{
					_config.OnEnumerate();
				}
				
				
				// now that we've successfully called Amazon, AmazonIAP.initiateItemDataRequest we are allowed to start other AmazonIAP operations.
				Coroutines.Run(RequestAmazonUserId());
			}			
		}
	
	
		void purchaseFailedEvent( string reason )
		{
			EB.Debug.Log( "purchaseFailedEvent: " + reason );
			if (_config.OnPurchaseFailed != null)
			{
				if (reason == "Unknown Reason")
				{
					EB.Debug.Log("purchaseFailedEvent: Unknown Reason -- assuming this means a closed window.");
					_config.OnPurchaseCanceled();
				}
				else
				{
					EB.Debug.LogError("Purchase failed: {0}", reason);
					_config.OnPurchaseFailed(reason);
				}
			}
		}
	
	
		void purchaseSuccessfulEvent( AmazonReceipt receipt )
		{
			//Note: sounds like this could be called as soon as the customer connects if they didn't receive their success response last time they bought something (e.g. connectivity loss or device shuts down) 
			
			EB.Debug.Log( "purchaseSuccessfulEvent: " + receipt );
			
			Transaction transaction = new Transaction();
			transaction.transactionId = receipt.token;
			transaction.signature = "";
			
			// doing this here so we don't have to modify the Amazon plugin code.
			Hashtable payload = new Hashtable();
			payload["token"] = receipt.token;
			payload["sku"] = receipt.sku;

			//TODO: do we need to check if _userId has been successfully populated?
			payload["userId"] = _userId;

			transaction.payload = EB.JSON.Stringify(payload);
			transaction.productId = receipt.sku;
			
			if (_config.ReceiptPersistance != null)
			{
				_config.ReceiptPersistance.AddPendingPurchaseReceipt(receipt.token, receipt.sku);	// save the token in case it doesn't get verified correctly, so we can retry later.
			}
			else
			{
				EB.Debug.Log("++++++++ NO RECEIPT PERSISTANCE!!");
			}
			
			if ( _config.Verify != null )
			{
				_config.Verify(transaction);
			}
			else
			{
				Complete(transaction);
			}			
		}
	
	
		void purchaseUpdatesRequestFailedEvent()
		{
			EB.Debug.LogWarning( "purchaseUpdatesRequestFailedEvent" );
		}
	
	
		void purchaseUpdatesRequestSuccessfulEvent( List<string> revokedSkus, List<AmazonReceipt> receipts )
		{
			EB.Debug.Log("purchaseUpdatesRequestSuccessfulEvent. revoked skus count: {0} - receipts count: {1}", revokedSkus.Count, receipts.Count);
			foreach(AmazonReceipt receipt in receipts )
			{
				EB.Debug.Log("Purchase Update Receipt: {0}", receipt );
				purchaseSuccessfulEvent(receipt);
			}
			foreach(string revoked in revokedSkus)
			{
				EB.Debug.LogError("Purchase Update Revoked SKU: {0}", revoked );
				//TODO: Think this should be handled on the server?
			}
		}
	
	
		void onSdkAvailableEvent( bool isTestMode )
		{
			EB.Debug.Log( "onSdkAvailableEvent. isTestMode: " + isTestMode );
		}
	
	
		void onGetUserIdResponseEvent( string userId )
		{
			if (System.String.IsNullOrEmpty(userId))
			{
				EB.Debug.Log( "onGetUserIdResponseEvent: failed - re-requesting!");
			}
			else
			{
				EB.Debug.Log( "onGetUserIdResponseEvent: " + userId );
				_userId = userId;
			}
		}		
		#endregion
		
		#region callbacks
		
//TODO: no cancelled event?	
//		void OnItemPurchaseCanceled( string ignore )
//		{
//			EB.Debug.Log("OnItemPurchaseCanceled:" );
//			if (_config.OnPurchaseCanceled != null)
//			{
//				_config.OnPurchaseCanceled();
//			}
//		}

		#endregion

		#region Provider implementation
		public string Name
		{
			get
			{
				return "amazonapp";
			}
		}
		#endregion
	}
}

class IAPAmazonProvider : EB.IAP.Internal.AmazonProvider 
{
	
}
#endif
