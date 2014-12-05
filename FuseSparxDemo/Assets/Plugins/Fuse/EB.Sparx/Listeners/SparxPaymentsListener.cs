using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface PaymentsListener
	{
		void OnOffersFetched();
		void OnOfferPurchaseFailed(string error);
		void OnOfferPurchaseSuceeded(EB.IAP.Item item);
		void OnOfferPurchaseCanceled();
	}
	
}
