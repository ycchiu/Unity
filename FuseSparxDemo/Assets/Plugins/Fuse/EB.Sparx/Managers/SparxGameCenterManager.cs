using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;


namespace EB.Sparx
{
	public class GameCenterConfig
	{
		public bool Enabled 				= true;
		public string LeaderboardPrefix 	= string.Empty;
		public string AchievementPrefix 	= string.Empty;
	}
	
	public class GameCenterManager : SubSystem
	{
#if UNITY_IPHONE && !UNITY_EDITOR
		[DllImport("__Internal")]
		static extern void _GetAchievementChallengeablePlayers(string achievementName);

		[DllImport("__Internal")]
		static extern void _IssueChallengeAchievement(string achievementName, string players, string message);
		
		[DllImport("__Internal")]
		static extern void _GetScoreChallengeablePlayers(string leaderboardId, int playerScore);

		[DllImport("__Internal")]
		static extern void _IssueChallengeScore(string leaderboardId, string players, string message);
		
		[DllImport("__Internal")]
		static extern void _GetActiveChallenges();
		
		[DllImport("__Internal")]
		static extern void _GetLeaderboardTitle(string leaderboardId);
		
		[DllImport("__Internal")]
		static extern void _DeclineChallenge(string challengerId, string challengeId);
		
#endif

		SparxGameCenterManager _callbacks;
		
		GameCenterAPI _api;
		GameCenterConfig _config;
		bool _isAuthenticating = false;
		
		Action<string, string> _leaderboardTitleCallback;
		
		public GameCenterManager()
		{
			var go = new GameObject("gc_callbacks", typeof(SparxGameCenterManager));
			GameObject.DontDestroyOnLoad(go);
			_callbacks = go.GetComponent<SparxGameCenterManager>();
			if (_callbacks == null)
			{
				Debug.LogError("Callbacks is null !!!");
			}
			
			_callbacks.emitter.On<object>("leaderboardTitle", OnLeaderboardTitle );
		}

		
		#region implemented abstract members of EB.Sparx.SubSystem
		public override void Initialize (Config config)
		{
			_config = config.GameCenterConfig;
			_api = new GameCenterAPI(Hub.ApiEndPoint);
		}
				
		public override void Connect ()
		{
			State = SubSystemState.Connected;
			
			ConnectToGameCenter();
		}
		
		public void ReportScore( int leaderboardId, int score )
		{
			if (!string.IsNullOrEmpty(_config.LeaderboardPrefix) )
			{
				Debug.Log("Report score: " + leaderboardId + " " + score);
#if UNITY_IPHONE && !UNITY_EDITOR				
				Social.ReportScore(score, _config.LeaderboardPrefix+leaderboardId, OnReportScore); 
#endif
			}
		}
		
		void OnReportScore( bool success )
		{
			Debug.Log("OnReportScore: " + success);
		}	
		
		public void ReportProgress( string achievementId, double progress )
		{
			if (!string.IsNullOrEmpty(_config.AchievementPrefix) )
			{
				if(progress > 100)
				{
					progress = 100;
				}
				if(progress < 0)
				{
					progress = 0;
				}
				EB.Debug.Log("Report progress: " + achievementId + " " + progress);
#if UNITY_IPHONE && !UNITY_EDITOR				
				Social.ReportProgress(_config.AchievementPrefix +achievementId, progress, OnReportProgress); 
#endif
			}
		}
		
		void OnReportProgress( bool success )
		{
			Debug.Log("OnReportProgress: " + success);
		}	
		
		public override void Disconnect (bool isLogout)
		{
			State = SubSystemState.Disconnected;
		}
		#endregion
		
		public void ShowLeaderboardUI()
		{
			Debug.Log("ShowLeaderboardUI");
			Social.ShowLeaderboardUI();
		}
		
		public void ShowAchievementsUI()
		{
			Debug.Log("ShowAchievementsUI");
			Social.ShowAchievementsUI();
		}
		
		public void IssueChallengeAchievement(string achievementName, string players, string message)
		{
#if UNITY_IPHONE && !UNITY_EDITOR

			_IssueChallengeAchievement(achievementName, players, message);
#endif
		}
		
		public void GetAchievementChallengeablePlayers(string achievementName, Action<string, string>callback)
		{
			_callbacks.emitter.Once<object>("achievementChallengePlayers", delegate(object result) {
					string players = Dot.String("players", result, string.Empty); // remove the delimiter at the end
					Debug.Log("Got list of challengeable players: " + players);
									
					if(callback != null)
					{
						callback(achievementName, players);
					}				
				});

#if UNITY_IPHONE && !UNITY_EDITOR

			_GetAchievementChallengeablePlayers(achievementName);
#endif
		}
		
		public void IssueChallengeScore(string leaderboardId, string players, string message)
		{
#if UNITY_IPHONE && !UNITY_EDITOR

			_IssueChallengeScore(leaderboardId, players, message);
#endif
		}
		
		public void GetScoreChallengeablePlayers(int leaderboardId, int playerScore, Action<string, string>callback)
		{
			_callbacks.emitter.Once<object>("scoreChallengePlayers", delegate(object result) {
					string players = Dot.String("players", result, string.Empty); // remove the delimiter at the end
					Debug.Log("Got list of challengeable players: " + players);
									
					if(callback != null)
					{
						callback(_config.LeaderboardPrefix+leaderboardId, players);
					}

				});

#if UNITY_IPHONE && !UNITY_EDITOR

			_GetScoreChallengeablePlayers(_config.LeaderboardPrefix+leaderboardId, playerScore);
#endif
		}
		
		public void GetLeaderboardTitle(int leaderboardId, Action<string, string> callback)
		{	
			_leaderboardTitleCallback = callback;
#if UNITY_IPHONE && !UNITY_EDITOR
			_GetLeaderboardTitle(_config.LeaderboardPrefix + leaderboardId);
#endif
		}
		
		private void OnLeaderboardTitle(object result)
		{
			string data = Dot.String("data", result, string.Empty); // remove the delimiter at the end
			
			char[] separators = {';'};
			string[] elements = data.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
							
			if(_leaderboardTitleCallback != null && elements.Length == 2)
			{
				_leaderboardTitleCallback(elements[0], elements[1]);
			}
		}
		
		public void GetActiveChallenges(Action<string> callback)
		{
			_callbacks.emitter.Once<object>("activeChallenges", delegate(object result){
				// parse the data
				string challenges = Dot.String("challenges", result, string.Empty);
				Debug.Log("Got list of active challenges: " + challenges);
				
				if(callback != null)
				{
					callback(challenges);
				}
			});
			
#if UNITY_IPHONE && !UNITY_EDITOR
			_GetActiveChallenges();
#endif
		}
		
		public void DeclineChallenge(string challengerId, string challengeId)
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			_DeclineChallenge(challengerId, challengeId);
#endif

		}
		
		void ConnectToGameCenter()
		{
			var localUser = Social.localUser;
			if (localUser != null)
			{
				// only connect if authenticated, we don't want to force this
				if (localUser.authenticated)
				{
					OnAuthenticate(true);	
				}
			}
		}

		void OnSetGameCenterId (string error)
		{
			//dont care
		}
					
		void OnAuthenticate(bool success)
		{
			_isAuthenticating = false;
			Debug.Log("On Gamecenter auth " + success);	
			if (success && State == SubSystemState.Connected)
			{
				var localUser = Hub.LoginManager.LocalUser;
				if ( localUser != null )
				{
					var gcid = localUser.GameCenterId;
					if ( gcid != Social.localUser.id && Social.localUser.id != "1000")
					{
						_api.SetGameCenterId(Social.localUser.id, Social.localUser.userName, OnSetGameCenterId);
					}
				}				
			}
		}
		
	}
	
}

public class SparxGameCenterManager : MonoBehaviour {

	public EB.EventEmitter emitter = new EB.EventEmitter();
	
	void OnAchievementChallengePlayerList( string playerString )
	{
		EB.Debug.Log("OnAchievementChallengePlayerList: " + playerString );
		object result = new Hashtable(){ {"players", playerString} };
		emitter.Emit("achievementChallengePlayers", result );				
	}	
	
	void OnScoreChallengePlayerList(string playerString )
	{
		EB.Debug.Log("OnScoreChallengePlayerList: " + playerString );
		object result = new Hashtable(){ {"players", playerString} };
		emitter.Emit("scoreChallengePlayers", result );				

	}
	
	void OnActiveChallengeList( string challengeString )
	{
		EB.Debug.Log("OnActiveChallengeList: " + challengeString);
		object result = new Hashtable() { {"challenges", challengeString}};
		emitter.Emit("activeChallenges", result);
	}
	
	void OnLeaderboardTitle( string dataString)
	{
		EB.Debug.Log("OnLeaderboardTitle: " + dataString);
		object result = new Hashtable() { {"data", dataString}};
		emitter.Emit("leaderboardTitle", result);
	}
}


