using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{	
	public class PaymentsConfig
	{
		public PaymentsListener Listener 	= new DefaultPaymentsListener();
		public string  IAPPublicKey 		= string.Empty;
	}
	
	public class PayoutSale
	{
		public class PayoutBanner
		{
			public PayoutBanner( Hashtable data )
			{
				this.Title = string.Empty;
				this.SubTitle = string.Empty;
				this.Chevron = string.Empty;
				this.Image = string.Empty;
				
				if( data != null )
				{
					this.Title = EB.Dot.String( "title", data, string.Empty );
					this.SubTitle = EB.Dot.String( "subtitle", data, string.Empty );
					this.Chevron = EB.Dot.String( "chevron", data, string.Empty );
					this.Image = EB.Dot.String( "image", data, string.Empty );
				}
			}
			
			public override string ToString()
			{
				return string.Format("{0}:{1} Image:{2} Chevron:{3}", this.Title, this.SubTitle, this.Image, this.Chevron );
			}
			
			public string Title { get; private set; }
			public string SubTitle { get; private set; }
			public string Chevron { get; private set; }
			public string Image { get; private set; }
		}
	
		public PayoutSale( Hashtable data )
		{
			this.Title = string.Empty;
			this.Description = string.Empty;
			this.Chevron = string.Empty;
			this.Image = string.Empty;
			this.Flash = string.Empty;
			this.Colour = Color.white;
			this.Notification = string.Empty;
			this.EndTime = 0;
			
			if( data != null )
			{
				this.Title = EB.Dot.String( "title", data, string.Empty );
				this.Description = EB.Dot.String( "desc", data, string.Empty );
				this.Chevron = EB.Dot.String ("chevron", data, string.Empty );
				this.Image = EB.Dot.String( "image", data, string.Empty );
				this.Flash = EB.Dot.String( "flash", data, string.Empty );
				this.Colour = EB.Dot.Colour( "colour", data, Color.white );
				this.Notification = EB.Dot.String( "notification", data, string.Empty );
				this.Banner = new PayoutBanner( EB.Dot.Object( "banner", data, null ) );
				this.EndTime = EB.Dot.Integer( "endtime", data, 0 );
			}
			else
			{
				this.Banner = new PayoutBanner( null );
			}
		}
		
		public override string ToString()
		{
			return string.Format("{0}:{1} Chevron:{7} Image:{2} Flash:{3} Colour:{4} Notification:{5} Banner:{6}", this.Title, this.Description, this.Image, this.Flash, this.Colour, this.Notification, this.Banner, this.Chevron );
		}
		
		public string Title { get; private set; }
		public string Description { get; private set; }
		public string Chevron { get; private set; }
		public string Image { get; private set; }
		public string Flash { get; private set; }
		public Color Colour { get; private set; }
		public string Notification { get; private set; }
		public PayoutBanner Banner { get; private set; }
		public int EndTime { get; private set; }
		public int SecondsRemaining
		{
			get
			{
				int remaining = -1;
				if( this.EndTime > 0 )
				{
					remaining = this.EndTime - EB.Time.Now;
				}
				return remaining;
			}
		}
	}
	
	public class PaymentsManager : SubSystem, Updatable
	{
		bool					_walletApiEnabled;
		PaymentsConfig			_config;
		PaymentsAPI				_api;
		IAP.Manager				_iapManager;
		
		PayoutSale				_sale;
		List<RedeemerItem>		_bonusItems;
		List<IAP.Item> 			_payouts;
		List<IAP.Transaction>	_verify;
		int						_lastFetchTime;

		string 					_externalId = string.Empty;
		bool					_enumerated = false;

		public PayoutSale Sale				{ get { return _sale; } }
		public RedeemerItem[] BonusItems 	{ get { return _bonusItems.ToArray(); } }
		public IAP.Item[] Payouts 			{ get { return _payouts.ToArray(); } }
		
		public int LastFetchTime 			{ get { return _lastFetchTime; } } 
		
		public bool UpdateOffline 			{ get { return false;} }
		
		public bool PayoutsContainRedeemer(EB.Sparx.RedeemerItem candidate)
		{
			foreach(IAP.Item item in _payouts)
			{
				if(item.ContainsRedeemer(candidate))
				{
					return true;
				}
			}
			
			foreach(RedeemerItem item in _bonusItems)
			{
				if(item.IsSameItem(candidate))
				{
					return true;
				}
			}
			
			return false;
		}
		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_api 		= new PaymentsAPI(Hub.ApiEndPoint);
			_config 	= config.PaymentsConfig;
			_bonusItems = new List<RedeemerItem>();
			_payouts	= new List<IAP.Item>();
			_verify 	= new List<IAP.Transaction>();
			_lastFetchTime = 0;
						
			if (_config.Listener == null )
			{
				throw new System.ArgumentNullException("Missing payments listener");
			}

			var iapConfig = new IAP.Config();
			iapConfig.OnEnumerate += OnEnumerate;
			iapConfig.Verify += OnVerify;
			iapConfig.OnPurchaseFailed += OnPurchaseFailed;
			iapConfig.OnPurchaseCanceled += OnPurchaseCanceled;
			iapConfig.PublicKey = _config.IAPPublicKey;

			#if UNITY_WEBPLAYER		// moko: added an OnComplete() for web payment (which skip server's OnVerify)
			iapConfig.OnCompleted += OnCompleted;
			#endif
			_iapManager = new IAP.Manager( iapConfig );
		}
		
		public override void Connect ()
		{
			State = SubSystemState.Connected;
			
			// clear all the payouts
			_lastFetchTime = 0;
			_sale = null;
			_bonusItems.Clear();
			_payouts.Clear();
			_verify.Clear();
		
			FetchPayouts();
		}
		
		public void Update ()
		{
			if (_verify.Count > 0 )
			{
				if (!string.IsNullOrEmpty(_externalId) && _enumerated)
				{
					var transaction = _verify[0];
					_verify.RemoveAt(0);
					VerifyPayout(transaction);
				}
			}
		}
		
		private void VerifyPayout( IAP.Transaction transaction )
		{
			IAP.Item payout = _payouts.Find(delegate(IAP.Item obj) {
				return obj.productId == transaction.productId;
			});
			
			if (payout == null)
			{
				EB.Debug.LogError("Failed to verify receipt! Cant find payout");
				_verify.Add(transaction);
				return;
			}
			
					
			Hashtable data = new Hashtable();
			data["cents"] = payout.cents;
			data["currency"] = payout.currencyCode;
			data["externalTrkid"] = _externalId;
			data["payoutid"] = payout.payoutId;
			data["platform"] = transaction.platform;
			
#if UNITY_IPHONE && !UNITY_EDITOR
			data["receipt-data"] = transaction.payload;
#else
			data["response-data"] = transaction.payload;
			data["response-signature"] = transaction.signature;
#endif
			
#if UNITY_WEBPLAYER		// moko: for web player there is nothing to verify with the sparx server... so just skip ahead
			this.OnVerifyPayout(payout, transaction, null, null);
#else
			_api.VerifyPayout(_iapManager.ProviderName, data, delegate(string err, Hashtable res){
				OnVerifyPayout(payout, transaction,err,res);
			});
#endif
		}
		
		private void OnVerifyPayout( IAP.Item item, IAP.Transaction t, string err, Hashtable data )
		{
			if (!string.IsNullOrEmpty(err))
			{
				EB.Debug.LogError("Fatal: failed to verify payout");
				_config.Listener.OnOfferPurchaseFailed(err);
				_verify.Add(t);
				FetchPayouts();
				return;
			}
			
			// clear last fetch so we need to reload the offers after the purchase.
			_lastFetchTime = 0;
			
			// refresh offers - maybe a sale appeared/disappeared
			FetchPayouts();
			
			// complete the transaction
			_iapManager.Complete(t);
			
			Coroutines.Run(HandlePurchsedSuceeded(item));
		}
		
		IEnumerator HandlePurchsedSuceeded( IAP.Item item ) 
		{
			// sync if we got items
			if (Hub.InventoryManager != null)
			{
				int id = Hub.InventoryManager.Sync();
				yield return Hub.InventoryManager.Wait(id);
			}
			_config.Listener.OnOfferPurchaseSuceeded(item);
		}

		public override void Disconnect (bool isLogout)
		{

		}
		#endregion

#if UNITY_WEBPLAYER	// moko: added an OnComplete() for web payment (which skip server's OnVerify)
		public void OnCompleted(IAP.Transaction transaction)
		{
			this.OnVerifyPayout(null, transaction, null, null);
		}
#endif
				
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
			case "offers":
			case "payouts":
				{
					FetchPayouts();
				}
				break;	
			}
		}

		void FetchPayouts()
		{
			_api.FetchPayouts(_iapManager.ProviderName, OnFetchedPayouts);
		}
		
		void OnFetchedPayouts(string err,Hashtable data)
		{
			if (!string.IsNullOrEmpty(err))
			{
				EB.Debug.LogError("Failed to get payout: " + err);
				FatalError(err);
				return;
			}

			_externalId = EB.Dot.String("externalTrkid", data, _externalId);
			_sale = null;
			_bonusItems.Clear();
			_payouts.Clear();
			_enumerated = false;
			
			var currentSet = EB.Dot.Find("data.payoutSets.0", data);
			if (currentSet != null)
			{
				var sale = EB.Dot.Object("sale", currentSet, null );
				if( sale != null )
				{
					_sale = new PayoutSale( sale );
				}
				
				var bonus = EB.Dot.Array( "redeemers", currentSet, new ArrayList() );
				foreach( object candidate in bonus )
				{
					Hashtable item = candidate as Hashtable;
					if( item != null )
					{
						RedeemerItem redeemerItem = new RedeemerItem( item );
						if( redeemerItem.IsValid == true )
						{
							_bonusItems.Add( redeemerItem );
						}
					}
				}
				
				var payouts = Dot.Array("payouts", currentSet, new ArrayList());	
				foreach( Hashtable payout in payouts )	
				{
					var item = new EB.IAP.Item( _externalId, payout );
					_payouts.Add(item);
				}
			}
			
			_iapManager.Enumerate(_payouts);

			// moko: adding injection for facebook-unity login for payv2
			var injectionStr = Dot.String("injection", data, string.Empty);
			if (injectionStr != string.Empty)
			{
				Application.ExternalEval(injectionStr);
			}

			State = SubSystemState.Connected;
		}
		
		public void PurchaseOffer( IAP.Item item ) 
		{
			if ( _iapManager != null)
			{
				_iapManager.PurchaseItem( item );
			}
			else
			{
				_config.Listener.OnOfferPurchaseFailed("ID_SPARX_ERROR_MUST_BE_LOGGED_IN");
			}
		}

		void OnEnumerate()
		{
			_payouts.RemoveAll(delegate(IAP.Item obj) {
				return obj.valid == false;
			});
			EB.Debug.Log("IAP OnEnumerate: " + _payouts.Count);
			_enumerated = true;
			_config.Listener.OnOffersFetched();
		}
	
		void OnVerify( IAP.Transaction transaction )
		{
			EB.Debug.Log("IAP Verify");
			_verify.Add(transaction);
		}
		
		void OnPurchaseFailed( string error )
		{
			EB.Debug.Log("Purchase failed! "  + error);
			_config.Listener.OnOfferPurchaseFailed(error);	
		}
		
		void OnPurchaseCanceled()
		{
			EB.Debug.Log("OnPurchaseCancled");
			_config.Listener.OnOfferPurchaseCanceled();
		}
				
		
	}
}
