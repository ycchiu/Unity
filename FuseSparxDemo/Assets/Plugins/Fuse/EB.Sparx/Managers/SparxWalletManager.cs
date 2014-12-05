using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{	
	public class WalletConfig
	{
		public bool Enabled = false;
		public WalletListener Listener = new DefaultWalletListener();
	}
		
	public class WalletManager : SubSystem
	{
		bool					_walletApiEnabled;
		WalletConfig			_config;
		WalletAPI				_api;
		EB.SafeInt				_balance;

		bool					_fetchBalance = false;
		
		public int Balance { get { return _balance; } }

		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_balance 	= 0;
			_walletApiEnabled = true;
			_api 		= new WalletAPI(Hub.ApiEndPoint);
			_config 	= config.WalletConfig;
		}
		
		public override void Connect ()
		{
			State = SubSystemState.Connecting;

			// clear all the payouts
			var wallet = Dot.Object("wallet", Hub.LoginManager.LoginData, null);
			if ( wallet != null)
			{
				_walletApiEnabled = Dot.Bool("enabled", wallet, true);
				if (_walletApiEnabled == false)
				{
					EB.Debug.LogError("SparxWalletManager disabled by server!");
				}
				OnFetch(null, wallet);
			}
			else
			{
				Fetch();	
			}
		}
		
		public override void OnEnteredForeground() 
		{
			base.OnEnteredForeground();
			if (_fetchBalance)
			{
				_fetchBalance = false;
				this.Fetch();
			}
		}

		public override void Disconnect (bool isLogout)
		{
			_balance = 0;
		}
		#endregion
				
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
			case "sync":
				{
					Fetch();
				}
				break;
			}
		}
		
		public void Fetch()
		{
			if (_walletApiEnabled)
			{
				EB.Debug.Log("SparxWalletManager > Fetch > Async Calling Fetch");
				_api.Fetch(OnFetch);					
			}
		}
		
		public int Credit( int value, string reason ) 
		{
			if (_walletApiEnabled == false)
			{
				return 0;
			}
			
			return _api.Credit( value, reason, OnCredit );
		}
		
		public int Debit( int value, string reason )
		{
			if (_walletApiEnabled == false)
			{
				return 0;
			}
		
			return _api.Debit( value, reason, OnDebit );
		}
		
		void OnCredit( int id, string error, Hashtable data )
		{
			if (!string.IsNullOrEmpty(error))
			{
				FatalError(error);	
				return;
			}
			
			StoreBalance(data);	
			_config.Listener.OnCreditSuceeded(id);
		}
		
		void OnDebit( int id, string error, Hashtable data )
		{
			if (!string.IsNullOrEmpty(error))
			{
				_config.Listener.OnDebitFailed(id);	
				return;
			}
			
			StoreBalance(data);	
			
			_config.Listener.OnDebitSuceeded(id);
		}
			
		public void StoreBalance( Hashtable data )
		{
			if (_walletApiEnabled == false)
			{
				return;
			}
		
			EB.Debug.Log("SparxWalletManager > StoreBalance > data = " + EB.JSON.Stringify(data));
			_balance = Dot.Integer("balance", data, Balance );
			
			_config.Listener.OnBalanceUpdated(Balance);
		}

		void OnFetch( string error, Hashtable data )
		{
			if (!string.IsNullOrEmpty(error))
			{
				if (error == "walletDisabled")
				{
					EB.Debug.Log("SparxWalletManager disabled by server!");
					_walletApiEnabled = false;
				}
				else
				{
					FatalError(error);
					return;
				}
			}
			
			StoreBalance(data);	
			
			if ( State == SubSystemState.Connecting )
			{
				State = SubSystemState.Connected;
			}
		}
		
	}
}
