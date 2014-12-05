using UnityEditor;
using UnityEngine;
using ExtensionMethods;
using System.Collections.Generic;

public class PrefabEditor<T> : EditorWindow where T : UnityEngine.Component
{
	private bool IsTargetTempPrefabInstance = false;
	protected T Target = null;
	
	private T _Source = null;
	protected T Source 
	{
		get
		{
			return this._Source;
		}
		private set
		{
			if( this._Source != value )
			{
				//Assume it is invalid, and assign it if it is not
				this._Source = null;
				if( value != null )
				{
					PrefabType prefabType = PrefabUtility.GetPrefabType( value );
					if( prefabType == PrefabType.Prefab )
					{
						this._Source = value;
					}
					else
					{
						Debug.Log("ERROR: Attempted to set the source to something other than a Prefab");
					}
				}
			}
		}
	}
	
	protected bool Dirty { get; set; }
	
	protected string UndoTitle = typeof( T ).Name;
	
	protected virtual bool CloseOnNonTargetSelection
	{
		get
		{
			return true;
		}
	}
	
	public new void Close()
	{
		ConfirmSave();
		DestroyTempPrefabInstance();
	}
	
	private void DestroyTempPrefabInstance()
	{
		if( this.Target != null )
		{
			if( this.IsTargetTempPrefabInstance == true )
			{
				DestroyImmediate( this.Target.gameObject );
			}
			this.Target = null;
		}
	}
	
	public void OnDestroy()
	{						
		Close();
	}
	
	public void RegisterUndo( string operation, bool atomic = true )
	{
		this.Dirty = true;
		if( atomic == true )
		{
			Undo.IncrementCurrentGroup();
		}
		string text = this.UndoTitle + " " + operation;
		Undo.RecordObject( this.Target, text );
	}
	
	public bool CommitPrefab()
	{
		bool commited = false;
		
		if( this.Source == null )
		{
			Debug.Log("ERROR: No Source Prefab");
		}
		else if( this.Target == null )
		{
			Debug.Log("ERROR: No Target Prefab Instance");
		}
		else
		{
			RegisterUndo( "Commiting Prefab" );
			var result = PrefabUtility.ReplacePrefab( this.Target.gameObject, this.Source.gameObject, ReplacePrefabOptions.ReplaceNameBased ); 
			if( result != null )
			{
				string msg = string.Format( "Saved {0}", this.Source.name );
				EditorUtility.DisplayDialog( GetType().Name, msg, "OK" );
				commited = true;
				this.Dirty = false;	
			}
			else
			{
				string msg = string.Format( "Error replacing Prefab {0}", this.Source.name );
				EditorUtility.DisplayDialog(GetType().Name, msg,"OK"); 
			}
			
			LocalizationUtils.SaveDbs();
		}
		
		return commited;
	}
	
	public void ConfirmSave()
	{
		if( this.Dirty == true )
		{
			if( ( this.Source != null ) && ( this.Target != null ) )
			{
				string msg = string.Format( "{0} has not been saved. Do you want to save?", this.Source.name );
				if( EditorUtility.DisplayDialog( "Sequence Editor", msg, "Yes", "No" ) == true )
				{
					CommitPrefab();				
				}
			}
		}
	}
	
	public virtual void OnTargetChanged()
	{
		
	}
	
	public virtual void Update()
	{
		//If the application is playing then we need to remove the temporary instance
		if( Application.isPlaying == true )
		{
			ConfirmSave();
			DestroyTempPrefabInstance();
			
			this.Source = null;
			this.Target = null;
		}
		else
		{
			//Check to see if the actively selected sequence prefab is different than the one we are currently editing
			T activePrefab = null;
			T active = null;
			if( Selection.activeGameObject != null )
			{
				active = Selection.activeGameObject.GetComponent< T >();
			}
			
			if( ( active == null ) && ( this.CloseOnNonTargetSelection == false ) )
			{
				active = this.Source;
			}
			
			PrefabType activeType = PrefabType.None;
			bool targetChanged = false;
			if( active != null )
			{
				activeType = PrefabUtility.GetPrefabType( active );
				switch( activeType )
				{
					case PrefabType.Prefab:
					{
						activePrefab = active;
						break;
					}
					case PrefabType.PrefabInstance:
					{
					 	activePrefab = PrefabUtility.GetPrefabParent( active ) as T;
						targetChanged = active != this.Target;
						break;
					}
					default:
					{
						if( active == this.Target )
						{
							activePrefab = this.Source;
						}
						else
						{ 
							targetChanged = true;
						}
						break;
					}
				}
			}
			
			if( ( activePrefab != this.Source ) || ( targetChanged == true ) )
			{
				//We need to check to see if we should be saving the any changes
				ConfirmSave();
				
				DestroyTempPrefabInstance();
				
				if( activePrefab != null )
				{
					switch( activeType )
					{
						case PrefabType.Prefab:
						{
							this.Target = Instantiate( activePrefab ) as T;
							this.Target.name = activePrefab.name + " - (for Editor)";
							this.IsTargetTempPrefabInstance = true;
							break;
						}
						case PrefabType.PrefabInstance:
						{
							this.Target = active;
							this.IsTargetTempPrefabInstance = false;
							break;
						}
						default:
						{
							break;
						}
					}
				}
				
				this.Source = activePrefab;
				if( this.Target != null )
				{
					this.LastFrameTargetCheckSum = GenerateTargetCheckSum( this.Target );
				}
				else
				{
					this.LastFrameTargetCheckSum = UnimplementedCheckSum;
				}
				OnTargetChanged();
				this.Dirty = false;
			}
			else if( this.Target != null )
			{
				long checksum = GenerateTargetCheckSum( this.Target );
				if( this.SkipNextTargetCheckSum == true )
				{
					this.SkipNextTargetCheckSum = false;
					this.LastFrameTargetCheckSum = checksum;
				}
				if( checksum != this.LastFrameTargetCheckSum )
				{
					if( this.CheckSumChanged != null )
					{
						this.CheckSumChanged();
					}
					this.LastFrameTargetCheckSum = checksum;
				}
			} 
		}
		
		Repaint();
	}
	
	#region Target CheckSum
	
	//Used to track if the target checksum has changed beneath the editor inheiriting from PrefabEditor.
	//This is usually caused by the Unity Undo system rolling back data without letting the editor know
	public delegate void TargetCheckSumChanged();
	protected event TargetCheckSumChanged CheckSumChanged;
	
	//Marks to skip the next difference in the checksum. This can be used 
	protected bool SkipNextTargetCheckSum { get; set; }
	
	public readonly static long UnimplementedCheckSum = -1; 
	private long LastFrameTargetCheckSum = UnimplementedCheckSum;
	public virtual long GenerateTargetCheckSum( T target )
	{
		return UnimplementedCheckSum;
	}
	
	#endregion
	
	#region Mouse
		
	protected enum MouseButton
	{
		Left = 0,
		Right = 1,
		Middle = 2
	};
	
	protected bool IsMouseOverEditor { get; private set; }
	protected Vector2 EditorMousePosition { get; private set; }
	
	protected delegate void MouseMovedDelegate( Vector2 position, bool isMouseOverEditor );
	protected event MouseMovedDelegate OnMouseMoved;
	
	private void UpdateMouse( Event e, Rect editorRect )
	{
		switch( e.type )
		{
			case EventType.MouseMove:
			case EventType.MouseDrag:
			case EventType.MouseUp:
			case EventType.MouseDown:
			{
				this.EditorMousePosition = e.mousePosition;
				break;
			}
			
			default:
			{
				break;
			}
		}
		
		this.IsMouseOverEditor = editorRect.Contains( this.EditorMousePosition ) == true;
		
		if( this.OnMouseMoved != null )
		{
			this.OnMouseMoved( this.EditorMousePosition, this.IsMouseOverEditor );
		}
	}
	
	#endregion
	
	#region RClick Popup
	
	protected bool IsShowingRClickPopup { get; private set; }
	private Vector2 PopupPosition = Vector2.zero;
	
	private Rect PopupRect;
	private Rect PopupRectActiveBox;
	
	List< Rect > PopupItemBoxes = new List<Rect>();
	
	private readonly float kPopupAlpha = 1.0f;
	private readonly float kPopupButtonHeight = 20.0f;
	private readonly float kPopupBorder = 6.0f;
	private readonly float kPopupButtonSpacer = 1.0f;
	private readonly float kPopupTextBorder = 3.0f;
	
	private readonly Color kPopupItemBackgroundColourNormal = Color.white;
	private readonly Color kPopupItemTextColourNormal = Color.black;
	
	private readonly Color kPopupItemBackgroundColourHover = Color.blue;
	private readonly Color kPopupItemTextColourHover = Color.white;
	
	protected class RClickPopupItem
	{
		public string Name { get; private set; }
		public EB.Action Execute { get; private set; }
	
		public RClickPopupItem( string name, EB.Action execute )
		{
			this.Name = name;
			this.Execute = execute;
		}
	}
	
	private List<RClickPopupItem> RClickPopupItems = new List<RClickPopupItem>();
	
	protected virtual List<RClickPopupItem> GetRClickPopupItems()
	{
		return null;
	}
	
	protected virtual bool CanShowRClickPopup
	{
		get
		{
			return false;
		}
	}
	
	
	
	protected void RenderRClickPopup()
	{
		if( this.IsShowingRClickPopup == true )
		{
			DrawingUtils.Quad( this.PopupRect, kPopupItemBackgroundColourNormal, kPopupItemBackgroundColourNormal, kPopupAlpha );
			
			for( int i = 0; i < this.RClickPopupItems.Count; ++i )
			{
				RClickPopupItem item = this.RClickPopupItems[ i ];
				Rect itemRect = this.PopupItemBoxes[ i ];
				
				bool isHover = itemRect.Contains( this.EditorMousePosition );
				Color backgroundColor = ( isHover ? kPopupItemBackgroundColourHover : kPopupItemBackgroundColourNormal );
				Color textColor = ( isHover ? kPopupItemTextColourHover : kPopupItemTextColourNormal );
				
				DrawingUtils.Quad( itemRect, backgroundColor, backgroundColor, kPopupAlpha );
				
				Vector2 textPos = new Vector2( itemRect.x + kPopupTextBorder, itemRect.y + ( kPopupButtonHeight / 2.0f ) );
				DrawingUtils.Text( item.Name, textPos, TextAnchor.MiddleLeft, textColor, false );
			}
		}
	}
	
	void UpdateRClickPopup( Event e )
	{
		if( this.IsShowingRClickPopup == false )
		{
			if( ( e.type == EventType.MouseDown ) && ( (MouseButton)e.button == MouseButton.Right ) && ( this.CanShowRClickPopup == true ) )
			{
				this.RClickPopupItems = this.GetRClickPopupItems();
				if( this.RClickPopupItems != null )
				{
					if( this.RClickPopupItems.Count > 0 )
					{
						this.IsShowingRClickPopup = true;
						
						//Clear out the old item boxes and rebuild all of the information for the current set of popup items
						this.PopupItemBoxes.Clear();
						this.PopupPosition = this.EditorMousePosition;
						
						//Determine the required width of the popup based on the text
						float width = 0.0f;
						foreach( RClickPopupItem item in this.RClickPopupItems )
						{
							Vector2 ts = DrawingUtils.TextSize( item.Name );
							width = Mathf.Max( width, ts.x );
						}
						width += kPopupTextBorder * 2.0f;
						
						this.PopupRect = new Rect( this.PopupPosition.x, this.PopupPosition.y, width, this.RClickPopupItems.Count * ( kPopupButtonHeight + kPopupButtonSpacer ) + kPopupBorder * 2 );
						this.PopupRectActiveBox = this.PopupRect.Inflate( 20.0f, 20.0f );
						
						float x = this.PopupPosition.x;
						float y = this.PopupPosition.y + kPopupBorder;

						for ( int k = 0; k < this.RClickPopupItems.Count; ++k)
						{
							Rect itemRect = new Rect( x, y, width, kPopupButtonHeight );
							this.PopupItemBoxes.Add( itemRect );
							
							y += ( kPopupButtonHeight + kPopupButtonSpacer );
						}
					}
				}
			}
		}
		else
		{
			bool isMouseOverPopup = this.PopupRect.Contains( this.EditorMousePosition );
			bool isMouseOverInflatedPopup = this.PopupRectActiveBox.Contains( this.EditorMousePosition );
			
			//First check all of the conditions that should just clear the popup straight up
			if( ( ( e.type == EventType.MouseDown ) && ( isMouseOverPopup == false ) ) ||
				( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Right ) && ( isMouseOverInflatedPopup == false ) ) )
			{
				this.IsShowingRClickPopup = false;
			}
			else
			{
				//We have the possibility of activating an option
				if( ( e.type == EventType.MouseDown ) || ( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Right ) ) )
				{
					//Check to see if we have Left Clicked an item in the popup
					if( this.PopupRect.Contains( this.EditorMousePosition ) == true )
					{
						for( int i = 0; i < this.RClickPopupItems.Count; ++i )
						{
							RClickPopupItem item = this.RClickPopupItems[ i ];
							Rect itemRect = this.PopupItemBoxes[ i ];
							
							bool isOver = itemRect.Contains( this.EditorMousePosition );
							if( isOver == true )
							{
								item.Execute();
								this.IsShowingRClickPopup = false;
								e.Use();
							}
						}
					}
				}
			}
		}
	}
	
	
	#endregion
	
		
	public virtual void OnGUI()
	{
		Event e = Event.current;
		Rect editorRect = new Rect( 0, 0, position.width, position.height );
		this.UpdateMouse( e, editorRect );
	
		this.UpdateRClickPopup( e );
	}
}
