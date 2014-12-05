// #define DEBUG_BUSY_BLOCKER_CLASS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BlockerFlag = BusyBlockerManager.BlockerFlag;
using BlockingMode = EB.UI.BlockingMode;

/////////////////////////////////////////////////////////////////////////////
/// BusyBlocker
/////////////////////////////////////////////////////////////////////////////
/// Requirements:
/// 1. When in blocking state, blocks input to screens below.
/// 2. When in non-blocking state, provides visual feedback to user that 
///    activity is in progress.
/// 3. Automatically has a delay before showing either state, though input 
///    will still be blocked for blocking states.
/// 4. Blocking state overrides non-blocking state, but they are maintained
///    separately.
/// 5. Fires events when states change.
/// 6. Re-uses API from old BusyBlocker versions.
/////////////////////////////////////////////////////////////////////////////
/// Art / Hierarchy notes:
/// This window is always loaded and therefore should be as lightweight as 
/// possible!
/// 
/// This blocker has two primary visual states: Blocking and Non-Blocking.
/// 
/// Blocking:
/// Blocking should provide visual feedback to the user that the game is 
/// waiting for something to happen and the user is locked out of input until 
/// it does.
/// In order to block input, THERE MUST BE A FULL SCREEN BOX COLLIDER 
/// SOMEWHERE ACTIVE IN THE HIERARCHY for this state!
/// 
/// Non-Blocking:
/// In this state, we are showing some visual indication to the user that 
/// the game is waiting for something to happen, but that it is a non-
/// intrusive transaction / delay. Examples: Quick save icon appearing in the
/// corner of a PC game, or a throbber showing an image is loaded on a web 
/// game.
/////////////////////////////////////////////////////////////////////////////
namespace EB.UI
{
	public class BusyBlocker : Window
	{
		public GameObject BlockingStateRoot;
		public GameObject NonBlockingStateRoot;
		
		public EB.Action OnBlockerOpened;
		public EB.Action OnBlockerClosed;
		
		public class BlockingState
		{
			public bool IsBlockerActive = false;
			public bool IsBlockerVisual = false;
			public bool IsNonBlocking = false;
			public BlockerFlag ActiveBlockerFlag = BlockerFlag.UiAnimation;

			public override string ToString ()
			{
				return string.Format ("[BlockingState IsBlockerActive:{0} IsBlockerVisual:{1} IsNonBlocking:{2} ActiveBlockerFlag:{3}]",
				                      (IsBlockerActive ? "Y" : "N"),
				                      (IsBlockerVisual ? "Y" : "N"),
				                      (IsNonBlocking ? "Y" : "N"),
				                      ActiveBlockerFlag);
			}
		}
		
		private BlockingState targetBlockingState;
		private BlockingMode blockingMode;
		private BlockingMode nonBlockingMode;
		
		private BlockerFlag[] blockerFlags;
		
		/////////////////////////////////////////////////////////////////////////
		#region Standard Busy Blocker Public Interface
		/////////////////////////////////////////////////////////////////////////
		public void UpdateBlocker()
		{
			BlockerFlag visibleFlag = BlockerFlag.ServerCommunication;
			bool active = false;
			bool visualBlocker = false;
			bool nonBlocking = false;
		
			foreach (BlockerFlag flag in blockerFlags)
			{
				if (BusyBlockerManager.Instance.IsFlagActive(flag))
				{
					active = true;
					if (!visualBlocker && BusyBlockerManager.IsVisualBlocker(flag))
					{
						visualBlocker = true;
						visibleFlag = flag;
					}
					if (BusyBlockerManager.IsNonBlockingFlag(flag))
					{
						nonBlocking = true;
					}
				}
				
				if (visualBlocker && !nonBlocking)
				{
					break;
				}
			}
			
			targetBlockingState.IsBlockerActive = active;
			targetBlockingState.IsBlockerVisual = visualBlocker;
			targetBlockingState.IsNonBlocking = nonBlocking;
			targetBlockingState.ActiveBlockerFlag = visibleFlag;

			// Feed this state into blockers.
			blockingMode.SetState(targetBlockingState);
			nonBlockingMode.SetState(targetBlockingState);
			
			// Finally, 
		}
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		protected override void SetupWindow()
		{
			blockerFlags = EB.Util.GetEnumValues<BlockerFlag>();
			base.SetupWindow();
			
			blockingMode = EB.Util.FindComponent<BlockingMode>(BlockingStateRoot);
			nonBlockingMode = EB.Util.FindComponent<BlockingMode>(NonBlockingStateRoot);
			
			targetBlockingState = new BlockingState();
			blockingMode.SetState(targetBlockingState);
			nonBlockingMode.SetState(targetBlockingState);
			
			blockingMode.OnDisplayStateChange += EB.SafeAction.Wrap(this, delegate(BlockingMode.DisplayState displayState) {
				HandleDisplayStateChange(true, displayState);
			});
			nonBlockingMode.OnDisplayStateChange += EB.SafeAction.Wrap(this, delegate(BlockingMode.DisplayState displayState) {
				HandleDisplayStateChange(false, displayState);
			});
			
			BusyBlockerManager.Instance.BusyBlockerInstance = this;
		}
		
		private void HandleDisplayStateChange(bool isBlockingMode, BlockingMode.DisplayState displayState)
		{
			if (displayState == BlockingMode.DisplayState.Showing)
			{
				if (OnBlockerOpened != null)
				{
					OnBlockerOpened();
				}
			}
			if (displayState == BlockingMode.DisplayState.Hidden)
			{
				if (OnBlockerClosed != null)
				{
					OnBlockerClosed();
				}
			}
		}
		
		/////////////////////////////////////////////////////////////////////////
		#region Editor Validation
		/////////////////////////////////////////////////////////////////////////
		#if UNITY_EDITOR
		// Validate GameObject references required for this component to function
		// correctly.
		protected int onValidateCheckFrame = -1;
		protected virtual void OnValidate()
		{
			if (UnityEditor.PrefabUtility.GetPrefabType(this) == UnityEditor.PrefabType.Prefab)
			{
				return;
			}
			if (onValidateCheckFrame == UnityEngine.Time.frameCount)
			{
				return;
			}
			onValidateCheckFrame = UnityEngine.Time.frameCount;
			
			string errors = GetHierarchyErrors();
			
			if (!string.IsNullOrEmpty(errors))
			{
				string report = "BusyBlocker OnValidate:\n" + GetHierarchyErrors();
				Debug.LogError(report, gameObject);
			}
		}
		
		public virtual string GetHierarchyErrors()
		{
			string errors = "";
			
			if (BlockingStateRoot == null)
			{
				errors += "blockingStateRoot is null\n";
			}
			else
			{
				if (EB.Util.FindComponent<BoxCollider>(BlockingStateRoot) == null)
				{
					errors += "blockingStateRoot does not contain a BoxCollider.\nA full screen collider is required to block input!\n";
				}
				if (EB.Util.FindComponent<BlockingMode>(BlockingStateRoot) == null)
				{
					errors += "blockingStateRoot does not have a BlockingMode component on it.\n";
				}
			}
			
			if (NonBlockingStateRoot == null)
			{
				errors += "nonBlockingStateRoot is null\n";
			}
			else
			{
				if (EB.Util.FindComponent<BlockingMode>(BlockingStateRoot) == null)
				{
					errors += "nonBlockingStateRoot does not have a BlockingMode component on it.\n";
				}
			}
			
			return errors;
		}
		#endif
		/////////////////////////////////////////////////////////////////////////
		#endregion
		/////////////////////////////////////////////////////////////////////////
		
		protected void Report(string msg)
		{
			#if DEBUG_BUSY_BLOCKER_CLASS
			EB.Debug.Log(string.Format("[{0}] BusyBlocker > {1}", Time.frameCount, msg));
			#endif
			#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
			UILogger.Instance.Log(string.Format("[{0}] BusyBlocker > {1}", UnityEngine.Time.frameCount, msg));
			#endif
		}
	}
}