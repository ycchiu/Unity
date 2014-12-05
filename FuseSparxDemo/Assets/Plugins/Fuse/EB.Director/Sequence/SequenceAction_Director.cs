using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Director")]
public class SequenceAction_Director : Action
{
	[EB.Sequence.Property]
	public GameObject DirectorData = null;
		
	[Variable(Show=false)]
    public Variable[] 	Groups = new Variable[0];
	
	[Trigger(Show=false)]
    public Trigger[] 	Events = new Trigger[0];
	
	private EB.Director.Component _instance = null;
	
	public override void Dispose ()
	{
		if ( _instance != null )
		{
			_instance.Stop();
			Object.Destroy(_instance.gameObject);
		}
	}
	
	public override void Init ()
	{
		var go = (GameObject)GameObject.Instantiate(DirectorData);
		_instance = go != null ? go.GetComponent<EB.Director.Component>() : null;
		if ( _instance == null )
		{
			EB.Debug.LogError("ERROR: missing director data on prefab!");
			Object.Destroy(go);
		}
		else
		{
			GameObject.DontDestroyOnLoad(_instance);
			_instance.Bind(this);
		}
	}
	
	public override bool Update ()
	{
		if ( _instance != null )
		{
			return _instance.IsPlaying;
		}
		return false;
	}
	
	[Entry]
	public void Play()
	{	
		if ( _instance != null )
		{
			//EB.Debug.Log("Playing director from "+Parent.name);
			_instance.Play();
			Activate();
		}
	}

	[Entry]
	public void Pause()
	{	
		if ( _instance != null )
		{
			_instance.Pause();
		}
	}
	
	[Entry]
	public void Resume()
	{	
		if ( _instance != null )
		{
			_instance.Resume();
		}
	}
			               
	[Entry]
	public void Stop()				
	{
		if ( _instance != null )
		{
			//EB.Debug.Log("Stopping director from "+Parent.name);
			_instance.Stop();
		}
	}
	
	[Trigger]
	public Trigger Started = new Trigger();
	
	[Trigger]
	public Trigger Stopped = new Trigger();
}
