using UnityEngine;
using System.Collections;

public class LoginListener : EB.Sparx.DefaultLoginListener
{
	public override void OnEnumerate (EB.Sparx.Account[] accounts)
	{
		base.OnEnumerate(accounts);
	}

	public override void OnLoginFailed (string error)
	{
		base.OnLoginFailed(error);

		// re-enumerate
		SparxHub.Instance.LoginManager.Enumerate();
	}
	
	public override void OnDisconnected (string error)
	{
		base.OnDisconnected(error);

		// relogin
		SparxHub.Instance.LoginManager.Relogin();
	}

}
