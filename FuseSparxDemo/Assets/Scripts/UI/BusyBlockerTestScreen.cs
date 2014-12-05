using UnityEngine;
using System.Collections;

public class BusyBlockerTestScreen : Window
{
	int fillMax;
	UISprite fill, onBlockerLoadedFill, onBlockerUnloadedFill;
	float clickTime = -1f;
	float lastUnloadedTime = -1f;
	float lastLoadedTime = -1f;
	
	protected override void SetupWindow()
	{
		base.SetupWindow();

		string transactionPrefix = "Button_Trans_";
		GameObject[] transactionButtons = EB.Util.GetObjects(gameObject, transactionPrefix);
		foreach (GameObject btn in transactionButtons)
		{
			float time;
			if (float.TryParse(btn.name.Substring(transactionPrefix.Length), out time))
			{
				UIEventListener eventListener = EB.Util.FindComponent<UIEventListener>(btn);
				eventListener.onClick = delegate(GameObject go) {
					HandleClick();
					BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
					if (time == 0f)
					{
						BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
					}
					else
					{
						EB.Coroutines.SetTimeout(delegate() {
							BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
						}, Mathf.RoundToInt(time * 1000f));
					}
				};
			}
		}

		string animationPrefix = "Button_Anim_";
		GameObject[] animationButtons = EB.Util.GetObjects(gameObject, animationPrefix);
		foreach (GameObject btn in animationButtons)
		{
			float time;
			if (float.TryParse(btn.name.Substring(animationPrefix.Length), out time))
			{
				UIEventListener eventListener = EB.Util.FindComponent<UIEventListener>(btn);
				eventListener.onClick = delegate(GameObject go) {
					HandleClick();
					BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.UiAnimation);
					if (time == 0f)
					{
						BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.UiAnimation);
					}
					else
					{
						EB.Coroutines.SetTimeout(delegate() {
							BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.UiAnimation);
						}, Mathf.RoundToInt(time * 1000f));
					}
				};
			}
		}

		string nonBlockingPrefix = "Button_NonBlocking_";
		GameObject[] nonBlockingButtons = EB.Util.GetObjects(gameObject, nonBlockingPrefix);
		foreach (GameObject btn in nonBlockingButtons)
		{
			float time;
			if (float.TryParse(btn.name.Substring(nonBlockingPrefix.Length), out time))
			{
				UIEventListener eventListener = EB.Util.FindComponent<UIEventListener>(btn);
				eventListener.onClick = delegate(GameObject go) {
					HandleClick();
					BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.NonBlockingCommunication);
					if (time == 0f)
					{
						BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.NonBlockingCommunication);
					}
					else
					{
						EB.Coroutines.SetTimeout(delegate() {
							BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.NonBlockingCommunication);
						}, Mathf.RoundToInt(time * 1000f));
					}
				};
			}
		}
		
		GameObject advBtn = EB.Util.GetObjectExactMatch(gameObject, "Button_Advanced_1");
		UIEventListener advEventListener = EB.Util.FindComponent<UIEventListener>(advBtn);
		advEventListener.onClick = delegate(GameObject go) {
			HandleClick();
			BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			}, Mathf.RoundToInt(5000));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.UiAnimation);
			}, Mathf.RoundToInt(5500));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.UiAnimation);
			}, Mathf.RoundToInt(10000));
		};
		
		advBtn = EB.Util.GetObjectExactMatch(gameObject, "Button_Advanced_2");
		advEventListener = EB.Util.FindComponent<UIEventListener>(advBtn);
		advEventListener.onClick = delegate(GameObject go) {
			HandleClick();
			BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			}, Mathf.RoundToInt(5000));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.FileLoad);
			}, Mathf.RoundToInt(5500));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.FileLoad);
			}, Mathf.RoundToInt(11500));
		};
		
		advBtn = EB.Util.GetObjectExactMatch(gameObject, "Button_Advanced_3");
		advEventListener = EB.Util.FindComponent<UIEventListener>(advBtn);
		advEventListener.onClick = delegate(GameObject go) {
			HandleClick();
			BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.ServerTransaction);
			}, Mathf.RoundToInt(5000));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.AddBlocker(BusyBlockerManager.BlockerFlag.FileLoad);
			}, Mathf.RoundToInt(3000));
			EB.Coroutines.SetTimeout(delegate() {
				BusyBlockerManager.Instance.RemoveBlocker(BusyBlockerManager.BlockerFlag.FileLoad);
			}, Mathf.RoundToInt(9000));
		};
		
		GameObject progressFill = EB.Util.GetObjectExactMatch(gameObject, "ProgressFill");
		fill = EB.Util.FindComponent<UISprite>(progressFill);
		fillMax = fill.width;
		
		GameObject onBlockerLoadedDisplay = EB.Util.GetObjectExactMatch(gameObject, "onBlockerLoadedFill");
		onBlockerLoadedFill = EB.Util.FindComponent<UISprite>(onBlockerLoadedDisplay);

		GameObject onBlockerUnloadedDisplay = EB.Util.GetObjectExactMatch(gameObject, "onBlockerUnloadedFill");
		onBlockerUnloadedFill = EB.Util.FindComponent<UISprite>(onBlockerUnloadedDisplay);

		BusyBlockerManager.Instance.onBlockerLoaded += onBlockerLoaded;
		BusyBlockerManager.Instance.onBlockerUnloaded += onBlockerUnloaded;
	}

	protected override void TeardownWindow()
	{
		base.TeardownWindow();

		BusyBlockerManager.Instance.onBlockerLoaded -= onBlockerLoaded;
		BusyBlockerManager.Instance.onBlockerUnloaded -= onBlockerUnloaded;
	}

	private void HandleClick()
	{
		fill.width = fillMax;
		clickTime = Time.realtimeSinceStartup;
	}
	
	private void onBlockerLoaded()
	{
		onBlockerLoadedFill.width = fillMax;
		lastLoadedTime = Time.realtimeSinceStartup;
	}
	
	private void onBlockerUnloaded()
	{
		onBlockerUnloadedFill.width = fillMax;
		lastUnloadedTime = Time.realtimeSinceStartup;
	}

	private void Update()
	{
		if (clickTime > 0f)
		{
			float progress = Mathf.Clamp(Time.realtimeSinceStartup - clickTime, 0f, 1f);
			fill.width = Mathf.RoundToInt(fillMax * progress);
		}
		if (lastLoadedTime > 0f)
		{
			float progress = Mathf.Clamp(Time.realtimeSinceStartup - lastLoadedTime, 0f, 1f);
			onBlockerLoadedFill.width = Mathf.RoundToInt(fillMax * progress);
		}
		if (lastUnloadedTime > 0f)
		{
			float progress = Mathf.Clamp(Time.realtimeSinceStartup - lastUnloadedTime, 0f, 1f);
			onBlockerUnloadedFill.width = Mathf.RoundToInt(fillMax * progress);
		}
	}
}
