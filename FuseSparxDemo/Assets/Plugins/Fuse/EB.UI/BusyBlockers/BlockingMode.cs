// #define DEBUG_BLOCKING_MODE_CLASS
using UnityEngine;
using BlockerFlag = BusyBlockerManager.BlockerFlag;

namespace EB.UI
{
	public class BlockingMode : MonoBehaviour, UIDependency
	{
		public bool IsBlockingState;
		
		public enum DisplayState
		{
			Hidden,
			Delaying,
			AnimatingIn,
			Showing,
			AnimatingOut,
		}
		
		public EB.Action<DisplayState> OnDisplayStateChange;
		
		/////////////////////////////////////////////////////////////////////////
		#region UIDependency Implementation
		/////////////////////////////////////////////////////////////////////////
		public EB.Action onReadyCallback
		{
			get
			{
				return _onReadyCallback;
			}
			set
			{
				_onReadyCallback = value;
			}
		}
		private EB.Action _onReadyCallback;
		
		public EB.Action onDeactivateCallback
		{
			get
			{
				return _onDeactivateCallback;
			}
			set
			{
				_onDeactivateCallback = value;
			}
		}
		private EB.Action _onDeactivateCallback;
		
		public bool IsReady()
		{
			return isReady;
		}
		private bool isReady = false;

		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////

		/////////////////////////////////////////////////////////////////////////
		#region currentDisplayState
		/////////////////////////////////////////////////////////////////////////
		public DisplayState currentDisplayState
		{
			get
			{
				return _currentDisplayState;
			}
			private set
			{
				if (_currentDisplayState != value)
				{
					_currentDisplayState = value;
					if (OnDisplayStateChange != null)
					{
						OnDisplayStateChange(value);
					}
				}
			}
		}
		private DisplayState _currentDisplayState = DisplayState.Hidden;
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		private UITransition uiTransition;
		private UILabel reasonLabel;
		private string pendingTransition;
		private float delayRemaining;
		private BoxCollider blockerCollider;
		private BusyBlocker.BlockingState blockingState;
		private BlockerFlag blockerReason = BlockerFlag.UiAnimation;
		private bool referencesCached = false;
		private int lastTransitionFrame = -1;

		/////////////////////////////////////////////////////////////////////////
		#region Public Interface
		/////////////////////////////////////////////////////////////////////////
		public void SetState(BusyBlocker.BlockingState state)
		{
			if (!referencesCached)
			{
				EB.Debug.LogError("Blocking Mode not ready yet!");
				return;
			}
			Report("SetState: " + state.ToString());

			// Reset pending transition state for this new update.
			pendingTransition = null;
			blockingState = state;
			
			if (IsBlockingState == state.IsNonBlocking ||
				!state.IsBlockerVisual ||
				!state.IsBlockerActive)
			{
				Hide();
			}
			else
			{
				Show(state);
			}

			UpdateRootObjectAndCollider();
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////////////////////////////////////////////
		#region Overridable Implementation
		/////////////////////////////////////////////////////////////////////////
		protected virtual void UpdateBlockerReason(BlockerFlag flag)
		{
			if (reasonLabel == null)
			{
				return;
			}
			
			if (blockerReason != flag)
			{
				blockerReason = flag;
				reasonLabel.text = "ID_UI_BLOCKING_REASON_" + flag.ToString().ToUpper();
			}
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////////////////////////////////////////////
		#region Private Implementation
		/////////////////////////////////////////////////////////////////////////
		private void UpdateRootObjectAndCollider()
		{
			bool isVisibleBlockerUp = (currentDisplayState != DisplayState.Hidden);

			if (IsBlockingState)
			{
				bool isInvisibleBlockerUp = (!blockingState.IsBlockerVisual && !blockingState.IsNonBlocking && blockingState.IsBlockerActive);

				blockerCollider.enabled = isVisibleBlockerUp || isInvisibleBlockerUp;
				gameObject.SetActive(blockerCollider.enabled);
			}
			else
			{
				gameObject.SetActive(isVisibleBlockerUp);
			}

			Report("UpdateRootObjectAndCollider: " + gameObject.name + " " + gameObject.activeSelf);
		}
		
		private void Show(BusyBlocker.BlockingState state)
		{
			Report("Show()");
			switch (currentDisplayState)
			{
				case DisplayState.Delaying:
				case DisplayState.Hidden:
				case DisplayState.AnimatingOut:
				{
					AnimateIn();
					break;
				}
				case DisplayState.AnimatingIn:
				case DisplayState.Showing:
				{
					return;
				}
			}
			
			UpdateBlockerReason(state.ActiveBlockerFlag);
		}
		
		private void Hide()
		{
			Report("Hide()");
			switch (currentDisplayState)
			{
				case DisplayState.Delaying:
				{
					currentDisplayState = DisplayState.Hidden;
					break;
				}
				case DisplayState.Hidden:
				case DisplayState.AnimatingOut:
				{
					return;
				}
				case DisplayState.AnimatingIn:
				case DisplayState.Showing:
				{
					AnimateOut();
					break;
				}
			}
		}
		
		private void AnimateIn()
		{
			Report("AnimateIn");
			enabled = true;
			bool requiresAnim = false;
			if (currentDisplayState == DisplayState.Hidden)
			{
				delayRemaining = BusyBlockerManager.Instance.GetDisplayDelay(blockingState.ActiveBlockerFlag);
				if (delayRemaining > 0f)
				{
					currentDisplayState = DisplayState.Delaying;
				}
				else
				{
					requiresAnim = true;
				}
			}
			else if (currentDisplayState == DisplayState.Delaying)
			{
				if (delayRemaining <= 0f)
				{
					requiresAnim = true;
				}
			}
			else
			{
				requiresAnim = true;
			}
			
			if (requiresAnim)
			{
				if (!uiTransition.IsPlaying)
				{
					currentDisplayState = DisplayState.AnimatingIn;
					// There is a bug in UITransition that if one animation leads to another
					// in the same frame, the second will not be played.
					EB.Action startNextAnimation = EB.SafeAction.Wrap(this, delegate() {
						Report("Starting Animation: IN");
						uiTransition.PlayTransitionByName("IN", delegate() {
							lastTransitionFrame = UnityEngine.Time.frameCount;
							Report("AnimateInComplete");
							currentDisplayState = DisplayState.Showing;
						});
					});

					if (lastTransitionFrame == UnityEngine.Time.frameCount)
					{
						EB.Coroutines.NextFrame(startNextAnimation);
					}
					else
					{
						startNextAnimation();
					}
				}
				else
				{
					pendingTransition = "IN";
					Report("Assign Pending Transition: IN");
				}
			}
		}
		
		private void AnimateOut()
		{
			Report("AnimateOut");
			if (!uiTransition.IsPlaying)
			{
				currentDisplayState = DisplayState.AnimatingOut;
				// There is a bug in UITransition that if one animation leads to another
				// in the same frame, the second will not be played.
				EB.Action startNextAnimation = EB.SafeAction.Wrap(this, delegate() {
					Report("Starting Animation: OUT");
					uiTransition.PlayTransitionByName("OUT", delegate() {
						lastTransitionFrame = UnityEngine.Time.frameCount;
						Report("AnimateOutComplete");
						currentDisplayState = DisplayState.Hidden;
						enabled = false;
						UpdateRootObjectAndCollider();
					});
				});

				if (lastTransitionFrame == UnityEngine.Time.frameCount)
				{
					EB.Coroutines.NextFrame(startNextAnimation);
				}
				else
				{
					startNextAnimation();
				}
			}
			else
			{
				pendingTransition = "OUT";
				Report("Assign Pending Transition: OUT");
			}
		}

		private void CacheReferences()
		{
			if (referencesCached)
			{
				return;
			}
			uiTransition = EB.Util.FindComponent<UITransition>(gameObject);
			uiTransition.GetTransition("IN").JumpToStart();
			
			blockerCollider = EB.Util.FindComponent<BoxCollider>(gameObject);
			reasonLabel = EB.Util.FindComponentUnder<UILabel>(gameObject, "ReasonLabel");

			referencesCached = true;
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////////////////////////////////////////////
		#region Monobehaviour Implementation
		/////////////////////////////////////////////////////////////////////////
		private void Awake()
		{
			currentDisplayState = DisplayState.Hidden;
			CacheReferences();
		}

		private void Update()
		{
			if (!IsReady())
			{
				isReady = true;
				if (onReadyCallback != null)
				{
					onReadyCallback();
				}
			}

			if (!string.IsNullOrEmpty(pendingTransition) && !uiTransition.IsPlaying)
			{
				if (pendingTransition == "IN")
				{
					AnimateIn();
				}
				else if (pendingTransition == "OUT")
				{
					AnimateOut();
				}
				pendingTransition = null;
			}
			
			if (currentDisplayState == DisplayState.Delaying)
			{
				delayRemaining -= Time.deltaTime;
				if (delayRemaining <= 0f)
				{
					AnimateIn();
				}
			}
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////

		private void Report(string msg)
		{
#if DEBUG_BLOCKING_MODE_CLASS
			UnityEngine.Debug.Log(string.Format("[{0}] BlockingMode({1}) > {2}",
			                           UnityEngine.Time.frameCount,
			                           (IsBlockingState ? "Blocking" : "Non-Blocking"),
			                           msg));
#endif
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
			UILogger.Instance.Log(string.Format("[{0}] BlockingMode({1}) > {2}", UnityEngine.Time.frameCount, (IsBlockingState ? "Blocking" : "Non-Blocking"), msg));
#endif
		}
	}
}