using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BlockerFlag = BusyBlockerManager.BlockerFlag;

public class DebugMenu : MonoBehaviour 
{
	// To add more options to the list of debug menu categories, add them to this enum and make a Draw_* method.
	private enum DebugMenuMode
	{
		Main,
		UI,
		Assets,
	};
	
	public static bool Show {get;set;}

	private const float hiResButtonSize = 70.0f;
	private const float loResButtonSize = 35.0f;
	private const float editorOffset = 75.0f;
	private const float deviceRetinaOffset = 40.0f;
	private const float deviceOffset = 20f;
	
	private DebugMenuMode mCurrentMode = DebugMenuMode.Main;
	private Vector2 scrollPos = new Vector2();
	private bool _waiting = false;
	private float _buttonSize;
	private float _yOffset;

	// Debug UI BG Textures
	private Texture2D uiRedBgTexture = null;
	private Texture2D uiGreenBgTexture = null;
	private Texture2D uiBlackBgTexture = null;

	// UI Category settings
	private bool _showBusyBlockerFlags = false;
	private bool _windowManagerTestingOpen = false;
	private bool _texturePoolManagerTestingOpen = false;
	private bool _coreAssetsContentsTestingOpen = false;
	private bool _loadedTexturesDisplayOpen = false;
	private bool _loadingScreenReasonListOpen = false;
	private bool _maskMaterialManagementOpen = false;
	private string _cachedLoadedTextures = "";
	private WindowManager.WindowInfo debugMenuInfo = new WindowManager.WindowInfo("DebugMenu", WindowManager.WindowLayer.BusyBlocker, null, null, null);

	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
	
	private void Start()
	{
		Input.multiTouchEnabled = true;
	}

	//--------------------------------------------------------------------------------------------------------
	void SetDebugMode( DebugMenuMode mode )
	{
		mCurrentMode = mode;
		scrollPos.x = 0.0F;
		scrollPos.y = 0.0F;
	}
	
	//--------------------------------------------------------------------------------------------------------
	public void OnGUI()
	{
		_buttonSize = loResButtonSize;
		if (Misc.IsRetina())
		{
			_buttonSize = hiResButtonSize;
		}
		
		_yOffset = deviceOffset;
		if(Application.isEditor)
		{
			_yOffset = editorOffset;
		}
		else if (Misc.IsRetina())
		{
			_yOffset = deviceRetinaOffset;
		}
		
		Event e = Event.current;
		
		if (Application.isEditor)
		{
			if (Event.current.type == EventType.KeyUp)
			{
				switch (e.keyCode)
				{
				case KeyCode.Backslash:
					Show = !Show;
					Event.current.Use();
					ToggleBlockingUI(Show);
					break;
				}
			}
		}
		
		if (Show)
		{
			GUI.Window( 9999, new Rect(Screen.width * 0.25f, _yOffset, Screen.width*0.5f, Screen.height * 0.8f), Window, "Console" );
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void Window( int id )
	{
		GUIStyle scrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
		if (!Application.isEditor)
		{
			scrollbarStyle.padding = new RectOffset((int)_buttonSize/3, 0,0,0); 
			
			scrollbarStyle.fixedWidth = _buttonSize;
			scrollbarStyle.stretchWidth = true;
		}
		
		scrollPos = GUILayout.BeginScrollView(scrollPos,GUI.skin.horizontalScrollbar,scrollbarStyle, null);

		// Close button.
		if (GUILayout.Button("Close", GUILayout.MinHeight(_buttonSize)))
		{
			Show = false;
			ToggleBlockingUI(false);
		}

		// Back button.
		if (mCurrentMode != DebugMenuMode.Main)
		{
			if (GUILayout.Button("Back", GUILayout.MinHeight(_buttonSize)))
			{
				SetDebugMode(DebugMenuMode.Main);
			}
		}
		
		GUILayout.Space(10);
		
		// Display current debug mode.
		try
		{
			var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
			GetType().InvokeMember("Draw_" + mCurrentMode, flags, null, this, null);
		}
		catch(System.Exception e)
		{
			GUILayout.Label(e.ToString());
		}
		
		GUILayout.Space(10);
		
		GUILayout.EndScrollView();
	}

	//--------------------------------------------------------------------------------------------------------
	private void ToggleBlockingUI(bool show)
	{
		if (WindowManager.Instance != null)
		{
			WindowInputBlocker wib = WindowManager.Instance.windowInputBlocker;
			if (wib != null)
			{
				wib.BlockWindow(debugMenuInfo, show);
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void Update()
	{
		if (!Application.isEditor)
		{
			if (Input.touchCount >= 4 && !_waiting)
			{
				Show = !Show;
				_waiting = true;
				Invoke("StopWaiting", 1.0f);
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void StopWaiting()
	{
		_waiting = false;
	}
	
	//--------------------------------------------------------------------------------------------------------
	private void Draw_Main()
	{
		if (EB.Sparx.Hub.Instance != null && EB.Sparx.Hub.Instance.LoginManager != null)
		{
			GUILayout.Label("LocalUserName: " + (EB.Sparx.Hub.Instance.LoginManager.LocalUser.HasName ? EB.Sparx.Hub.Instance.LoginManager.LocalUser.Name : "no name"));
		}
		
		if (EB.Sparx.Hub.Instance != null && EB.Sparx.Hub.Instance.LoginManager != null)
		{
			GUILayout.Label("User ID: " + EB.Sparx.Hub.Instance.LoginManager.LocalUser.Id);
		}
		
		GUILayout.Label("TimeZone Offset: " + EB.Version.GetTimeZoneOffset() );
		
		if (GUILayout.Button("Quit", GUILayout.MinHeight(_buttonSize)))
		{
			Application.Quit();
		}
		
		if(GUILayout.Button("Salesforce", GUILayout.MinHeight(_buttonSize)))
		{
			SparxHub.Instance.LoginManager.GetSupportUrl( delegate( string err, string url ){
				if( string.IsNullOrEmpty( url ) == false )
				{
					Application.OpenURL( url );
				}
			});
		}


		if(GUILayout.Button("Link Account", GUILayout.MinHeight(_buttonSize)))
		{
			SparxHub.Instance.LoginManager.Link("facebook");
		}

		
		if(GUILayout.Button("Simulate Disconnect", GUILayout.MinHeight(_buttonSize)))
		{
			if(SparxHub.Instance.LoginManager.State == EB.Sparx.SubSystemState.Connected)
			{
				SparxHub.Instance.FatalError("Debug Disconnect");
			}
		}
		
		List<DebugMenuMode> debugMenuModes = new List<DebugMenuMode>();
		foreach( DebugMenuMode mode in System.Enum.GetValues(typeof(DebugMenuMode)))
		{
			debugMenuModes.Add(mode);
		}
		
		debugMenuModes.Sort(delegate(DebugMenuMode s1, DebugMenuMode s2) {
			return s1.ToString().CompareTo(s2.ToString());
		});
		
		foreach( DebugMenuMode mode in debugMenuModes)
		{
			if (mode != DebugMenuMode.Main && GUILayout.Button(mode.ToString(), GUILayout.MinHeight(_buttonSize)))
			{
				SetDebugMode(mode);
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------
	private int IntField( int value, params GUILayoutOption[] options )
	{
		var str = GUILayout.TextField( value.ToString(), options );
		int newValue;
		if (int.TryParse(str, out newValue))
		{
			return newValue;
		}
		return value;
	}

	private void Draw_Assets()
	{
		float windowWidth = Screen.width * 0.5f;

		// Core Assets contents
		ShowUiDebugBool("EB.Assets Contents", ref _coreAssetsContentsTestingOpen);
		
		float labelWidth = windowWidth * 0.06f;
		GUILayoutOption[] miniLabelOpt = { GUILayout.Width(labelWidth) };
		
		if (_coreAssetsContentsTestingOpen)
		{
			GUI.color = Color.white;
			
			GUIStyle uiCoreAssetsStyle = new GUIStyle();
			if (uiGreenBgTexture == null)
			{
				uiGreenBgTexture = MakeTex(1, 1, new Color(0.0f, 0.5f, 0.0f, 0.8f));
			}
			uiCoreAssetsStyle.normal.background = uiGreenBgTexture;
			GUILayout.BeginVertical(uiCoreAssetsStyle);
			var data = EB.Assets.GetUseCounts();
			// Copy into tuple for sorting.
			data.Sort(delegate(EB.Collections.Tuple<string, int> p1, EB.Collections.Tuple<string, int> p2) {
				return p1.Item1.CompareTo(p2.Item1);
			});

			// Display sorted list:
			for (int i = 0; i < data.Count; ++i)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(data[i].Item1);
				GUILayout.Label(data[i].Item2.ToString(), miniLabelOpt);
				GUI.color = Color.white;
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}
		
	}
	
	private void Draw_UI()
	{
		float windowWidth = Screen.width * 0.5f;
		float btnWidth = windowWidth / 5f;
		
		GUILayoutOption[] btnOpt = { GUILayout.Width(btnWidth), GUILayout.Height(_buttonSize) };

		GUIStyle uiStyle = new GUIStyle();
		if (uiBlackBgTexture == null)
		{
			uiBlackBgTexture = MakeTex(1, 1, new Color(0.0f, 0.0f, 0.0f, 0.8f));
		}
		uiStyle.normal.background = uiBlackBgTexture;
		GUILayout.BeginVertical(uiStyle);

		// Show / Hide WindowManagerDebug.
		if (WindowManager.Instance != null)
		{
			GameObject root = WindowManager.Instance.GetUiRoot();
			GameObject wmd = EB.Util.GetObjectExactMatch(root, "WindowManagerDebug");
			if (wmd != null)
			{
				WindowManagerDebug windowManagerDebug = wmd.GetComponent<WindowManagerDebug>();
				if (windowManagerDebug != null)
				{
					bool debugActive = windowManagerDebug.gameObject.activeSelf;
					bool prevDebugActive = debugActive;
					ShowUiDebugBool("WindowManagerDebug", ref debugActive, "SHOW", "HIDE");
					if (prevDebugActive != debugActive)
					{
						windowManagerDebug.gameObject.SetActive(debugActive);
					}
				}
				else
				{
					GUI.color = Color.red;
					GUILayout.Label("Missing Component: WindowManagerDebug");
				}
			}
			else
			{
				GUI.color = Color.red;
				GUILayout.Label("Missing Container: WindowManagerDebug");
			}
		}
		
		// Testing UIResolution
		if (UIResolutionManager.Instance != null)
		{
			UIResolutionManager.Resolution res = UIResolutionManager.Instance.CurrentResolution;
			UIResolutionManager.Resolution[] allRes = EB.Util.GetEnumValues<UIResolutionManager.Resolution>();
			
			GUILayout.BeginHorizontal();
			GUI.color = Color.white;
			GUILayout.Label("UI Resolution");
			
			foreach (UIResolutionManager.Resolution r in allRes)
			{
				GUI.color = (r == res) ? Color.green : Color.grey;
				if (GUILayout.Button(r.ToString().ToUpper(), btnOpt))
				{
					UIResolutionManager.Instance.SwitchResolution(r);
				}
			}
			GUILayout.EndHorizontal();
		}
		else
		{
			GUI.color = Color.red;
			GUILayout.Label("Missing Component: UIResolutionManager");
		}
		
		if (WindowManager.Instance != null)
		{
			WindowInputBlocker wib = WindowManager.Instance.windowInputBlocker;
			if (wib != null)
			{
				bool blockerActive = wib.IsDisplayingWidgets;
				bool prevBlockerActive = blockerActive;
				ShowUiDebugBool("Show Input Blocker", ref blockerActive);
				if (prevBlockerActive != blockerActive)
				{
					wib.DisplayWidgets(blockerActive);
				}
			}
			else
			{
				GUILayout.Label("Missing Component: WindowInputBlocker");
			}
		}
		else
		{
			GUI.color = Color.red;
			GUILayout.Label("Missing Component: WindowManager");
		}

		// Testing Busy Blocker
		BusyBlockerManager bbm = BusyBlockerManager.Instance;
		if (bbm != null)
		{
			ShowUiDebugBool("Busy Blocker Flags", ref _showBusyBlockerFlags);
			
			if (_showBusyBlockerFlags)
			{
				GUIStyle uiBlockerStyle = new GUIStyle();
				if (uiRedBgTexture == null)
				{
					uiRedBgTexture = MakeTex(1, 1, new Color(0.5f, 0.0f, 0.0f, 0.8f));
				}
				uiBlockerStyle.normal.background = uiRedBgTexture;
				GUILayout.BeginVertical(uiBlockerStyle);
				BlockerFlag[] flags = EB.Util.GetEnumValues<BlockerFlag>();
				foreach (BlockerFlag flag in flags)
				{
					bool flagBool = BusyBlockerManager.Instance.CheckBlocker(flag);
					bool oldFlagBool = flagBool;
					ShowUiDebugBool(flag.ToString(), ref flagBool, "YES", "NO");
					if (flagBool != oldFlagBool)
					{
						if (flagBool)
						{
							BusyBlockerManager.Instance.AddBlocker(flag);
						}
						else
						{
							BusyBlockerManager.Instance.RemoveBlocker(flag);
						}
					}
				}
				GUILayout.EndVertical();
				GUILayout.Space(_buttonSize / 3f);
			}
		}
		else
		{
			GUI.color = Color.red;
			GUILayout.Label("Missing Component: BusyBlockerManager");
		}
		
		// Testing Window Manager
		if (WindowManager.Instance != null)
		{
			ShowUiDebugBool("WindowManager Testing", ref _windowManagerTestingOpen);
			
			if (_windowManagerTestingOpen)
			{
				GUIStyle uiLayerStyle = new GUIStyle();
				if (uiGreenBgTexture == null)
				{
					uiGreenBgTexture = MakeTex(1, 1, new Color(0.0f, 0.5f, 0.0f, 0.8f));
				}
				uiLayerStyle.normal.background = uiGreenBgTexture;
				foreach (WindowManager.WindowLayer layer in WindowManager.GetWindowLayers())
				{
					GUILayout.BeginVertical(uiLayerStyle);
					GUI.color = Color.yellow;
					GUILayout.Label(layer.ToString());
					GUI.color = Color.white;
					List<string> windowsInLayer = GetAllWindowsInLayer(layer);
					foreach (string winName in windowsInLayer)
					{
						ShowOpenWindow(winName, layer);
					}
					GUILayout.EndVertical();
					GUILayout.Space(4f);
				}
				if (GUILayout.Button("Close All", btnOpt))
				{
					WindowManager.Instance.CloseAll();
				}
			}
		}

		// Texture Pool Manager contents
		if (TexturePoolManager.Instance != null)
		{
			ShowUiDebugBool("TexturePoolManager Contents", ref _texturePoolManagerTestingOpen);
			
			if (_texturePoolManagerTestingOpen)
			{
				GUI.color = Color.white;
				// Testing texture pool manager
				string data = TexturePoolManager.Instance.ToString();
				bool prevWordWrap = GUI.skin.button.wordWrap;
				GUI.skin.button.wordWrap = true;
				GUILayout.Label(data);
				GUI.skin.button.wordWrap = prevWordWrap;

				if (GUILayout.Button("Clear Pool", btnOpt))
				{
					TexturePoolManager.Instance.ClearPool(true);
				}
			}
		}
		else
		{
			GUI.color = Color.red;
			GUILayout.Label("Missing Component: TexturePoolManager");
		}

		// Texture usage debug
		{
			bool prevOpen = _loadedTexturesDisplayOpen;
			ShowUiDebugBool("Display Loaded Textures", ref _loadedTexturesDisplayOpen);
			
			// This is a really heavy process, so don't try to do it every update.
			if (prevOpen != _loadedTexturesDisplayOpen && _loadedTexturesDisplayOpen)
			{
				_cachedLoadedTextures = "(Cached Results @ " + Time.realtimeSinceStartup + ")\n\n";
				_cachedLoadedTextures += PrintType(typeof(Texture));
			}
			
			if (_loadedTexturesDisplayOpen)
			{
				GUI.color = Color.white;
				
				bool prevWordWrap = GUI.skin.button.wordWrap;
				GUI.skin.button.wordWrap = true;
				GUILayout.Label(_cachedLoadedTextures);
				GUI.skin.button.wordWrap = prevWordWrap;
			}
		}
		
		// Loading screen reasons debug
		if (WindowManager.Instance != null)
		{
			ShowUiDebugBool("Loading Screen Reason List", ref _loadingScreenReasonListOpen);
			
			if (_loadingScreenReasonListOpen)
			{
				GUI.color = Color.white;
				string data = "";
				foreach (string reason in WindowManager.Instance.GetLoadingScreenReasons())
				{
					data += reason + "\n";
				}
				bool prevWordWrap = GUI.skin.button.wordWrap;
				GUI.skin.button.wordWrap = true;
				GUILayout.Label(data);
				GUI.skin.button.wordWrap = prevWordWrap;
			}
		}

		ShowUiDebugBool("Masked Sprite Material Management", ref _maskMaterialManagementOpen);

		if (_maskMaterialManagementOpen)
		{
			float countWidth = windowWidth / 10f;
			// Masked sprite material management debug
			var useCounts = UIMaskMaterialManager.GetUseCounts();
			foreach (var item in useCounts)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(item.Item1);
				GUILayout.Label(item.Item2.ToString(), GUILayout.Width(countWidth));
				GUILayout.EndHorizontal();
			}
		}

		GUILayout.EndVertical();
	}

	private static Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] pix = new Color[width*height];
		
		for(int i = 0; i < pix.Length; i++)
			pix[i] = col;
		
		Texture2D result = new Texture2D(width, height);
		result.name = "Debug Menu Texture";
		result.SetPixels(pix);
		result.Apply();
		return result;
	}
	
	//--------------------------------------------------------------------------------------------------------
	// UI Mode helper function.
	private void ShowOpenWindow(string windowName, WindowManager.WindowLayer layer)
	{
		float windowWidth = Screen.width * 0.5f;
		float btnWidth = windowWidth / 5f;
		GUILayoutOption[] btnOpt = { GUILayout.Width(btnWidth), GUILayout.Height(_buttonSize) };
		
		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("{0} ({1})", windowName, layer.ToString()));
		if (GUILayout.Button("OPEN", btnOpt))
		{
			WindowManager.Instance.Open(layer, windowName);
		}
		if (GUILayout.Button("CLOSE", btnOpt))
		{
			WindowManager.Instance.Close(layer, windowName);
		}
		GUILayout.EndHorizontal();
	}
	
	private string ObjectPath( UnityEngine.Object obj )
	{
		Transform parent = null;
		if ( obj is GameObject )
		{
			parent = ((GameObject)obj).transform.parent;
		}
		else if ( obj is Component )
		{
			try
			{
				parent = ((Component)obj).transform.parent;
			}
			catch {}
		}
		
		if ( parent != null )
		{
			return ObjectPath(parent) + "/" + obj.name;
		}
		
		return obj.name;
	}
	
	private string PrintType( System.Type type ) 
	{
		var all = Resources.FindObjectsOfTypeAll( type );
		var list = new List<string>(all.Length);
		
		foreach( Object obj in all )
		{
			if (obj is Texture)
			{
				Texture t = obj as Texture;
				list.Add( string.Format("{0},{1},{2}x{3}, {4}", obj.GetType().Name, ObjectPath(obj), t.width, t.height, Profiler.GetRuntimeMemorySize(obj)));
			}
			else if (obj is Mesh)
			{
				Mesh m = obj as Mesh;
				list.Add( string.Format("{0}: {1}, v:{2}, id:{3} mem:{4}", obj.GetType().Name, ObjectPath(obj),  m.vertexCount, obj.GetInstanceID(), Profiler.GetRuntimeMemorySize(obj) ) );
			}
			else
			{
				list.Add( string.Format("{1}: {0}, id:{2} mem:{3}", ObjectPath(obj), obj.GetType().Name, obj.GetInstanceID(), Profiler.GetRuntimeMemorySize(obj) ) );
			}
		}
		list.Sort();
		
		var sb = new System.Text.StringBuilder();
		sb.AppendFormat("{0} objects\n", all.Length);
		foreach( string obj in list )
		{
			sb.AppendFormat("\t{0}\n",obj);
		}
		return sb.ToString();
	}
	
	private List<string> GetAllWindowsInLayer(WindowManager.WindowLayer layer)
	{
		string path = WindowManager.Instance.GetLayerPath(layer);
		UnityEngine.Object[] objects = Resources.LoadAll(path, typeof(GameObject));
		List<string> windowNames = new List<string>();
		foreach (UnityEngine.Object o in objects)
		{
			GameObject prefab = o as GameObject;
			if (prefab != null)
			{
				if (prefab.GetComponent<Window>() != null)
				{
					windowNames.Add(o.name);
				}
			}
		}
		
		return windowNames;
	}
	
	private void ShowUiDebugBool(string name, ref bool open, string trueName = "OPEN", string falseName = "CLOSE")
	{
		float windowWidth = Screen.width * 0.5f;
		float btnWidth = windowWidth / 5f;
		
		GUILayoutOption[] btnOpt = { GUILayout.Width(btnWidth), GUILayout.Height(_buttonSize) };
		
		GUILayout.BeginHorizontal();
		GUI.color = Color.white;
		GUILayout.Label(name);
		GUI.color = (open) ? Color.green : Color.grey;
		if (GUILayout.Button(trueName, btnOpt))
		{
			open = true;
		}
		GUI.color = (!open) ? Color.green : Color.grey;
		if (GUILayout.Button(falseName, btnOpt))
		{
			open = false;
		}
		GUILayout.EndHorizontal();
	}
}
