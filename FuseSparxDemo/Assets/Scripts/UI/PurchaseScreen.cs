using UnityEngine;
using System.Collections;

public class PurchaseScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();
		
		GameObject motdButton = EB.Util.GetObjectExactMatch(gameObject, "Purchase");
		GameObject motdInteractive = EB.Util.FindComponent<BoxCollider>(motdButton).gameObject;
		UIEventListener.Get(motdInteractive).onClick += OnPurchase;
	}

	private void OnPurchase(GameObject caller)
	{
		var payouts = SparxHub.Instance.PaymentsManager.Payouts;
		if (payouts.Length > 0 )
		{
			SparxHub.Instance.PaymentsManager.PurchaseOffer(payouts[0]);
		}
	}
}
