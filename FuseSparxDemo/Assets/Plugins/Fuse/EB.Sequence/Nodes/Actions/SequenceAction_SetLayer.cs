using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Set Layer")]
public class SequenceAction_SetLayer : Action 
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[EB.Sequence.Property]
	public string Layer = string.Empty;
	
	private int _layer = 0;
	
	public override void Init ()
	{
		base.Init ();
		
		if ( string.IsNullOrEmpty(Layer) == false )
		{
			_layer = LayerMask.NameToLayer(Layer);
		}	
	}
	       
	[EntryAttribute]
	public void SetLayer()
	{
        GameObject[] gos = Utils.GetObjectList<GameObject>(Target.Value);
        foreach (var go in gos)
        {
            if ( go != null )
            {
                go.layer = _layer;
            }
        }		
		Finished.Invoke();
	}
	
	[Trigger]
	public Trigger Finished = new Trigger();
}
