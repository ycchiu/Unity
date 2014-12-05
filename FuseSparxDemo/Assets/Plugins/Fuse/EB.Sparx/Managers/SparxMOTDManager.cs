using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class MOTDConfig
	{
		public bool UseSecureEndpoint = false;
		public bool TemplatesWithStatus = false;
	}
	
	public class MOTDTemplate
	{
		public MOTDTemplate( Hashtable data = null )
		{
			this.Name = string.Empty;
			this.Title = string.Empty;
			this.CallToAction = string.Empty;
			this.ActionButton = string.Empty;
			this.ActionImage = string.Empty;
			this.ActionColour = Color.white;
			this.Params = new Dictionary<string, string>();
			this.CDN = string.Empty;
			
			if( data != null )
			{
				this.Name = EB.Dot.String( "name", data, string.Empty );
				this.Title = EB.Dot.String( "title", data, string.Empty );
				this.CallToAction = EB.Dot.String( "calltoaction", data, string.Empty );
				this.ActionButton = EB.Dot.String( "actionbutton", data, string.Empty );
				this.CDN = EB.Dot.String( "cdn", data, string.Empty );
				this.ActionImage = EB.Dot.String( "actionimage", data, string.Empty );
				if(!string.IsNullOrEmpty(this.ActionImage))
				{
					this.ActionImage = this.CDN + "/" + this.ActionImage; 
				}
				this.ActionColour = EB.Dot.Colour( "actioncolour", data, Color.white );
				Hashtable templateParams = EB.Dot.Object( "params", data, new Hashtable() );
				foreach( object candidate in templateParams.Keys )
				{
					string key = candidate as string;
					string value = templateParams[ key ] as string;
					if( ( key != null ) && ( value != null ) )
					{
						this.Params[ key ] = value;
					}
				}
			}
		}
		
		public string GetParamAsImage( string param )
		{
			string image = string.Empty;
			if( string.IsNullOrEmpty( this.CDN ) == false )
			{
				string value = null;
				if( this.Params.TryGetValue( param, out value ) )
				{
					if( string.IsNullOrEmpty( value ) == false )
					{
						image = this.CDN + "/" + value;
					}
				}
			}
			return image;
		}
		
		public bool IsValid { get{ return string.IsNullOrEmpty( this.Name ) == false; } }
		public string Name { get; private set; }
		public string Title { get; private set; }
		public string CallToAction { get; private set; }
		public string ActionButton { get; private set; }
		public string ActionImage { get; private set; }
		public Color ActionColour { get; private set; }
		public readonly Dictionary< string, string > Params;
		private string CDN { get; set; }
	}

	public class MOTDStatus
	{
		public MOTDStatus( Hashtable data = null )
		{
			this.NumMOTDs = 0;
			this.Url = string.Empty;
			this.Templates = new List<MOTDTemplate>();
			
			if( data != null )
			{
				this.NumMOTDs = EB.Dot.Integer("count", data, 0 );
				this.Url = EB.Dot.String("url", data, string.Empty);
				ArrayList templates = EB.Dot.Array( "templates", data, new ArrayList() );
				foreach( Hashtable template in templates )
				{
					if( template != null )
					{
						this.Templates.Add( new MOTDTemplate( template ) );
					}
				}
			}
		}
		
		public override string ToString()
		{
			return string.Format("Active:{0} Url:{1} Templates:{2}", this.Active, this.Url, this.Templates.Count );
		}
		
		public bool Active
		{
			get
			{
				bool active = string.IsNullOrEmpty( this.Url ) == false;
				return active;
			}
		}
		
		public int NumMOTDs { get; private set; }
		public string Url { get; private set; }
		public List<MOTDTemplate> Templates { get; private set; }
	}

	public class MOTDManager : SubSystem, Updatable
	{
		MOTDConfig _config = null;
		MOTDAPI _api = null;
		MOTDStatus _status = new MOTDStatus();
		
		public bool IsActive
		{ 
			get
			{
				return this._status.Active;
			}
		}
		public string Url
		{
			get
			{
				string url = this._status.Url;
				if( this._config.UseSecureEndpoint == false )
				{
					url = url.Replace( "https://", "http://" );
				}
				return url;
			}
		}
		public List<MOTDTemplate> Templates
		{
			get
			{
				return this._status.Templates;
			}
		}
		
		public void Sync( Action<bool, string> cb )
		{
			this._api.FetchStatus( this._config.TemplatesWithStatus, delegate( string error, Hashtable data ) {
				this.OnFetchStatus( error, data );
				cb( this.IsActive, this.Url );
			});
		}
		
		private void OnFetchStatus( string error, Hashtable data )
		{
			this._status = new MOTDStatus( data );
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			_config = config.MOTDConfig;
			_api = new MOTDAPI(Hub.ApiEndPoint);
		}

		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var motdData = Dot.Object( "motd", Hub.LoginManager.LoginData, null );
			if( motdData != null )
			{
				this.OnFetchStatus( null, motdData );
				State = SubSystemState.Connected;
			}
			else
			{
				this.Sync( delegate( bool active, string url ) {
					State = SubSystemState.Connected;
				});
			}
		}

		public void Update ()
		{
		}

		public override void Disconnect (bool isLogout)
		{
			this._status = new MOTDStatus();
		}
		#endregion
	
	}
}
