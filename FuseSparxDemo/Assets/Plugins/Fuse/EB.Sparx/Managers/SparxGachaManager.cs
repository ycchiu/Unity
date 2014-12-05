using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class GachaConfig
	{
		public string[] Groups = new string[] { "base", "tutorial", "events" };
	}
	
	public class GachaManager : AutoRefreshingManager
	{
		GachaConfig	_config = new GachaConfig();
		GachaAPI _api = null;
		Dictionary< string, GachaSet > _sets = new Dictionary< string, GachaSet >();
		Dictionary< string, int > _tokens = new Dictionary<string, int>();
		
		public event EB.Action< List<string> > GroupsChanged;
		public event EB.Action< Dictionary< string, int > > TokensChanged;
		public event EB.Action< List< GachaBox > > FreeTimesChanged;
		
		public int MaxCombinedSpins { get; private set; }
		
		public GachaManager()
			:
		base( "gacha", GachaAPI.GachaAPIVersion )
		{
		}
		
		public int GetTokenCount( string box )
		{
			int tokens = 0;
			
			if( string.IsNullOrEmpty( box ) == true )
			{
				foreach (string key in _tokens.Keys)
				{
					tokens += _tokens[key];
				}
			}
			else if( this._tokens.TryGetValue( box, out tokens ) == false )
			{
				tokens = 0;
			}
			
			return tokens;
		}
		
		public GachaSet GetGachaSet( string name )
		{
			GachaSet gachaSet = null;
			if( this._sets.TryGetValue( name, out gachaSet ) == false )
			{
				gachaSet = null; 
			}
			
			return gachaSet;
		}
		
		public void PickFromBox( string group, string boxName, string payment, int spins, EB.Action<string,GachaPickResult> onComplete)
		{
			GachaSet set = null;
			if( this._sets.TryGetValue( group, out set ) == false )
			{
				set = null;
			}
			
			if( set != null )
			{
				GachaBox box = null;
				foreach( GachaBox candidate in set.Boxes )
				{
					if( candidate.Name == boxName )
					{
						box = candidate;
						break;
					}
				}
				
				this.PickFromBox( box, payment, spins, onComplete );
			}
			else
			{
				string error = string.Format( "Requested a pick on an unknown group: {0}", group );
				EB.Debug.LogError( error );
				onComplete( error, null );
			}
		}
		
		public void PickFromBox( GachaBox box, string payment, int spins, EB.Action<string,GachaPickResult> onComplete )
		{
			if( box != null )
			{
				this._api.PickFromBox( box.Group, box.Version, box.PickSet, box.Name, payment, spins, delegate( string error, Hashtable data ){
					if( data != null )
					{
						GachaPickResult pickResult = new GachaPickResult( data );
						if( pickResult.Items.Count > 0 )
						{
							onComplete( null, pickResult );
						}
						else
						{
							string errMsg = ( error != null ) ? error : string.Format( "Couldn't afford the Gacha Spin {0}-{1}", box, payment );
							EB.Debug.LogError( errMsg );
							onComplete( errMsg, null );
						}
					}
					else
					{
						EB.Debug.LogError("Server responded with error to Gacha Pick: '{0}'", error );
						onComplete( error, null );
					}
				});
			}
			else
			{
				string error = string.Format( "Requested a pick on an null box" );
				EB.Debug.LogError( error );
				onComplete( error, null );
			}
		}
		
		public void ClaimFreeBox( GachaBox box, EB.Action<string,GachaTokenCount,int> onComplete )
		{
			if( box != null )
			{
				this._api.ClaimFreeBox( box.Group, box.Version, box.PickSet, box.Name, delegate( string error, Hashtable data ){
					if( data != null )
					{
						GachaTokenCount token = new GachaTokenCount( data );
						int freeTime = EB.Dot.Integer( "free", data, -1 );
						onComplete( null, token, freeTime );
					}
					else
					{
						EB.Debug.LogError("Server responded with error to ClaimFreeBox: '{0}'", error );
						onComplete( error, null, -1 );
					}
				});
			}
			else
			{
				string error = string.Format( "Requested a pick on an null box" );
				EB.Debug.LogError( error );
				onComplete( error, null, -1 );
			}
		}
		
		public string GetTokenTexture( string token )
		{
			foreach( KeyValuePair< string, GachaSet > set in this._sets )
			{
				foreach( GachaBox box in set.Value.Boxes )
				{
					if( ( box.Token == token ) && ( string.IsNullOrEmpty( box.TokenImage ) == false ) )
					{
						return box.TokenImage;
					}
				}
			}
			
			return string.Empty;
		}
		
		public string GetTokenLabel( string token )
		{
			foreach( KeyValuePair< string, GachaSet > set in this._sets )
			{
				foreach( GachaBox box in set.Value.Boxes )
				{
					if( ( box.Token == token ) && ( string.IsNullOrEmpty( box.DisplayName ) == false ) )
					{
						return box.DisplayName;
					}
				}
			}
			
			return string.Empty;
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			base.Initialize( config );
			
			this._config = config.GachaConfig;
			_api = new GachaAPI( Hub.ApiEndPoint );
		}
		#endregion
		
		public override void Async (string message, object payload)
		{
			switch(message.ToLower())
			{
				case "token":
				{
					Hashtable data = payload as Hashtable;
					if( data != null )
					{
						List<GachaTokenCount> changes = EB.Dot.List< GachaTokenCount >( "changes", data, null );
						this.OnGachaTokens( changes );
					}
					break;
				}
				
				case "free":
				{
					Hashtable data = payload as Hashtable;
					if( data != null )
					{
						List<GachaFreeTimeChange> changes = EB.Dot.List< GachaFreeTimeChange >( "changes", data, null );
						this.OnGachaFreeTimeChanges( changes );
					}
					break;
				}
				default:
				{
					break;
				}
			}
		}
		
		public override void OnData( Hashtable data, Action< bool > cb )
		{
			this._api.OnGachaData( data, delegate( List<GachaGroup> groups, List<GachaTokenCount> tokens, int maxSpins ) {
				if( maxSpins > 0 )
				{
					this.MaxCombinedSpins = maxSpins;
				}
				
				this.OnGachaGroups( groups );
				this.OnGachaTokens( tokens );
				cb( true );
				return;
			});
		}
		
		private void OnGachaGroups( List<GachaGroup> groups  )
		{
			if( groups != null )
			{
				foreach( GachaGroup group in groups )
				{
					this._sets[ group.Name ] = group.Set;
					foreach( GachaBox box in group.Set.Boxes )
					{
						if( box.IsValid == true ) 
						{
							if( ( box.BackgroundOnCDN == true ) && ( string.IsNullOrEmpty( box.BackgroundImage ) == false ) )
							{
								EB.Cache.Precache( box.BackgroundImage );
							}
							
							if( ( box.OpenOnCDN == true ) && ( string.IsNullOrEmpty( box.OpenImage ) == false ) )
							{
								EB.Cache.Precache( box.OpenImage );
							}
	
							if( ( box.ClosedOnCDN == true ) && ( string.IsNullOrEmpty( box.ClosedImage ) == false ) )
							{
								EB.Cache.Precache( box.ClosedImage );
							}
							
							if( ( box.TokenImageOnCDN == true ) && ( string.IsNullOrEmpty( box.TokenImage ) == false ) )
							{
								EB.Cache.Precache( box.TokenImage );
							}
						}
					}
				}
				
				if( groups.Count > 0 )
				{
					if( this.GroupsChanged != null )
					{
						List<string> names = new List<string>();
						foreach( GachaGroup group in groups )
						{
							names.Add( group.Name );
						}
						this.GroupsChanged( names );
					}
				}
			}
		}
		
		private void OnGachaTokens( List<GachaTokenCount> tokens  )
		{
			if( tokens != null )
			{
				foreach( GachaTokenCount token in tokens )
				{
					this._tokens[ token.Name ] = token.Count;
				}
				
				if( tokens.Count > 0 )
				{
					if( this.TokensChanged != null )
					{
						Dictionary<string,int> balances = new Dictionary<string,int>();
						foreach( GachaTokenCount token in tokens )
						{
							balances[ token.Name ] = token.Count;
						}
						this.TokensChanged( balances );
					}
				}
			}
		}
		
		private void OnGachaFreeTimeChanges( List<GachaFreeTimeChange> changes  )
		{
			if( changes != null )
			{
				List<GachaBox> boxes = new List<GachaBox>();
			
				foreach( GachaFreeTimeChange change in changes )
				{
					GachaSet set = null;
					if( this._sets.TryGetValue( change.Group, out set ) == true )
					{
						foreach( GachaBox box in set.Boxes )
						{
							if( box.Name == change.Box )
							{
								box.FreeTime = change.FreeTime;
								boxes.Add( box );
								break;
							}
						}
					}
				}
				
				if( boxes.Count > 0 )
				{
					if( this.FreeTimesChanged != null )
					{
						this.FreeTimesChanged( boxes );
					}
				}
			}
		}
		
		public override string ToString ()
		{
			string output = string.Empty;
			
			output += "\n\tGroups: " + this._sets.Count;
			foreach( var s in this._sets )
			{
				output += "\n\t\t" + s.Key + " -> " + s.Value.ToString();
			}
			output += "\n\tTokens: " + this._tokens.Count;
			foreach( var t in this._tokens )
			{
				output += "\n\t\t" + t.Key + " -> " + t.Value.ToString();
			}
			
			return output;
		}
	}
}
