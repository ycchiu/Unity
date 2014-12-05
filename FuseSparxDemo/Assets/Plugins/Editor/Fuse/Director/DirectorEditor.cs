using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EB.Director;
using EB.Director.Serialization;

public class DirectorEditor : PrefabEditor<Director>
{
	private static float ToolBarHeight = 25.0f;
	private static float GroupPaneWidth = 200.0f;
	private static float PropertiesHeight = 200.0f;
	private static float PropertiesWidth = 500.0f;
	
	private object _selected = null;
	private Track _selectedTrack = null;
	private Group _selectedGroup = null;
	private bool _dragging = false;
	private int _selectedFrame = 0;
	
	private Hashtable _rects = new Hashtable();
	
	private string[] _imageEffects = new string[0];
	
	//private static DirectorEditor _this = null;
	
	[UnityEditor.MenuItem("EBG/Director/Editor")]
	static void ShowWindow()
	{	
		EditorWindow.GetWindow (typeof(DirectorEditor)); 
	}
	
	Vector2 _groupScrollPos = Vector2.zero;
	Vector2 _timelineScrollPos = Vector2.zero;
	
	public static void RemoveGroup( MenuCommand command )
	{
		Director target = command.context as Director;
		if ( target != null )
		{
			target.Groups.RemoveAt(command.userData);
		}
	}
	
	public static void AddTrack( MenuCommand command, TrackType type )
	{
		var track = new Track();
		track.type = type;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			target.Groups[command.userData].tracks.Add(track);
		}
	}
	
	[MenuItem("CONTEXT/Groups/Camera/Remove Group")]
	public static void RemoveGroupCamera( MenuCommand cmd ) { RemoveGroup(cmd); }
	
	[MenuItem("CONTEXT/Groups/Camera/Add Transform Track")]
	public static void AddCameraTransformTrack( MenuCommand cmd ) { AddTrack(cmd, TrackType.Transform); }
	
	[MenuItem("CONTEXT/Groups/Camera/Add FOV Track")]
	public static void AddFOVTrackCamera( MenuCommand cmd ) { AddTrack(cmd, TrackType.FOV); }
	
	[MenuItem("CONTEXT/Groups/Camera/Add Orthographic Size Track")]
	public static void AddOSTrackCamera( MenuCommand cmd ) { AddTrack(cmd, TrackType.OrthographicSize); }

	[MenuItem("CONTEXT/Groups/Actor/Add Transform Track")]
	public static void AddActorTransformTrack( MenuCommand cmd ) { AddTrack(cmd, TrackType.Transform); }
	
	[MenuItem("CONTEXT/Groups/Actor/Add TimeScale Track")]
	public static void AddTimeScaleActorTrack( MenuCommand cmd ) { AddTrack(cmd, TrackType.TimeScale); }
	
	[MenuItem("CONTEXT/Groups/Director/Add TimeScale Track")]
	public static void AddTimeScaleTrack( MenuCommand cmd ) { AddTrack(cmd, TrackType.TimeScale); }
	
	[MenuItem("CONTEXT/Groups/ImageEffect/Add Variable Track")]
	public static void AddImageEffectVariableScaleTrack( MenuCommand cmd ) { AddTrack(cmd, TrackType.Variable); }
	
	[MenuItem("CONTEXT/Groups/Director/Remove Group")]
	public static void RemoveGroupDirector( MenuCommand cmd ) { RemoveGroup(cmd); }

	[MenuItem("CONTEXT/Groups/Actor/Remove Group")]
	public static void RemoveGroupActor( MenuCommand cmd ) { RemoveGroup(cmd); }
	
	[MenuItem("CONTEXT/Groups/Event/Remove Group")]
	public static void RemoveGroupEvent( MenuCommand cmd ) { RemoveGroup(cmd); }

	[MenuItem("CONTEXT/Groups/ImageEffect/Remove Group")]
	public static void RemoveImageEffectEvent( MenuCommand cmd ) { RemoveGroup(cmd); }
	
	[MenuItem("CONTEXT/Groups/Global/New Image Effect Group")]
	public static void AddImageEffectGroup( MenuCommand command )
	{
		var group = new Group();
		group.name = "New Image Effect Group";
		group.type = GroupType.ImageEffect;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			if ( target.AddGroup(group) )
			{
				var track = new Track();
				track.type = TrackType.EnableComponent;
				group.tracks.Add(track);
			}
			
		}
	}
	
	[MenuItem("CONTEXT/Groups/Global/New Camera Group")]
	public static void AddCameraGroup( MenuCommand command )
	{
		var group = new Group();
		group.name = "New Camera Group";
		group.type = GroupType.Camera;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			target.AddGroup(group);
		}
	}
	
	[MenuItem("CONTEXT/Groups/Global/New Actor Group")]
	public static void AddActorGroup( MenuCommand command )
	{
		var group = new Group();
		group.name = "New Actor Group";
		group.type = GroupType.Actor;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			target.AddGroup(group);
		}
	}
	
	[MenuItem("CONTEXT/Groups/Global/New Director Group")]
	public static void AddDirectorGroup( MenuCommand command )
	{
		var group = new Group();
		group.name = "Director Group";
		group.type = GroupType.Director;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			if ( target.AddGroup(group) )
			{
				var track = new Track();
				track.type = TrackType.Director;
				group.tracks.Add(track);
			}
		}
	}
	
	[MenuItem("CONTEXT/Groups/Global/New Event Group")]
	public static void AddEventGroup( MenuCommand command )
	{
		var group = new Group();
		group.name = "Event Group";
		group.type = GroupType.Event;
		
		Director target = command.context as Director;
		if ( target != null )
		{
			if ( target.AddGroup(group) )
			{
				var track = new Track();
				track.type = TrackType.Event;
				group.tracks.Add(track);
			}
		}
	}
	
	private void OnEnable()
	{
		// get all the image effects classes
		List<string> images = new List<string>();
			
		//GetClasses(typeof(ImageEffectBase), typeof(Director).Assembly, images );
		//GetClasses(typeof(ImageEffectBase), typeof(MotionBlur).Assembly, images );

		images.Sort();
		
		_imageEffects = images.ToArray();
		
		Debug.Log("Found " + _imageEffects.Length + " effects" );
	}
	
	public System.Type GetTypeFromName( string typeName )
	{
		var type = typeof(EB.Director.Component).Assembly.GetType(typeName,false);
		if ( type != null ) return type;
		
		type = typeof(Director).Assembly.GetType(typeName,false);
		if ( type != null ) return type;
		
		return System.Type.GetType(typeName, false);
	}
	
	private void GetClasses( System.Type subType, System.Reflection.Assembly assembly, List<string> names ) 
	{
		foreach( var type in assembly.GetTypes() )
		{
			if ( type.IsSubclassOf(subType) && type.IsAbstract == false )
			{
				names.Add(type.Name);
			}
		}
	}
	
	public override void OnTargetChanged()
	{
		_selected = null;	
		_selectedTrack = null;
		_rects.Clear();
		this.Dirty = true; ///hack
	}
	
	private void SaveRect( object key, Rect rect )
	{
		if ( key != null && rect.width > 0 )
		{
			_rects[key] = rect;
		}
	}
	
	public Rect GetRect( object key )
	{
		var obj = _rects[key];
		if ( obj == null )
		{
			obj = new Rect(0,0,0,0);
		}
		return (Rect)obj;
	}
	
	private void Groups( Rect rect )
	{		
		_groupScrollPos = GUILayout.BeginScrollView(_groupScrollPos, false, true );
				
		GUILayout.BeginVertical();
		GUILayout.Label("Groups");
		
		int i = 0;
		var ev = Event.current;
		
		foreach( var group in Target.Groups ) 
		{
			if ( GUILayout.Button( group.name ) )
			{
				if ( ev.button ==1 )
				{
					EditorUtility.DisplayPopupMenu (new Rect (ev.mousePosition.x,ev.mousePosition.y,0,0), "CONTEXT/Groups/" + group.type.ToString(), new MenuCommand(Target,i));
	            	ev.Use();					
				}
				else
				{
					_selected = group;
					_selectedGroup = group;
				}
			}
			
			SaveRect(group, GUILayoutUtility.GetLastRect() );
			
			// tracks
			foreach ( var track in group.tracks )
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(25);
				
				if ( GUILayout.Button(track.name) )
				{
					_selected = track;
					_selectedTrack = track;
					_selectedGroup = group;
				}
			
				GUILayout.EndHorizontal();
				
				SaveRect(track, GUILayoutUtility.GetLastRect() );
			}			

			++i;
		}
			
		GUILayout.Space(20);
		GUILayout.EndVertical();
		
		if ( ev.type == EventType.ContextClick && rect.Contains(ev.mousePosition) )
		{
			EditorUtility.DisplayPopupMenu (new Rect (ev.mousePosition.x,ev.mousePosition.y,0,0), "CONTEXT/Groups/Global", new MenuCommand(Target));
            ev.Use();
		}
		
		GUILayout.EndScrollView();
	}
		
	void GroupProperties( Group group )
	{
		group.name = EditorGUILayout.TextField( "Name", group.name );
		
		switch(group.type)
		{
		case GroupType.Director:
			break;
			
		case GroupType.ImageEffect:
			{
				int index = System.Array.IndexOf( _imageEffects, group.targetName );
				index 	  = EditorGUILayout.Popup("Effect", index, _imageEffects);
				group.targetName = (index>=0&&index<_imageEffects.Length) ? _imageEffects[index] : string.Empty;
			}
			break;
		default:
			{
				var go = GetGameObject(group);
				if ( go != null ) 
				{
					go = (GameObject)EditorGUILayout.ObjectField( "Target", go, typeof(GameObject), true ); 	
					group.targetName = go ? go.name : group.targetName;
				}
				else
				{
					group.targetName = EditorGUILayout.TextField("Target", group.targetName);
				}
			}
			break;
		}
		
	}
	
	void TrackProperties( Track track )
	{
		track.restoreState = EditorGUILayout.Toggle("Save/Restore Object State", track.restoreState );
		
		switch( track.type )
		{
		case TrackType.Rotation:
		case TrackType.Position:
		case TrackType.Transform:
			{
				track.space = (SpaceType)EditorGUILayout.EnumPopup("Transform Space", track.space);
			}
			break;
		case TrackType.Variable:
			{
				if ( _selectedGroup != null )
				{
					System.Type componentType = GetTypeFromName( _selectedGroup.targetName );
				
					List<string> fields = new List<string>();
					if ( componentType != null )
					{
						foreach( var field in componentType.GetFields( BindingFlags.Instance | BindingFlags.Public ) )
						{
							if ( Utils.GetVariableType(field) != VariableType.None )
							{
								fields.Add(field.Name);
							}
						}
					}
					else
					{
						Debug.LogWarning("Failed to find type:" + _selectedGroup.targetName);
					}
				
					int index = fields.IndexOf(track.target);
					index 	  = EditorGUILayout.Popup("Field", index, fields.ToArray() );
					if ( index >= 0 && index < fields.Count )
					{
						track.target = fields[index];
					}
				}
			}
			break;
		}
		
	}
	
	void KeyFrameProperties( KeyFrame kf )
	{
		EditorGUILayout.LabelField( "Frame", kf.frame.ToString() );
				
		if ( _selectedTrack != null )
		{
			if ( Utils.HasBlendMode(_selectedTrack.type) )
			{
				kf.mode = (EB.Director.BlendMode)EditorGUILayout.EnumPopup("Blend Mode", kf.mode);
			}
			
			switch( _selectedTrack.type )
			{
			case TrackType.Transform:	
				{
					var qp = kf.QuatPosValue;
					qp.pos = EditorGUILayout.Vector3Field("Position", qp.pos);
					
					var angles = qp.quat.eulerAngles;
					angles = EditorGUILayout.Vector3Field("Rotation:", angles);
					qp.quat = Quaternion.Euler(angles);
				
					kf.QuatPosValue = qp;
				}
				break;
			case TrackType.FOV:
				{
					var fov = kf.FloatValue;
					fov = EditorGUILayout.FloatField("FOV", fov);
					kf.FloatValue = fov;
				}
				break;
			case TrackType.OrthographicSize	:
				{
					var size = kf.FloatValue;
					size = EditorGUILayout.FloatField("Orthographic Size", size);
					kf.FloatValue = size;
				}
				break;	
			case TrackType.Director:
				{
					var ids = GetCameraIds();
					int index = System.Array.IndexOf(ids, (int)kf.ByteValue );
					var names = GetCameraNames(ids);
					index = EditorGUILayout.Popup("Camera", index, names );
					if ( index >=0 && index < ids.Length )
					{
						kf.ByteValue = (byte)ids[index];
					}
				}
				break;
			case TrackType.Event:
				{
					kf.StringValue = EditorGUILayout.TextField("Event Name", kf.StringValue );
				}
				break;
			case TrackType.TimeScale:
				{
					kf.FloatValue = EditorGUILayout.FloatField("Time Scale", kf.FloatValue);
				}
				break;	
			case TrackType.EnableComponent:
				{
					kf.BoolValue = EditorGUILayout.Toggle("Enabled", kf.BoolValue );
				}
				break;
			case TrackType.Variable:
				{
					System.Type componentType = GetTypeFromName( _selectedGroup.targetName );
					if ( componentType != null )
					{
						var field = componentType.GetField(_selectedTrack.target);
						VariableKeyFrame( Utils.GetVariableType(field), kf ); 
					}
				}
				break;	
			default:
				break;
			}			
		}
		
	}
	
	private void VariableKeyFrame( VariableType type, KeyFrame kf ) 
	{
		switch( type )
		{
		case VariableType.Float:
			kf.FloatValue = EditorGUILayout.FloatField("Value", kf.FloatValue );
			break;
		case VariableType.Vector2:
			kf.Vector2Value = EditorGUILayout.Vector2Field("Value", kf.Vector2Value );
			break;
		case VariableType.Vector3:
			kf.Vector3Value = EditorGUILayout.Vector3Field("Value", kf.Vector3Value );
			break;	
		case VariableType.Vector4:
			kf.Vector4Value = EditorGUILayout.Vector4Field("Value", kf.Vector4Value );
			break;	
		case VariableType.Color:
			kf.ColorValue = EditorGUILayout.ColorField("Value", kf.ColorValue );
			break;	
		case VariableType.None:
			break;
		}
	}
	
	
	private static float kKeyFrameWidth = 10.0f;
	
	GameObject GetGameObject( Group group ) 
	{
		var go = GameObject.Find( group.targetName ); 
		return go;
	}
	
	
	int[] GetCameraIds()
	{
		List<int> ids = new List<int>();
		ids.Add(0);
		
		// add in scene camera groups
		foreach( var group in Target.Groups )
		{
			if ( group.type == GroupType.Camera )
			{
				ids.Add(group.id);
			}
		}
			
		return ids.ToArray();
	}
	
	string[] GetCameraNames( int[] ids )
	{
		List<string> names = new List<string>();
		foreach( var id in ids )
		{
			names.Add( GetCameraName(id) ); 
		}
		return names.ToArray();
	}
	
	string GetCameraName( int id )
	{
		switch( id )
		{
		case 0: return "Main";
		default:
			var group = Target.GetGroupById(id);
			if ( group != null )
			{
				return group.name;
			}
			return string.Empty;
		}
	}
	
	void MoveTargetToKeyFrame( Group group, Track track, KeyFrame kf  )  
	{
		var go = GetGameObject(group);
			
		switch( track.type )
		{
			case TrackType.Transform:
			{
				if ( go != null )
				{
					var qp = kf.QuatPosValue;
					qp.Apply(go.transform, track.space);
				}
			}
			break;
		}
	}
	
	void AddKeyFrame( Group group, Track track )
	{
		var go = GetGameObject(group);
		
		KeyFrame kf = null;
		
		switch( track.type )
		{
		case TrackType.Transform:
			{
				if ( go != null )
				{
					kf = new KeyFrame();
					kf.QuatPosValue = QuatPos.FromTransform(go.transform, track.space);
				}
			}
			break;
		case TrackType.FOV:
			{
				if ( go != null && go.camera )
				{
					kf = new KeyFrame();
					kf.FloatValue = go.camera.fieldOfView;
				}
			}
			break;
		case TrackType.OrthographicSize:
			{
				if ( go != null && go.camera )
				{
					kf = new KeyFrame();
					kf.FloatValue = go.camera.orthographicSize;
				}
			}
			break;	
		case TrackType.Director:
			{
				kf = new KeyFrame();
				kf.ByteValue = 0; // main camera
			}
			break;
		default:
			{
				kf = new KeyFrame();
			}
			break;
		}
		
		if ( kf != null )
		{
			kf.frame = _selectedFrame;
			track.Add(kf);
			
			_selected = kf;
			_selectedTrack = track;
			_selectedGroup = group;
		}
		
	}
	
	private static Vector2 _timeLineOffset = new Vector2(20,20);
	
	public float FrameToX( int frame ) 
	{
		return frame * kKeyFrameWidth - _timelineScrollPos.x + _timeLineOffset.x;
	}
	
	public int XToFrame( float x )
	{
		int frame = (int)(( x - _timeLineOffset.x + _timelineScrollPos.x )/kKeyFrameWidth);
		return Mathf.Max(0,frame);
	}
	
	void Sort()
	{
		Debug.Log("Sorting");
		foreach( var group in Target.Groups ) 	
		{
			foreach( var track in group.tracks )
			{
				track.Sort();
			}
		}
	}
	
	void Timeline( Rect rect )
	{
		//DrawingUtils.Clip(rect);
		
		// calculate the scrollrect
		int numFrames 	= (int)(Target.Length) * Director.kFPS;
		float width 	= Mathf.Max( numFrames * kKeyFrameWidth + _timeLineOffset.x, rect.width);
		float height 	= rect.height;
		float scrollSize=15.0f;
		
		//Debug.Log("frames:" + numFrames );
		
		DrawingUtils.Clip(new Rect(0,0,rect.width,rect.height));
		DrawingUtils.Quad( new Rect(0,0,rect.width,rect.height), Color.black ); 
		
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		
		_timelineScrollPos.x = GUILayout.HorizontalScrollbar( _timelineScrollPos.x, rect.width, 0, width ); 
				
		DrawingUtils.Clip( new Rect(0.0f,0.0f, rect.width, rect.height-scrollSize) ); 
		
		DrawingUtils.Line( new Vector3(0, _timeLineOffset.y), new Vector2(width,_timeLineOffset.y), Color.grey ); 
		
		float mx = Event.current.mousePosition.x;
		int hover = XToFrame(mx);
		int playFrame = Target.IsPlaying ? (int)(Target.PlayTime*Director.kFPS) : -1;
		
		if ( Event.current.type == EventType.MouseUp )
		{
			if ( _dragging )
			{
				_dragging = false;
				Sort();
			}
		}
		else if( _dragging )
		{
			if ( _selected is KeyFrame )
			{
				KeyFrame kf = (KeyFrame)_selected;
				kf.frame = hover;
			}
		}

		if ( Event.current.type == EventType.MouseDown )
		{
			_selectedFrame = hover;
		}
		
		Color[] trackColors = new Color[]
		{
			new Color( 25/255f, 25/255f, 25/255f, 1.0f ), 
			new Color( 25/255f, 50/255f, 25/255f, 1.0f ),
		};
		
		foreach( var group in Target.Groups )
		{
			// group name
			Rect groupRect = GetRect(group);
			
			DrawingUtils.Text(group.name, new Vector3(_timeLineOffset.x+5, groupRect.yMax), TextAnchor.LowerLeft, Color.grey ); 
			
			int index = 0;
			foreach( var track in group.tracks )
			{
				Rect trackRect = GetRect(track);
				// draw background rect
				Rect bgRect = new Rect(_timeLineOffset.x - _timelineScrollPos.x, trackRect.y, width, trackRect.height);
				DrawingUtils.Quad( bgRect, trackColors[index % trackColors.Length] ); 				
				
				if ( Event.current.type == EventType.MouseDown )
				{
					if ( bgRect.Contains(Event.current.mousePosition) )
					{
						// add key frame
						if ( Event.current.button == 0 && Event.current.modifiers == EventModifiers.Shift )
						{
							AddKeyFrame(group, track);
							Event.current.Use();
							break;
						}
					}
				}
				
				++index;
			}
		}
		
		for ( int i = 0; i <= numFrames; ++i ) 
		{
			// draw the ticks
			float x = FrameToX(i);

			if ( (i%Director.kFPS) == 0 )
			{
				DrawingUtils.Line( new Vector3(x, _timeLineOffset.y), new Vector2(x,height), new Color(0.25f,0.25f,0.25f,1.0f) ); 
				
				int seconds = i / Director.kFPS;
				string text = string.Format("{0:00}:{1:00}", seconds/60, seconds%60);
				
				DrawingUtils.Text(text, new Vector3(x, _timeLineOffset.y), TextAnchor.LowerLeft);  
			}
			
			if ( i == _selectedFrame )
			{
				DrawingUtils.Line( new Vector3(x, _timeLineOffset.y), new Vector2(x,height), Color.red ); 				
			}
			
			if ( i == hover )
			{
				DrawingUtils.Line( new Vector3(x, _timeLineOffset.y), new Vector2(x,height), Color.yellow ); 				
			}
			
			if ( i == playFrame )
			{
				DrawingUtils.Line( new Vector3(x, _timeLineOffset.y), new Vector2(x,height), Color.green ); 				
			}
			
		}
		
		// draw the groups
		foreach( var group in Target.Groups )
		{
			int index = 0;
			foreach( var track in group.tracks )
			{
				Rect trackRect = GetRect(track);
							
				int frameIndex = 0;
				foreach( var kf in track.frames )
				{
					string text = string.Empty;
					int duration = 1;
					
					if ( track.type == TrackType.Director)
					{
						if ( frameIndex < track.frames.Count -1 )						
						{
							duration = track.frames[frameIndex+1].frame - kf.frame;
						}
						else
						{
							duration = numFrames - kf.frame;
						}
						text = GetCameraName( (int)kf.ByteValue ); 
					}
					
					Rect kfRect = new Rect( FrameToX(kf.frame), trackRect.yMin, duration * kKeyFrameWidth, trackRect.height ); 
					
					if ( Event.current.type == EventType.MouseDown )
					{
						if ( kfRect.Contains(Event.current.mousePosition) )
						{
							if ( Event.current.button == 0 )
							{
								_selected = kf;
								_selectedTrack = track;
								_selectedGroup = group;
								
								if ( Event.current.modifiers == EventModifiers.Control )
								{
									_dragging = true;
								}
								else if ( Event.current.modifiers == EventModifiers.Alt )
								{
									MoveTargetToKeyFrame(group, track, kf );
								}
								
								Event.current.Use();
								return;
							}
							else if ( Event.current.button == 1 )
							{
								// delete 
								if ( Event.current.modifiers == EventModifiers.Shift )
								{
									track.Rmv(kf);							
								}
								else if ( Event.current.modifiers == EventModifiers.Control )
								{
									KeyFrame dup = kf.Clone();
									dup.frame = kf.frame+1;
									track.Add(dup);
								}
								
								Event.current.Use();
								return;		
							}
						}
					}
					GUI.color = (kf==_selected) ? Color.red : Color.white;
					GUI.Button(kfRect, text );
					GUI.color = Color.white;
					//DrawingUtils.Quad(kfRect, Color.green );
					
					++frameIndex;
				}
				
				++index;
			}
		}
		
		
		GUILayout.EndVertical();
	}
	
	private float _time = 0.0f;
	
	protected override bool CloseOnNonTargetSelection
	{
		get
		{
			return false;
		}
	}
	
	public override void Update ()
	{
		base.Update ();

		float dT  = Time.realtimeSinceStartup - _time;
		_time = Time.realtimeSinceStartup;
		//Debug.Log(dT);

		if ( Target != null )
		{
			dT = Mathf.Clamp(dT, 0.0f, Time.maximumDeltaTime);
			
			Target.UpdateTracks( dT);
		}
	}
	
	void CalculateLength()
	{
		int maxFrame = 0;
		foreach( var group in Target.Groups )
		{
			foreach( var track in group.tracks )
			{
				if ( track.frames.Count > 0 )
				{
					// TODO: duration?
					KeyFrame kf = track.frames[track.frames.Count-1];
					int duration = 1;
					maxFrame = Mathf.Max( maxFrame, kf.frame + duration ); 
				}
			}
		}
		Target.Length = (float)maxFrame / (float)Director.kFPS;
	}
	
	void Curve( Rect rc, EB.Director.BlendMode mode ) 
	{
		if ( Event.current.type != EventType.Repaint ) return;
		
		DrawingUtils.Clip(new Rect(0,0,10000,10000) );
		DrawingUtils.Quad(rc, Color.black);
		
		DrawingUtils.Text("Blend Curve", new Vector3(rc.xMin, rc.yMin,0), TextAnchor.LowerLeft);  
		
		DrawingUtils._solidMaterial.SetPass(0);
		GL.Begin(GL.LINES);
		GL.Color( Color.red );
		
		float y = rc.yMax;
		float x = rc.xMin;
		
		Vector3 last = new Vector3(x,y,0);		
		for ( int i = 0; i <= rc.width; ++i )
		{
			float t = Mathf.Clamp01( (float)i/rc.width );
			float v = EB.Director.Runtime.BlendableTrackInstance.Blend(mode,t);
			Vector3 next = new Vector3(x + t *rc.width, y-v*rc.height, 0.0f);
			
			GL.Vertex(last);
			GL.Vertex(next);
			last = next;
		}
		//GL.Vertex(last);
		//GL.Vertex3(rc.xMax,rc.yMin-1,0);
		
		GL.End();
		
	}
	
	public override void OnGUI()
	{		
		
		if ( Target == null )
		{
			return;
		}
		
		// toolbar
		var tbRect = new Rect(0,0,position.width,ToolBarHeight);
		GUILayout.BeginArea( tbRect);
		{
			GUILayout.BeginHorizontal();
			if ( GUILayout.Button("Save") )
			{
				CommitPrefab();
			}
			
			if ( Target.IsPlaying )
			{
				if ( GUILayout.Button("Stop") )
				{
					Target.Stop();
				}
			}
			else			
			{
				if ( GUILayout.Button("Play") )
				{
					Target.Play();
				}				
			}
			
						
			GUILayout.Label("Duration:");
			Target.Length = EditorGUILayout.FloatField(Target.Length );
			Target.Length = Mathf.Max(Target.Length,0);
			
			if ( GUILayout.Button("Auto") )
			{
				CalculateLength();
			}
			
			GUILayout.FlexibleSpace();
			
			if ( GUILayout.Button("Close") )
			{
				Close();
			}
			
			GUILayout.EndHorizontal();
		}	
		GUILayout.EndArea();
		
		// group pane
		var groupRect = new Rect(0,ToolBarHeight,GroupPaneWidth,position.height-ToolBarHeight-PropertiesHeight);
		GUILayout.BeginArea( groupRect ); 
		{
			Groups(groupRect);
		}
		GUILayout.EndArea();
		
		// timeline pane
		var tlRect = new Rect(GroupPaneWidth,ToolBarHeight,position.width-GroupPaneWidth,position.height-ToolBarHeight-PropertiesHeight);		
		GUILayout.BeginArea(tlRect);
		{
			Timeline(tlRect);
		}
		GUILayout.EndArea();
		
		// properties window
		GUILayout.BeginArea( new Rect( 0, position.height-PropertiesHeight, Mathf.Min(position.width, PropertiesWidth), PropertiesHeight) );
		{
			GUILayout.BeginVertical();
			
			GUILayout.Label("Properties:");
			
			if( _selected != null )
			{
				if ( _selected is Group )
				{
					GroupProperties( (Group)_selected);  
				}
				else if ( _selected is KeyFrame )
				{
					KeyFrameProperties( (KeyFrame)_selected ); 
				}
				else if ( _selected is Track )
				{
					TrackProperties( (Track)_selected ); 
				}
			}
			
			GUILayout.EndVertical();
		}
		GUILayout.EndArea();
		
		
		// curve window
		var curveRect = new Rect( PropertiesWidth + 50, position.height-PropertiesHeight + 50, 100, 100);
		{
			if ( _selected != null && _selected is KeyFrame )
			{
				Curve( curveRect, ((KeyFrame)_selected).mode ); 
			}			
		}
		
		// helper text
		GUILayout.BeginArea( new Rect(curveRect.xMax+50, position.height-PropertiesHeight, 400, PropertiesHeight) );
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Help:");
			GUILayout.Label("Add KeyFrame: Left Button + Shift");
			GUILayout.Label("Rmv KeyFrame: Right Button + Shift");
			GUILayout.Label("Move KeyFrame: Left Button + Ctrl");
			GUILayout.Label("Duplicate KeyFrame: Right Button + Ctrl");
			GUILayout.Label("Snap Target To KeyFrame: Left Button + Alt");
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
		}
		GUILayout.EndArea();
		
	}
	
	
}
