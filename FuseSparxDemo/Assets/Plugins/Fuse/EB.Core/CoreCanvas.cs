using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace EB
{
	public partial class Canvas
	{
		public static void OpenURL(string url)
		{
			if (!string.IsNullOrEmpty(url))
			{
#if UNITY_WEBPLAYER && !UNITY_EDITOR
				url = url.Replace("http://", "https://");
				EB.Canvas.ExternalCall("showFacebox", url);
#else
				Application.OpenURL(url);
#endif
				EB.Debug.Log("EB.Canvas > OpenURL: " + url);
			}
		}

#if UNITY_WEBPLAYER
		private static string GetString(string cmd)
		{
			TextAsset obj = Resources.Load<TextAsset>("Canvas/" + cmd);
			if ((obj != null) && (obj.text.Length > 0))
			{
				return obj.text;
			}
			return cmd;
		}

		public static void ExternalEval(string cmd)
		{
			ExternalEval(cmd, null);
		}

		public static void ExternalEval(string cmd, params string[] args)
		{
			var str = GetString(cmd);
			if (args != null)
			{
				str = string.Format(str, args);
			}
			EB.Debug.Log("EB.Canvas > ExternalEval > Injecting: " + str);
			Application.ExternalEval(str);
		}

		public static void ExternalCall(string cmd, params string[] args)
		{
			var str = GetString(cmd);
			EB.Debug.Log("EB.Canvas > ExternalCall > Injecting: " + str + " with " + string.Join(" ", args));
			Application.ExternalCall(str, args);
		}

		#region Old_Rivets_Canvas_Setup
		public static void SetMotdCSS(string key, string value)
		{
			EB.Canvas.ExternalEval(string.Format("$('#motd-banner').css('{0}','{1}')", key, value));
		}

		static public void ShowMotd(string title, string uri, Hashtable query)
		{
			query = query ?? new Hashtable();
			query["stoken"] = SparxHub.Instance.ApiEndPoint.GetData("stoken");
			var url = SparxHub.Instance.ApiEndPoint.Url + uri + "?" + EB.QueryString.Stringify(query);
			url = url.Replace("http://", "https://");
			string cmd = ("$('#motd-banner').empty().load('" + url + "', ''," +
				"function(){ $('#motdNewLink').bind('click', {url:'"
				+ url + "&fullview=1'}, function(event){showFacebox(event.data.url);}) });");
			EB.Canvas.ExternalEval(cmd);
		}

		static public void SetupCanvas(int width = 800, int height = 600)
		{
			TextAsset canvasText = Resources.Load("Canvas/canvas-css", typeof(TextAsset)) as TextAsset;
			if ((canvasText != null) && !string.IsNullOrEmpty(canvasText.text))
			{
				Hashtable canvasTable = (Hashtable)EB.JSON.Parse(canvasText.text);

				// moko: set the game canvas style from json file
				Hashtable cssTable = EB.Dot.Object("css", canvasTable, null);
				if ((cssTable != null) && (cssTable.Count > 0))
				{
					string cssText = "$('body').css({";
					foreach (DictionaryEntry i in cssTable)
					{
						cssText += string.Format("'{0}': '{1}',", i.Key.ToString(), i.Value.ToString());
					}
					cssText += "});";
					EB.Debug.Log("Setup > SetupCanvas > Running CSS: " + cssText);
					EB.Canvas.ExternalEval(cssText);
				}

				// moko: set up fb game layout from json file
				List<FBScreen.Layout> layouts = new List<FBScreen.Layout>();
				Hashtable layoutTable = EB.Dot.Object("layout", canvasTable, null);
				if ((layoutTable != null) && (layoutTable.Count > 0))
				{
					foreach (DictionaryEntry i in layoutTable)
					{
						switch (i.Key.ToString())
						{
							case "topPadding":
							{
								layouts.Add(new FBScreen.Layout.OptionTop() { Amount = float.Parse(i.Value.ToString()) });
								break;
							}
							case "leftPadding":
							{
								layouts.Add(new FBScreen.Layout.OptionLeft() { Amount = float.Parse(i.Value.ToString()) });
								break;
							}
							case "centerHorizontal":
							{
								if (System.Boolean.Parse(i.Value.ToString()) == true)
								{
									layouts.Add(new FBScreen.Layout.OptionCenterHorizontal());
								}
								break;
							}
							case "centerVertical":
							{
								if (System.Boolean.Parse(i.Value.ToString()) == true)
								{
									layouts.Add(new FBScreen.Layout.OptionCenterVertical());
								}
								break;
							}
							default:
							{
								EB.Debug.LogError("Setup > SetupCanvas > Unregonized layout pairs: " + i.Key.ToString() + " = " + i.Value.ToString());
								break;
							}
						}
					}
				}

				Hashtable aspectTable = EB.Dot.Object("aspectRatio", canvasTable, null);
				if (aspectTable != null)
				{
					int w = EB.Dot.Integer("width", aspectTable, 4);
					int h = EB.Dot.Integer("height", aspectTable, 3);
					FB.Canvas.SetAspectRatio(w, h, layouts.ToArray());
					EB.Debug.Log("Setup > SetupCanvas > Set aspect ratio to : " + w + "x" + h + " - " + layouts.ToString());
				}

				Hashtable resTable = EB.Dot.Object("resolution", canvasTable, null);
				int canvasWidth = Screen.width;
				if (resTable != null)
				{
					int h = EB.Dot.Integer("height", resTable, height);
					canvasWidth = EB.Dot.Integer("width", resTable, width);
					FB.Canvas.SetResolution(canvasWidth, h, false, 0, layouts.ToArray());
					SetMotdCSS("width", canvasWidth.ToString());
					EB.Debug.Log("Setup > SetupCanvas > Set resolution to : " + canvasWidth + "x" + h + " - " + layouts.ToString());
				}

				// moko: insert img link for the banner
				var imglink = EB.Dot.String("banner-img", canvasTable, "");
				//if (!string.IsNullOrEmpty(imglink))
				{
					string injectionBanner = "$('<div id=\"motd-banner\"><img src=\"" + imglink + "\"/></div>').insertBefore($('#unityPlayerEmbed'))";
					EB.Debug.Log("Setup > SetupCanvas > Injecting Banner: " + injectionBanner);
					EB.Canvas.ExternalEval(injectionBanner);
				}

				// moko: set the banner style from json file
				Hashtable bannerTable = EB.Dot.Object("banner-css", canvasTable, null);
				if ((bannerTable != null) && (bannerTable.Count > 0))
				{
					foreach (DictionaryEntry i in bannerTable)
					{
						SetMotdCSS(i.Key.ToString(), i.Value.ToString());
					}
				}

				// moko: facebook 'like' button
				var appPage = EB.Dot.String("app-page", canvasTable, "");
				string injectionLike = 
					"$('#motd-banner').after(\"<div style='margin:0px auto 4px auto; width:" + canvasWidth + "px;'>" + 
					"<fb:like href='" + appPage + "' colorscheme='dark' layout='button_count' action='like' show_faces='true' share='true'></fb:like></div>\");";
				EB.Canvas.ExternalEval(injectionLike);
			}
		}
		#endregion
#endif
	}
}
