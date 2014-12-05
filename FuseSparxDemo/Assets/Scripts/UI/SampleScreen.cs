using UnityEngine;
using System.Collections;

public class SampleScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();
		
		GameObject motdButton = EB.Util.GetObjectExactMatch(gameObject, "MOTD");
		GameObject motdInteractive = EB.Util.FindComponent<BoxCollider>(motdButton).gameObject;
		UIEventListener.Get(motdInteractive).onClick += OnMOTDClicked;
		
		GameObject webViewButton = EB.Util.GetObjectExactMatch(gameObject, "WebView");
		GameObject webViewInteractive = EB.Util.FindComponent<BoxCollider>(webViewButton).gameObject;
		UIEventListener.Get(webViewInteractive).onClick += OnWebViewClicked;
	}
	
	protected override void WindowReady()
	{
		if( SparxHub.Instance.MOTDManager.IsActive == true ) {
			string url = SparxHub.Instance.MOTDManager.Url;
			SparxHub.Instance.WebViewManager.OpenWebPopup( url, 0.1f, 0.1f, 0.8f, 0.8f );
		}

		ShowWindow();
	}

	private void OnMOTDClicked(GameObject caller)
	{
		SparxHub.Instance.MOTDManager.Sync (delegate( bool active, string url ) {
			SparxHub.Instance.WebViewManager.OpenWebPopup (url, 0.1f, 0.1f, 0.8f, 0.8f);
		});
	}
	
	private void OnWebViewClicked(GameObject caller)
	{
		SparxHub.Instance.WebViewManager.OpenTabbedWebView( "messages" );
	}
}
