using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class DefaultPaymentsListener : PaymentsListener
	{
		#region PaymentsListener implementation
		public void OnOffersFetched ()
		{
			EB.Util.BroadcastMessage("OnOffersFetched");
		}

		public void OnOfferPurchaseFailed (string error)
		{
			EB.Util.BroadcastMessage("OnOfferPurchaseFailed", error);
		}

		public void OnOfferPurchaseSuceeded (EB.IAP.Item item)
		{
			EB.Util.BroadcastMessage("OnOfferPurchaseSuceeded", item);
		}

		public void OnOfferPurchaseCanceled ()
		{
			EB.Util.BroadcastMessage("OnOfferPurchaseCanceled");
		}
		#endregion
		
	}
	
}

