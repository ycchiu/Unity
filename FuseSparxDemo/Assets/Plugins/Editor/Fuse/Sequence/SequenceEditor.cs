using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace EB.Sequence.Editor
{
	public partial class SequenceEditor : PrefabEditor<EB.Sequence.Component>
	{	
		// The look of the editor
		public Color 			EventColor;
		public Color   		 	ActionColor;
		public Color   		 	VariableColor;
		public Color    		ConditionColor;
		
		readonly static int ScrollbarSize = 13;
		
		readonly static float MaxZoomIn  = 0.25f;
		readonly static float MaxZoomOut = 3.00f;
		readonly static float ZoomSensitivity = 0.05f;
		
		// General Selection
		public bool 			mbDisplaySelectWindow = false;
	
		List<string>			mPrefabList;
		List<string>			mPrefabQuestList;
		List<string>			sequenceFilenameList;		//S tored and only refresh when 'refresh' is hit..
		LinkedList<string>		sequenceHistory;
		int 					selectionDepth = 0;			// SR TODO This is garbage, started off as one list, now we're munging other functionality...
		string 					selectionItem="";
		EB.Sequence.Serialization.Property				selectionTarget;
		
		bool 					RebuildVisualObjectsFlag = true;
		
		MenuTree				mMenu = new MenuTree();
		
	    #region Sequence Selection
	    
		public bool IsSelectingASequence { get; set; }
		
		private Vector2	SequenceSelectionScrollPos = Vector2.zero;
		
		void UpdateSequenceSelectionWindow(int id)
		{		
			GUI.color = Color.white;
			if (selectionDepth==0)
			{
				// Loop through all sequences...then opent he sequence target..
				this.SequenceSelectionScrollPos = GUILayout.BeginScrollView(this.SequenceSelectionScrollPos);
	
				if (GUILayout.Button("Non Quest Sequences"))
				{
					selectionItem = "";
					selectionDepth=1;
				}
				
				for (int i=0; i<mPrefabQuestList.Count;i++)
				{
					if (GUILayout.Button(mPrefabQuestList[i]))
					{
						selectionItem = mPrefabQuestList[i];
						selectionDepth=1;
						break;
					}
				}
		
				GUILayout.EndScrollView();
				// Loop through all quests...we need a way to not add dupes..
			}
			else
			{
				if (GUILayout.Button("Back"))
				{
					selectionDepth=0;
					return;
				}
				
				// Loop through all sequences...then opent he sequence target..
				this.SequenceSelectionScrollPos = GUILayout.BeginScrollView(this.SequenceSelectionScrollPos);
					
				for (int i=0; i<mPrefabList.Count;i++)
				{		
					string displayName = Path.GetFileNameWithoutExtension(mPrefabList[i]);
	
					if (selectionItem == ResourceUtils.GetQuestId(displayName))
					{
						if (GUILayout.Button(displayName))
						{
							if (selectionTarget != null )
							{
								if (selectionTarget.stringValue == displayName )
								{
									return;
								}
								
								RegisterUndo( "Selecting Sequence" );
								selectionTarget.stringValue = displayName;
							}
							else
							{
								ConfirmSave();
								Selection.activeObject = LoadPrefab(mPrefabList[i]);
								UpdateSequenceHistory(Selection.activeObject.name);
							}
							
							selectionTarget = null;
							this.IsSelectingASequence = false;
							GUI.UnfocusWindow();
							break;
						}
					}
				}
				GUILayout.EndScrollView();
			}
	
			if (GUILayout.Button("Close Window"))
			{
				selectionTarget = null;
				this.IsSelectingASequence = false;
				GUI.UnfocusWindow();
			}
			else
			{
				GUI.BringWindowToFront(id);
				GUI.FocusWindow(id);
			}
			
			GUI.DragWindow();
		}
		
		#endregion
		
		#region Selected Nodes/Links
		
		private List<EB.Sequence.Serialization.Node> SelectedNodes = new List<EB.Sequence.Serialization.Node>();
		private List<EB.Sequence.Serialization.Link> SelectedLinks = new List<EB.Sequence.Serialization.Link>();
		
		enum SelectObjectOperation
		{
			Toggle,
			Additive,
			Replace
		}
		
		SelectObjectOperation TranslateSelectObjectOperation( Event e )
		{
			SelectObjectOperation operation = SelectObjectOperation.Replace;
			
			//If shift if down then we need to add
			if( e.shift == true )
			{
				operation = SelectObjectOperation.Additive;
			}
			//If command or control is selected then we need to toggle
			else if( ( e.command == true ) || ( e.control == true ) )
			{
				operation = SelectObjectOperation.Toggle;
			}
			
			return operation;
		}
		
		
		
		private void SelectObjects( IEnumerable< EB.Sequence.Serialization.Node > nodes, IEnumerable< EB.Sequence.Serialization.Link > links, SelectObjectOperation operation )
		{
			//If we in replace mode then clear the selected list
			if( operation == SelectObjectOperation.Replace )
			{
				this.SelectedNodes.Clear();
				this.SelectedLinks.Clear();
			}
			
			if( nodes != null )
			{
				foreach( EB.Sequence.Serialization.Node node in nodes )
				{
					switch( operation )
					{
						case SelectObjectOperation.Additive:
						case SelectObjectOperation.Replace:
						{
							this.SelectedNodes.AddUnique( node );
							break;
						}
						
						case SelectObjectOperation.Toggle:
						{
							this.SelectedNodes.Toggle( node );
							break;
						}
						
						default:
						{
							break;
						}
					}
				}
			}
			else
			{
				this.SelectedNodes.Clear();
			}
			
			if( links != null )
			{
				foreach( EB.Sequence.Serialization.Link link in links )
				{
					switch( operation )
					{
						case SelectObjectOperation.Additive:
						case SelectObjectOperation.Replace:
						{
							this.SelectedLinks.AddUnique( link );
							break;
						}
						
						case SelectObjectOperation.Toggle:
						{
							this.SelectedLinks.Toggle( link );
							break;
						}
						
						default:
						{
							break;
						}
					}
				}
			}
			else
			{
				this.SelectedLinks.Clear();
			}
		}
		
		#endregion
		
		#region Clipboaard
		
		private ClipboardData Clipboard = null;
		
		class ClipboardData
		{
			private List< EB.Sequence.Serialization.Node > Nodes { get; set; }
			private List< EB.Sequence.Serialization.Link > Links { get; set; }
			private Vector2 Root { get; set; }
			
			public delegate EB.Sequence.Component GetTargetDelegate();
			private GetTargetDelegate GetTarget { get; set; }
			
			public bool IsEmpty
			{
				get
				{
					return this.Nodes.Count == 0;
				}
			}
			
			public EB.Sequence.Component Target
			{
				get
				{
					EB.Sequence.Component target = null;
					
					if( this.GetTarget != null )
					{
						target = this.GetTarget();
					}
					
					return target;
				}
			}
			
			public ClipboardData( GetTargetDelegate target )
			{
				this.Nodes = new List< EB.Sequence.Serialization.Node >();
				this.Links = new List< EB.Sequence.Serialization.Link >();
				this.Root = Vector2.zero;
				this.GetTarget = target;
			}
			
			public void Reset( Vector2 root )
			{
				this.Nodes.Clear();
				this.Links.Clear();
				this.Root = root;
			}
				
			public void Add( EB.Sequence.Serialization.Node node )
			{
				if( node != null )
				{
					this.Nodes.Add( node );
					
					//Determine the list of links that would need to be cloned if this Clipboard is cloned
					this.Links.Clear();
					if( this.Target != null )
					{
						foreach( EB.Sequence.Serialization.Link link in this.Target.Links)
						{
							EB.Sequence.Serialization.Node start = this.Nodes.Find( delegate( EB.Sequence.Serialization.Node candidate ) { return candidate.id == link.outId; } );
							EB.Sequence.Serialization.Node end = this.Nodes.Find( delegate( EB.Sequence.Serialization.Node candidate ) { return candidate.id == link.inId; } );
							if( ( start != null ) && ( end != null ) )
							{ 
								this.Links.Add( link );
							}
						}
					}
				}
			}
			
			public List< EB.Sequence.Serialization.Node > Clone( Vector2 position )
			{
				List< EB.Sequence.Serialization.Node > clones = new List< EB.Sequence.Serialization.Node >();
				
				//Clamp the position to be at least Vector2.zero
				position = Vector2.Max( position, Vector2.zero );
				
				//Determine if the given root will generate a valid position for all of the node. I.E. nothing below Vector2.zero
				Vector2 root = this.Root;
				if( this.Nodes.Count == 1 )
				{
					//If there is only one node then use a root that would get the node at the position passed in
					EB.Sequence.Serialization.Node first = this.Nodes[ 0 ];
					root.x = first.rect.x;
					root.y = first.rect.y;
				}
				else
				{
					//If using the current root would cause any node to be invalid then we need to update the root
					foreach( EB.Sequence.Serialization.Node source in this.Nodes )
					{
						//If the current root would result in something invalid
						if( ( position.x + source.rect.x - root.x ) < 0.0f )
						{
							root.x = position.x + source.rect.x;
						}
						
						if( ( position.y + source.rect.y - root.y ) < 0.0f )
						{
							root.y = position.y + source.rect.y;
						}
					}
				}
				
				//Create a mapping of the old ids to the cloned ids
				Dictionary< int, EB.Sequence.Serialization.Node > cloneMapping = new Dictionary<int, EB.Sequence.Serialization.Node>();
				foreach( EB.Sequence.Serialization.Node source in this.Nodes )
				{
					EB.Sequence.Serialization.Node clone = source.Clone() as EB.Sequence.Serialization.Node;
					if( clone != null )
					{
						Vector2 offsetFromRoot = new Vector2( clone.rect.x - root.x, clone.rect.y - root.y );
						clone.rect.x = position.x + offsetFromRoot.x;
						clone.rect.y = position.y + offsetFromRoot.y;
						
						//Add to the list of nodes and create the mapping of ids to cloned ids
						cloneMapping[ source.id ] = this.Target.AddNode( clone );
						clones.Add( clone );
					}
				}
				
				//Copy all of hte lines that are in the clipboard
				foreach( EB.Sequence.Serialization.Link link in this.Links )
				{
					EB.Sequence.Serialization.Node start = cloneMapping[ link.outId ];
					EB.Sequence.Serialization.Node end = cloneMapping[ link.inId ];
					if( ( start != null ) && ( end != null ) )
					{
						this.Target.AddLink( start, link.outName, end, link.inName );
					}
				}
				
				return clones;
			}
		}
		
		
		
		void AddToClipboard( IEnumerable< EB.Sequence.Serialization.Node > nodes, Vector2 root )
		{
			this.Clipboard.Reset( root );
			foreach( EB.Sequence.Serialization.Node node in nodes )
			{
				EB.Sequence.Serialization.Node copy = node.Clone() as EB.Sequence.Serialization.Node;
				if( copy != null )
				{
					this.Clipboard.Add( copy );
				}
			}
		}
		
		IEnumerable<EB.Sequence.Serialization.Node> CloneFromClipboard( Vector2 position )
		{
			List< EB.Sequence.Serialization.Node > clones = new List< EB.Sequence.Serialization.Node >();
			
			if( Target != null )
			{
				clones = this.Clipboard.Clone( position );
				
			}
			
			return clones;
		}
		
		#endregion
		
		
		#region Mouse
		
		enum MousePanels
		{
			None,
			MainView,
			Properties,
			Toolbox,
			Search
		};
		
		private Vector2 MainViewMousePosition = Vector2.zero;	//Note: scaled by the CurrentViewScale
		private Vector2 UnscaledMousePosition = Vector2.zero;

		private MousePanels MouseOverPanel = MousePanels.None;
		
		private float currentViewScale = 1.0f;
		
		public float CurrentViewScale
		{
			get
			{
				return currentViewScale;
			}
			
			set
			{				
				if (value > MaxZoomOut)
					value = MaxZoomOut;
				if (value < MaxZoomIn)
					value = MaxZoomIn;
				currentViewScale = value;
			}
		}
		
		void MouseMoved( Vector2 mousePosition, bool isMouseOverEditor )
		{
			this.UnscaledMousePosition = ( mousePosition + this.MainViewsScrollPos );		
		
			// Need to offset by the kUnityTopBarAdjustment because it's not accounted for in the GUI.matrix
			Matrix4x4 translation = Matrix4x4.TRS(new Vector3(0.0f, kUnityTopBarAdjustment, 0.0f), Quaternion.identity, Vector3.one );
			Matrix4x4 scaledMatrix = translation * CurrentViewMatrix;
			
			Vector3 scaledMouseVector =  scaledMatrix.inverse.MultiplyPoint(new Vector3(this.UnscaledMousePosition.x, this.UnscaledMousePosition.y, 0));
			this.MainViewMousePosition = new Vector2(scaledMouseVector.x, scaledMouseVector.y);
		}
		
		void UpdateMoveOverPanel( Rect mainViewRect, Rect toolboxRect, Rect propertyRect, Rect searchRect )
		{
			this.MouseOverPanel = MousePanels.None;
			if( this.IsMouseOverEditor == true )
			{
				if( mainViewRect.Contains( this.EditorMousePosition ) == true )
				{
					this.MouseOverPanel = MousePanels.MainView;
				}
				else if( toolboxRect.Contains( this.EditorMousePosition ) == true )
				{
					this.MouseOverPanel = MousePanels.Toolbox;
				}
				else if( propertyRect.Contains( this.EditorMousePosition ) == true )
				{
					this.MouseOverPanel = MousePanels.Properties;
				}
				else if( searchRect.Contains( this.EditorMousePosition ) == true )
				{
					this.MouseOverPanel = MousePanels.Search;
				}
			}
		}
		
		#endregion
				
		#region Commands
		
		private KeyCode CommandKeyDown { get; set; }

		
		private bool CanExecuteCommands
		{
			get
			{
				return this.ActiveMode == MainViewMode.Default;
			}
		}
		
		private void OnDelete()
		{
			RegisterUndo( "Deleting Nodes" );
			this.Delete( this.SelectedNodes, this.SelectedLinks );
			this.SelectedNodes.Clear();
			this.SelectedLinks.Clear();
		}
		
		private void OnDeleteAttachedLinks( ConnectionPoint connectionPoint )
		{
			RegisterUndo( "Deleting Attached Links" );
			this.SelectedNodes.Clear();
			this.SelectedLinks.Clear();
			foreach( Serialization.Link link in this.Target.Links )
			{
				bool connected = connectionPoint.IsLinkAttached( link );
				if( connected == true )
				{
					this.SelectedLinks.Add( link );
				}
			}
			this.Delete( this.SelectedNodes, this.SelectedLinks );
		}
		
		private void OnSelectAll()
		{
			if( Target != null )
			{
				this.SelectObjects( this.Target.Nodes, null, SelectObjectOperation.Replace );
			}
			else
			{
				EB.Debug.Log( "Cannot 'SelectAll' beacuse no Sequence is currently active in the Editor" );
			}
		}
		
		private void OnCut()
		{
			if( this.SelectedNodes.Count > 0 )
			{
				RegisterUndo( "Cutting Nodes" );
				this.AddToClipboard( this.SelectedNodes, this.MainViewMousePosition );
				this.Delete( this.SelectedNodes, null ); 
				this.SelectedNodes.Clear();
				this.SelectedLinks.Clear();
			}
		}
		
		void OnCopy()
		{
			if( this.SelectedNodes.Count > 0 )
			{
				this.AddToClipboard( this.SelectedNodes, this.MainViewMousePosition );
			}
		}
		
		
		void OnDuplicate()
		{
			if( this.SelectedNodes.Count > 0 )
			{
				RegisterUndo( "Duplicating Nodes" );
				
				//Use the first node as the duplicate root so we can keep a relative structure
				Vector2 duplicateRoot = this.SelectedNodes[ 0 ].rect.Position();
				this.AddToClipboard( this.SelectedNodes, duplicateRoot );
				IEnumerable< EB.Sequence.Serialization.Node > clones = this.CloneFromClipboard( this.MainViewMousePosition );
				
				RebuildVisualObjects();
				this.SelectObjects( clones, null, SelectObjectOperation.Replace );
			}
		}
		
		void OnPaste()
		{
			if( this.Clipboard.IsEmpty == false )
			{
				RegisterUndo( "Pasting Nodes" );
				IEnumerable< EB.Sequence.Serialization.Node > clones = this.CloneFromClipboard( this.MainViewMousePosition );
				
				RebuildVisualObjects();
				this.SelectObjects( clones, null, SelectObjectOperation.Replace );
			}
		}
		
		
		
		void UpdateCommands( Event e )
		{
			switch( e.type )
			{
				case EventType.ValidateCommand:
				{
					if( this.CanExecuteCommands == true )
					{
						switch( e.commandName )
						{
							case "Delete":
							{
								if( ( this.SelectedNodes.Count > 0 ) || ( this.SelectedLinks.Count > 0 ) )
								{
									e.Use();
								}
								break;
							}
							
							case "SelectAll":
							{
								e.Use();
								break;
							}
						
							case "Cut":
							case "Copy":
							case "Duplicate":
							{
								if( this.SelectedNodes.Count > 0 )
								{
									e.Use();
								}
								break;
							}
							case "Paste":
							{
								if( this.Clipboard.IsEmpty == false )
								{
									e.Use();
								}
								break;
							}
							default:
							{
								break;
							}
						}
					}
					break;
				}
				
				case EventType.ExecuteCommand:
				{
					switch( e.commandName )
					{
						case "Delete":
						{
							this.OnDelete();
							break;
						}
						case "SelectAll":
						{
							this.OnSelectAll();
							break;
						}
						case "Cut":
						{
							this.OnCut();
							break;
						}
						case "Copy":
						{
							this.OnCopy();
							break;
						}
						case "Duplicate":
						{
							this.OnDuplicate();
							break;
						}
						case "Paste":
						{
							this.OnPaste();
							break;
						}
						
						default:
						{
							break;
						}
					}
					break;
				}
				
				case EventType.KeyDown:
				{
					if( this.CanExecuteCommands == true )
					{
						if( e.keyCode != KeyCode.None )
						{
							this.CommandKeyDown = e.keyCode;
						}
					}
					else
					{
						this.CommandKeyDown = KeyCode.None;
					}
					break;
				}
				
				case EventType.KeyUp:
				{
					if( this.CommandKeyDown == e.keyCode )
					{
						switch( e.keyCode )
						{
							case KeyCode.Backspace:
							case KeyCode.Delete:
							{
								this.OnDelete();
								break;
							}
							default:
							{
								break;
							}
						}
					}
					else
					{
						this.CommandKeyDown = KeyCode.None;
					}
					break;
				}
				
				default:
				{
					break;
				}
			}
		}
		
		private void Delete( IEnumerable< EB.Sequence.Serialization.Node > nodes, IEnumerable< EB.Sequence.Serialization.Link > links )
		{
			if( this.Target != null )
			{
				bool flagRedraw = false;
				if( nodes != null )
				{
					foreach( EB.Sequence.Serialization.Node node in nodes )
					{
						this.RemoveConnectionPoints( node );
						this.Target.RemoveNode( node.id );
						flagRedraw = true;
					}
				}
				
				if( links != null )
				{
					foreach( EB.Sequence.Serialization.Link link in links )
					{
						this.Target.Links.Remove( link );
						flagRedraw = true;
					}
				}
				if (flagRedraw)
				{
					this.FlagMainViewRedraw();
				}
			}
			else
			{
				UnityEngine.Debug.Log("[SequenceEditor] Attempting to Delete Nodes when no active sequence is selected in the editor");
			}
		}
		
		#endregion
			
		[UnityEditor.MenuItem("EBG/Sequencer/Sequence Editor")]
	    static void init()
	    {			
			EditorWindow.GetWindow (typeof (SequenceEditor)); 
	    }
	    
	    public override long GenerateTargetCheckSum( EB.Sequence.Component target )
		{
			long checksum = UnimplementedCheckSum;
			foreach( EB.Sequence.Serialization.Node node in target.Nodes )
			{
				checksum += node.id;
			}
			return checksum;
		}
	    
	    void OnCheckSumChanged()
	    {
	    	this.RebuildVisualObjects();
	    }
			
		void OnEnable()
		{
			this.UndoTitle = "Sequence:";
		
			this.CheckSumChanged -= OnCheckSumChanged;
			this.CheckSumChanged += OnCheckSumChanged;
			
			this.OnMouseMoved -= this.MouseMoved;
			this.OnMouseMoved += this.MouseMoved;
		
			//Setup some global colours for the system
			this.EventColor = Color.green;
			this.ActionColor = Color.blue;
			this.ConditionColor = Color.yellow;
			this.VariableColor = Color.magenta;
			
			this.Clipboard = new ClipboardData( delegate { return this.Target; }  );
			
			this.IsSelectingASequence = false;
			this.AdjustingGroupMode = AdjustGroupMode.None;
					          			          		          			          		          			          		          			          
			mPrefabList = new List<string>();
			mPrefabQuestList = new List<string>();
			
			sequenceFilenameList = new List<string>();
			sequenceHistory = new LinkedList<string>();

			CreateMainView();
			CreateToolbox();
			CreateSearch();
	
			ResourceUtils.Initialize();
		
			
			wantsMouseMove = true;

		}
		
		readonly bool DebugTargetSummary = false;
		
		public override void OnTargetChanged()
		{
			if( DebugTargetSummary == true )
			{
				if( this.Target != null )
				{
					System.Text.StringBuilder builder = new System.Text.StringBuilder();
					builder.Append( string.Format( "SequenceEditor: {0}\n", this.Target.name ) );
					builder.Append( string.Format( "Nodes {0}\n", this.Target.Nodes.Count ) );
					foreach( Serialization.Node node in this.Target.Nodes )
		            {
		            	builder.Append( string.Format( "({0}) {1} : {2}\n", node.id, node.nodeType.ToString(), node.runtimeTypeName ) );
		            }
			        
			        builder.Append( string.Format( "Links {0}\n", this.Target.Links.Count ) );
					foreach( Serialization.Link link in this.Target.Links )
					{
						builder.Append( string.Format( "{0}:{1} => {2}:{3}\n", link.outId, link.outName, link.inId, link.inName ) );
					}
					EB.Debug.Log( builder.ToString() );
				}
			}
		
			RebuildVisualObjects();
			RegisterUndo( "Selecting Target" );
		}
		
		void RebuildVisualObjects()
		{
			this.RebuildVisualObjectsFlag = true;
		}
		
		void DoRebuildVisualObjects()
		{
			//If we are rebuilding the visual objects then unselect everything 
			this.SelectedNodes.Clear();
			this.SelectedLinks.Clear();
			
			if( this.Target != null )
			{
				//Clear out the extra information nodes 
	            this.ExtendedNodeInfos.Clear();
	            this.ConnectionPoints.Clear();
	            
	            //Attempt to clear out bad data from the target
	            this.Target.Nodes.RemoveAll( EB.Sequence.Utils.InvalidNode );
	            this.Target.Links.RemoveAll( delegate( EB.Sequence.Serialization.Link link ) { return EB.Sequence.Utils.InvalidLink( this.Target, link ); } );
	            this.Target.Groups.RemoveAll( delegate( EB.Sequence.Serialization.Group group ) { return EB.Sequence.Utils.InvalidGroup( this.Target, group ); } );
	            
	            foreach( var node in this.Target.Nodes )
	            {
	            	this.GetNodeInfo( node );
	                this.AddConnectionPoints( node );
	            }
			}
			
			this.SkipNextTargetCheckSum = true;
			this.FlagMainViewRedraw();
		}
		
		void CheckRebuildVisualObjects()
		{
			if (this.RebuildVisualObjectsFlag)
			{
				this.RebuildVisualObjectsFlag = false;
				DoRebuildVisualObjects();
			}
		}
		
		GameObject LoadPrefab(string fileName)
		{
			return (GameObject)AssetDatabase.LoadAssetAtPath(fileName, typeof(GameObject));
		}
		
		public override void OnGUI() 
		{
			if (Application.isPlaying)
			{
				GUI.Label(new Rect(10,10,500,20),"Stop the game before editing a sequence");
				return;
			}
	
			if (Target==null)
			{
				GUI.Label(new Rect(10,10,500,20),"No valid sequence selected. The sequence must of been created from a valid prefab. Check if the prefab exists.");
				return;
			}
			
			base.OnGUI();
		
			Event e = Event.current;
			
			
			
	        //This is the defined window layout. Really all of this should be dockable off the main window entry, but for the moment just trying to clean everything up
			//		*************************************************************************
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*						MainView						*	Toolbox		*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		*														*				*
			//		****************************************************************200******
			//		*					*													*
			//		*					*													*
			//		*	  Properties	*	  				Search							200
			//		*					*													*
			//		*					*													*
			//		*					*													*
			//		********600*********************************800**************************
	        
	        const float LowerPanelHeight = 200;
	        const float LowerPanelSeperator = 10;
	        
	        //Determine the sizes for all of the windows
	        Rect editorRect = new Rect( 0, 0, position.width, position.height );
	        Rect mainViewRect = new Rect( 0, 0, position.width - SequenceEditor.ToolBoxMinWidth, position.height - LowerPanelHeight );
	        Rect toolboxRect = new Rect( position.width - SequenceEditor.ToolBoxMinWidth, 0, SequenceEditor.ToolBoxMinWidth, position.height - LowerPanelHeight );
	        
	        float lowerPanelY = position.height - LowerPanelHeight;
	        Rect propertyRect = new Rect( LowerPanelSeperator, lowerPanelY, SequenceEditor.PropertiesMinWidth, LowerPanelHeight );
	        float searchWidth = SequenceEditor.SearchMinWidth > ( position.width - SequenceEditor.PropertiesMinWidth ) ? SequenceEditor.SearchMinWidth : ( position.width - SequenceEditor.PropertiesMinWidth );
	        Rect searchRect = new Rect( propertyRect.xMax + LowerPanelSeperator, lowerPanelY, searchWidth, LowerPanelHeight );	
	        
	        this.UpdateMoveOverPanel( mainViewRect, toolboxRect, propertyRect, searchRect );
			
			UpdateMainView( e, editorRect, mainViewRect );
			
			GUI.BeginGroup( toolboxRect );
				Rect localSpaceToolboxRect = new Rect( 0.0f, 0.0f, toolboxRect.width, toolboxRect.height );
				UpdateToolbox( e, editorRect, localSpaceToolboxRect );
			GUI.EndGroup();
			
			GUILayout.BeginArea( propertyRect );
				Rect localSpacePropertyRect = new Rect( 0.0f, 0.0f, propertyRect.width, propertyRect.height );
				UpdateProperties( e, editorRect, localSpacePropertyRect );
			GUILayout.EndArea();
			
			GUILayout.BeginArea( searchRect );
				Rect localSpaceSearchRect = new Rect( 0.0f, 0.0f, propertyRect.width, propertyRect.height );
				UpdateSearch( e, editorRect, localSpaceSearchRect );
			GUILayout.EndArea();
			
			UpdateCommands( e );
	    }
	    
	    public override void Update()
	    {
	    	base.Update();
	    	
	    	// We need to take the current buffer & write it to a texture, and do it during the Update, not the render so that all the render operations are completed.
	    	// Still a bit brittle, but seems to be the best we can do so we don't end up with half-rendered canvases.
	    	if (PopulateTextureNextUpdate)
	    	{
	    		PopulateTextureNextUpdate = false;
    			DoPopulateTexture(this);
	    	}
	    	CheckRebuildVisualObjects();
	    }
	    
	}
		
}

class SequenceEditor : EB.Sequence.Editor.SequenceEditor
{
}
