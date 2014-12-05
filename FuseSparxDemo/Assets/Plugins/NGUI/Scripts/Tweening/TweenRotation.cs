//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Tween the object's rotation.
/// </summary>

[AddComponentMenu("NGUI/Tween/Tween Rotation")]
public class TweenRotation : UITweener
{
	public Vector3 from;
	public Vector3 to;
	// EBG START - lerpAsAngles set to false by default.  Set to true if you are seeing undesired 360-degree spin
	public bool lerpAsAngles;
	// EBG END

	Transform mTrans;

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	[System.Obsolete("Use 'value' instead")]
	public Quaternion rotation { get { return this.value; } set { this.value = value; } }

	/// <summary>
	/// Tween's current value.
	/// </summary>

	public Quaternion value { get { return cachedTransform.localRotation; } set { cachedTransform.localRotation = value; } }

	/// <summary>
	/// Tween the value.
	/// </summary>

	protected override void OnUpdate (float factor, bool isFinished)
	{
		// EBG START - use LerpAngle instead as these are degrees
		if (lerpAsAngles)
		{
			value = Quaternion.Euler(new Vector3(
				Mathf.LerpAngle(from.x, to.x, factor),
				Mathf.LerpAngle(from.y, to.y, factor),
				Mathf.LerpAngle(from.z, to.z, factor)));
		}
		else
		{
			value = Quaternion.Euler(new Vector3(
				Mathf.Lerp(from.x, to.x, factor),
				Mathf.Lerp(from.y, to.y, factor),
				Mathf.Lerp(from.z, to.z, factor)));
		}
		// EBG END
	}

	/// <summary>
	/// Start the tweening operation.
	/// </summary>

	static public TweenRotation Begin (GameObject go, float duration, Quaternion rot)
	{
		TweenRotation comp = UITweener.Begin<TweenRotation>(go, duration);
		comp.from = comp.value.eulerAngles;
		comp.to = rot.eulerAngles;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}

	[ContextMenu("Set 'From' to current value")]
	public override void SetStartToCurrentValue () { from = value.eulerAngles; }

	[ContextMenu("Set 'To' to current value")]
	public override void SetEndToCurrentValue () { to = value.eulerAngles; }

	[ContextMenu("Assume value of 'From'")]
	void SetCurrentValueToStart () { value = Quaternion.Euler(from); }

	[ContextMenu("Assume value of 'To'")]
	void SetCurrentValueToEnd () { value = Quaternion.Euler(to); }
}
