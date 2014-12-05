using UnityEngine;
using System.Collections;

public class FlashAnimationScreen : Window {

	protected override void SetupWindow()
	{
		base.SetupWindow();

		// Create a GameObject that contains a UILWFObject component, and add it as a child under another GameObject called 'FlashContainer'.
		GameObject go = NGUITools.AddChild(EB.Util.GetObjectExactMatch(gameObject, "FlashContainer"));
		go.name = "FlashWidget";
		mLWFObject = go.AddComponent<UILWFObject>();
		// Set the LWF data path.  This points to the .bytes file you want to load
		mLWFObject.path = "UI/FlashAssets/animwithbutton.lwfdata/animwithbutton";
		// Set the LWF font adapter that LWF uses to interface with the game's font system. See SampleFlashFontAdapter for more details.
		mLWFObject.fontAdapter = SampleFlashFontAdapter.Instance;
		// Set the LWF texture adapter that LWF uses to interface with the game's texture loading system. See SampleFlashTextureAdapter for more details.
		mLWFObject.textureAdapter = SampleFlashTextureAdapter.Instance;

		// The entirety of the movie clip may not have been loaded yet, so we will start a coroutine.
		StartCoroutine(WaitForLWF());
	}

	IEnumerator WaitForLWF()
	{
		// Poll for the existence for an LWF.LWF object within the UILWFObject component.
		while (mLWFObject.lwfObject == null)
		{
			yield return new WaitForFixedUpdate();
		}

		// Register a button event handler, corresponding to the button named 'btn'.  This button corresponds to a Button in the FLA that must be named 'btn'.
		mLWFObject.lwfObject.lwf.AddButtonEventHandler(instanceName: "btn", release: delegate(LWF.Button button)
		{
			StartCoroutine(PrepareToCloseWindow(button));
		});

		yield break;
	}

	IEnumerator PrepareToCloseWindow(LWF.Button button)
	{
		// Tell the root movie clip to search for a child movie clip named 'buttonMC', and then instruct it to goto the frame 'animate' and play.
		LWF.Movie buttonMC = button.lwf.SearchMovieInstance("buttonMC");
		buttonMC.GotoAndPlay("animate");

		// Poll for animation completion... this relies on the FLA being set up correctly to stop() at the final state of the animation, instead of looping.
		while (buttonMC.playing)
		{
			yield return new WaitForFixedUpdate();
		}

		CloseWindow();
		yield break;
	}

	// Private member variables
	private UILWFObject mLWFObject;

}
