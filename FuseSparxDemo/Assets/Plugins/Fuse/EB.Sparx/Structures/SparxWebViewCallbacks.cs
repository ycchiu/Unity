using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class WebViewCallbacks : MonoBehaviour 
	{
		void Awake () 
		{
			DontDestroyOnLoad(this.gameObject);
		}
		
		void WebViewDidShow()
		{
			if( Camera.main != null )
			{
				Camera.main.SendMessage( "DisableInput" );
				Camera.main.SendMessage( "DisableRender" );
			}
		}
		
		void WebViewWillHide()
		{
			if( Camera.main != null )
			{
				Camera.main.SendMessage("EnableRender");
				Camera.main.SendMessage("EnableInput");
			}
		}
		
		void OnPopupUrl(string url)
		{
		}
	}
}
