using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public abstract class AutoRefreshingManager : SubSystem, Updatable
	{
		private string APIName = string.Empty;
		private int APIVersion = 1;
		private string CheckHash = string.Empty;
		private int NextRefresh = 0;
		private bool IsRefreshing = false;

		private AutoRefreshAPI Api = null;
		
		public override int Version { get { return this.APIVersion; } }
		public override string Name { get { return this.APIName; } }
		
		public event EB.Action DataChanged;
		public abstract void OnData( Hashtable data, Action< bool > cb );
		
		public AutoRefreshingManager( string name, int api )
		{
			this.APIName = name;
			this.APIVersion = api;
		}
		
		private void Refresh( EB.Action<bool> cb )
		{
			this.Api.Refresh( this.Name, this.APIVersion, this.CheckHash, delegate( string err, string updatedCheckHash, int nextRefresh, Hashtable data ) {
				if( string.IsNullOrEmpty( err ) == false )
				{
					EB.Debug.LogError( "Error Refreshing '{0}' -> {1}", this.Name, err );
					cb( false );
				}
				else
				{
					if( this.CheckHash == updatedCheckHash )
					{
						this.NextRefresh = nextRefresh;
						cb( false );
					}
					else
					{
						this.OnData( data, delegate( bool success ) {
							if( success == true )
							{
								this.CheckHash = updatedCheckHash;
								this.NextRefresh = nextRefresh;
								if( this.DataChanged != null )
								{
									this.DataChanged();
								}
								cb( true );
							}
						});	
					}
				}
			});
		}
		
		public override string ToString ()
		{
			string output = string.Empty;
			
			output += "AutoRefreshing " + this.APIName + "\n";
			output += "\tAPI Version " + this.APIVersion + "\n";
			output += "\tHash: " + this.CheckHash + "\n";
			output += "\tNext Refresh: " + Time.FromPosixTime( this.NextRefresh ) + "\n";
			
			return output;
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize (Config config)
		{
			this.Api = new AutoRefreshAPI( Hub.ApiEndPoint );
		}
		
		public bool UpdateOffline { get { return false;} }

		public void Update ()
		{
			if( ( this.State == SubSystemState.Connected ) && ( this.NextRefresh >= 0 ) && ( EB.Time.Now > this.NextRefresh ) && ( this.IsRefreshing == false ) )
			{
				this.IsRefreshing = true;
				this.Refresh( delegate( bool updated ) {
					this.IsRefreshing = false;
				});
			}
		}
		
		public override void Connect()
		{
			var refreshData = Dot.Object( this.Name, Hub.LoginManager.LoginData, null );
			if( refreshData != null )
			{
				this.Api.OnRefreshData( this.Name, refreshData, delegate( string err, string updatedCheckHash, int nextRefresh, Hashtable data ) {
					this.OnData( data, delegate( bool success ) {
						if( success == true )
						{
							this.CheckHash = updatedCheckHash;
							this.NextRefresh = nextRefresh;
							if( this.DataChanged != null )
							{
								this.DataChanged();
							}
						}
						this.State = SubSystemState.Connected;
					});
				});
			}
			else
			{
				this.Refresh( delegate( bool updated ) {
					this.State = SubSystemState.Connected;
				});
			}
		}

		public override void Disconnect (bool isLogout)
		{
		}
		#endregion
	}
}
