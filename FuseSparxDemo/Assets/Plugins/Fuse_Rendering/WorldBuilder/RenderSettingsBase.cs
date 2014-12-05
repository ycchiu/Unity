using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public abstract class RenderSettingsBase : MonoBehaviour 
{
	//build-time light probles
	public float LightProbeOffset = 0.0f;
	public float LightProbeScale = 1.0f;
	public float LightProbeMax = 1.0f;

	public virtual void Clone(RenderSettingsBase toClone)
	{
		LightProbeOffset = toClone.LightProbeOffset;
		LightProbeScale = toClone.LightProbeScale;
		LightProbeMax = toClone.LightProbeMax;
	}

	protected abstract void ApplyAtSceneLoad();
	protected abstract void ApplyEveryFrame();

	public virtual void Start()
	{
		ApplyAtSceneLoad();
	}

	void Update()
	{
		ApplyEveryFrame();
	}

}
