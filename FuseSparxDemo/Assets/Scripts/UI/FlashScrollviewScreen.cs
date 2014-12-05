using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlashScrollviewScreen : Window
{
	protected override void SetupWindow()
	{
		base.SetupWindow();

		// Cache references to the scroll view
		GameObject scroll = EB.Util.GetObjectExactMatch(gameObject, "ScrollView");
		_scrollGrid = scroll.GetComponent<UIGrid>();
		_scrollView = scroll.GetComponent<UIScrollView>();

		// Add event handler to CloseScreenButton so that it closes this window.
		GameObject closeButton = EB.Util.GetObjectExactMatch(gameObject, "CloseScreenButton");
		GameObject interactive = EB.Util.FindComponent<BoxCollider>(closeButton).gameObject;
		UIEventListener.Get(interactive).onClick += delegate(GameObject go) {
			CloseWindow();
		};

		// Cache information labels as we will be modifying their text based on user input
		pressedLabel = EB.Util.GetObjectExactMatch(gameObject, "DynamicLabel").GetComponent<UILabel>();
		imageLabel = EB.Util.GetObjectExactMatch(gameObject, "DynamicLabelImage").GetComponent<UILabel>();

		// Create multiple instances of the scrolltestitem flash asset.  Each one will potentially display something different.
		int count = 10;
		for (int i = 0; i < count; ++i)
		{
			int iconIndex = Random.Range(0, 100) < 50 ? 0 : 1;
			GameObject go = NGUITools.AddChild(_scrollGrid.gameObject);
			go.name = "scrolltestitem_" + i.ToString("D2") + "_" + items[iconIndex];
			go.AddComponent<UIDragScrollView>().scrollView = _scrollView;
			UILWFObject lwf = go.AddComponent<UILWFObject>();
			// Set the LWF data path, this points to the .bytes file you want to load
			lwf.path = "UI/FlashAssets/scrolltestitem.lwfdata/scrolltestitem";
			// Set the LWF font adapter that LWF uses to interface with the game's font system. See SampleFlashFontAdapter for more details.
			lwf.fontAdapter = SampleFlashFontAdapter.Instance;
			// Set the LWF texture adapter that LWF uses to interface with the game's texture loading system. See SampleFlashTextureAdapter for more details.
			lwf.textureAdapter = SampleFlashTextureAdapter.Instance;
			lwfs.Add(lwf);
			lwfIndices.Add(iconIndex);
			isLoaded.Add(false);
		}
	}

	protected override void OnIntroTransitionComplete ()
	{
		base.OnIntroTransitionComplete();
		// Start a coroutine that will wait for all LWF instances to complete initialiation.  Once this is done, trigger their initialization animations in sequential order.
		StartCoroutine(InitializeLWFAnimations());
	}

	IEnumerator InitializeLWFAnimations()
	{
		while (true)
		{
			bool isIniting = false;
			for (int i = 0; i < lwfs.Count; ++i)
			{
				if (!isLoaded[i])
				{
					isIniting = true;
					isLoaded[i] = InitializeClip(i);
				}
			}

			if (!isIniting)
			{
				break;
			}

			yield return new WaitForFixedUpdate();
		}

		// All LWF clips have loaded, refresh the scroll grid
		_scrollGrid.Reposition();

		// Trigger intro animation sequences one-by-one
		for (int i = 0; i < lwfs.Count; ++i)
		{
			UILWFObject lwf = lwfs[i];
			int iconIndex = lwfIndices[i];
			LWF.Movie mcBackground = lwf.lwfObject.lwf.SearchMovieInstance("background_frame");
			LWF.Movie mcIcon = lwf.lwfObject.lwf.SearchMovieInstance("icon");
			while (mcIcon.playing)
			{
				yield return new WaitForFixedUpdate();
			}
			mcBackground.GotoAndPlay ("intro");

			LWF.Movie mcIconPart = mcIcon.SearchMovieInstance("icon_" + items[iconIndex]);
			if (mcIconPart != null)
			{
				mcIconPart.GotoAndPlay("intro");
			}

			yield return new WaitForSeconds(0.1f);
		}
	}
	
	bool InitializeClip(int i)
	{
		UILWFObject lwf = lwfs[i];
		if (lwf.lwfObject != null && !isLoaded[i])
		{
			LWF.Movie iconMovie = lwf.lwfObject.lwf.rootMovie.SearchMovieInstance("icon");
			if (iconMovie != null)
			{
				int lwfIndex = lwfIndices[i];
				lwf.lwfObject.AddButtonEventHandler(instanceName: "btn_icon", release: delegate(LWF.Button button)
            	{
					LWF.Movie backgroundMovie = button.lwf.rootMovie.SearchMovieInstance("background_frame");
					LWF.Movie subIconMovie = button.lwf.rootMovie.SearchMovieInstance("icon_" + items[lwfIndex]);
					if ((backgroundMovie != null && backgroundMovie.playing) || (subIconMovie != null && subIconMovie.playing))
					{
						return;
					}

					if (subIconMovie != null)
					{
						subIconMovie.GotoAndPlay("animate");
					}
					if (backgroundMovie != null)
					{
						backgroundMovie.GotoAndPlay("animate");
					}

					pressedLabel.text = i.ToString();

					// The following demonstrates how one can reach inside a bitmap within the flash animation, and replace it with another texture.
					LWF.Bitmap bitmap = subIconMovie.GetBitmap();
					if (bitmap != null)
					{
						LWF.NGUIRenderer.BitmapRenderer br = bitmap.renderer as LWF.NGUIRenderer.BitmapRenderer;
						if (br != null)
						{
							int chance = Random.Range (0, 32);
							if (chance == 0)
							{
								// This restores the bitmap to its original state
								br.LoadDefault();
							}
							else
							{
								// this will eventually make a call to our SampleFlashTextureAdapter.LoadTexture
								br.LoadTexture("UI/Streaming/" + chance);
							}
							imageLabel.text = br.texture.name;
						}
					}
				});

				iconMovie.GotoAndPlay(items[lwfIndex]);
				return true;
			}
		}
		return false;
	}

	// Private member variables

	// LWF scroll elements
	private static string [] items = { "chassis", "engine" };//, "exhaust" };//, "gearbox", "nos", "suspension", "tire" };
	private UIScrollView _scrollView;
	private UIGrid _scrollGrid;
	private List<UILWFObject> lwfs = new List<UILWFObject>();
	private List<int> lwfIndices = new List<int>();
	private List<bool> isLoaded = new List<bool>();
	
	// press label variables
	private UILabel pressedLabel;
	private UILabel imageLabel;
}
