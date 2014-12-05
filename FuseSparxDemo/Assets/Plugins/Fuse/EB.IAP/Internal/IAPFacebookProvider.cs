using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// TODO-moko: we shouldnt need this anymore
#if UNITY_WEBPLAYER && false 
namespace EB.IAP.Internal
{
	class FacebookProvider : MonoBehaviour, Provider
	{
		private static Config 	_config;
		private List<Item> 		_items;

		public static FacebookProvider Create(Config config)
		{
			_config = config;
			var go = new GameObject("iap_plugin_facebook");
			return go.AddComponent<IAPFacebookProvider>();
		}

		void Awake()
		{
			// moko: initialize sparx facebook store API
			DontDestroyOnLoad(gameObject);
		}

		#region Provider Implementation
		public void PurchaseItem(Item item)
		{
			var payoutid = item.info["payoutid"];
			string injectionStr = string.Format("DoItemPurchase('{0}');", payoutid);
			EB.Debug.LogError("[IAPFacebookProvider::PurchaseItem] injection: " + injectionStr);
			Application.ExternalEval(injectionStr);

#if false	// TODO-moko: debug stuff. to be remove
			{
				string s = "[IAPFacebookProvider::PurchaseItem] " + item.info.ToString();
				foreach(DictionaryEntry i in item.info)
				{
					s += ("[IAPFacebookProvider::PurchaseItem] Payout [" + item.productId + "] => " + i.Key + " -> " + i.Value + "\n");
				}
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.cost = " + item.cost;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.cents = " + item.cents;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.value = " + item.value;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.localizedCost = " + item.localizedCost;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.productId = " + item.productId;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.type = " + item.type;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.show = " + item.show;
				s += "\n[IAPFacebookProvider::PurchaseItem] Item.developerPayload = " + item.developerPayload;
				EB.Debug.LogWarning(s);

				EB.Debug.Log("[IAPFacebookProvider::PurchaseItem] PurchaseItem: " + payoutid + " = [injects] " + injectionStr);
				EB.Debug.Log(item.info.ToString());
				EB.Debug.Log(item.productId);
				EB.Debug.Log(item.type);
			}
#endif
		}

		public void Enumerate(List<Item> items)
		{
			EB.Debug.Log("[IAPFacebookProvider::Enumerate]: " + items.ToString());

			_items = items;

			var productIds = ArrayUtils.Map<Item,string>( items, delegate(Item item){
				if (item.type == ItemType.Soft) return null;
				return item.productId;
			});
			var offersString = ArrayUtils.Join(productIds, ',');

			// TODO-moko: clean this up into a resource text file later
			string injectionStr =
				"var DoEnumerateCheck = function() {"
				+ "	if (typeof(KBPAY) != 'undefined') {"
				+ " 	DoEnumerate('" + offersString + "');"
				+ "} else {"
				+ "		console.log('[DoEnumerateCheck] dead time spinning.....');"
				+ "		setTimeout(DoEnumerateCheck, 100);"
				+ "}}; DoEnumerateCheck();";

			EB.Debug.Log("[IAPFacebookProvider::Enumerate]: injection: " + injectionStr);
			Application.ExternalEval(injectionStr);
		}

		public void Complete(Transaction transaction)
		{
			EB.Debug.Log("[IAPFacebookProvider] Complete: " + (transaction==null?"NULL":transaction.ToString()));
		}

		public string Name
		{
			get
			{
				return "facebook";
			}
		}
		#endregion

		public void OnIAPUnsupported()
		{
			EB.Debug.Log("[IAPFacebookProvider::OnIAPUnsupported]: ");
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

		void OnIAPSupported(string skuDetails)
		{
			EB.Debug.Log("[IAPFacebookProvider::OnIAPSupported]: " + skuDetails);

			string res = "";
			if (_items != null && string.IsNullOrEmpty(skuDetails) == false)
			{
				var objects = JSON.Parse(skuDetails) as ArrayList;

				foreach (var item in _items)
				{
					if (item.type != ItemType.Soft)
					{
						item.valid = false;
					}

					foreach (var obj in objects)
					{
						if (Dot.String("thirdPartyId", obj, string.Empty) == item.productId)
						{
							item.valid = true;
							item.localizedCost = Dot.String("cost", obj, string.Empty);
							item.localizedTitle = Dot.String("longName", obj, string.Empty);
							item.localizedDesc = Dot.String("description", obj, string.Empty);
							item.currencyCode = Dot.String("currency", obj, string.Empty);
							//item.cents = Dot.String("centsPerIGC", obj, string.Empty);

							res += "[IAPFacebookProvider::OnIAPSupported]: Setting \"" + item.productId + "\" to valid (" + item.localizedDesc + ")";
							break;
						}
					}
				}

				EB.Debug.Log("[IAPFacebookProvider::OnIAPSupported]: " + res);
				if (_config.OnEnumerate != null)
				{
					_config.OnEnumerate();
				}
			}
		}

		void OnItemPurchased(string jsonParts)
		{
			EB.Debug.Log("[IAPFacebookProvider::OnItemPurchased]: " + jsonParts);

			var obj = JSON.Parse(jsonParts);
			Transaction transaction = new Transaction();
			transaction.transactionId = Dot.String("payment_id", obj, string.Empty);
			transaction.signature = Dot.String("signed_request", obj, string.Empty);
			transaction.payload = jsonParts;
			transaction.productId = Dot.String("product_id", obj, string.Empty);

			// TODO-moko: nothing really to verify for web payment....
			//if (_config.Verify != null)
			//{
			//	_config.Verify(transaction);
			//}
			//else
			{
				if (_config.OnCompleted != null)
				{
					_config.OnCompleted(transaction);
				}
				Complete(transaction);
			}
		}

		void OnItemPurchaseFailed(string error)
		{
			EB.Debug.LogError("[IAPFacebookProvider::OnItemPurchaseFailed]: " + error);
			_config.OnPurchaseFailed(error);
		}
	}
}

class IAPFacebookProvider : EB.IAP.Internal.FacebookProvider
{
}
#endif
