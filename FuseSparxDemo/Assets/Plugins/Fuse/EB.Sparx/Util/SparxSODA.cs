using UnityEngine;
using System.Collections;

public class SparxSODA : Kabam.KabamSODA
{
	public class Reward
	{
		public string Signature { get; private set; }
		public string Receipt { get; private set; }

		public Reward(object json)
		{
			Signature = EB.Dot.String("signature", json, string.Empty);
			Receipt = EB.Dot.String("receipt", json, string.Empty);		
		}
	}

	public EB.Action<bool> 		OnVisibilityChanged;
	public EB.Action<Reward> 	OnRewardRedeemed;
	public EB.Action			OnCertificateExpired;



	// Use this for initialization
	public void Init(string clientId, string mobileKey, string wskeUrl) 
	{
		this.SODAConfig(clientId, mobileKey, wskeUrl);
		this.SODAInit();
	}


	protected override void SODAOnReward (string message)
	{
		try 
		{
			var obj = EB.JSON.Parse(message);
			var reward = new Reward(obj);
			if (OnRewardRedeemed != null)
			{
				OnRewardRedeemed(reward);
			}
		}
		catch {}
	
	}

	protected override void SODAOnVisibilityChange(string message) 
	{
		base.SODAOnVisibilityChange(message);

		Debug.Log ("Kabam SODA Visibility Changed: " + message);
		var visible = false;
		try 
		{
			var obj = EB.JSON.Parse(message);
			visible = EB.Dot.Bool("visible", obj, visible);
		}
		catch {}
		
		if (this.OnVisibilityChanged != null)
		{
			this.OnVisibilityChanged(visible);
		}

	}

	protected override void SODAOnCertificateExpired(string message) 
	{
		Debug.Log ("Kabam SODA Player Certificate Expired: " + message);
		if (this.OnCertificateExpired != null)
		{
			this.OnCertificateExpired();
		}
	}

}
