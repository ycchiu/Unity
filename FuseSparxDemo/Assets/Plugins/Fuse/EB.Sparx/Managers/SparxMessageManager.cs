using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class MessageConfig
	{
		public MessageConfig()
		{
			this.MessageTypeMapping.Add( "mass_basic", typeof( BasicMessage ) );
		}
		
		public Dictionary< string, System.Type > MessageTypeMapping = new Dictionary<string, System.Type>();
	}
	
	public class MessageManager : AutoRefreshingManager
	{
		MessageConfig _config = null;
		MessageAPI _api = null;
		
		public string Error { get; private set; }
		public List<MessengerMessage> Messages { get; private set; }
		
		public MessageManager()
			:
		base( "message", MessageAPI.MessageAPIVersion )
		{
			this.Error = string.Empty;
			this.Messages = new List<MessengerMessage>();
		}
		
		public override void OnData( Hashtable data, Action< bool > cb )
		{
			this._api.OnMessagesData( data, delegate( string err, List<MessengerMessage> messages ) {
				if( err != null ) {
					this.Error = err;
				}
				else
				{
					this.Messages = messages;
				}
				
				cb( true );
			});
		}
		
		public List<T> GetAllMessages<T>() where T : MessengerMessage
		{
			List<T> messages = new List<T>();
			foreach( MessengerMessage candidate in this.Messages )
			{
				T msg = candidate as T;
				if( msg != null )
				{
					messages.Add( msg );
				}
			}
			return messages;
		}
		
		public override string ToString ()
		{
			string output = string.Empty;
			
			output += "Messages: " + this.Messages.Count + "\n";
			foreach( var message in this.Messages )
			{
				output += "\t" + message + "\n";
			}
			output += base.ToString();
			return output;
		}
		
		public void Read( MessengerMessage msg, EB.Action<string> cb )
		{
			if( msg == null )
			{
				cb( "nomessage" );
			}
			
			this._api.Read( msg.MessageID, delegate( string err, MessengerMessage updated ) {
				cb( err );
			});
		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize(Config config)
		{
			base.Initialize( config );
			
			this._config = config.MessageConfig;
			this._api = new MessageAPI( this._config, Hub.ApiEndPoint );
		}	
		
		#endregion
	}
}
