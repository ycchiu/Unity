using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class DefaultWalletListener : WalletListener
	{
		#region WalletListener implementation
		public void OnBalanceUpdated (int balance)
		{
			EB.Util.BroadcastMessage("OnBalanceUpdated", balance);
		}

		public void OnCreditSuceeded (int id)
		{
			EB.Util.BroadcastMessage("OnCreditSuceeded", id);
		}

		public void OnDebitFailed (int id)
		{
			EB.Util.BroadcastMessage("OnDebitFailed", id);
		}

		public void OnDebitSuceeded (int id)
		{
			EB.Util.BroadcastMessage("OnDebitSuceeded", id);
		}
		#endregion
		
	}
	
}

