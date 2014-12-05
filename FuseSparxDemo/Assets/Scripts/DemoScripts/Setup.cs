using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EB.Sparx;
using System;

public class Setup : MonoBehaviour {
	
	public static Setup Instance { get; private set; }
	
	public bool DidShowCrash { get; private set; }
	public bool LoadMainSceneOnLogin { get; private set; }
	public bool IsLoadingMainScene { get; private set; }
	
	public static string ApiEndPoint = "https://api.sandbox.sparx.io";
	public static EB.Language Locale = EB.Language.Unknown;
	public static bool NoStats = false;

	[HideInInspector]
	public string ApiKey = "]!Q>>r21CHR<GG]||@s/6qc/^w3+kw?|Qty3}N|Kb|H+qK(<Comba/g^+1-_tQ)W";

	public string[] managers;

	void Awake()
	{
		Instance = this;
		
		Debug.Log("**********************************");
		Debug.Log("Model " + SystemInfo.deviceModel );
		Debug.Log("CPU " + SystemInfo.processorType );
		Debug.Log("GPU " + SystemInfo.graphicsDeviceName );
		Debug.Log("**********************************");
		
		string countryCode = EB.Version.GetCountryCode().ToLower();
		EB.Options.defaultUnit = (countryCode.Equals("us") || countryCode.Equals("uk")) ? 1 : 0; // 1 is imperial, 0 is metric 
		
		this.DidShowCrash = false;
		this.LoadMainSceneOnLogin = true;
		this.IsLoadingMainScene = false;
	}
	
	public void OnLoggedIn()
	{
		if( this.LoadMainSceneOnLogin == true )
		{
			this.LoadMainScene();
		}

		if (SparxHub.Instance.LoginManager.LocalUser.HasName==false) {
			SparxHub.Instance.LoginManager.SetName("newb"+SparxHub.Instance.LoginManager.LocalUserId, delegate(string err){
				EB.Debug.Log("SetName: {0}", SparxHub.Instance.LoginManager.LocalUser.Name);
			});
		}
	}
	
	public void LoadMainScene()
	{
		if( this.IsLoadingMainScene == false )
		{
			WindowManager.Instance.ShowLoadingScreen(false, this.name);
			WindowManager.Instance.Open(WindowManager.WindowLayer.Screen, "LandingScreen");
		}
	}
	
	void SetupSparx()
	{
		var config = new EB.Sparx.Config();
		config.ApiEndpoint = ApiEndPoint;
		config.ApiKey = new EB.Sparx.Key(this.ApiKey);
		
		config.Locale = (Setup.Locale == EB.Language.Unknown) ? EB.Version.GetDefaultLanguageFromLanguageCode() : Setup.Locale;
		config.LoadLocalizer = false;

		config.GameManagerConfig.Listener = new EB.Replication.GameListener();
		config.GameManagerConfig.Enabled = true;

		config.PaymentsConfig.IAPPublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAjLtJUDbKn66g4XeYtgcf22DFsPGrslJN+XzxUJmyqIaW6f6X7QCcCEX74/aoGX7NTBvVe6qp0mkR9BtG5WoEf4chTVax1ILNsvRSlyTa9Z833TNAxKkHHKDEsC9KyqwGxKMCF0+8usRx8UwdWD0N90pjrOoA9aM2mKIiRdfG+jSxMMHdHHKC05G7ieJu7s966r8DZPtTtvpQs223XRhCdHy+9cXX5cgYYNKMiodnmIgQ3WLUvk8takMkPlQ166OToQXorhPIzK1/89OFG3MhwIb4GBlDjmRspgVREMU7b1T9pTg3kIlHSR2wuKhsFX5Basgha5yXRYLF3oX5SZLBfQIDAQAB";

		config.LoginConfig.Listener = new LoginListener();

		//Initialize managers by class names (must include namespaces if not internal)
		Type type;
		foreach (string manager in managers) {
			type = Type.GetType(manager);
			Debug.Log ("Adding component: " + type);
			config.GameComponents.Add(type);
		}

		// register the enet library
		EB.Sparx.NetworkFactory.Register("udp", typeof(EB.Sparx.NetworkENet));	
		EB.Sparx.Hub.Create(config);

		#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN 
		// exlcude these platform from affecting the analytics
		NoStats = true;
		#endif	
		
		if (NoStats)
		{	
			Debug.Log("excluding stats");
			SparxHub.Instance.ApiEndPoint.AddData(string.Empty, "nostats", "1");
		}
	}

	//---------------------------------------------------------------------------------
	IEnumerator Start()
	{
		DontDestroyOnLoad(gameObject);
		
		
		InitializeWindowManager();
		InitializeTexturePoolManager();
		InitializeFlash();
		SetupSparx();

		EB.Language lang = EB.Version.GetDefaultLanguageFromLanguageCode();
		
		// load the localizer
		EB.Localizer.Clear();
		EB.Localizer.LoadAllFromResources(lang, true);
		
		yield return new WaitForFixedUpdate();
		
		// disable the screen slepping
		var prev = Screen.sleepTimeout;
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		
		// this disables loading tips
		WindowManager.WindowInitInfo initInfo = new WindowManager.WindowInitInfo();
		initInfo.data = false;
		//BTTODO
		//WindowManager.WindowInfo windowInfo = WindowManager.Instance.ShowLoadingScreen(true, this.name, initInfo);
		
		// setup the assets
		var success = false;
		yield return EB.Assets.LoadPacks(true,delegate(bool s){
			success = s;
		});

		if (!success) {
			Debug.LogError("Failed to load packs.... waiting for success.");
			while(!success) {
				yield return 1;
			}
			Debug.LogError("Packs loaded.");
		}
		
		//BTTODO
		//var loading = windowInfo.screenObject;
		//EB.UIUtils.SetLabelContents(loading,"Label_Loading", "ID_UI_LOADING");
		//yield return new WaitForSeconds( 1.0f );

		Login();
		
		Screen.sleepTimeout = prev;
		
		yield break;
	}
	
	//---------------------------------------------------------------------------------
	public void Login()
	{
		SparxHub.Instance.LoginManager.Enumerate();
	}
	
	//---------------------------------------------------------------------------------
	public void Logout()
	{
		//BTTODO
		//loginListener.Logout();
	}
	
	private void InitializeTexturePoolManager()
	{
		GameObject texturePoolManager = new GameObject("TexturePoolManager");
		texturePoolManager.AddComponent<TexturePoolManager>();
		DontDestroyOnLoad(texturePoolManager);
	}
	
	private void InitializeWindowManager()
	{
		GameObject windowManager = new GameObject("WindowManager");
		windowManager.AddComponent<UILogger>();
		windowManager.AddComponent<EBUI_TransitionManager>();
		windowManager.AddComponent<UIResolutionManager>();
		WindowManager wm = windowManager.AddComponent<WindowManager>();
		windowManager.transform.localPosition = Vector3.zero;
		DontDestroyOnLoad(windowManager);
		
		BusyBlockerManager.Config.DisplayDelayPerBlockerFlag[BusyBlockerManager.BlockerFlag.ServerCommunication] = 3f;
		BusyBlockerManager.Config.DisplayDelayPerBlockerFlag[BusyBlockerManager.BlockerFlag.ServerTransaction] = 3f;
		
		GameObject focusManager = new GameObject("FocusManager");
		FocusManager fm = focusManager.AddComponent<FocusManager>();
		fm.windowManager = wm;
		DontDestroyOnLoad(focusManager);

		// Create and init the debug stack display.
		GameObject debugContainer = NGUITools.AddChild(wm.GetUiRoot(), (GameObject)EB.Assets.Load(WindowManagerDebug.AssetPath));
		debugContainer.name = "WindowManagerDebug";
		WindowManagerDebug debug = debugContainer.GetComponent<WindowManagerDebug>();
		debug.windowManager = wm;
		debug.Show(false);
	}
	
	private void InitializeFlash()
	{
		gameObject.AddComponent<SampleFlashTextureAdapter>();
		GameObject flashFontAdapter = GameObject.Instantiate(Resources.Load("UI/FlashAssets/SampleFlashFontAdapter") as GameObject) as GameObject;
		DontDestroyOnLoad(flashFontAdapter);
	}
	
	void Update()
	{
		if( ( EB.BugReport.DidCrash == true ) && ( this.DidShowCrash == false ) )
		{
			this.DidShowCrash = true;
			WindowManager.Instance.ShowError( "ID_SPARX_ERROR_UNKNOWN", delegate(WindowManager.WindowInfo wi) {
				Application.Quit();
			});
		}
	}
}
