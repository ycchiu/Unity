using UnityEngine;

/// <summary>
/// Trivial script that fills the label's contents gradually, as if someone was typing.
/// </summary>

[RequireComponent(typeof(UILabel))]
[AddComponentMenu("EB UI/Typewriter")]
public class TypeWriter : MonoBehaviour
{
	public int charsPerSecond = 40;
	public bool staticallySized = true;
	public float endOfLinePauseFactor = 4f;

	void Awake()
	{
		ALPHA_OUT_STRING = "[" + NGUIText.EncodeColor(ALPHA_OUT_COLOR, true) + "]";
	}

	void Start()
	{
		CacheLabel();
	}

	private void CacheLabel()
	{
		mLabel = GetComponent<UILabel>();
		if (staticallySized)
		{
			mLabel.supportEncoding = true;
		}
		mText = mLabel.text;
	}

	void Update()
	{
		if(!mDestroyMe && !mDestroyed)
		{
			if (mLabel == null)
			{
				CacheLabel();
			}
			
			if (mOffset < mText.Length)
			{
				if (mNextChar <= Time.time)
				{
					charsPerSecond = Mathf.Max(1, charsPerSecond);
					
					// Periods and end-of-line characters should pause for a longer time.
					float delay = 1f / charsPerSecond;
					char c = mText[mOffset];
					if (c == '.' || c == '\n' || c == '!' || c == '?') delay *= endOfLinePauseFactor;
					
					mNextChar = Time.time + delay;

					int offset = 1;
					if(delay < Time.deltaTime)
					{
						offset = Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime/delay));
					}
					int prevOffset = mOffset;
					mOffset += offset;

					// handle color encoding
					if (mLabel.supportEncoding)
					{
						// Did we encounter an open bracket?
						int symbolIndex = mText.IndexOf('[', prevOffset, mOffset - prevOffset);
						if (symbolIndex >= 0)
						{
							// if we did, we must read it entirely and then skip ahead
							NGUIText.ParseSymbol(mText, ref symbolIndex);
							mOffset = symbolIndex;
						}
					}
					
					if (staticallySized)
					{
						int len = Mathf.Min(mOffset, mText.Length);
						// achieve static sizing of the label by merely alpha-ing out the printed font that remains to be typed in.
						if (len < mText.Length)
						{
							string modString = mText.Insert(len, ALPHA_OUT_STRING);
							
							// need to go through and remove all other encoding symbols
							len += ALPHA_OUT_STRING.Length;
							string end = NGUIText.StripSymbols(modString.Substring(len));
							mLabel.text = modString.Substring(0,len) + end;
						}
						else
						{
							mLabel.text = mText;
						}
					}
					else
					{
						mLabel.text = mText.Substring(0, Mathf.Min(mOffset, mText.Length));
					}
				}
			}
			else 
			{
				mDestroyMe = true;
			}
		}

		if(mDestroyMe && !mDestroyed)
		{
			mDestroyed = true;
			Destroy(this);
		}
	}
	
	public void Complete()
	{
		if(!string.IsNullOrEmpty(mText) && mOffset < mText.Length && mLabel != null)
		{
			mLabel.text = mText;
			mOffset = mText.Length;
		}
		mDestroyMe = true; // destroy on the next frame in case we get a double destroy
	}


	private UILabel mLabel;
	private string mText = string.Empty;
	private int mOffset = 0;
	private float mNextChar = 0f;
	
	private bool mDestroyMe = false;
	private bool mDestroyed = false;

	private static string ALPHA_OUT_STRING = string.Empty;

	private static Color ALPHA_OUT_COLOR = new Color(0,0,0,0);
}
