using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface WalletListener
	{
		void OnBalanceUpdated( int balance );	
		void OnCreditSuceeded( int id);
		void OnDebitFailed( int id );
		void OnDebitSuceeded( int id );
	}
	
}
