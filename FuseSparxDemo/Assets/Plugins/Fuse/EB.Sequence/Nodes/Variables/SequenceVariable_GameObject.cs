using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Variables/GameObject",VariableType=typeof(UnityEngine.GameObject))]
public class SequenceVariable_GameObject : Variable
{
	private UnityEngine.GameObject GameObject = null;
	
	public override object Value 
	{
		get 
		{
			//EB.Debug.Log("Gameobject being retrieved: " + this.Id + " " + this.Parent.name + " " + this.Parent.GetInstanceID());
			return this.GameObject;
		}
		set 
		{
			//EB.Debug.Log("Gameobject being set: " + this.Id + " " + this.Parent.name + " " + this.Parent.GetInstanceID() + " Value: " + value);
			this.GameObject = value as UnityEngine.GameObject;
			ValueChanged();
		}
	}
}
