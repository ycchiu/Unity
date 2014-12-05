using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class SodaManager : SubSystem
	{
		public class RedeemReport
		{
			public RedeemReport( Hashtable data = null )
			{
				this.TransactionID = string.Empty;
				List< RedeemerItem > rewards = new List< RedeemerItem >();
				if( data != null )
				{
					this.TransactionID = EB.Dot.String( "transactionId", data, string.Empty );
					ArrayList rewardList = EB.Dot.Array("rewards", data, new ArrayList() );
					foreach( object candidate in rewardList )
					{
						Hashtable reward = candidate as Hashtable;
						if( reward != null )
						{
							RedeemerItem redeemer = new RedeemerItem(reward);
							if(redeemer.IsValid)
							{
								rewards.Add(redeemer);
							}
						}
					}
				}
				this.Rewards = rewards.ToArray();
			}
			
			public override string ToString()
			{
				return string.Format("TransactionID:{0} Rewards:{1}", this.TransactionID, this.Rewards.Length );
			}
			
			public bool Success
			{
				get
				{
					return ( string.IsNullOrEmpty( this.TransactionID ) == false );
				}
			}
			
			public readonly string TransactionID;
			public readonly RedeemerItem[] Rewards;
		}
		
		public bool ShouldShowBomb { get; private set; }

		private SodaAPI _api = null;
		private SparxSODA _plugin;
		
		private string _playerId = string.Empty;

		public override void Initialize (Config config)
		{
			_api = new SodaAPI( Hub.ApiEndPoint );
		}

		public override void Connect ()
		{
			var data = Dot.Object("wske", Hub.LoginManager.LoginData, null);
			if (data != null)
			{
				if (_plugin == null)
				{
					_plugin = new UnityEngine.GameObject("wske_sdk", typeof(SparxSODA)).GetComponent<SparxSODA>();
					GameObject.DontDestroyOnLoad(_plugin.gameObject);
					_plugin.OnCertificateExpired = this.OnCertificateExpired;
					_plugin.OnRewardRedeemed = this.OnRewardRedeemed;
					_plugin.OnVisibilityChanged = this.OnVisibilityChanged;
					_plugin.Init(Dot.String("clientId", data, string.Empty), Dot.String("mobileKey", data, string.Empty), Dot.String("wskeUrl", data, string.Empty));
				}

				_playerId = Dot.String("playerId", data, string.Empty);
				string certificate = Dot.String("playerCertificate", data, string.Empty);
				_plugin.SODALogin( _playerId, certificate, null, null );
			}
			State = SubSystemState.Connected;
		}

		public override void Disconnect (bool isLogout)
		{

		}

		public void ShowUI ()
		{
			_plugin.SODAStartGUI ();
		}

		void OnRewardRedeemed (SparxSODA.Reward reward)
		{		
			_api.RedeemLoyaltyReward( reward, delegate(string err, Hashtable data) {
				if( string.IsNullOrEmpty( err ) == false )
				{
					EB.Debug.LogError( "WSKE Reward Redeem Failed. Error:{0}", err );
				}
				else
				{
					RedeemReport report = new RedeemReport( data );
					if( report.Success == true )
					{
						if( _plugin != null )
						{
							_plugin.SODAFulfillReward( report.TransactionID );
						}
					}
					else
					{
						EB.Debug.LogError( "WSKE Not reporting an error, but wasn't successful." );
					}
				}
			});
		}

		void OnVisibilityChanged( bool showBomb )
		{
			ShouldShowBomb = showBomb;
		}

		void OnCertificateExpired()
		{
			_api.GenerateCertificate( delegate(string err, string certificate) {
				if( ( string.IsNullOrEmpty( certificate ) == false ) && ( _plugin != null ) )
				{
					_plugin.SODALogin( _playerId, certificate, null, null );
				}
			});
		}
	}
}
