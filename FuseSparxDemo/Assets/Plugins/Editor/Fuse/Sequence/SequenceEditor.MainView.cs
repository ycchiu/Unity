
using UnityEngine;
using UnityEditor;
using ExtensionMethods;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace EB.Sequence.Editor
{
	public partial class SequenceEditor : PrefabEditor<EB.Sequence.Component>
	{
		enum MainViewMode
		{
			Default,
			SelectingNodes,
			MovingNodes,
			Panning,
			Zooming,
			Linking,
			CreationPlacement
		}
		
		MainViewMode ActiveMode { get; set; }
		
		private Vector2 MainViewsScrollPos;
		public Texture2D BackgroundTexture = null;

		#region RenderToTexture
		
		// Texture we render everything in the sequencer view to, double buffered.
		protected RenderTexture[] MainViewRenderedTexture = new RenderTexture[2] { null, null };
		// RenderTexture gets copied to a Texture2D which we re-use for each refresh in OnGUI->UpdateMainView
		Texture2D[] MainViewRenderedTexture2D = new Texture2D[] { null, null };
		// Index we are currently saving to. Opposite of the index we are currently rendering.
		int MainViewRenderTexLoadingIndex = 0;
		// Dimensions of the RenderTexture		
		Vector2 MainViewRenderedTextureDimensions = Vector2.zero;
		// Scale factor of our currently cached view.
		float CachedScaleFactorRender = -1.0f;
		// Is the cache markd for refresh
		bool IsMainViewRefreshQueued = false;
		// Force redrawing - definitely a bit of a hack, we constanly refresh the view while we are moving things or zooming, etc. Otherwise we were getting half-rendered screens.
		bool IsRedrawForced = false;
		// The current zoomed view matrix
		Matrix4x4 CurrentViewMatrix = Matrix4x4.identity;
		// Should populate the currently loading texture during the next Update()
		bool PopulateTextureNextUpdate = true;
		
		void FlagMainViewRedraw()
		{
			IsMainViewRefreshQueued = true;
		}
			
		static void DoPopulateTexture(SequenceEditor parent)
		{
			
			if (parent.MainViewRenderedTexture[parent.MainViewRenderTexLoadingIndex] && parent.MainViewRenderedTexture[parent.MainViewRenderTexLoadingIndex].IsCreated())
			{
				RenderTexture prevRenderTexture = RenderTexture.active;
				RenderTexture.active = parent.MainViewRenderedTexture[parent.MainViewRenderTexLoadingIndex];
				parent.MainViewRenderedTexture2D[parent.MainViewRenderTexLoadingIndex].ReadPixels(new Rect(0, 0, parent.MainViewRenderedTextureDimensions.x, parent.MainViewRenderedTextureDimensions.y), 0, 0);
				parent.MainViewRenderedTexture2D[parent.MainViewRenderTexLoadingIndex].Apply();
				RenderTexture.active = prevRenderTexture;
				parent.MainViewRenderTexLoadingIndex = (parent.MainViewRenderTexLoadingIndex + 1) % 2;
			}
		}
		
		#endregion
		
		void CreateMainView()
		{
			this.ActiveMode = MainViewMode.Default;
			
			this.LinkingFirstPoint = null;
			this.GroupSelectionColor.a = 0.5f;
			
			this.ExtendedNodeInfos.Clear();
			this.ConnectionPoints.Clear();
		}
		
		#region ExtendedNodeInfo
		
		private Dictionary< int, CachedNodeInfo > ExtendedNodeInfos = new Dictionary< int, CachedNodeInfo >();
	    
	    
	    private class CachedNodeInfo
	    {
	    	public CachedNodeInfo( EB.Sequence.Serialization.Node owner )
	    	{
	    		this.Owner = owner;
	    	}
	    
	        public EB.Sequence.Serialization.Node Owner { get; private set; }
			public string nodeName;
	        public LinkInfo[] inputLinks;
	        public LinkInfo[] ouputLinks;
	        public LinkInfo[] varInLinks;
	        public LinkInfo[] varOutLinks;
			
			public Vector2 inputLinksSize;
			public Vector2 ouputLinksSize;
			public Vector2 varLinksSize;
			public Vector2 nodeNameSize;
			
	        public bool dirty = true;
	    };
	
	    CachedNodeInfo GetNodeInfo( EB.Sequence.Serialization.Node owner )
	    {
	    	CachedNodeInfo info = null;
	    	if( this.ExtendedNodeInfos.TryGetValue( owner.id, out info ) == false )
	    	{
	    		info = new CachedNodeInfo( owner );
	    		this.ExtendedNodeInfos.Add( owner.id, info );
	    	}
	    	else
	    	{
	    		if( info.Owner != owner )
	    		{
	    			info = new CachedNodeInfo( owner );
	    			this.ExtendedNodeInfos[ owner.id ] = info;
	    		}
	    	}
	
	        if ( info.dirty )
	        {
				EB.Sequence.Utils.UpdateSerializationNode( owner );
				
	            info.inputLinks = SequenceUtils.GetInputLinks( owner );
	            info.ouputLinks = SequenceUtils.GetOutputLinks( owner );
	            info.varInLinks = SequenceUtils.GetVariableInLinks( owner );
	            info.varOutLinks = SequenceUtils.GetVariableOutLinks( owner );
				info.nodeName = EB.Sequence.Utils.NiceName( owner.runtimeTypeName ) + " id:" + owner.id;
				
	            UpdateRect(info);
	
	            info.dirty = false;
	            
	            this.Dirty = true;
	        }
	
	        return info;
	    }
		
		#endregion
		
		#region Connection Points
		private Dictionary< string, ConnectionPoint > ConnectionPoints = new Dictionary< string, ConnectionPoint >();
		
		private class ConnectionPoint
		{
			public ConnectionPoint( EB.Sequence.Serialization.Node owner, string name )
			{
				this.Owner = owner;
				this.Name = name;
				this.ConnectionType = Utils.GetLinkType( this.Owner, this.Name );
				this.LinkDirection = Utils.GetLinkDirection( this.Owner, this.Name );
			}
		
			public Rect BoundingBox { get; set; }
			public string Name { get; private set; }
			public EB.Sequence.Serialization.Node Owner { get; private set; }
			public Utils.LinkType ConnectionType { get; private set; }
			public Direction LinkDirection { get; private set; }
			
			public Vector2 Position
			{
				get
				{
					return DrawingUtils.RectCenter( this.BoundingBox );
				}
			}
			
			public Vector2 Tangent
			{
				get
				{
					Vector2 tangent = Vector2.zero;
					switch( this.ConnectionType )
					{
						case Utils.LinkType.Trigger:
						case Utils.LinkType.Entry:
						{
							tangent.x = 1.0f;
							break;
						}
						case Utils.LinkType.Variable:
						{
							if( Owner.nodeType == EB.Sequence.Serialization.NodeType.Variable )
							{
								tangent.y = 1.0f;
							}
							else
							{
								tangent.y = -1.0f;
							}
							break;
						}
						default:
						{
							break;
						}
					}
					
					return tangent;
				}
			}
			
			public EB.Sequence.Serialization.NodeType Type
			{
				get
				{
					EB.Sequence.Serialization.NodeType nodeType = EB.Sequence.Serialization.NodeType.None;
					if( this.Owner != null )
					{
						nodeType = this.Owner.nodeType;
					}
					return nodeType;
				}
			}
			
			public bool IsLinkAttached( Serialization.Link link )
			{
				bool attached = false;
				
				if( link.outId == this.Owner.id )
				{
					if( link.outName == this.Name )
					{
						attached = true;
					}
				}
				
				if( link.inId == this.Owner.id )
				{
					if( link.inName == this.Name )
					{
						attached = true; 
					}
				}
				
				return attached;
			} 

		}
		
		private string GenerateConnectionPointID( EB.Sequence.Serialization.Node owner, string name )
		{
			string id = owner.runtimeTypeName + "_" + owner.id + "_" + name;
			return id;
		}
		
		private void AddConnectionPoints( EB.Sequence.Serialization.Node owner )
		{
			var info = this.GetNodeInfo( owner );
			if( info != null )
			{
				switch( owner.nodeType )
				{
					case EB.Sequence.Serialization.NodeType.Action:
					case EB.Sequence.Serialization.NodeType.Condition:
					case EB.Sequence.Serialization.NodeType.Event:
					{
						foreach( var inVarLink in info.varInLinks )
						{
							string id = this.GenerateConnectionPointID( owner, inVarLink.name );
							this.ConnectionPoints[ id ] = new ConnectionPoint( owner, inVarLink.name );
						}
				
						foreach( var outVarLink in info.varOutLinks )
						{
							string id = this.GenerateConnectionPointID( owner, outVarLink.name );
							this.ConnectionPoints[ id ] = new ConnectionPoint( owner, outVarLink.name );
						}
						
						foreach( var inLink in info.inputLinks )
						{
							string id = this.GenerateConnectionPointID( owner, inLink.name );
							this.ConnectionPoints[ id ] = new ConnectionPoint( owner, inLink.name );
						}
						foreach( var outLink in info.ouputLinks )
						{
							string id = this.GenerateConnectionPointID( owner, outLink.name );
							this.ConnectionPoints[ id ] = new ConnectionPoint( owner, outLink.name );
						}
						break;
					}
					
					case EB.Sequence.Serialization.NodeType.Variable:
					{
						string id = this.GenerateConnectionPointID( owner, EB.Sequence.Runtime.Variable.ValueLinkName );
						this.ConnectionPoints[ id ] = new ConnectionPoint( owner, EB.Sequence.Runtime.Variable.ValueLinkName );
						break;
					}
					
					default:
					{
						break;
					}
				}
			}
		}
		
		private void RemoveConnectionPoints( EB.Sequence.Serialization.Node owner )
		{
			var info = this.GetNodeInfo( owner );
			if( info != null )
			{
				switch( owner.nodeType )
				{
					case EB.Sequence.Serialization.NodeType.Action:
					case EB.Sequence.Serialization.NodeType.Condition:
					case EB.Sequence.Serialization.NodeType.Event:
					{
						foreach( var inVarLink in info.varInLinks )
						{
							string id = this.GenerateConnectionPointID( owner, inVarLink.name );
							this.ConnectionPoints.Remove( id );
						}
				
						foreach( var outVarLink in info.varOutLinks )
						{
							string id = this.GenerateConnectionPointID( owner, outVarLink.name );
							this.ConnectionPoints.Remove( id );
						}
						
						foreach( var inLink in info.inputLinks )
						{
							string id = this.GenerateConnectionPointID( owner, inLink.name );
							this.ConnectionPoints.Remove( id );
						}
						foreach( var outLink in info.ouputLinks )
						{
							string id = this.GenerateConnectionPointID( owner, outLink.name );
							this.ConnectionPoints.Remove( id );
						}
						break;
					}
					
					case EB.Sequence.Serialization.NodeType.Variable:
					{
						string id = this.GenerateConnectionPointID( owner, EB.Sequence.Runtime.Variable.ValueLinkName );
						this.ConnectionPoints.Remove( id );
						break;
					}
					
					default:
					{
						break;
					}
				}
			}
		}
		
		private void UpdateConnectionPoint( EB.Sequence.Serialization.Node owner, string name, Rect bbox )
		{
			string id = this.GenerateConnectionPointID( owner, name );
			ConnectionPoint connectionPoint = null;
			if( this.ConnectionPoints.TryGetValue( id, out connectionPoint ) == true )
			{
				connectionPoint.BoundingBox = bbox;
			}
		}
		
		private ConnectionPoint GetConnectionPoint( int ownerId, string name )
		{
			ConnectionPoint connectionPoint = null;
			
			if( this.Target != null )
			{
				EB.Sequence.Serialization.Node owner = Target.FindById( ownerId );
				if( owner != null )
				{
					string id = this.GenerateConnectionPointID( owner, name );
					this.ConnectionPoints.TryGetValue( id, out connectionPoint );
				}
			}
			
			return connectionPoint;
		}
		
		#endregion
		
		#region Linking
		
		ConnectionPoint LinkingFirstPoint = null;
		
		Vector2 LinkStartPosition
		{
			get
			{
				Vector2 position = Vector2.zero;
				if( this.ActiveMode == MainViewMode.Linking )
				{
					if( this.LinkingFirstPoint != null )
					{
						position = this.LinkingFirstPoint.Position;
					}
				}
				return position;
			}
		}
		
		private void LinkingBegin( ConnectionPoint connectionPoint )
		{
			this.LinkingFirstPoint = connectionPoint;
		}
		
		private void LinkingComplete( ConnectionPoint connectionPoint )
		{
			if( this.Target != null )
			{
				if( ( connectionPoint != null ) && ( connectionPoint != this.LinkingFirstPoint ) )
				{
					RegisterUndo( "Adding Link" );
					ConnectionPoint outConnection = connectionPoint;
					ConnectionPoint inConnection = this.LinkingFirstPoint;
					
					if( connectionPoint.LinkDirection == Direction.In )
					{
						outConnection = this.LinkingFirstPoint;
						inConnection = connectionPoint;
					}
					
					this.Target.AddLink( outConnection.Owner, outConnection.Name, inConnection.Owner, inConnection.Name );
				}
			}
			
			this.LinkingFirstPoint = null;
		}
		
		#endregion
		
		#region Group Selection
		private Color GroupSelectionColor = Color.blue;
		
		private Vector2 GroupSelectionStartPos { get; set; }
		private Vector2 GroupSelectionEndPos { get; set; }
		private Rect GroupSelectionBox
		{
			get
			{
				float top = ( this.GroupSelectionStartPos.y < this.GroupSelectionEndPos.y ) ? this.GroupSelectionStartPos.y : this.GroupSelectionEndPos.y;
				float left = ( this.GroupSelectionStartPos.x < this.GroupSelectionEndPos.x ) ? this.GroupSelectionStartPos.x : this.GroupSelectionEndPos.x;
				float width = Mathf.Abs( this.GroupSelectionStartPos.x - this.GroupSelectionEndPos.x );
				float height = Mathf.Abs( this.GroupSelectionStartPos.y - this.GroupSelectionEndPos.y );
				Rect groupBox = new Rect( left, top, width, height );
				return groupBox;
			}
		}
		
		private void GroupSelectionBegin( Vector2 position )
		{
			this.GroupSelectionStartPos = position;
			this.GroupSelectionEndPos = position;
		}
		
		private void GroupSelectionComplete( Vector2 position, SelectObjectOperation mode )
		{
			this.GroupSelectionEndPos = position;
			
			//Generate a list of node that are interesting the selection box
			List< EB.Sequence.Serialization.Node > intersectingNodes = new List< EB.Sequence.Serialization.Node >();
			Rect groupBox = this.GroupSelectionBox;
			foreach( Serialization.Node candidate in this.Target.Nodes )
			{
				if( groupBox.Intersect( candidate.rect ) == true )
				{
					intersectingNodes.Add( candidate );
				}
			}
			
			if( intersectingNodes.Count > 0 )
			{
				//If we are intersecting node then select then
				this.SelectObjects( intersectingNodes, null, mode );
			}
			else
			{
				//Not intersecting any nodes so check to see if we are intersecting any links
				List< EB.Sequence.Serialization.Link > containedLinks = new List< EB.Sequence.Serialization.Link >();
				foreach( EB.Sequence.Serialization.Link link in Target.Links)
				{	
					ConnectionPoint start = this.GetConnectionPoint( link.outId, link.outName );
					ConnectionPoint end = this.GetConnectionPoint( link.inId, link.inName );
					if( ( start != null ) && ( end != null ) )
					{
						if( groupBox.Contains( start.Position, start.Tangent, end.Position, end.Tangent, 5 ) == true )
						{
							containedLinks.Add( link );
						}
					}
				}
				
				if( containedLinks.Count > 0 )
				{
					this.SelectObjects( null, containedLinks, mode );
				}
				else
				{
					this.SelectObjects( null, null, SelectObjectOperation.Replace );
				}
			}
		}
		#endregion
		
		#region Creation
		
		private Serialization.Node CreationProxy = null;
		
		void StartCreationPlacement( System.Type type )
		{
			if( type != null )
			{
				this.CreationProxy = this.Target.AddNode( type, false );
				this.CreationProxy.rect.x = this.EditorMousePosition.x;
				this.CreationProxy.rect.y = this.EditorMousePosition.y;
				if( this.ActiveMode == MainViewMode.Default )
				{
					this.ActiveMode = MainViewMode.CreationPlacement;
				}
			}
		}
		
		void UpdateToCreationToMouse()
		{
			if( this.CreationProxy != null )
			{
				this.CreationProxy.rect.x = this.EditorMousePosition.x > 0.0f ? this.EditorMousePosition.x : 0.0f;
				this.CreationProxy.rect.y = this.EditorMousePosition.y > 0.0f ? this.EditorMousePosition.y : 0.0f;
			}
		}
		
		void EndCreationPlacement( bool mouseInMainView )
		{
			//Only add the node if the current position is within the MainView window
			if( mouseInMainView == true )
			{
				RegisterUndo( "Add " + this.CreationProxy.runtimeTypeName );
			
				//Update to include the ScrollView so the thing is placed in the correct area.
				this.CreationProxy.rect.x += this.MainViewsScrollPos.x;
				this.CreationProxy.rect.y += this.MainViewsScrollPos.y;
				
				this.Target.AddNode( this.CreationProxy );
				RebuildVisualObjects();
			}
			this.CreationProxy = null;
			this.ActiveMode = MainViewMode.Default;
		}
		
		void RenderCreationProxy( Rect editorRect )
		{
			//Adjust the clipping pane for the creation proxy so it can draw anywhere in the main screen
			DrawingUtils.Clip(editorRect);
		
			if( this.ActiveMode == MainViewMode.CreationPlacement )
			{
				if( this.CreationProxy != null )
				{
	        		this.DrawNode( this.CreationProxy, true ); 
	        	}
	        }
		}
		
		#endregion
		
		#region Adjusting A Group
		
		private enum AdjustGroupMode
		{
			None,
			Creating,
			Editing
		};
		private AdjustGroupMode AdjustingGroupMode { get; set; }
		
		private class AdjustingGroupInfo
		{
			public enum GroupingColours
			{
				Red,
				Yellow,
				Green,
				White,
				Cyan,
				Grey
			};
			public string Description = string.Empty;
			public GroupingColours Colour = GroupingColours.Yellow;
			public List<int> Ids = new List<int>();
			public Serialization.Group ExistingGroup = null;
			
			public void Reset( IEnumerable< EB.Sequence.Serialization.Node > nodes, Serialization.Group existingGroup = null )
			{
				this.Description = ( existingGroup != null ) ? existingGroup.Description : string.Empty;
				this.Colour = ( existingGroup != null ) ? (GroupingColours)System.Enum.Parse( typeof( GroupingColours ), existingGroup.Colour ) : GroupingColours.Yellow;
				this.ExistingGroup = existingGroup;
				this.Ids.Clear();
				foreach( Serialization.Node node in nodes )
				{
					this.Ids.Add( node.id );
				}
			}
		}
		AdjustingGroupInfo AdjustingInfo = new AdjustingGroupInfo();
		
		private Vector2 CreateAGroupBoxPosition = Vector2.zero;
		readonly private float kGroupBoxAlpha = 0.2f;
		readonly private float kGroupBoxInflateSize = 20.0f;
		
		private void UnGroup( IEnumerable< EB.Sequence.Serialization.Node > nodes )
		{
			if( this.Target != null )
			{
				RegisterUndo( "Removing Nodes from Groups" );
				foreach( Serialization.Node node in nodes )
				{
					foreach( Serialization.Group group in this.Target.Groups )
					{
						group.Ids.Remove(  node.id );
					}
				}
			}
			else
			{
				UnityEngine.Debug.Log("[SequenceEditor] Attempting to UnGroup Nodes when no active sequence is selected in the editor");
			}
		}
		
		private void StartGrouping( IEnumerable< EB.Sequence.Serialization.Node > nodes )
		{
			if( this.Target != null )
			{
				//Check to see if we are editing an existing group or creating a new group
				List< Serialization.Group > existingGroups = new List<Serialization.Group>();
				foreach( EB.Sequence.Serialization.Node node in nodes )
				{
					foreach( Serialization.Group group in this.Target.Groups )
					{
						if( group.Ids.Contains( node.id ) == true )
						{
							existingGroups.AddUnique( group );
						}
					}
				}
				
				//If the current nodes selected are only in one group then edit the existing group
				Serialization.Group editGroup = null;
				if( existingGroups.Count == 1 )
				{
					this.AdjustingGroupMode = AdjustGroupMode.Editing;
					editGroup = existingGroups[ 0 ];
				}
				else
				{
					this.AdjustingGroupMode = AdjustGroupMode.Creating;
				}
				this.AdjustingInfo.Reset( nodes, editGroup );
				
				this.CreateAGroupBoxPosition = this.EditorMousePosition;
			}
			else
			{
				UnityEngine.Debug.Log("[SequenceEditor] Attempting to Group Nodes when no active sequence is selected in the editor");
			}
		}
		
		private void EndGrouping()
		{	
			if( this.Target != null )
			{
				Serialization.Group group = null;
				switch( this.AdjustingGroupMode )
				{
					case AdjustGroupMode.Creating:
					{
						group = new Serialization.Group();
						this.Target.Groups.Add( group );
						break;
					}
					case AdjustGroupMode.Editing:
					{
						group = this.AdjustingInfo.ExistingGroup;
						break;
					}
					default:
					{
						break;
					}
				}
				
				if( group != null )
				{
					RegisterUndo( "Adjust Sequence Groups" );
					group.Description = this.AdjustingInfo.Description;
					group.Colour = this.AdjustingInfo.Colour.ToString();
					group.Ids.Clear();
					group.Ids.AddRange( this.AdjustingInfo.Ids );
				}
			}
			else
			{
				UnityEngine.Debug.Log("[SequenceEditor] Attempting to Group Nodes when no active sequence is selected in the editor");
			}
		}
		
		void UpdateCreateGroup(int id)
		{
			GUI.color = Color.white;

			this.AdjustingInfo.Colour = (AdjustingGroupInfo.GroupingColours)EditorGUILayout.EnumPopup( "Colour", this.AdjustingInfo.Colour );
			
			GUILayout.Label( "Description" );
			this.AdjustingInfo.Description = GUILayout.TextArea( this.AdjustingInfo.Description, GUILayout.MinHeight(100) );
	
			GUILayout.BeginHorizontal();
			if( GUILayout.Button( "Cancel" ) == true )
			{
				this.AdjustingGroupMode = AdjustGroupMode.None;
			}
			if( GUILayout.Button( "OK" ) == true )
			{
				this.EndGrouping();
				this.AdjustingGroupMode = AdjustGroupMode.None;
			}
			GUILayout.EndHorizontal();
			
			GUI.DragWindow();
		}
		
		#endregion
		
		Vector2 LastScaledMousePosition = new Vector2(-1f, -1f);
		
		static float kUnityTopBarAdjustment = 21.0f; // #  pixels to move things down
		
		void UpdateActiveMode( Event e )
		{
			MainViewMode old = this.ActiveMode;
			
			float scaleFactor = this.CurrentViewScale;
			
			// fix up the delta based on the scaling of the window
			e.delta = e.delta / scaleFactor;
						
			switch( this.ActiveMode )
			{
				case MainViewMode.Default:
				{
					//We don't want to move out of the Default unless the mouse is in the mainview
					if( ( this.MouseOverPanel == MousePanels.MainView ) && ( this.IsSelectingASequence == false )  && ( this.IsShowingRClickPopup == false ) && ( this.AdjustingGroupMode == AdjustGroupMode.None ) )
					{
						if( e.type == EventType.MouseDown )
						{
							switch( (MouseButton)e.button )
							{
								case MouseButton.Middle:
								{
									if( e.alt == true )
									{
										this.ActiveMode = MainViewMode.Panning;
									}
									else if( e.control == true )
									{
										this.ActiveMode = MainViewMode.Zooming;
									}
									break;
								}
								case MouseButton.Left:
								{
									//Check to see if we are starting a click on a connection point
									ConnectionPoint mouseDownConnectionPoint = null;
									
									foreach( ConnectionPoint connectionPoint in this.ConnectionPoints.Values )
									{
										if( connectionPoint.BoundingBox.Contains( MainViewMousePosition ) == true )
										{
											mouseDownConnectionPoint = connectionPoint;
											break;
										}
									}
									
									//Check to see if we are starting a click on a node
									Stack<Serialization.Node> mouseDownNodes = new Stack< Serialization.Node >();
									foreach( Serialization.Node candidate in this.Target.Nodes )
									{
										if( candidate.rect.Contains( MainViewMousePosition ) == true )
										{
											mouseDownNodes.Push( candidate );
										}
									}
									
									if( ( mouseDownConnectionPoint == null ) && ( mouseDownNodes.Count == 0 ) )
									{
										//If we aren't selecting a connection point or a node then go into group select
										this.GroupSelectionBegin( MainViewMousePosition );
										this.ActiveMode = MainViewMode.SelectingNodes;
									}
									else
									{
										//We need a special case becase the connection point for the variable is the same sizes as the variable
										if( ( mouseDownConnectionPoint != null ) && ( mouseDownConnectionPoint.Owner.nodeType != EB.Sequence.Serialization.NodeType.Variable ) )
										{
											//If we have a valid connection point then move into Linking
											this.LinkingBegin( mouseDownConnectionPoint );
											this.ActiveMode = MainViewMode.Linking;
										}
										else
										{
											//Check to see if we are double-clicking
											bool executingDoubleClick = false;
											if( e.clickCount == 2 )
											{
												Serialization.Node mouseDownNode = mouseDownNodes.Peek();
												foreach( var prop in mouseDownNode.properties )
												{
													if (prop.stringValue.Length>0)
													{
												        var attribute = mouseDownNode.GetPropertyAttribute(prop.name);
														if (attribute.Hint=="Sequence")	
														{
															ConfirmSave();
															GotoSequence(prop.stringValue);
															executingDoubleClick = true;
														}
													}
												}
											}
											
											if( executingDoubleClick == false )
											{
												SelectObjectOperation operation = TranslateSelectObjectOperation( e );
												
												//Check to see if any of the mouseDownNodes are selected
												bool alreadySelected = false;
												foreach( Serialization.Node mouseDownNode in mouseDownNodes )
												{
													if( this.SelectedNodes.Contains( mouseDownNode ) == true )
													{
														alreadySelected = true;
														break;
													}
												}
												
												//If we are going to be replace the item in the list and it is already in there then we just want to move the list
												if( ( operation == SelectObjectOperation.Replace ) && ( alreadySelected == true ) )
												{
													this.ActiveMode = MainViewMode.MovingNodes;
												}
												else
												{
													this.SelectObjects( mouseDownNodes.Peek().ToEnumerable(), null, operation );
													this.ActiveMode = MainViewMode.MovingNodes;
												}
											}
										}
									}
									break; 
								}
							}
						}
						else if( e.type == EventType.KeyDown )
						{
							if( ( e.keyCode == KeyCode.G ) && ( ( e.control == true ) || ( e.command == true ) ) )
							{
								if( this.SelectedNodes.Count > 0 )
								{
									this.StartGrouping( this.SelectedNodes );
								}
							}
							else if( ( e.keyCode == KeyCode.U ) && ( ( e.control == true ) || ( e.command == true ) ) )
							{
								if( this.SelectedNodes.Count > 0 )
								{
									this.UnGroup( this.SelectedNodes );
								}
							}
						}
					}
					break;
				}
				
				case MainViewMode.Linking:
				{
					if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Left ) )
					{
						ConnectionPoint mouseUpConnectionPoint = null;
						foreach( ConnectionPoint connectionPoint in this.ConnectionPoints.Values )
						{
							if( connectionPoint.BoundingBox.Contains( MainViewMousePosition ) == true )
							{
								mouseUpConnectionPoint = connectionPoint;
								break;
							}
						}
						
						this.LinkingComplete( mouseUpConnectionPoint );		
						this.ActiveMode = MainViewMode.Default;
					}
					break;
				}
				
				case MainViewMode.Panning:
				{
					//Check to see if we should be leaving panning move.
					bool leavePanning = false;
				
					//Not pressing alt anymore
					if( e.alt == false )
					{
						leavePanning = true;
					}
					else if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Middle ) )
					{
						leavePanning = true;
					}
					
					if( leavePanning == true )
					{
						this.ActiveMode = MainViewMode.Default;
					}
					break;
				}
				
				case MainViewMode.Zooming:
				{
					// Check to see if we should be leaving zooming mode.
					bool leaveZooming = false;
					
					//Not pressing ctrl anymore
					if (e.control == false)
					{
						leaveZooming = true;
					}
					else if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Middle ) )
					{
						leaveZooming = true;
					}
					
					if( leaveZooming == true )
					{
						this.ActiveMode = MainViewMode.Default;
					}
					break;
				}
				
				case MainViewMode.SelectingNodes:
				{
					bool endSelecting = false;
					if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Left ) )
					{
						endSelecting = true;
					}
					else if( e.type == EventType.MouseMove )
					{
						endSelecting = true;
					}
					
					if( endSelecting == true )
					{
						SelectObjectOperation mode = this.TranslateSelectObjectOperation( e );
						this.GroupSelectionComplete( MainViewMousePosition, mode );
						this.ActiveMode = MainViewMode.Default;
					}
					break;
				}
				
				case MainViewMode.MovingNodes:
				{
					if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Left ) )
					{
						this.ActiveMode = MainViewMode.Default;
					}
					break;
				}
				
				case MainViewMode.CreationPlacement:
				{
					bool endPlacement = false;
					if( ( e.type == EventType.MouseUp ) && ( (MouseButton)e.button == MouseButton.Left ) )
					{
						endPlacement = true;
					}
					else if( e.type == EventType.MouseMove )
					{
						endPlacement = true;
					}
					
					if( endPlacement == true )
					{
						this.EndCreationPlacement( this.MouseOverPanel == MousePanels.MainView );
					}
					break;
				}
				
				default:
				{
					break;
				}
			}
			
			bool isChanging = this.ActiveMode != old;
			if( isChanging == false )
			{
				switch( this.ActiveMode )
				{
					case MainViewMode.Panning:
					{
						if( e.type == EventType.MouseDrag )
						{
							this.MainViewsScrollPos -= e.delta;
						}
						break;
					}
					
					case MainViewMode.Zooming:
					{
						if (e.type == EventType.MouseDrag)
						{
							this.CurrentViewScale += e.delta.y * ZoomSensitivity;
						}
						break;
					}
					
					case MainViewMode.SelectingNodes:
					{
						if( e.type == EventType.MouseDrag )
						{
							this.GroupSelectionEndPos = MainViewMousePosition;
						}
						break;
					}
					
					case MainViewMode.MovingNodes:
					{
						if( e.type == EventType.MouseDrag )
						{
							foreach( EB.Sequence.Serialization.Node node in this.SelectedNodes )
							{
								node.rect.x += e.delta.x;
								if( node.rect.x < 0.0f )
								{
									node.rect.x = 0.0f;
								}
								node.rect.y += e.delta.y;
								if( node.rect.y < 0.0f )
								{
									node.rect.y = 0.0f;
								}
							}
							//FlagMainViewRedraw();
						}
						break;
					}
					
					case MainViewMode.CreationPlacement:
					{
						if( e.type == EventType.MouseDrag )
						{
							UpdateToCreationToMouse();
						}
						break;
					}
					
					case MainViewMode.Default:
					default:
					{
						break;
					}
				}
			}
			
			if (this.LastScaledMousePosition != MainViewMousePosition || !isChanging)
			{
				this.LastScaledMousePosition = MainViewMousePosition;			
			
				switch (this.ActiveMode)
				{
					case MainViewMode.MovingNodes:
						FlagMainViewRedraw();
						break;
					case MainViewMode.Zooming:
						IsRedrawForced = true;
						break;						
					case MainViewMode.Default:
						if (!isChanging && IsRedrawForced)
						{
							FlagMainViewRedraw();
							IsRedrawForced = false;
						}
						break;
					default:
						{
						FlagMainViewRedraw();
						break;
						}
				}
			}
			
		}
		
	
		void UpdateMainView( Event e, Rect editorRect, Rect mainViewRect )
		{
		
	
			float scaleFactor = this.CurrentViewScale;
			//Get the usable draw area for the mainview. I.E. the area it is allocated minus the scrollbar size
			Rect usableMainViewRect = new Rect( mainViewRect.x, mainViewRect.y, mainViewRect.width - SequenceEditor.ScrollbarSize, mainViewRect.height - SequenceEditor.ScrollbarSize );
		
			//Add the background 
			DrawingUtils.Fill( usableMainViewRect, new Color(195f/255f,208f/255f,196f/255f,1.0f) );
		
			//Determine how big we need the overall scroll window, start at the size of the render window
			Rect mainViewScrollRect = new Rect( 0, 0, usableMainViewRect.width, usableMainViewRect.height );
			
			// Now check with each node, find the max extents and use that as the scroll region if it exceeds
			// maxScroll( if applicable )
			foreach( EB.Sequence.Serialization.Node node in Target.Nodes)
			{
				mainViewScrollRect = DrawingUtils.Union( node.rect, mainViewScrollRect );
			}

			this.UpdateActiveMode( e );			
			
			//Add an additional 100 to each side
			mainViewScrollRect.width += 100.0f;
			mainViewScrollRect.height += 100.0f;			
			
			Rect textureScrollRect = mainViewScrollRect.ScaleSizeBy(scaleFactor);

			Rect scaledMainViewScrollRect = mainViewScrollRect;
			if (scaleFactor > 1.0f)
			{
				scaledMainViewScrollRect = mainViewScrollRect.ScaleSizeBy(scaleFactor, Vector2.zero);
			}					
				
			if( this.BackgroundTexture == null )
			{
				this.BackgroundTexture = AssetDatabase.LoadMainAssetAtPath("Assets/Editor/EB.Sequence.Editor/Assets/EBG_BG.png") as Texture2D;
			}
										
			if (IsRedrawForced || ((CachedScaleFactorRender != scaleFactor) && !PopulateTextureNextUpdate))
			{
			
				int renderedTextureWidth = (int)textureScrollRect.width;
				int renderedTextureHeight = (int)textureScrollRect.height;
				if ( IsRedrawForced ||  this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex] == null || this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex].width != renderedTextureWidth || this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex].height != renderedTextureHeight)
				{
					if (this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex] && this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex].IsCreated())
						this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex].Release();
					this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex] = new RenderTexture(renderedTextureWidth, renderedTextureHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
					this.MainViewRenderedTexture[MainViewRenderTexLoadingIndex].Create();
					if (this.MainViewRenderedTexture2D[MainViewRenderTexLoadingIndex] == null)
					{
						this.MainViewRenderedTexture2D[MainViewRenderTexLoadingIndex] = new Texture2D(renderedTextureWidth, renderedTextureHeight, TextureFormat.ARGB32, false);					
					}
					else
					{
						this.MainViewRenderedTexture2D[MainViewRenderTexLoadingIndex].Resize(renderedTextureWidth, renderedTextureHeight);
					}
					MainViewRenderedTextureDimensions = new Vector2(renderedTextureWidth, renderedTextureHeight);
					FlagMainViewRedraw();
					RenderTexture.active = MainViewRenderedTexture[MainViewRenderTexLoadingIndex];
					GL.Clear(false, true, new Color (0.0f, 0.0f, 0.0f, 0.0f));					
					RenderTexture.active = null;
				}
				else
				{
					if (IsRedrawForced || CachedScaleFactorRender != scaleFactor)
					{
						CachedScaleFactorRender = scaleFactor;
						PopulateTextureNextUpdate = true;
					}
					
					Matrix4x4 unscaledMatrix = GUI.matrix;
					Vector3 scaleVector = new Vector3(scaleFactor, scaleFactor, 1.0f);
					CurrentViewMatrix = Matrix4x4.Scale(scaleVector);					
					
					RenderTexture prevRenderTexture = RenderTexture.active;	
					RenderTexture.active = MainViewRenderedTexture[MainViewRenderTexLoadingIndex];
					GUI.matrix = CurrentViewMatrix;
					Rect clip = new Rect(0, 0, System.Single.MaxValue, System.Single.MaxValue); 
					GUI.BeginGroup(clip);
					DrawingUtils.Clip(clip);
					GUILineHelper.BeginGroup(clip);
					GL.Clear(false, true, new Color (0.0f, 0.0f, 0.0f, 0.0f));
					
					RenderMainView( clip, clip );

					GUI.EndGroup();
					GUILineHelper.EndGroup();
								

					RenderTexture.active = prevRenderTexture;
					GUI.matrix = unscaledMatrix;
				
				}
			}
			else if (IsMainViewRefreshQueued && !PopulateTextureNextUpdate)
			{
				IsMainViewRefreshQueued = false;
				CachedScaleFactorRender = -1.0f;
			}				
			
			GUI.BeginGroup(usableMainViewRect); 
			this.MainViewsScrollPos = GUI.BeginScrollView( usableMainViewRect, this.MainViewsScrollPos, scaledMainViewScrollRect, true, true );

			//Rect scrolledView = new Rect( this.MainViewsScrollPos.x, this.MainViewsScrollPos.y, usableMainViewRect.width, usableMainViewRect.height);
			//Rect scrolledTexture = scrolledView;
			GUI.Box(scaledMainViewScrollRect, "scrolled");
			
			
			
			int validTexIndex = (MainViewRenderTexLoadingIndex + 1 ) % 2;
			if (this.MainViewRenderedTexture2D[validTexIndex] == null)
				validTexIndex = MainViewRenderTexLoadingIndex;
			GUI.DrawTexture(textureScrollRect , this.MainViewRenderedTexture2D[validTexIndex]);	
			
//			//NOTE: could just draw the part of the texture that will be visible through the viewport. this seemed to work mostly but the gains seemed insubstantial. commenting out for now.
//			scrolledTexture.xMin /= scaledMainViewScrollRect.width;
//			float yMin, yMax;
//			scrolledTexture.xMax /= scaledMainViewScrollRect.width;
//			yMax = (scaledMainViewScrollRect.height - scrolledTexture.yMax) / scaledMainViewScrollRect.height;
//			yMin =  (scaledMainViewScrollRect.height - scrolledTexture.yMin) / scaledMainViewScrollRect.height;			
//			scrolledTexture.yMin = yMax;	// using pixel coordinates, 0,0 is bottom left, so have to swap yMin & yMax
//			scrolledTexture.yMax = yMin;			
//			GUI.DrawTextureWithTexCoords(scrolledView, this.MainViewRenderedTexture2D, scrolledTexture);
			
			RenderMainInfoText( editorRect, usableMainViewRect );
			
			//If we are group selecting then render the box
			if( this.ActiveMode == MainViewMode.SelectingNodes )
			{
				//Rect transformed = this.GroupSelectionBox.Transform(scaledMatrix);
				Rect transformed = this.GroupSelectionBox.ScaleSizeBy(scaleFactor);

				transformed.y += kUnityTopBarAdjustment;
				
				DrawingUtils.Quad( transformed, this.GroupSelectionColor );
			}
			GUI.EndScrollView();
			
			GUI.EndGroup();
	        
	        
	        BeginWindows();
			
			if( this.IsSelectingASequence == true )
			{
				// SR THis garbage need to move it to new window!
				GUILayout.Window(10000, new Rect(50,100,500,600), UpdateSequenceSelectionWindow, "Sequence Select",GUILayout.MinWidth(100), GUILayout.MinHeight(100));
			}
			else if( this.AdjustingGroupMode != AdjustGroupMode.None )
			{
				string title = "Create a new Group";
				switch( this.AdjustingGroupMode )
				{
					case AdjustGroupMode.Editing:
					{
						title = "Editing existing Group";
						break;
					}
					default:
					{
						break;
					}
				}
				GUILayout.Window(10001, new Rect( this.CreateAGroupBoxPosition.x, this.CreateAGroupBoxPosition.y, 200, 100 ), UpdateCreateGroup, title, GUILayout.MinWidth(100), GUILayout.MinHeight(100) );
			}
		
			this.RenderRClickPopup();

			EndWindows();
	        
	        //If we are in creation placement then need to draw the item overtop of everything
	        this.RenderCreationProxy( editorRect );
		}
		
		#region Popup
		
		protected override bool CanShowRClickPopup
		{
			get
			{
				bool canShow = false;
				
				if( ( this.MouseOverPanel == MousePanels.MainView ) && ( this.ActiveMode == MainViewMode.Default ) )
				{
					canShow = true;
				} 
				
				return canShow;
			}
		}
		
		protected override List<RClickPopupItem> GetRClickPopupItems()
		{
			List<RClickPopupItem> popupItems = new List<RClickPopupItem>();
		
			//If the mouse button is over the main view panel
			if( this.MouseOverPanel == MousePanels.MainView )
			{
				//Check to see if we are over a connection point
				ConnectionPoint mouseOverConnectionPoint = null;
				foreach( ConnectionPoint connectionPoint in this.ConnectionPoints.Values )
				{
					if( connectionPoint.BoundingBox.Contains( this.MainViewMousePosition ) == true )
					{
						mouseOverConnectionPoint = connectionPoint;
						break;
					}
				}
				
				if( mouseOverConnectionPoint != null )
				{
					//If there are any links coming off this connection point the don't add the Detach
					bool attached = false;
					foreach( Serialization.Link link in this.Target.Links )
					{
						bool connected = mouseOverConnectionPoint.IsLinkAttached( link );
						if( connected == true )
						{
							attached = true;
							break;
						}
					}
				
					if( attached == true )
					{
						RClickPopupItem deleteAttachedLinks = new RClickPopupItem( "Delete Attached Links", delegate { this.OnDeleteAttachedLinks( mouseOverConnectionPoint ); } );
						popupItems.Add( deleteAttachedLinks );
					}																														
				}
			}
			
			return popupItems;
		}
		
		#endregion
		
		#region Rendering
		
		private static Vector2 _rectPadding = new Vector2(5,5);
		private static Vector2 _textPadding = new Vector2(10,10);
		private static float _minVarSize = 10.0f;
		private static float _titleSpace = 5.0f;
		private static float _triangleHeight = 20.0f;
		private static float _centerSpace = 10.0f;
		
		private readonly static float LinkHalfWidth = 5.0f;
		private readonly static float LinkWidth = LinkHalfWidth * 2.0f;
		private readonly static float LinkHalfHeight = 5.0f;
		private readonly static float LinkHeight = LinkHalfHeight * 2.0f;
		
		private readonly static float LinkInflateSize = 5.0f;
		
		static private Vector2 CalcHorzVariableSize( IEnumerable< LinkInfo> links ) 
		{
			Vector2 size = Vector2.zero;
			Vector2 padding = Vector2.zero;
			foreach( var link in links  )
			{
				var ts = DrawingUtils.TextSize(link.editor);
				size.x += ts.x + padding.x;
				size.y = Mathf.Max(size.y,ts.y);
				
				padding = _textPadding;
			}
			size.x += ( padding.x / 2.0f );
			return size;
		}
		
		static private Vector2 CalcVertVariableSize( IEnumerable< LinkInfo> links ) 
		{
			Vector2 size = Vector2.zero;
			Vector2 padding = Vector2.zero;
			foreach( var link in links  )
			{
				var ts = DrawingUtils.TextSize(link.editor);
				size.y += ts.y + padding.y;
				size.x = Mathf.Max(size.x,ts.x);
				
				padding = _textPadding;
			}
			return size;
		}
		
		void UpdateRect( CachedNodeInfo info)
	    {
			// new stuff
			info.inputLinksSize = CalcVertVariableSize(info.inputLinks);
			info.ouputLinksSize = CalcVertVariableSize(info.ouputLinks);
			Vector2 inLinksSize = CalcHorzVariableSize(info.varInLinks);
			Vector2 outLinksSize = CalcHorzVariableSize(info.varOutLinks);
			info.varLinksSize = new Vector2( inLinksSize.x + outLinksSize.x, Mathf.Max( inLinksSize.y, outLinksSize.y ) );
			info.nodeNameSize = DrawingUtils.TextSize(info.nodeName);
			
			Vector2 size = new Vector2(1,1);
			switch( info.Owner.nodeType )
			{
			case EB.Sequence.Serialization.NodeType.Variable:
				float radius = Mathf.Max(_minVarSize, info.nodeNameSize.magnitude + _textPadding.x);
				size.x = size.y = radius;
				break;
			case EB.Sequence.Serialization.NodeType.Event:
				size.x = Mathf.Max( info.nodeNameSize.x, info.varLinksSize.x ) + _rectPadding.x*2;
				size.y = (info.nodeNameSize.y) + _rectPadding.y*2 + _triangleHeight * 2 + _rectPadding.y*2 + info.ouputLinksSize.y + Mathf.Max( info.nodeNameSize.y, info.varLinksSize.y) + _rectPadding.y*2;
				break;	
			default:
				size.x	 = Mathf.Max( info.nodeNameSize.x, info.varLinksSize.x, info.inputLinksSize.x + _centerSpace + info.ouputLinksSize.x ) + _rectPadding.x*2;
				size.y 	 = (info.nodeNameSize.y) + _rectPadding.y*2 + _titleSpace + info.varLinksSize.y + _textPadding.y + Mathf.Max( info.inputLinksSize.y, info.ouputLinksSize.y) + _rectPadding.y * 2;   
				break;
			}
			
			info.Owner.rect.width = size.x;
			info.Owner.rect.height = size.y;
	    }
		
		private void DrawInLinkTriange( Vector2 center, float halfWidth, float height, Color color, float alpha = 1.0f )
		{
			Vector2 baseLeft = new Vector2( center.x - halfWidth, center.y + height );
			Vector2 baseRight = new Vector2( center.x + halfWidth, center.y + height );
			Vector2 tip = center;
			
			DrawingUtils.Triangle( baseLeft, tip, baseRight, color, alpha );
		}
		
		private void DrawOutLinkTriange( Vector2 center, float halfWidth, float height, Color color, float alpha = 1.0f )
		{
			Vector2 baseLeft = new Vector2( center.x - halfWidth, center.y );
			Vector2 baseRight = new Vector2( center.x + halfWidth, center.y );
			Vector2 tip = new Vector2( center.x, center.y + height );
			
			DrawingUtils.Triangle( baseLeft, tip, baseRight, color, alpha );
		}
	
		void DrawVariableLinks( Vector2 pt, EB.Sequence.Serialization.Node node, CachedNodeInfo info, float alpha = 1.0f ) 
		{
			Vector2 size = info.varLinksSize;
			Vector2 start = pt - new Vector2(size.x*.5f, size.y + _rectPadding.y);
			foreach( var inLink in info.varInLinks )
			{
				Rect rc = DrawingUtils.Text( inLink.editor, start, TextAnchor.UpperLeft );
				start.x = rc.xMax + _textPadding.x;
				
				Vector2 anchor = new Vector2( (rc.xMin+rc.xMax)*0.5f, pt.y );
				Rect connectionBox = new Rect( anchor.x - LinkHalfWidth, anchor.y, LinkWidth, LinkHeight ).Inflate( LinkInflateSize, LinkInflateSize );
				this.UpdateConnectionPoint( node, inLink.name, connectionBox );
				
				Color color = LinkColors.DefaultLinkColor;
				if( this.ActiveMode == MainViewMode.Linking )
				{
					if( connectionBox.Contains( this.MainViewMousePosition ) == true )
					{
						color = LinkColors.LinkingLinkColor;
					}
				}
				DrawInLinkTriange( anchor, LinkHalfWidth, LinkHeight, color, alpha );
			}
			
			foreach( var outLink in info.varOutLinks )
			{
				Rect rc = DrawingUtils.Text( outLink.editor, start, TextAnchor.UpperLeft );
				start.x = rc.xMax + _textPadding.x;
				
				Vector2 anchor = new Vector2( (rc.xMin+rc.xMax)*0.5f, pt.y );
				Rect connectionBox = new Rect( anchor.x - LinkHalfWidth, anchor.y, LinkWidth, LinkHeight ).Inflate( LinkInflateSize, LinkInflateSize );
				this.UpdateConnectionPoint( node, outLink.name, connectionBox );
				
				Color color = LinkColors.DefaultLinkColor;
				if( this.ActiveMode == MainViewMode.Linking )
				{
					if( connectionBox.Contains( this.MainViewMousePosition ) == true )
					{
						color = LinkColors.LinkingLinkColor;
					}
				}
				DrawOutLinkTriange( anchor, LinkHalfWidth, LinkHeight, color, alpha );
			}
		}
		
		void DrawInputLinks( Vector2 pt, EB.Sequence.Serialization.Node node, CachedNodeInfo info, float alpha = 1.0f ) 
		{
			Vector2 start = pt + new Vector2(_rectPadding.x, _rectPadding.y);
			
			foreach( var link in info.inputLinks )
			{
				var textRc = DrawingUtils.Text(link.editor, start, TextAnchor.UpperLeft );
				start.y = textRc.yMax + _textPadding.y;
				
				Vector2 anchor = new Vector2( pt.x, (textRc.yMin+textRc.yMax)*0.5f );
				Rect visualBox = new Rect( anchor.x - LinkWidth, anchor.y - LinkHalfHeight, LinkWidth, LinkHeight );
				Rect connectionBox = visualBox.Inflate( LinkInflateSize, LinkInflateSize );
				this.UpdateConnectionPoint( node, link.name, connectionBox );
				
				Color color = LinkColors.DefaultLinkColor;
				if( this.ActiveMode == MainViewMode.Linking )
				{
					if( connectionBox.Contains( this.MainViewMousePosition ) == true )
					{
						color = LinkColors.LinkingLinkColor;
					}
				}
				DrawingUtils.Quad( visualBox, color, alpha );
			}
		}
		
		void DrawOutputLinks( Vector2 pt, EB.Sequence.Serialization.Node node, CachedNodeInfo info, float alpha = 1.0f ) 
		{
			Vector2 start = pt + new Vector2(-_rectPadding.x, _rectPadding.y);
			
			foreach( var link in info.ouputLinks )
			{
				var textRc = DrawingUtils.Text(link.editor, start, TextAnchor.UpperRight );
				start.y = textRc.yMax + _textPadding.y;
				
				Vector2 anchor = new Vector2( pt.x, (textRc.yMin+textRc.yMax)*0.5f );
				Rect visualBox = new Rect( anchor.x, anchor.y - LinkHalfHeight, LinkWidth, LinkHeight );
				Rect connectionBox = visualBox.Inflate( LinkInflateSize, LinkInflateSize );
				this.UpdateConnectionPoint( node, link.name, connectionBox );
				
				Color color = LinkColors.DefaultLinkColor;
				if( this.ActiveMode == MainViewMode.Linking )
				{
					if( connectionBox.Contains( this.MainViewMousePosition ) == true )
					{
						color = LinkColors.LinkingLinkColor;
					}
				}
				DrawingUtils.Quad( visualBox, color, alpha );
			}
		}
	
		void DrawEvent( EB.Sequence.Serialization.Node node, float alpha = 1.0f )
		{
			bool isSelected = this.SelectedNodes.Contains( node );
		
			// calculate the inner size
			var info = this.GetNodeInfo(node);		
			var center = DrawingUtils.RectCenter(node.rect);
			
			var outLinkRc = DrawingUtils.RectFromPtSize( center, info.ouputLinksSize + _rectPadding*2 );
			DrawingUtils.DiamondThing( outLinkRc, _triangleHeight, Color.gray, this.EventColor, alpha );
					
			DrawOutputLinks( new Vector2(outLinkRc.xMax,outLinkRc.yMin), node, info, alpha ); 
			
			string nodeName = info.nodeName;
			
			Vector2 titleSize = DrawingUtils.TextSize(nodeName);
			titleSize.x = Mathf.Max( info.nodeNameSize.x, info.varLinksSize.x ); 
			var size = titleSize + _rectPadding*2;
			
			Rect titleRect = new Rect( center.x-size.x*0.5f, outLinkRc.yMin - _triangleHeight - size.y, size.x, size.y); 
			DrawingUtils.Quad( titleRect, Color.gray, this.EventColor, alpha );
			DrawingUtils.Text( nodeName, DrawingUtils.RectCenter(titleRect), TextAnchor.MiddleCenter, isSelected ? Color.yellow : Color.white, alpha );
			
			// draw variables
			Rect variableLinks = new Rect( titleRect.xMin, outLinkRc.yMax + _triangleHeight, size.x, size.y); 
			DrawingUtils.Quad( variableLinks, Color.gray, this.EventColor, alpha );
			DrawVariableLinks( new Vector2(center.x, variableLinks.yMax), node, info, alpha );
		}
		
		void DrawNode( EB.Sequence.Serialization.Node node, Color color, float alpha = 1.0f ) 
		{
			bool isSelected = this.SelectedNodes.Contains( node );
		
			var info = this.GetNodeInfo(node);
			
			// title bar
			var titleRect = new Rect( node.rect.x, node.rect.y, node.rect.width, info.nodeNameSize.y + _rectPadding.y*2);
			DrawingUtils.Quad( titleRect, Color.gray, color, alpha );
			DrawingUtils.Text( info.nodeName,  DrawingUtils.RectCenter(titleRect), TextAnchor.MiddleCenter, isSelected ? Color.yellow : Color.white, alpha );   
			
			// links rect
			var linksRect = Rect.MinMaxRect( node.rect.x, titleRect.yMax + _titleSpace, node.rect.xMax, node.rect.yMax );
			DrawingUtils.Quad( linksRect, Color.gray, color, alpha );
			
			DrawInputLinks( new Vector2(linksRect.xMin,linksRect.yMin), node, info, alpha ); 
			DrawOutputLinks( new Vector2(linksRect.xMax,linksRect.yMin), node, info, alpha );
			DrawVariableLinks( new Vector2( (linksRect.xMin+linksRect.xMax)*0.5f,linksRect.yMax), node, info, alpha );
		}
	
		void DrawVariable( EB.Sequence.Serialization.Node node, float alpha = 1.0f ) 
		{
			bool isSelected = this.SelectedNodes.Contains( node );
		
			var info = this.GetNodeInfo(node);	
			Vector3 pt 	 = DrawingUtils.RectCenter(node.rect);
			DrawingUtils.Circle( pt, node.rect.width*0.5f, Color.gray, this.VariableColor, alpha ); 
			DrawingUtils.Text( info.nodeName, pt, TextAnchor.MiddleCenter, isSelected ? Color.yellow : Color.white, alpha ); 
			this.UpdateConnectionPoint( node, EB.Sequence.Runtime.Variable.ValueLinkName, node.rect );
		}
		
		void DrawNode( EB.Sequence.Serialization.Node node, bool isProxy ) 
		{
			float alpha = ( isProxy == true ) ? 0.2f : 1.0f;
			switch( node.nodeType )
			{
				case EB.Sequence.Serialization.NodeType.Variable:
				{
					DrawVariable( node, alpha );
					break;
				}
				case EB.Sequence.Serialization.NodeType.Event:
				{
					DrawEvent( node, alpha );
					break;
				}
				case EB.Sequence.Serialization.NodeType.Action:
				{
					DrawNode( node, this.ActionColor, alpha );
					break;
				}
				case EB.Sequence.Serialization.NodeType.Condition:
				{
					DrawNode( node, this.ConditionColor, alpha );
					break;
				}
				default:
				{
					break;
				}
			}
		}
		
		public class LinkColors
		{
			public static readonly Color DefaultLinkColor = Color.black; 		//Default color used if the link is not selected 
			public static readonly Color SelectedLinkColor = Color.red;		//Color used if the link is actively selected
			public static readonly Color CutAndPasteLinkColor = Color.green;	//Color used if the link will be added to the cut and paste buffer because the selected nodes
			public static readonly Color ConnectedLinkColor = Color.blue;		//Color used if the link is connected to the selected nodes but will not be part of the cut and paste buffer
			public static readonly Color LinkingLinkColor = Color.yellow;		//Color used if we are actively connecting a link
		}
		
		bool DisplayDebugWindowInfo = false;
		
		
		void RenderMainInfoText( Rect editorRect, Rect mainViewRect)
		{
			const float textYOffset = 12.0f;
			Vector2 infoTextPosition = new Vector2( this.MainViewsScrollPos.x + 5, this.MainViewsScrollPos.y + textYOffset );
			
			
			if( this.Target != null )
			{
				DrawingUtils.Text( this.Target.name, infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "\tNodes: {0}", this.Target.Nodes.Count ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "\tLinks: {0}", this.Target.Links.Count ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "\tGroups: {0}", this.Target.Groups.Count ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
			}
			
			infoTextPosition.y += textYOffset;
			DrawingUtils.Text( string.Format( "Editor Mode: {0}", this.ActiveMode.ToString() ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
			infoTextPosition.y += textYOffset;
			DrawingUtils.Text( string.Format( "Scale Factor {0}", this.CurrentViewScale ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
			infoTextPosition.y += textYOffset;			
			
			if( this.DisplayDebugWindowInfo == true )
			{
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Editor Window {0}", editorRect ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Main View {0}", mainViewRect ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Main View Scroll Pos {0}", this.MainViewsScrollPos ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Editor Mouse Position {0}", this.EditorMousePosition ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Main View Mouse Position {0}", this.MainViewMousePosition ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
				DrawingUtils.Text( string.Format( "Mouse Over Panel {0}", this.MouseOverPanel ), infoTextPosition, TextAnchor.LowerLeft, Color.yellow );
				infoTextPosition.y += textYOffset;
			}
		}
		
		void RenderMainView( Rect editorRect, Rect mainViewRect )
		{
			if( this.Target != null )
			{
				foreach( EB.Sequence.Serialization.Link link in Target.Links)
				{	
					ConnectionPoint start = this.GetConnectionPoint( link.outId, link.outName );
					ConnectionPoint end = this.GetConnectionPoint( link.inId, link.inName );
					if( ( start != null ) && ( end != null ) )
					{
						//Determine what color to draw the line
						Color linkColor = LinkColors.DefaultLinkColor;
						if( this.SelectedLinks.Contains( link ) == true )
						{
							linkColor = LinkColors.SelectedLinkColor;
						}
						else
						{
							bool startIsSelectedNode = ( this.SelectedNodes.Contains( start.Owner ) == true );
							bool endIsSelectedNode = ( this.SelectedNodes.Contains( end.Owner ) == true );
							if( ( startIsSelectedNode == true ) && ( endIsSelectedNode == true ) )
							{
								linkColor = LinkColors.CutAndPasteLinkColor;
							}
							else if( ( startIsSelectedNode == true ) || ( endIsSelectedNode == true ) )
							{
								linkColor = LinkColors.ConnectedLinkColor;
							}
						}
						
						//If one of the connection points is a variable then we want it to be the start point
						if( end.Type == EB.Sequence.Serialization.NodeType.Variable )
						{
							GUILineHelper.MidPoint( end.Position, end.Tangent, start.Position, start.Tangent, linkColor );
						}
						else
						{
							GUILineHelper.MidPoint( start.Position, start.Tangent, end.Position, end.Tangent, linkColor );
						}
					}				
				}
			}
			
			if( this.ActiveMode == MainViewMode.Linking )
			{
				GUILineHelper.MidPoint( this.LinkStartPosition, this.MainViewMousePosition, LinkColors.LinkingLinkColor );
			}
			
			foreach( EB.Sequence.Serialization.Node node in Target.Nodes)
			{
				DrawingUtils.Text( node.comment, new Vector2(node.rect.xMin, node.rect.yMin-2), TextAnchor.LowerLeft, Color.black, false ); 
				this.DrawNode( node, false );
			}
			
			
			
			foreach( EB.Sequence.Serialization.Group group in this.Target.Groups )
			{
				Rect groupRect = new Rect( 0.0f, 0.0f, 0.0f, 0.0f );
				for( int i = 0; i < group.Ids.Count; ++i )
				{
					EB.Sequence.Serialization.Node node = Target.FindById( group.Ids[ i ] );
					if( node != null )
					{
						if( groupRect.width == 0.0f )
						{
							groupRect = node.rect;
						}
						groupRect = DrawingUtils.Union( node.rect, groupRect );
					}
				}
				
				if( ( groupRect.width > 0.0f ) && ( groupRect.height > 0.0f ) )
				{
					groupRect = groupRect.Inflate( kGroupBoxInflateSize, kGroupBoxInflateSize );
					
					//See if we need to make extra space for the description
					Vector2 ts = DrawingUtils.TextSize( group.Description );
					if( ts.y > kGroupBoxInflateSize )
					{
						groupRect.y -= ( ts.y - kGroupBoxInflateSize );
						groupRect.height += ( ts.y - kGroupBoxInflateSize );
					}
					
					Vector2 textPos = new Vector2( groupRect.x, groupRect.y );
					Color colour = ColorExtensions.FromName( group.Colour );
					DrawingUtils.Text( group.Description, textPos, TextAnchor.UpperLeft, colour );
					DrawingUtils.Quad( groupRect, colour, colour, kGroupBoxAlpha );
				} 
			}
		}
		
		#endregion
	}
}
