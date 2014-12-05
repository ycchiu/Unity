using UnityEngine;
using System;
using System.Collections;
using EB.Sparx;

public class SodaTestScreen : Window {

	private bool _visible = false;
	private GameObject _bomb;

	protected override void SetupWindow ()
	{
		base.SetupWindow ();

		_bomb = EB.Util.GetObjectExactMatch(gameObject, "Bomb");
		UIEventListener.Get (_bomb).onClick += delegate(GameObject go) {
			SparxHub.Instance.SodaManager.ShowUI();
		};

		UpdateBomb();

		GameObject closeScreenButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(closeScreenButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};
	}
	
	private void UpdateBomb()
	{
		string message = _visible ? "Bomb should be shown" : "Bomb should not be shown";
		_bomb.SetActive(_visible);
		EB.UIUtils.SetLabelContents(gameObject, "LabelResults", message);
	}

	void Update()
	{
		if (_visible != SparxHub.Instance.SodaManager.ShouldShowBomb)
		{
			_visible = SparxHub.Instance.SodaManager.ShouldShowBomb;
			Debug.Log(string.Format("Changing visible to: {0}", _visible));
			UpdateBomb();
		}
	}
}
