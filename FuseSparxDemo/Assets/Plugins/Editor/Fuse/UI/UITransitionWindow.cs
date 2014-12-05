using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class UITransitionWindow : EditorWindow 
{
	private UITransition _uiTransition;
	private UITransition.Transition _currentTransition;

	private UITransition.Track _clipboardTrack;
	private UITransition.Transition _clipboardTransition;
	
	public static void Init()
	{
		// Get existing open window or if none, make a new one:
		UITransitionWindow window = (UITransitionWindow) EditorWindow.GetWindow(typeof(UITransitionWindow));
		window.Focus();
	}
	
	//-------------------------------------------------------------------------------------------------
	void OnInspectorUpdate()
	{
		if (Selection.activeGameObject != null)
		{
			if (Selection.activeGameObject.GetComponent<UITransition>() != null)
			{
				_uiTransition = Selection.activeGameObject.GetComponent<UITransition>();
			}	
		}
	}
	
	private Vector2 _mainWindowScrollPosition;
	
	//-------------------------------------------------------------------------------------------------
	void OnGUI()
	{
		if (_uiTransition == null)
		{
			OnInspectorUpdate();	
		}
		
		if (_uiTransition == null) return;
		
		_mainWindowScrollPosition =	EditorGUILayout.BeginScrollView(_mainWindowScrollPosition);

		if (_uiTransition.Transitions != null && _uiTransition.Transitions.Length >= 2)
		{
			_uiTransition.Transitions[0] = _uiTransition.In;
			_uiTransition.Transitions[0].id = 0;
			_uiTransition.Transitions[1] = _uiTransition.Out;
			_uiTransition.Transitions[1].id = 1;
		}
		else
		{
			_uiTransition.Transitions = new UITransition.Transition[2];
			_uiTransition.Transitions[0] = _uiTransition.In;
			_uiTransition.Transitions[0].id = 0;
			_uiTransition.Transitions[1] = _uiTransition.Out;
			_uiTransition.Transitions[1].id = 1;
		}

		_uiTransition.Transitions[0].UITransition = _uiTransition;
		_uiTransition.Transitions[1].UITransition = _uiTransition;
		
		if (_uiTransition.CurrentFilterType == UITransition.FilterType.SHOW_ALL)
		{
			GUILayout.BeginHorizontal();
				GUI.backgroundColor = Color.blue;
				if (GUILayout.Button("Show All", GUILayout.Width(Screen.width*0.485f-5)))
				{
				}
				GUI.backgroundColor = Color.white;
				if (GUILayout.Button("Show Selected", GUILayout.Width(Screen.width*0.485f-5)))
				{
					_uiTransition.CurrentFilterType = UITransition.FilterType.SHOW_SELECTED;
				}
			GUILayout.EndHorizontal();
		}
		else if (_uiTransition.CurrentFilterType == UITransition.FilterType.SHOW_SELECTED)
		{
			GUILayout.BeginHorizontal();
				GUI.backgroundColor = Color.white;
				if (GUILayout.Button("Show All", GUILayout.Width(Screen.width*0.5f-5)))
				{
					_uiTransition.CurrentFilterType = UITransition.FilterType.SHOW_ALL;
				}
				GUI.backgroundColor = Color.blue;
				if (GUILayout.Button("Show Selected", GUILayout.Width(Screen.width*0.5f-5)))
				{
				}
			GUILayout.EndHorizontal();
			
			GUI.backgroundColor = Color.white;
		}
		
		if (_uiTransition.CurrentFilterType == UITransition.FilterType.SHOW_ALL)
		{
			_uiTransition.In.Name = "IN";
			_uiTransition.Out.Name = "OUT";
			DrawTransitionInspector(_uiTransition.In, false, 0);
			DrawTransitionInspector(_uiTransition.Out, false, 1);
			
			NGUIEditorTools.DrawSeparator();
			NGUIEditorTools.DrawSeparator();
			
			for (int i = 2; i < _uiTransition.Transitions.Length; i++)
			{
				DrawTransitionInspector(_uiTransition.Transitions[i], true, i);
			}
			
			if (GUILayout.Button("Add Transition", GUILayout.Width(Screen.width*0.97f-10)))
			{
				Undo.RecordObject(_uiTransition, "Add Transition");
				AddTransition();
			}
		}
		else
		{	
			for (int i = 0; i < _uiTransition.Transitions.Length; i++)
			{
				ShowSelectedIn(_uiTransition.Transitions[i], Selection.activeGameObject, (i > 1));
			}
		}	
		
		if (GUI.changed)
		{
			EditorUtility.SetDirty(_uiTransition.gameObject);
		}
		
		EditorGUILayout.EndScrollView();
	}
	
	//-------------------------------------------------------------------------------------------------
	private UITransition.Transition AddTransition(int index = 0)
	{
		if (index == 0)	
		{
			index = _uiTransition.Transitions.Length;
		}
		
		UITransition.Transition newTransition = new UITransition.Transition();
		newTransition.Name = "New Transition";
		newTransition.UITransition = _uiTransition;
		newTransition.id = _uiTransition.CurrentTransitionID;
		_uiTransition.CurrentTransitionID++;
		ArrayUtility.Insert<UITransition.Transition>(ref _uiTransition.Transitions, index, newTransition);
		
		return newTransition;
	}
	
	//-------------------------------------------------------------------------------------------------
	void OnSelectionChange()	
	{
		OnInspectorUpdate();
		Repaint();
	}
	
	//-------------------------------------------------------------------------------------------------
	private void ShowSelectedIn(UITransition.Transition transition, GameObject selectedObject, bool allowNameChange)
	{
		transition.GetDuration();
		
		GUI.contentColor = Color.green;
		transition.IsOpen = EditorGUILayout.Foldout(transition.IsOpen, transition.Name);
		GUI.contentColor = Color.white;
		
		if (!transition.IsOpen) return;
		
		GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Looping", GUILayout.Width(50));
			transition.IsLooping = EditorGUILayout.Toggle(transition.IsLooping, GUILayout.Width(20));
			EditorGUILayout.LabelField("Fast Devices Only", GUILayout.Width(100));
			transition.FastDeviceOnly = EditorGUILayout.Toggle(transition.FastDeviceOnly, GUILayout.Width(20));
		GUILayout.EndHorizontal();
		
		transition.SimTime = EditorGUILayout.Slider("Time", transition.SimTime, 0.0f, transition.Duration);
		
		GUILayout.BeginHorizontal();
			GUI.backgroundColor = Color.red;
			if (GUILayout.Button("Stop", GUILayout.Width(Screen.width*0.5f-5)))
			{
				transition.IsPreviewing = false;
			}
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Preview", GUILayout.Width(Screen.width*0.5f-5)))
			{
				_currentTransition = transition;
				transition.Preview();
			}
			GUI.backgroundColor = Color.white;
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
			if (GUILayout.Button("Jump To Start", GUILayout.Width(Screen.width*0.5f-5)))
			{
				transition.JumpToStart();
			}
			if (GUILayout.Button("Jump To End", GUILayout.Width(Screen.width*0.5f-5)))
			{
				transition.JumpToEnd();
			}
		GUILayout.EndHorizontal();
		
		if (allowNameChange)
		{
			transition.Name = EditorGUILayout.TextField("Name", transition.Name);
		}
		
		for (int i = 0; i < (int)UITransition.Track.TYPE.COUNT; i++)
		{
			UITransition.Track.TYPE type = (UITransition.Track.TYPE)i;
			
			GUILayout.Label("");
			GUI.contentColor = Color.yellow;
			GUILayout.Label(type.ToString().ToUpper().Replace("_"," ") + " TRACKS");
			GUI.contentColor = Color.white;
			GUILayout.Label("");
			
			for (int j = 0; j < transition.Tracks.Length; j++)
			{
				UITransition.Track track = transition.Tracks[j];
				
				if (track.Type != type) continue;
				
				if (track.Target == selectedObject)
				{
					DrawTrackInfo(track, transition, j);
					GUILayout.Label("");
				}
			}
			
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("+", GUILayout.Width(30)))
			{
				if (selectedObject != null)
				{
					Undo.RecordObject(_uiTransition, "Add Track");
					AddTrack(transition, type, selectedObject);
				}
			}
			GUI.backgroundColor = Color.white;
		}
		
		NGUIEditorTools.DrawSeparator();
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void DrawTransitionInspector(UITransition.Transition transition, bool allowNameChange, int index)
	{
		NGUIEditorTools.DrawSeparator();
		
		Color origColor = GUI.contentColor;
		
		transition.GetDuration();
		
		GUILayout.BeginHorizontal();
			GUI.color = Color.green;
			GUILayout.BeginHorizontal(GUILayout.Width(300.0f));
				transition.IsOpen = EditorGUILayout.Foldout(transition.IsOpen, transition.Name);
			GUILayout.EndHorizontal();
			if (index < 2)
			{
				GUILayout.Space(Screen.width-445f);
			}
			else
			{
				GUILayout.Space(Screen.width-505f);
			}
			GUI.color = Color.white;	
			ShowTransitionOptions(transition, index);
		GUILayout.EndHorizontal();
		
		if (!transition.IsOpen) return;
		
		GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Looping", GUILayout.Width(50));
			transition.IsLooping = EditorGUILayout.Toggle(transition.IsLooping, GUILayout.Width(20));
			EditorGUILayout.LabelField("Fast Devices Only", GUILayout.Width(100));
			transition.FastDeviceOnly = EditorGUILayout.Toggle(transition.FastDeviceOnly, GUILayout.Width(20));
		GUILayout.EndHorizontal();
		
		transition.SimTime = EditorGUILayout.Slider("Time", transition.SimTime, 0.0f, transition.Duration, GUILayout.Width(Screen.width*0.97f-5));
		
		GUILayout.BeginHorizontal();
		GUI.backgroundColor = Color.red;
			if (GUILayout.Button("Stop", GUILayout.Width(Screen.width*0.485f-5)))
			{
				transition.IsPreviewing = false;
			}
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Preview", GUILayout.Width(Screen.width*0.485f-5)))
			{
				_currentTransition = transition;
				transition.Preview();
			}
			GUI.backgroundColor = Color.white;
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
			if (GUILayout.Button("Jump To Start", GUILayout.Width(Screen.width*0.485f-5)))
			{
				transition.JumpToStart();
			}
			if (GUILayout.Button("Jump To End", GUILayout.Width(Screen.width*0.485f-5)))
			{
				transition.JumpToEnd();
			}
		GUILayout.EndHorizontal();
		
		if (allowNameChange)
		{
			transition.Name = EditorGUILayout.TextField("Name", transition.Name);
		}
		
		GUILayout.BeginHorizontal();
			transition.TrackType = (UITransition.Track.TYPE) EditorGUILayout.EnumPopup("Add Track", transition.TrackType, GUILayout.Width(Screen.width*0.485f-5));
			if (GUILayout.Button("+", GUILayout.Width(Screen.width*0.485f-5)))
			{
				Undo.RecordObject(_uiTransition, "Add Track");
				AddTrack(transition, transition.TrackType);
				GUILayout.EndHorizontal();
				return;
			}
		GUILayout.EndHorizontal();
		
		// Draw Tracks
		
		for (int i = 0; i < (int)UITransition.Track.TYPE.COUNT; i++)
		{
			UITransition.Track.TYPE type = (UITransition.Track.TYPE)i;
			
			if (transition.Tracks != null)
			{
				if (transition.Tracks.Length > 0)
				{
					int tracksToDraw = 0;
					
					for (int j = 0; j < transition.Tracks.Length; j++)
					{
						if (transition.Tracks[j] == null) continue;
						if (transition.Tracks[j].Type != type) continue;
						tracksToDraw++;
					}
					
					if (tracksToDraw > 0)
					{
						NGUIEditorTools.DrawSeparator();
						GUI.color = Color.yellow;
						transition.SetTracksOpen(type, EditorGUILayout.Foldout(transition.TracksOpen(type), type.ToString().ToUpper().Replace("_"," ") + " TRACKS (" + tracksToDraw + ")"));
						GUI.color = origColor;
						
						if (transition.TracksOpen(type))
						{
							for (int j = 0; j < transition.Tracks.Length; j++)
							{
								if (transition.Tracks[j] == null) continue;
								if (transition.Tracks[j].Type != type) continue;
								
								GUILayout.Label("");
								if (!DrawTrackInfo(transition.Tracks[j], transition, j)) break;
							}
						}
					}
				}
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private bool ShowTrackOptions(UITransition.Transition transition, UITransition.Track.TYPE type, int index)
	{
		int option = 0;
		
		if (GUILayout.Button("\u25B2", EditorStyles.toolbarButton, GUILayout.Width(30)))
		{
			UITransition.Track track = transition.Tracks[index];
			
			// Find prior position track and insert this track before it
			for (int i = index-1; i >= 0; i--)
			{
				if (transition.Tracks[i].Type == type)
				{
					// MOVE TRACK UP
					Undo.RecordObject(_uiTransition, "Move Track Up");
					ArrayUtility.RemoveAt<UITransition.Track>(ref transition.Tracks, index);
					ArrayUtility.Insert<UITransition.Track>(ref transition.Tracks, Mathf.Min(i, transition.Tracks.Length), track);
					return false;
				}	
			}
			return false;
		}
		if (GUILayout.Button("\u25BC", EditorStyles.toolbarButton, GUILayout.Width(30)))
		{
			UITransition.Track track = transition.Tracks[index];
			
			// Find next position track and insert this track before it
			for (int i = index + 1; i < transition.Tracks.Length; i++)
			{
				if (transition.Tracks[i].Type == type)
				{
					// MOVE TRACK DOWN
					Undo.RecordObject(_uiTransition, "Move Track Down");
					ArrayUtility.RemoveAt<UITransition.Track>(ref transition.Tracks, index);
					ArrayUtility.Insert<UITransition.Track>(ref transition.Tracks, Mathf.Min(i, transition.Tracks.Length), track);
					return false;
				}	
			}
			return false;
		}
		
		option = EditorGUILayout.Popup(option, new string[]{"Track Options", "Copy Track", "Paste Track", "Paste Track Reversed", "Insert Track Before", "Insert Track After", "Duplicate Track", "Delete Track"}, EditorStyles.toolbarButton, GUILayout.Width(110.0f));
			
		if (option == 0)
		{
			return true;
		}
		else if (option == 1) // COPY TRACK
		{
			Undo.RecordObject(_uiTransition, "Copy Track");
			_clipboardTrack = transition.Tracks[index];
		}
		else if (option == 2) // PASTE TRACK
		{
			if (_clipboardTrack == null)
			{
				EditorUtility.DisplayDialog("Paste Failed", "You must copy track data before attempting to paste it!", "Continue");
			}
			else
			{
				Undo.RecordObject(_uiTransition, "Paste Track");
				transition.Tracks[index] = CopyTrackInfo(_clipboardTrack, transition.Tracks[index], false, _clipboardTrack.Parent);
			}
		}
		else if (option == 3) // PASTE TRACK REVERSED
		{
			if (_clipboardTrack == null)
			{
				EditorUtility.DisplayDialog("Paste Failed", "You must copy track data before attempting to paste it!", "Continue");
			}
			else
			{
				Undo.RecordObject(_uiTransition, "Paste Track Reversed");
				transition.Tracks[index] = CopyTrackInfo(_clipboardTrack, transition.Tracks[index], true, _clipboardTrack.Parent);
			}
		}
		else if (option == 4) // INSERT TRACK BEFORE
		{
			Undo.RecordObject(_uiTransition, "Insert Track Before");
			AddTrack(transition, type, null, Mathf.Max(index, 0));
		}
		else if (option == 5) // INSERT TRACK AFTER
		{
			Undo.RecordObject(_uiTransition, "Insert Track After");
			AddTrack(transition, type, null, index+1);
		}
		else if (option == 6) // DUPLICATE TRACK
		{	
			Undo.RecordObject(_uiTransition, "Duplicate Track");
			UITransition.Track currentTrack = transition.Tracks[index];
			UITransition.Track newTrack = new UITransition.Track();
			newTrack = CopyTrackInfo(currentTrack, newTrack, false, transition);
			newTrack.Parent = currentTrack.Parent;
			ArrayUtility.Insert<UITransition.Track>(ref transition.Tracks, index+1, newTrack);
			return false;
		}
		else if (option == 7) // DELETE TRACK
		{
			if (EditorUtility.DisplayDialog("Delete Track", "Are you sure you'd like to delete this track?", "Delete", "Cancel"))
			{	
				Undo.RecordObject(_uiTransition, "Delete Track");
				ArrayUtility.RemoveAt<UITransition.Track>(ref transition.Tracks, index);
				return false;
			}
		}
			
		return true;
	}	
	
	//--------------------------------------------------------------------------------------------------------
	private void ShowTransitionOptions(UITransition.Transition transition, int index)
	{
		if (index > 1)
		{
			if (GUILayout.Button("\u25B2", EditorStyles.toolbarButton, GUILayout.Width(30)))
			{
				// MOVE TRACK UP
				Undo.RecordObject(_uiTransition, "Move Transition Up");
				ArrayUtility.RemoveAt<UITransition.Transition>(ref _uiTransition.Transitions, index);
				ArrayUtility.Insert<UITransition.Transition>(ref _uiTransition.Transitions, Mathf.Max(index-1, 2), transition);
			}
			if (GUILayout.Button("\u25BC", EditorStyles.toolbarButton, GUILayout.Width(30)))
			{
				// MOVE TRACK DOWN
				Undo.RecordObject(_uiTransition, "Move Transition Down");
				ArrayUtility.RemoveAt<UITransition.Transition>(ref _uiTransition.Transitions, index);
				ArrayUtility.Insert<UITransition.Transition>(ref _uiTransition.Transitions, Mathf.Min(index+1, _uiTransition.Transitions.Length), transition);
			}
		}
		
		int option = 0;
		
		if (index < 2)
		{
			option = EditorGUILayout.Popup(option, new string[]{"Transition Options", "Copy Transition", "Paste Transition", "Paste Transition Reversed", "Duplicate Transition", "Duplicate Transition Reversed"}, EditorStyles.toolbarButton, GUILayout.Width(110.0f));
		}
		else
		{
			option = EditorGUILayout.Popup(option, new string[]{"Transition Options", "Copy Transition", "Paste Transition", "Paste Transition Reversed", "Duplicate Transition", "Duplicate Transition Reversed", "Delete Transition"}, EditorStyles.toolbarButton, GUILayout.Width(110.0f));
		}
		
		if (option == 1)
		{
			Undo.RecordObject(_uiTransition, "Copy Transition");
			_clipboardTransition = transition;
		}
		else if (option == 2)
		{
			if (_clipboardTransition == null)
			{
				EditorUtility.DisplayDialog("Paste Failed", "You must copy transition data before attempting to paste it!", "Continue");
			}
			else
			{
				Undo.RecordObject(_uiTransition, "Paste Transition");
				CopyTransitionValues(_clipboardTransition, ref transition, false);
			}
		}
		else if (option == 3)
		{
			if (_clipboardTransition == null)
			{
				EditorUtility.DisplayDialog("Paste Failed", "You must copy transition data before attempting to paste it!", "Continue");	
			}
			else
			{
				Undo.RecordObject(_uiTransition, "Paste Transition Reversed");
				CopyTransitionValues(_clipboardTransition, ref transition, true);
			}
		}
		else if (option == 4)
		{
			Undo.RecordObject(_uiTransition, "Duplicate Transition");
			UITransition.Transition newTranstion = AddTransition(index+1);
			CopyTransitionValues(transition, ref newTranstion, false);
		}
		else if (option == 5)
		{
			Undo.RecordObject(_uiTransition, "Duplicate Transition Reversed");
			UITransition.Transition newTranstion = AddTransition(index+1);
			CopyTransitionValues(transition, ref newTranstion, true);
		}
		else if (option == 6)
		{
			Undo.RecordObject(_uiTransition, "Duplicate Transition");
			ArrayUtility.RemoveAt<UITransition.Transition>(ref _uiTransition.Transitions, index);
		}
	}	
	
	//--------------------------------------------------------------------------------------------------------
	private void CopyTransitionValues(UITransition.Transition fromTransition, ref UITransition.Transition toTransition, bool reverse)
	{
		toTransition.InitializeTracks();
		
		if (reverse)
		{
			for (int i = 0; i < fromTransition.Tracks.Length; i++)
			{
				int index = fromTransition.Tracks.Length-1-i;
				
				AddTrack(toTransition, fromTransition.Tracks[index].Type);
				toTransition.Tracks[i] = CopyTrackInfo(fromTransition.Tracks[index], toTransition.Tracks[i], reverse, fromTransition);
				toTransition.Tracks[i].Parent = toTransition;
			}
		}
		else
		{
			for (int i = 0; i < fromTransition.Tracks.Length; i++)
			{
				AddTrack(toTransition, fromTransition.Tracks[i].Type);
				toTransition.Tracks[i] = CopyTrackInfo(fromTransition.Tracks[i], toTransition.Tracks[i], reverse, fromTransition);
				toTransition.Tracks[i].Parent = toTransition;
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private UITransition.Track CopyTrackInfo(UITransition.Track fromTrack, UITransition.Track toTrack, bool reverse, UITransition.Transition transitionContainer)
	{
		UITransition.Track newTrack = new UITransition.Track();
		
		newTrack.Type = fromTrack.Type;
		
		newTrack.id = transitionContainer.UITransition.CurrentTrackID++;
		
		if (toTrack.Target != null)
		{
			newTrack.Target = toTrack.Target;
		}
		else
		{
			newTrack.Target = fromTrack.Target;
		}
		
		newTrack.Name = fromTrack.Name;
		
		newTrack.EasingType = fromTrack.EasingType;
		newTrack.AnimMode   = fromTrack.AnimMode;
		
		if (!reverse)
		{
			newTrack.StartVector3 = fromTrack.StartVector3;
			newTrack.EndVector3   = fromTrack.EndVector3;
			newTrack.StartFloat   = fromTrack.StartFloat;
			newTrack.EndFloat	 = fromTrack.EndFloat;
		}
		else
		{
			if (fromTrack.AnimMode == EZAnimation.ANIM_MODE.By)
			{
				newTrack.StartVector3 = fromTrack.StartVector3 + fromTrack.EndVector3;
				newTrack.EndVector3   = Vector3.Scale(fromTrack.EndVector3, new Vector3(-1,-1,-1));
			}
			else
			{
				newTrack.StartVector3 = fromTrack.EndVector3;
				newTrack.EndVector3   = fromTrack.StartVector3;
				newTrack.StartFloat   = fromTrack.EndFloat;
				newTrack.EndFloat     = fromTrack.StartFloat;
			}
		}
		
		transitionContainer.GetDuration();
		
		newTrack.StartTime = fromTrack.StartTime;
		newTrack.Duration  = fromTrack.Duration;
		
		/*
		if (transitionContainer == null || !reverse)
		{
			newTrack.StartTime = fromTrack.StartTime;
			newTrack.Duration  = fromTrack.Duration;
		}
		else if (transitionContainer != null && reverse)
		{
			newTrack.StartTime = Mathf.Max(transitionContainer.Duration-(fromTrack.StartTime + fromTrack.Duration), 0.0f);
			newTrack.Duration  = fromTrack.Duration;
		}
		*/
		
		// ALPHA SPECIFIC
		toTrack.AlphaType  = fromTrack.AlphaType;
		
		// PARTICLE SPECIFIC
		toTrack.ParticleTrackType = fromTrack.ParticleTrackType;
		
		newTrack.IsSet = true;
		
		return newTrack;
	}

	//--------------------------------------------------------------------------------------------------------
	private UITransition.Track AddTrack(UITransition.Transition transition, UITransition.Track.TYPE type, GameObject target = null, int index = -1)
	{	
		UITransition.Track track = new UITransition.Track();
		track.Type = type;
		track.Parent = _uiTransition.GetTransitionFromID(transition.id);
		track.id = _uiTransition.CurrentTrackID;
		
		_uiTransition.CurrentTrackID++;
		
		if (target != null)
		{
			track.Target = target;
		}
		
		if (index == -1)
		{
			ArrayUtility.Add<UITransition.Track>(ref transition.Tracks, track);
		}
		else
		{
			ArrayUtility.Insert<UITransition.Track>(ref transition.Tracks, index, track);
		}
			
		transition.SetTracksOpen(type, true);
		
		return track;
	}

	//--------------------------------------------------------------------------------------------------------
	private bool DrawTrackInfo(UITransition.Track track, UITransition.Transition transition, int index)
	{
		GUILayout.BeginHorizontal();
			GUI.color = Color.cyan;
			GUILayout.BeginHorizontal(GUILayout.Width(300.0f));
				track.IsOpen = EditorGUILayout.Foldout(track.IsOpen, track.DisplayedName);
			GUILayout.EndHorizontal();
			GUILayout.Space(Screen.width-500f);
			GUI.color = Color.white;	
			if (!ShowTrackOptions(transition, track.Type, index))
			{
				return false;
			}
		GUILayout.EndHorizontal();
		
		GameObject currentTarget = track.Target;
		
		if (track.IsOpen) track.Target = (GameObject) EditorGUILayout.ObjectField("Target", track.Target, typeof(GameObject), true, GUILayout.Width(Screen.width*0.45f-10));
		
		bool newTarget = (currentTarget != track.Target);
		
		switch (track.Type)
		{
			// Vector3 Values
			case UITransition.Track.TYPE.POSITION:
			case UITransition.Track.TYPE.ROTATION:
			case UITransition.Track.TYPE.SCALE:
			{
				if (track.IsOpen)
				{
					if (track.Target != null && !track.IsSet)
					{
						track.IsSet = true;
					
						if (track.Type == UITransition.Track.TYPE.POSITION)
						{
							track.StartVector3 = track.Target.transform.localPosition;
							track.EndVector3 = (track.AnimMode == EZAnimation.ANIM_MODE.By) ? Vector3.zero : track.Target.transform.localPosition;
						}
						else if (track.Type == UITransition.Track.TYPE.ROTATION)
						{
							track.StartVector3 = track.Target.transform.localEulerAngles;
							track.EndVector3 = (track.AnimMode == EZAnimation.ANIM_MODE.By) ? Vector3.zero : track.Target.transform.localEulerAngles;
						}
						else if (track.Type == UITransition.Track.TYPE.SCALE)
						{
							track.StartVector3 = track.Target.transform.localScale;
							track.EndVector3 = (track.AnimMode == EZAnimation.ANIM_MODE.By) ? Vector3.zero : track.Target.transform.localScale;
						}
					}
	
					GUILayout.BeginHorizontal();
						track.StartTime = EditorGUILayout.FloatField("Start Time", track.StartTime, GUILayout.Width(Screen.width*0.45f-5));
				
						GUILayout.Space(Screen.width*0.02f);
				
						string label = "";
						if (track.Type == UITransition.Track.TYPE.POSITION)
						{
							label = "Position";
						}
						else if (track.Type == UITransition.Track.TYPE.ROTATION)
						{
							label = "Rotation";
						}
						else if (track.Type == UITransition.Track.TYPE.SCALE)
						{
							label = "Scale";
						}
					
						string startPositionLabel = (track.AnimMode == EZAnimation.ANIM_MODE.FromTo) ? ("Start " + label) : ("Assumed Start " + label);
						int startSelection = 0;
						startSelection = EditorGUILayout.Popup(startSelection, new string[]{startPositionLabel, "Copy values from object", "Apply values to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
						track.StartVector3 = EditorGUILayout.Vector3Field("", track.StartVector3, GUILayout.Width(Screen.width*0.35f-5), GUILayout.Height(18.0f));
					
						if (startSelection == 1)
						{
							Undo.RecordObject(_uiTransition, "Copy Start Value");
					
							if (track.Type == UITransition.Track.TYPE.POSITION)
							{
								track.StartVector3 = track.Target.transform.localPosition;
							}
							else if (track.Type == UITransition.Track.TYPE.ROTATION)
							{
								track.StartVector3 = track.Target.transform.localEulerAngles;
							}
							else if (track.Type == UITransition.Track.TYPE.SCALE)
							{
								track.StartVector3 = track.Target.transform.localScale;
							}
						}
						else if (startSelection == 2)
						{
							Undo.RecordObject(_uiTransition, "Apply Start Value");
					
							if (track.Type == UITransition.Track.TYPE.POSITION)
							{
								track.Target.transform.localPosition = track.StartVector3;
							}
							else if (track.Type == UITransition.Track.TYPE.ROTATION)
							{
								track.Target.transform.localEulerAngles = track.StartVector3;
							}
							else if (track.Type == UITransition.Track.TYPE.SCALE)
							{
								 track.Target.transform.localScale = track.StartVector3;
							}
						}
					GUILayout.EndHorizontal();	
				
					GUILayout.BeginHorizontal();
						track.Duration = EditorGUILayout.FloatField("Duration", track.Duration, GUILayout.Width(Screen.width*0.45f-5));
						
						GUILayout.Space(Screen.width*0.02f);
				
						string endPositionLabel = (track.AnimMode == EZAnimation.ANIM_MODE.FromTo || track.AnimMode == EZAnimation.ANIM_MODE.To) ? ("End " + label) : "Move By";
						int endSelection = 0;
						endSelection = EditorGUILayout.Popup(endSelection, new string[]{endPositionLabel, "Copy values from object", "Apply values to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
						track.EndVector3 = EditorGUILayout.Vector3Field("", track.EndVector3, GUILayout.Width(Screen.width*0.35f-5), GUILayout.Height(18.0f));
						if (endSelection == 1)
						{
							Undo.RecordObject(_uiTransition, "Copy End Value");
					
							if (track.Type == UITransition.Track.TYPE.POSITION)
							{
								track.EndVector3 = track.Target.transform.localPosition;
							}
							else if (track.Type == UITransition.Track.TYPE.ROTATION)
							{
								track.EndVector3 = track.Target.transform.localEulerAngles;
							}
							else if (track.Type == UITransition.Track.TYPE.SCALE)
							{
								track.EndVector3 = track.Target.transform.localScale;
							}
						}
						else if (endSelection == 2)
						{
							Undo.RecordObject(_uiTransition, "Apply End Value");
					
							if (track.Type == UITransition.Track.TYPE.POSITION)
							{
								track.Target.transform.localPosition = track.EndVector3;
							}
							else if (track.Type == UITransition.Track.TYPE.ROTATION)
							{
								track.Target.transform.localEulerAngles = track.EndVector3;
							}
							else if (track.Type == UITransition.Track.TYPE.SCALE)
							{
								 track.Target.transform.localScale = track.EndVector3;
							}
						}
					GUILayout.EndHorizontal();	
					
					GUI.color = (track.ExtraOptionsOpen) ? Color.white : Color.grey;
					track.ExtraOptionsOpen = EditorGUILayout.Foldout(track.ExtraOptionsOpen, "More Options");
					GUI.color = Color.white;
				
					if (track.ExtraOptionsOpen)
					{
						GUILayout.BeginHorizontal();
							track.EasingType = (EZAnimation.EASING_TYPE)EditorGUILayout.EnumPopup("Easing", track.EasingType, GUILayout.Width(Screen.width*0.45f-5));
							GUILayout.Space(Screen.width*0.02f);
							track.AnimMode = (EZAnimation.ANIM_MODE)EditorGUILayout.EnumPopup("Animation Mode", track.AnimMode, GUILayout.Width(Screen.width*0.49f-5));
						GUILayout.EndHorizontal();	
					}
				}
			}
			break;
				
			// Widget Tracks
			case UITransition.Track.TYPE.WIDGET_SIZE:
			case UITransition.Track.TYPE.COLOR:
			case UITransition.Track.TYPE.ALPHA:
			{
				if (track.IsOpen)
				{
					if (track.Target != null && (!track.IsSet || newTarget))
					{
						// Get Panel or Widgets
						UIPanel panel = track.Target.GetComponent<UIPanel>();
					
						if (panel != null && track.Type == UITransition.Track.TYPE.ALPHA)
						{
							track.AlphaType = UITransition.Track.AlphaTrackType.PANEL;
							track.IsSet = true;
						
							if (track.Panel != null)
							{
								track.StartFloat = track.Panel.alpha;
								track.EndFloat = track.Panel.alpha;
							}
						}
						else
						{
							UIWidget widget = track.Target.GetComponent<UIWidget>();
							
							if (widget != null)
							{
								track.AlphaType = UITransition.Track.AlphaTrackType.WIDGET;
							}
							else
							{
								track.AlphaType = UITransition.Track.AlphaTrackType.CHILDREN;	
							}
						
							track.IsSet = true;
						
							switch (track.Type)
							{
								case UITransition.Track.TYPE.ALPHA:
								{
									if (track.Widgets[0] != null)
									{
										track.StartFloat = track.Widgets[0].alpha;
										track.EndFloat = track.Widgets[0].alpha;
									}
								}
								break;
							
								case UITransition.Track.TYPE.COLOR:
								{
									if (track.Widgets[0] != null)
									{
										track.StartColor = track.Widgets[0].color;
										track.EndColor = track.Widgets[0].color;
									}
								}
								break;
							
								case UITransition.Track.TYPE.WIDGET_SIZE:
								{
									if (track.Widgets[0] != null)
									{
										track.StartVector3 = new Vector3(track.Widgets[0].width, track.Widgets[0].height);
										track.EndVector3 = new Vector3(track.Widgets[0].width, track.Widgets[0].height);
									}
								}
								break;
							}
						}
					}
					
					GUILayout.BeginHorizontal();
						track.StartTime = EditorGUILayout.FloatField("Start Time", track.StartTime, GUILayout.Width(Screen.width*0.45f-5));
				
						GUILayout.Space(Screen.width*0.02f);
						
						int startSelection = 0;
				
						switch (track.Type)
						{
							case UITransition.Track.TYPE.ALPHA:
							{
								startSelection = EditorGUILayout.Popup(startSelection, new string[]{"Start Alpha", "Copy alpha from object", "Apply alpha to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.StartFloat = EditorGUILayout.Slider("", track.StartFloat, 0.0f, 1.0f, GUILayout.Width(Screen.width*0.35f-5));
				
								if (startSelection == 1)
								{
									Undo.RecordObject(_uiTransition, "Copy Start Value");
						
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										track.StartFloat = Selection.activeGameObject.GetComponent<UIPanel>().alpha;
									}
									else if (Selection.activeGameObject.GetComponent<UIWidget>())
									{
										track.StartFloat = Selection.activeGameObject.GetComponent<UIWidget>().alpha;
									}
								}
								else if (startSelection == 2)
								{
									Undo.RecordObject(_uiTransition, "Apply Start Value");
						
									track.UpdateAlpha(0.0f, true);
								}
							}
							break;
					
							case UITransition.Track.TYPE.COLOR:
							{
								startSelection = EditorGUILayout.Popup(startSelection, new string[]{"Start Color", "Copy color from object", "Apply color to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.StartColor = EditorGUILayout.ColorField("", track.StartColor, GUILayout.Width(Screen.width*0.35f-5)); 
					
								if (startSelection == 1)
								{
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										Undo.RecordObject(_uiTransition, "Copy Start Value");
							
										UIWidget widget = Selection.activeGameObject.GetComponent<UIWidget>();
										if (widget != null) track.StartColor = widget.color;
									}
								}
								else if (startSelection == 2)
								{
									Undo.RecordObject(_uiTransition, "Apply Start Value");
						
									track.UpdateColor(0.0f, true);
								}
							}
							break;
					
							case UITransition.Track.TYPE.WIDGET_SIZE:
							{
								startSelection = EditorGUILayout.Popup(startSelection, new string[]{"Start Size", "Copy size from object", "Apply size to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.StartVector3 = EditorGUILayout.Vector3Field("", track.StartVector3, GUILayout.Width(Screen.width*0.35f-5), GUILayout.Height(18.0f)); 
					
								if (startSelection == 1)
								{
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										Undo.RecordObject(_uiTransition, "Copy Start Value");
							
										UIWidget widget = Selection.activeGameObject.GetComponent<UIWidget>();
										if (widget != null && track.Widgets != null) track.StartVector3 = new Vector3(track.Widgets[0].width, track.Widgets[0].height);
									}
								}
								else if (startSelection == 2)
								{
									Undo.RecordObject(_uiTransition, "Apply Start Value");
						
									track.UpdateWidgetSize(0.0f, true);
								}
							}
							break;
						}
					GUILayout.EndHorizontal();	
				
					GUILayout.BeginHorizontal();
						track.Duration = EditorGUILayout.FloatField("Duration", track.Duration, GUILayout.Width(Screen.width*0.45f-5));
						
						GUILayout.Space(Screen.width*0.02f);
				
						int endSelection = 0;
						
						switch (track.Type)
						{
							case UITransition.Track.TYPE.ALPHA:
							{
								endSelection = EditorGUILayout.Popup(endSelection, new string[]{"End Alpha", "Copy values from object", "Apply values to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.EndFloat = EditorGUILayout.Slider("", track.EndFloat, 0.0f, 1.0f, GUILayout.Width(Screen.width*0.35f-5));
								if (endSelection == 1)
								{
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										Undo.RecordObject(_uiTransition, "Copy End Value");
										track.EndFloat = Selection.activeGameObject.GetComponent<UIPanel>().alpha;
									}
									else if (Selection.activeGameObject.GetComponent<UIWidget>())
									{
										Undo.RecordObject(_uiTransition, "Apply End Value");
										track.EndFloat = Selection.activeGameObject.GetComponent<UIWidget>().alpha;
									}
								}
								else if (endSelection == 2)
								{
									track.UpdateAlpha(track.Duration, true);
								}
							}
							break;
					
							case UITransition.Track.TYPE.COLOR:
							{
								endSelection = EditorGUILayout.Popup(startSelection, new string[]{"End Color", "Copy color from object", "Apply color to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.EndColor = EditorGUILayout.ColorField("", track.EndColor, GUILayout.Width(Screen.width*0.35f-5)); 
					
								if (endSelection == 1)
								{
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										Undo.RecordObject(_uiTransition, "Copy End Value");
										UIWidget widget = Selection.activeGameObject.GetComponent<UIWidget>();
										if (widget != null) track.EndColor = widget.color;
									}
								}
								else if (endSelection == 2)
								{
									Undo.RecordObject(_uiTransition, "Apply End Value");
									track.UpdateColor(track.Duration, true);
								}
							}
							break;
					
							case UITransition.Track.TYPE.WIDGET_SIZE:
							{
								endSelection = EditorGUILayout.Popup(startSelection, new string[]{"End Size", "Copy size from object", "Apply size to object"}, EditorStyles.label, GUILayout.Width(Screen.width*0.15f-5));
								track.EndVector3 = EditorGUILayout.Vector3Field("", track.EndVector3, GUILayout.Width(Screen.width*0.35f-5), GUILayout.Height(18.0f)); 
					
								if (endSelection == 1)
								{
									if (Selection.activeGameObject.GetComponent<UIPanel>())
									{
										UIWidget widget = Selection.activeGameObject.GetComponent<UIWidget>();
										if (widget != null && track.Widgets != null) 
										{
											if (track.Widgets.Count > 0)
											{
												Undo.RecordObject(_uiTransition, "Copy End Value");
												track.EndVector3 = new Vector3(track.Widgets[0].width, track.Widgets[0].height);
											}
										}
									}
								}
								else if (endSelection == 2)
								{
									Undo.RecordObject(_uiTransition, "Apply End Value");
									track.UpdateWidgetSize(track.Duration, true);
								}
							}
							break;
						}
					GUILayout.EndHorizontal();	
				
					if (track.AlphaType != UITransition.Track.AlphaTrackType.PANEL && track.Type != UITransition.Track.TYPE.WIDGET_SIZE)
					{
						track.ShowContents = EditorGUILayout.Foldout(track.ShowContents, "UIWidgets: " + track.Widgets.Count);
				
						if (track.ShowContents)
						{
							for (int i = 0; i < track.Widgets.Count; i++)
							{
								GUILayout.Label("          " + track.Widgets[i].name);		
							}
						}
					}
				
					GUI.color = (track.ExtraOptionsOpen) ? Color.white : Color.grey;
					track.ExtraOptionsOpen = EditorGUILayout.Foldout(track.ExtraOptionsOpen, "More Options");
					GUI.color = Color.white;
				
					if (track.ExtraOptionsOpen)
					{
						switch (track.Type)
						{
							case UITransition.Track.TYPE.ALPHA:
							{
								GUILayout.BeginHorizontal();
									track.EasingType = (EZAnimation.EASING_TYPE)EditorGUILayout.EnumPopup("Easing", track.EasingType, GUILayout.Width(Screen.width*0.45f-5));
									GUILayout.Space(Screen.width*0.02f);
									track.AlphaType = (UITransition.Track.AlphaTrackType)EditorGUILayout.EnumPopup("Target Type", track.AlphaType, GUILayout.Width(Screen.width*0.45f-5));
								GUILayout.EndHorizontal();
							}
							break;
						
							case UITransition.Track.TYPE.COLOR:
							{
								GUILayout.BeginHorizontal();
									UITransition.Track.AlphaTrackType origType = track.AlphaType;
									track.EasingType = (EZAnimation.EASING_TYPE)EditorGUILayout.EnumPopup("Easing", track.EasingType, GUILayout.Width(Screen.width*0.45f-5));
									GUILayout.Space(Screen.width*0.02f);
									track.AlphaType = (UITransition.Track.AlphaTrackType) EditorGUILayout.EnumPopup("Target Type", track.AlphaType, GUILayout.Width(Screen.width*0.45f-5));
									if (track.AlphaType == UITransition.Track.AlphaTrackType.PANEL) track.AlphaType = origType;
								GUILayout.EndHorizontal();
							}
							break;
						
							case UITransition.Track.TYPE.WIDGET_SIZE:
							{
								GUILayout.BeginHorizontal();
									track.EasingType = (EZAnimation.EASING_TYPE)EditorGUILayout.EnumPopup("Easing", track.EasingType, GUILayout.Width(Screen.width*0.45f-5));
								GUILayout.EndHorizontal();
							}
							break;
						}
					}
				}
			}
			break;
			
			case UITransition.Track.TYPE.PARTICLE:
			{
				if (track.IsOpen)
				{
					if (track.Target != null && !track.IsSet)
					{
						track.ParticleTrackType = UITransition.Track.ParticleType.TARGET;
						track.IsSet = true;
					}
					
					GUILayout.BeginHorizontal();
						track.StartTime = EditorGUILayout.FloatField("Start Time", track.StartTime, GUILayout.Width(Screen.width*0.45f-5));
						GUILayout.Space(Screen.width*0.02f);
						track.Duration = EditorGUILayout.FloatField("Stop Time", track.Duration, GUILayout.Width(Screen.width*0.50f-5));
					GUILayout.EndHorizontal();	
				
					track.ShowContents = EditorGUILayout.Foldout(track.ShowContents, "Particle Systems: " + track.ParticleSystems.Count);
					
					if (track.ShowContents)
					{
						for (int i = 0; i < track.ParticleSystems.Count; i++)
						{
							GUILayout.Label("          " + track.ParticleSystems[i].name);		
						}
					}
				
					GUI.color = (track.ExtraOptionsOpen) ? Color.white : Color.grey;
					track.ExtraOptionsOpen = EditorGUILayout.Foldout(track.ExtraOptionsOpen, "More Options");
					GUI.color = Color.white;
				
					if (track.ExtraOptionsOpen)
					{	
						track.ParticleTrackType = (UITransition.Track.ParticleType)EditorGUILayout.EnumPopup("Target Type", track.ParticleTrackType, GUILayout.Width(Screen.width*0.45f-5));
					}
				}
			}
			break;
			
			case UITransition.Track.TYPE.EVENT:
			{
				if (track.IsOpen)
				{
					GUILayout.BeginHorizontal();
						track.StartTime = EditorGUILayout.FloatField("Start Time", track.StartTime, GUILayout.Width(Screen.width*0.45f-5));
						GUILayout.Space(Screen.width*0.02f);
						track.Function = EditorGUILayout.TextField("Method Name", track.Function, GUILayout.Width(Screen.width*0.50f-5));
					GUILayout.EndHorizontal();	
				
					GUI.color = (track.ExtraOptionsOpen) ? Color.white : Color.grey;
					track.ExtraOptionsOpen = EditorGUILayout.Foldout(track.ExtraOptionsOpen, "More Options");
					GUI.color = Color.white;
				
					if (track.ExtraOptionsOpen)
					{	
						track.BoolValue = EditorGUILayout.Toggle("Fire as Game Event", track.BoolValue, GUILayout.Width(Screen.width*0.45f-5));
					}
				}
			}
			break;
		}
			
		// GENERIC EXTRA OPTIONS
		if (track.ExtraOptionsOpen && track.IsOpen)
		{
			DrawTrackNameOptions(track);
			DrawTriggerTrackOptions(transition, track, index);
		}
		
		return true;
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void DrawTrackNameOptions(UITransition.Track track)
	{
		track.Name = EditorGUILayout.TextField("Name", track.Name, GUILayout.Width(Screen.width*0.45f-5));
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void DrawTriggerTrackOptions(UITransition.Transition transition, UITransition.Track track, int index)
	{
		GUILayout.BeginHorizontal();
			int triggerSelection = 0;
			triggerSelection = EditorGUILayout.Popup(triggerSelection, new string[]{"Trigger", "Paste Track", "Use Previous Track", "Use Next Track", "Clear"}, EditorStyles.toolbarButton, GUILayout.Width(150));
		
			if (triggerSelection == 1)
			{
				if (_clipboardTrack != null && _clipboardTrack.Parent != null)
				{
					if (_clipboardTrack.Parent.UITransition != null)
					{
						Undo.RecordObject(_uiTransition, "Paste Trigger Track");
						track.SetTriggerTrack(_clipboardTrack.Parent.UITransition, _clipboardTrack.Parent.id, _clipboardTrack.id);
					}
				}	
			}
			else if (triggerSelection == 2)
			{
				UITransition.Track trackToUse = null;
			
				for (int i = 0; i < index; i++)
				{
					if (transition.Tracks[i].Type == track.Type)
					{
						trackToUse = transition.Tracks[i];
					}
				}
			
				if (trackToUse != null) 
				{
					Undo.RecordObject(_uiTransition, "Set Trigger Track");
					track.SetTriggerTrack(trackToUse.Parent.UITransition, trackToUse.Parent.id, trackToUse.id);
				}
			}
			else if (triggerSelection == 3)
			{
				UITransition.Track trackToUse = null;
			
				for (int i = index+1; i < transition.Tracks.Length; i++)
				{
					if (transition.Tracks[i].Type == track.Type)
					{
						trackToUse = transition.Tracks[i];
						break;
					}
				}
			
				if (trackToUse != null) 
				{
					Undo.RecordObject(_uiTransition, "Set Trigger Track");
					track.SetTriggerTrack(trackToUse.Parent.UITransition, trackToUse.Parent.id, trackToUse.id);
				}
			}
			else if (triggerSelection == 4)
			{
				Undo.RecordObject(_uiTransition, "Clear Trigger Track");
				track.SetTriggerTrack(null,-1,-1);
			}
			string triggerTrackLabel = (track.TriggerTrack != null) ? (track.TriggerTrack.DisplayedName + " (" + track.TriggerTrack.Type.ToString() + ")") : "No Trigger Track";
			GUILayout.Label(triggerTrackLabel);
		GUILayout.EndHorizontal();
		
		if (track.TriggerTrack != null)
		{
			GUILayout.BeginHorizontal();
				track.TriggerMode = (UITransition.Track.TriggerType) EditorGUILayout.EnumPopup("Mode", track.TriggerMode, GUILayout.Width(Screen.width*0.45f-5f));
			GUILayout.EndHorizontal();
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void SetDirty()
	{
		EditorUtility.SetDirty(_uiTransition.gameObject);
	}	
	
	//--------------------------------------------------------------------------------------------------------
	// To deal with previewing
	//--------------------------------------------------------------------------------------------------------
	private void CallbackFunction()
	{
		if (_currentTransition != null)
		{
			if (_uiTransition != null)
			{
				if (_currentTransition.IsPreviewing)
				{
					_uiTransition.OfflineUpdate(_currentTransition);
				}
			}
		}
	}
	//--------------------------------------------------------------------------------------------------------
	void OnEnable()
	{
		EditorApplication.update += CallbackFunction;
	}
	//--------------------------------------------------------------------------------------------------------
	void OnDisable()
	{
		EditorApplication.update -= CallbackFunction;
	}
	//--------------------------------------------------------------------------------------------------------
}