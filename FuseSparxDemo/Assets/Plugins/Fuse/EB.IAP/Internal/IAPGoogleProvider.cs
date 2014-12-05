using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID	
namespace EB.IAP.Internal
{

	public class GoogleProvider : MonoBehaviour, Internal.Provider
	{
		private static Config _config;
		
		// our plugin class
		private AndroidJavaClass _class;
	
		private List<Item> _items;
				
		public static GoogleProvider Create( Config config)
		{
			_config 	= config;
			var go 		= new GameObject("iap_plugin_android" );
			return go.AddComponent<IAPGoogleProvider>();
		}
		
		void Awake()	
		{
			_class 		= new AndroidJavaClass("com.explodingbarrel.iap.Manager");	
			_class.CallStatic("Init", _config.PublicKey, true);
			DontDestroyOnLoad(gameObject);
		}
		
		public void PurchaseItem( Item item ) 
		{
			var typeString = "inapp";
			if (item.type == ItemType.Subscription)
			{
				typeString = "subs";
			}
			
			Debug.Log("PurchaseItem: " + item.productId);
			_class.CallStatic("PurchaseItem", item.productId, typeString, item.developerPayload);
		}
		
		#region Provider implementation
		public void Enumerate (List<Item> items)
		{
			_items = items;
			
			var productIds = ArrayUtils.Map<Item,string>( items, delegate(Item item){ 
				return item.productId; 
			} );
			var offersString = ArrayUtils.Join( productIds, ',' );
			
			_class.CallStatic("Enumerate", offersString);
		}
		
		void OnIAPUnsupported()
		{
			Debug.Log("OnIAPUnsupported");
			if (_items != null)
			{
				foreach( var item in _items)
				{
					item.valid = false;
				}
				
				if (_config.OnEnumerate != null)
				{
					_config.OnEnumerate();
				}
			}
		}
		
		void OnIAPSuppored(string skuDetails)
		{
			Debug.Log("OnIAPSuppored: " + skuDetails);
			
			if (_items != null && string.IsNullOrEmpty(skuDetails) == false )
			{
				var objects = JSON.Parse(skuDetails) as ArrayList;
				
				foreach( var item in _items )
				{
					item.valid = false;
					foreach( var obj in objects )
					{
						if ( Dot.String("productId", obj, string.Empty) == item.productId )
						{
							item.valid = true;
							item.localizedCost = Dot.String("price", obj, string.Empty);
							item.localizedTitle = Dot.String("title", obj, string.Empty);
							item.localizedDesc = Dot.String("description", obj, string.Empty);
														
							item.currencyCode = Dot.String("price_currency_code", obj, string.Empty);
							item.cents = Dot.Integer("cents",obj, (int)(item.cost*100));

							break;
						}
					}
				}
				
				if (_config.OnEnumerate != null)
				{
					_config.OnEnumerate();
				}
			}
			
		}
		
		public void Complete (Transaction transaction)
		{
			_class.CallStatic("CompletePurchase", transaction.payload, transaction.signature);
		}
	
		#endregion
		
		void OnItemPurchaseCanceled( string ignore )
		{
			Debug.Log("OnItemPurchaseCanceled:" );
			if (_config.OnPurchaseCanceled != null)
			{
				_config.OnPurchaseCanceled();
			}
		}
		
		void OnItemPurchaseFailed( string error )
		{
			Debug.Log("OnItemPurchaseFailed: " + error);
			if (_config.OnPurchaseFailed != null)
			{
				_config.OnPurchaseFailed(error);
			}
		}
		
		void OnItemPurchased( string jsonParts )
		{
			Debug.Log("OnItemPurchased: " + jsonParts);
			var parts = jsonParts.Split('|');
			var json	  = parts[0];
			var signature = parts[1];
			
			var obj = JSON.Parse(json);
			Transaction transaction = new Transaction();
			transaction.transactionId = Dot.String("orderId", obj, string.Empty);
			transaction.signature = signature;
			transaction.payload = json;
			transaction.productId = Dot.String("productId", obj, string.Empty);
			
			if ( _config.Verify != null )
			{
				_config.Verify(transaction);
			}
			else
			{
				Complete(transaction);
			}
		}

		#region Provider implementation
		public string Name
		{
			get
			{
				return "googleapp";
			}
		}
		#endregion
	}
}

class IAPGoogleProvider : EB.IAP.Internal.GoogleProvider 
{
	
}
#endif