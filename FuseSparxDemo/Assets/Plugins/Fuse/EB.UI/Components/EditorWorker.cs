#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/////////////////////////////////////////////////////////////////////////////
/// Window
/////////////////////////////////////////////////////////////////////////////
[ExecuteInEditMode]
public class EditorWorker : MonoBehaviour
{
	public bool HasWorkToDo { get; protected set; }
	
	protected WorkerState _workerState;
	
	protected class WorkerState
	{
		public virtual void Initialize()
		{
		}
		
		public virtual bool IsDone()
		{
			return false;
		}
		
		public virtual string GetStatus()
		{
			return "<unknown>";
		}
		
		public virtual void DoWork()
		{
		}
		
		public virtual WorkerState GetNextWorkerState()
		{
			return null;
		}
	}

	public virtual string GetProgressText()
	{
		return "Placeholder text...";
	}
	
	public virtual void Initialize()
	{
		HasWorkToDo = true;
	}
	
	// Override this method to do whatever this worker is built to do.
	// This method will be called multiple times per frame until it is time 
	// to yield, so it is preferable to do work in smaller slices if 
	// possible, as this will allow the Unity UI to remain responsive while
	// work continues.
	protected virtual void DoWorkSlice()
	{
		if (_workerState != null)
		{
			_workerState.DoWork();
			if (_workerState.IsDone())
			{
				_workerState = _workerState.GetNextWorkerState();
			}
		}
		else
		{
			HasWorkToDo = false;
		}
	}
	
	private void OnEnable()
	{
		EditorApplication.update += OnEditorUpdate;
	}
	
	private void OnDisable()
	{
		EditorApplication.update -= OnEditorUpdate;
	}
	
	private void OnEditorUpdate()
	{
		if (!Application.isPlaying && HasWorkToDo)
		{
			Pump();
		}
	}
	
	private void Pump()
	{
		System.DateTime yieldTime = System.DateTime.Now.AddMilliseconds(30f);
		// For each prefab ...
		while (HasWorkToDo && System.DateTime.Now < yieldTime)
		{
			DoWorkSlice();
		}
	}
}
#endif