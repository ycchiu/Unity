using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_IPHONE
namespace EB.IAP.Internal
{
	class AppleProvider :  MonoBehaviour, Provider
	{
		const string DLL_NAME = "__Internal";
		private static Config _config;
		private static bool _init = false;
		
		[DllImport(DLL_NAME)]
		static extern void _IAPInitialize();
		
		[DllImport(DLL_NAME)]
		static extern void _IAPEnumerate(string offerString);
		
		[DllImport(DLL_NAME)]
		static extern void _IAPPurchase(string offerId);
		
		[DllImport(DLL_NAME)]
		static extern void _IAPComplete(string identifier);
		
		public static AppleProvider Create( Config config )
		{
			_config 	= config;
			var go 		= new GameObject("iap_plugin_apple" );
			DontDestroyOnLoad(go);
			return go.AddComponent<IAPAppleProvider>();
		}
		
		event Action<string> _onEnumerate;
		
		void Awake()
		{
			if (!_init)
			{
				_init = true;
				_IAPInitialize();
			}
		}

		#region Provider implementation
		public void PurchaseItem (Item item)
		{
			_IAPPurchase(item.productId);
		}
		#endregion

		#region Provider implementation
		public void Enumerate (List<Item> items)
		{
			var productIds = ArrayUtils.Map<Item,string>( items, delegate(Item item){ 
				return item.productId; 
			});
			var offersString = ArrayUtils.Join( productIds, ',' );
			
			_onEnumerate += delegate(string json) {
				var objects = JSON.Parse(json) as ArrayList;
				
				foreach( var item in items )
				{
					item.valid = false;
					foreach( var obj in objects )
					{
						if ( Dot.String("oid", obj, string.Empty) == item.productId )
						{
							item.valid = true;
							item.localizedCost = Dot.String("price", obj, string.Empty);
							item.currencyCode = Dot.String("currency", obj, string.Empty);
							item.localizedTitle = Dot.String("title", obj, string.Empty);
							item.localizedDesc = Dot.String("desc", obj, string.Empty);
							item.cents = Dot.Integer("cents",obj, (int)item.cost*100);
							break;
						}
					}
				}
				
				if ( _config.OnEnumerate != null )
				{
					_config.OnEnumerate();
				}
				
			};
			
			EB.Debug.Log("Enumerating IAP: " + offersString );
			_IAPEnumerate(offersString); 
		}
		#endregion
		
		void OnIAPEnumerate( string json )
		{
			EB.Debug.Log("OnIAPEnumerate: " + json );
			if (_onEnumerate != null)
			{
				_onEnumerate(json);
				_onEnumerate = null;
			}
		}
		
		void OnIAPPurchaseCanceled(string ignore)
		{
			EB.Debug.Log("User canceled purchase");
			if (_config.OnPurchaseCanceled != null)
			{
				_config.OnPurchaseCanceled();
			}
		}
		
		void OnIAPPurchaseFailed(string localizedError)
		{
			EB.Debug.LogError("Purchase failed: " + localizedError );
			if (_config.OnPurchaseFailed != null)
			{
				_config.OnPurchaseFailed(localizedError);
			}
		}
		
		void OnIAPComplete(string data)
		{
			EB.Debug.Log("OnIAPComplete:"+data);
			var parts = data.Split(',');
			var transaction = new Transaction();
			transaction.transactionId = parts[0];
			transaction.payload = parts[1];
			transaction.productId = parts[2];
			
			// see if we need to verify
			if ( _config.Verify != null )
			{
				_config.Verify(transaction);
			}
			else
			{
				Complete(transaction);
			}
		}

		public void Complete (Transaction transaction)
		{
			_IAPComplete( transaction.transactionId ); 
		}
		
		public void OnIAPFinalize(string transactionId)
		{
			
		}

		#region Provider implementation
		public string Name 
		{
			get 
			{
				return "itunes";
			}
		}
		#endregion
	}
}

class IAPAppleProvider : EB.IAP.Internal.AppleProvider 
{
	
}
#endif
