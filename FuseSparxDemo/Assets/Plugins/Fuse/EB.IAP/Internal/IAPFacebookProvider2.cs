using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_WEBPLAYER
namespace EB.IAP.Internal
{
	class FacebookProvider2 : MonoBehaviour, Provider
	{
		private static Config 	_config;
		private List<Item> 		_items;

		public static FacebookProvider2 Create(Config config)
		{
			_config = config;
			var go = new GameObject("iap_plugin_facebook");
			return go.AddComponent<IAPFacebookProvider2>();
		}


		void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		#region Provider Implementation
		public void PurchaseItem(Item item)
		{
			Application.ExternalCall("DoItemPurchase", item.developerPayload, item.productId);
		}

		public void Enumerate(List<Item> items)
		{
			// support all!
			foreach ( var item in items )
			{
				item.valid = true;
			}

			// nothing to do
			if (_config.OnEnumerate != null)
			{
				_config.OnEnumerate();
			}

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

		void OnItemPurchased(string jsonParts)
		{
			EB.Debug.Log("[IAPFacebookProvider::OnItemPurchased]: " + jsonParts);

			var obj = JSON.Parse(jsonParts);
			Transaction transaction = new Transaction();
			transaction.transactionId = Dot.String("payment_id", obj, string.Empty);
			transaction.signature = Dot.String("signed_request", obj, string.Empty);
			transaction.payload = jsonParts;
			transaction.productId = Dot.String("product_id", obj, string.Empty);


			/*
			if (_config.Verify != null)
			{
				_config.Verify(transaction);
			}*/

			_config.OnCompleted(transaction);
		}

		void OnItemPurchaseFailed(string error)
		{
			EB.Debug.LogError("[IAPFacebookProvider::OnItemPurchaseFailed]: " + error);
			if (_config.OnPurchaseFailed != null )
			{
				_config.OnPurchaseFailed(error);
			}
		}
	}
}

class IAPFacebookProvider2 : EB.IAP.Internal.FacebookProvider2
{
}
#endif
