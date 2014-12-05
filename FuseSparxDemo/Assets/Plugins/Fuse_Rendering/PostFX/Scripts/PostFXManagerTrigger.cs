using UnityEngine;
using EB.Rendering;

namespace EB.Rendering
{
	[ExecuteInEditMode]
	public class PostFXManagerTrigger : MonoBehaviour
	{
		public void Start()
		{
			//turn off the warp layer
			this.camera.cullingMask &= ~(1 << LayerMask.NameToLayer("Warp"));
		}

		public void OnRenderImage(RenderTexture src, RenderTexture dst)
		{
			PostFXManager.Instance.PostRender(this.camera, src, dst);
		}
	}
}