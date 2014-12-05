using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{

	public enum TutorialState
	{
		not_started,
		started,
		completed
	}

	public class TutorialBranchData
	{
		public TutorialBranchData( string bid, Hashtable data = null )
		{
			this.BranchID = bid;
			if( data != null )
			{
				this.State = EB.Util.GetEnumValueFromString<TutorialState>( EB.Dot.String( bid, data, TutorialState.not_started.ToString() ) );
			}
			else
			{
				this.State = TutorialState.not_started;
			}
		}
	
		public string BranchID;
		public TutorialState State;
	}
	public class TutorialBranchDict : Dictionary<string, TutorialBranchData> {}
			
	public class TutorialUserData
	{
		public TutorialUserData( string tutorialId, Hashtable data = null )
		{
			this.TutorialId = tutorialId;
			this.Branches = new TutorialBranchDict();
			if( data != null )
			{
				this.CurrentBranchID = EB.Dot.String( "current_bid", data, string.Empty );
				this.State = EB.Util.GetEnumValueFromString<TutorialState>( EB.Dot.String( "s", data, TutorialState.not_started.ToString() ) );
				Hashtable branches = EB.Dot.Object( "branches", data, null );
				if( branches != null )
				{	
					foreach( DictionaryEntry branch in branches )
					{
						this.Branches[branch.Key.ToString()] = new TutorialBranchData( branch.Key.ToString(), branches );
					}
				}
			}
			else
			{
				this.CurrentBranchID = string.Empty;
				this.State = TutorialState.not_started;
			}
		}
		
		public string TutorialId { get; private set; }
		public string CurrentBranchID { get; private set; }
		public TutorialState State { get; private set; }
		public TutorialBranchDict Branches { get; private set; }
	}
	
	public class TutorialUserDataDict : Dictionary<string, TutorialUserData>{}
	
	public class TutorialManager : EB.Sparx.SubSystem
	{
		private TutorialAPI _api;
		private TutorialUserDataDict _userData;
		
		public TutorialUserDataDict UserData { get { return _userData; } }
		
		// Static instance for the manager.
		public static TutorialManager Instance { get; private set; }
		
		public override void Initialize (EB.Sparx.Config config)
		{
			Instance = this;
			_api = new TutorialAPI (SparxHub.Instance.ApiEndPoint);
			_userData = new TutorialUserDataDict();
		}	
		
		public override void Connect ()
		{
			var tutorialData = EB.Dot.Object( "tut", Hub.LoginManager.LoginData, null );
			if( tutorialData != null )
			{
				this.OnTutorialData( tutorialData, delegate( string err ) {		
					State = EB.Sparx.SubSystemState.Connected;
					EB.Debug.Log("TutorialManager.Connect: Data retrieved via login data");										
				});
			}
			else
			{
				this.Fetch( delegate( bool updated ) {
					State = EB.Sparx.SubSystemState.Connected;
					EB.Debug.Log("TutorialManager.Fetch: We've fetched everything!...Success");										
				});
			}	
		}
		
		public override void Disconnect (bool isLogout)
		{
			if( isLogout )
			{
				ClearLocalData();
			}
		}
		
		public void Fetch( EB.Action<bool> cb )
		{
			this._api.GetLoginData( delegate( string err1, Hashtable tutorialData ) {
				if( !string.IsNullOrEmpty( err1 ) )
				{
					Debug.LogWarning ("TutorialManager.Fetch: Failed");
					FatalError( err1 );										
					return cb( false );
				}
				
				this.OnTutorialData( tutorialData, delegate( string err2 ) {
					if( !string.IsNullOrEmpty( err2 ) )
					{
						FatalError( err2 );	
					}
					return cb( true );
				});
			});
		}
		
		private void OnTutorialData( Hashtable data, EB.Action<string> cb ) 
		{
			// Read in the users tutorial state.
			foreach( DictionaryEntry tutorial in data )
			{
				string key = tutorial.Key.ToString();
				TutorialUserData tutorialData = new TutorialUserData( key, EB.Dot.Object( key, data, null ) );
				_userData[tutorialData.TutorialId] = tutorialData; 
			}
			
			cb( string.Empty );
		}
		
		private void ClearLocalData()
		{
			_userData.Clear();
		}
		
		public void StartTutorial( string tutorialId, EB.Action<string,TutorialUserData> cb )
		{
			_api.StartTutorial( tutorialId, delegate( string err, Hashtable result ) {
				if( !string.IsNullOrEmpty(err) )
				{
					return cb( err, null );
				}
				
				UpdateTutorialData( result );
				
				return cb( null, _userData[tutorialId] );				
			});
		}

		public void EarlyStartBranch( string tutorialId, string branchId, EB.Action<string,TutorialUserData> cb )
		{
			_api.EarlyStartBranch( tutorialId, branchId, delegate( string err, Hashtable result ) {
				if( !string.IsNullOrEmpty(err) )
				{
					return cb( err, null );
				}
				
				UpdateTutorialData( result );
				
				return cb( null, _userData[tutorialId] );				
			});
		}
		
		public void StartBranch( string tutorialId, string branchId, EB.Action<string,TutorialUserData> cb )
		{
			_api.StartBranch( tutorialId, branchId, delegate( string err, Hashtable result ) {
				if( !string.IsNullOrEmpty(err) )
				{
					return cb( err, null );
				}

				UpdateTutorialData( result );
				
				return cb( null, _userData[tutorialId] );				
			});
		}

		public void CompleteTutorial( string tutorialId, EB.Action<string,TutorialUserData> cb )
		{
			_api.CompleteTutorial( tutorialId, delegate( string err, Hashtable result ) {
				if( !string.IsNullOrEmpty(err) )
				{
					return cb( err, null );
				}

				UpdateTutorialData( result );
				
				return cb( null, _userData[tutorialId] );				
			});
		}
		
		public void UpdateTutorialData( Hashtable data )
		{
			foreach( DictionaryEntry tutorial in data )
			{
				string key = tutorial.Key.ToString();
				TutorialUserData tutorialData = new TutorialUserData( key, EB.Dot.Object( key, data, null ) );
				_userData[tutorialData.TutorialId] = tutorialData;
			}
		}
	}		
}