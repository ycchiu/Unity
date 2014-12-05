using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public abstract class MessengerMessage
	{
		public class MessageGift
		{
			public MessageGift( Hashtable data = null )
			{
				this.Items = null;
				this.Box = string.Empty;
				this.ClaimUrl = string.Empty;
				this.Claimed = true;
				
				if( data != null )
				{
					this.Items = EB.Dot.List< RedeemerItem >( "items", data, null );
					this.Box = EB.Dot.String( "box", data, string.Empty );
					this.ClaimUrl = EB.Dot.String( "claim", data, string.Empty );
					this.Claimed = EB.Dot.Bool( "c", data, true );
				}
				
				if( this.Items == null )
				{
					this.Items = new List<RedeemerItem>();
				}
			}
			
			public bool IsValid
			{
				get
				{
					return ( this.Items.Count > 0 ) && ( string.IsNullOrEmpty( this.Box ) == false ) && ( string.IsNullOrEmpty( this.ClaimUrl ) == false );
				}
			}
			
			public override string ToString ()
			{
				return string.Format( "Item Count:{0} Claimed:{1}", this.Items.Count, this.Claimed );
			}
			
			public List<RedeemerItem> Items { get; private set; }
			public string Box { get; private set; }
			public string ClaimUrl { get; private set; }
			public bool Claimed { get; private set; }
		}
	
		public MessengerMessage( string cdn, Hashtable data = null )
		{
			string[] splits = this.GetType().Name.Split( new char[ '.' ] );
			this.Prefab = splits[ splits.Length - 1 ];
		
			this.CDN = cdn;
			this.MessageID = string.Empty;
			this.Subject = string.Empty;
			this.ShortSubject = string.Empty;
			this.Title = string.Empty;
			this.Read = false;
			Hashtable gift = null;
			
			if( data != null )
			{
				this.CDN = EB.Dot.String( "cdn", data, this.CDN );
				this.MessageID = EB.Dot.String( "mid", data, string.Empty );
				this.Subject = EB.Dot.String( "subject", data, string.Empty );
				this.ShortSubject = EB.Dot.String( "ss", data, string.Empty );
				this.Title = EB.Dot.String( "title", data, string.Empty );
				this.Read = EB.Dot.Bool( "r", data, false );
				gift = EB.Dot.Object( "g", data, null );
			}
			
			if( string.IsNullOrEmpty( this.ShortSubject ) == true )
			{
				this.ShortSubject = this.Subject;
			}
			
			this.Gift = new MessageGift( gift );
		}
		
		public string Prefab { get; private set; }
		public string CDN { get; private set; }
		public string MessageID { get; private set; }
		public string Subject { get; private set; }
		public string ShortSubject { get; private set; }
		public string Title { get; private set; }
		public bool Read { get; private set; }
		public MessageGift Gift { get; private set; }  
		
		public virtual bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.MessageID ) == false ) && ( string.IsNullOrEmpty( this.Subject ) == false );
			}
		}
		
		public override string ToString ()
		{
			return string.Format( "Prefab:{0} ID:{1} Subject:{2} Title:{3} Read:{4} Gift:{5}", this.Prefab, this.MessageID, this.Subject, this.Title, this.Read, this.Gift );
		}
	}
	
	public class MessageAPI
	{
		public static readonly int MessageAPIVersion = 2;
		
		MessageConfig Config = null;
		EndPoint API = null;
		
		public MessageAPI( MessageConfig config, EndPoint api )		
		{
			this.Config = config;
			this.API = api;
		}
		
		public void OnMessagesData( Hashtable data, EB.Action< string, List<MessengerMessage> > cb )
		{
			string err = null;
			List<MessengerMessage> messages = new List<MessengerMessage>();
			
			string cdn = EB.Dot.String( "cdn", data, string.Empty );
			ArrayList messagesData = EB.Dot.Array( "messages", data, new ArrayList() );
			foreach( Hashtable messageData in messagesData )
			{
				MessengerMessage msg = this.OnMessageData( cdn, messageData );
				if( ( msg != null ) && ( msg.IsValid == true ) )
				{
					messages.Add( msg );
				}
			}
			
			cb( err, messages );
		}
		
		public MessengerMessage OnMessageData( string cdn, Hashtable data )
		{
			MessengerMessage msg = null;
		
			string mtype = EB.Dot.String( "mtype", data, string.Empty );
			if( string.IsNullOrEmpty( mtype ) == false )
			{
				System.Type type = typeof( EventsEvent );
				this.Config.MessageTypeMapping.TryGetValue( mtype, out type );
				if( type != null )
				{
					if( type.IsSubclassOf( typeof( MessengerMessage ) ) == true )
					{
						msg = (MessengerMessage)System.Activator.CreateInstance( type, cdn, data );
					}
					else
					{
						EB.Debug.LogError( "MessageManager: {0} is using {1}, but it must be subclassed from MessengerMessage", data, type );
					}
				}
				else
				{
					EB.Debug.LogError( "MessageManager: Unknown message type '{0}' for {1}", mtype, data );
				}
			}
			
			return msg;
		}
		
		public void Read( string messageID, EB.Action< string, MessengerMessage > cb )
		{
			EB.Sparx.Request request = this.API.Post("/messages/read");
			request.AddData("mid", messageID );
			this.API.Service( request, delegate( Response result ) {
				if( result.sucessful == true )
				{
					MessengerMessage msg = this.OnMessageData( string.Empty, result.hashtable );
					cb( null, msg );
				}
				else
				{
					EB.Debug.Log( "MessageManager Read Error: {0}", result.localizedError );
					cb( result.localizedError, null );
				}
			});
		}
	}
}
