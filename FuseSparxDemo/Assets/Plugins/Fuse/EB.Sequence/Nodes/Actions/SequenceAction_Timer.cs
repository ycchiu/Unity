using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Timer")]
public class SequenceAction_Timer : Action
{
	[Variable(ExpectedType=typeof(float))]
	public Variable Interval = Variable.Null;
	
	[Variable(ExpectedType=typeof(float))]
	public Variable RemainingTime = Variable.Null;
	
	[EB.Sequence.Property]
	public bool OneShot = true;
	
	private float _remaining = 0;
	private bool _running = false;
	private bool _pause = false;
	
	[Entry]
	public void Start()
	{
		this.Activate();
		_remaining = Interval.GetValue<float>();
		_running = true;
		_pause = false;
	}	
	
	[Entry]
	public void Stop()
	{ 
		_running = false;
	}

	// cannot stop and then resume on the next frame, use pause instead.
	[Entry]
	public void Pause()
	{
		_pause = true;
	}
	
	[Entry]
	public void Continue()
	{
		_running = true;
		_pause = false;
		this.Activate();
	}
	
	public override void Dispose ()
	{
		Stop();
		base.Dispose ();
	}
	
	public override bool Update()
	{
		if ( !_running )
		{
			return false;
		}
		
		if(_pause)
		{
			return true; // do NOT remove us from the list of ops to update
		}
		
		_remaining -= Time.deltaTime;
		RemainingTime.Value = _remaining;
		
		if ( _remaining <= 0.0f ) 
		{
			OnTimer.Invoke();
			
			if ( OneShot )
			{
				return false;
			}
			else
			{
				Start();
				return true;
			}
		}
		else
		{
			return true;
		}
	}
	
	[Trigger]
	public Trigger OnTimer = new Trigger();
}
