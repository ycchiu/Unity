using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Lock Position Rotation")]
public class SequenceAction_LockPositionRotation : Action 
{
	[Variable(ExpectedType=typeof(GameObject))]
	public Variable Target = Variable.Null;
	
	[EB.Sequence.Property]
	public bool _lockHeight = false;
	
	[EB.Sequence.PropertyAttribute]
	public bool _lockLocalHeight = false;
	
	[EB.Sequence.PropertyAttribute]
	public bool _onlyRotateAroundX = false;
	
	[EB.Sequence.PropertyAttribute]
	public bool _onlyRotateAroundY = false;
	
	[EB.Sequence.PropertyAttribute]
	public bool _onlyRotateAroundZ = false;
	
	private Vector3 _originalLocalPosition;
	private Vector3 _originalPosition;
	private Vector3 _originalEulerAngles;
	
	private GameObject _gameObject;
	
	[Entry]
	public void Start()
	{
		if (Target.Value != null)
		{
			_gameObject = (GameObject) Target.Value;
			this.Activate();
			_started.Invoke();
		}
		else
		{
			EB.Debug.LogError("GameObject does not exist attached to SequenceAction_LockPositionRotation!");
		}
	}
	
	public override bool Update()
	{
		if (_gameObject != null)
		{
			// Height Locks
			if (_lockHeight)	
			{
				_gameObject.transform.position = new Vector3(_gameObject.transform.position.x, _originalPosition.y, _gameObject.transform.position.z);
			}
			else if (_lockLocalHeight)
			{
				_gameObject.transform.localPosition = new Vector3(_gameObject.transform.localPosition.x, _originalLocalPosition.y, _gameObject.transform.localPosition.z);
			}
			
			if (_onlyRotateAroundX)
			{
				_gameObject.transform.eulerAngles = new Vector3(_gameObject.transform.eulerAngles.x, _originalEulerAngles.y, _originalEulerAngles.z);
			}
			else if (_onlyRotateAroundY)
			{
				_gameObject.transform.eulerAngles = new Vector3(_originalEulerAngles.x, _gameObject.transform.eulerAngles.y, _originalEulerAngles.z);
			}
			else if (_onlyRotateAroundZ)
			{
				_gameObject.transform.eulerAngles = new Vector3(_originalEulerAngles.x, _originalEulerAngles.y, _gameObject.transform.eulerAngles.z);
			}
		}
		
		return true;
	}
	
	[Trigger]
	public Trigger _started = new Trigger();
}
