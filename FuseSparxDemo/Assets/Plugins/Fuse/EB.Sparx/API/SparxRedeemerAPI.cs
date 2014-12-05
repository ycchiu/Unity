using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ServerRedeemerTranslation : EB.Dot.IDotListItem
	{
		public class TranslatorTextureInfo
		{
			class TranslatorTaggedTexture : EB.Dot.IDotListItem
			{
				public TranslatorTaggedTexture( Hashtable data = null )
				{
					this.Tag = string.Empty;
					this.Texture = string.Empty;
					
					if( data != null )
					{
						this.Tag = EB.Dot.String( "tag", data, string.Empty );
						this.Texture = EB.Dot.String( "texture", data, string.Empty );
					}
				}
				
				public bool IsValid
				{
					get
					{
						return ( string.IsNullOrEmpty( this.Tag ) == false ) && ( string.IsNullOrEmpty( this.Texture ) == false );
					}
				}
			    
				public string Tag { get; private set; }
				public string Texture { get; private set; }
			}
		
			public TranslatorTextureInfo( Hashtable data = null )
			{
				this.GlobalTexture = string.Empty;
				this.TaggedTextures = new Dictionary<string, string>();
				
				if( data != null )
				{
					this.GlobalTexture = EB.Dot.String( "gbl", data, string.Empty );
					List<TranslatorTaggedTexture> tags = EB.Dot.List<TranslatorTaggedTexture>( "tags", data, null );
					if( tags != null )
					{
						foreach( TranslatorTaggedTexture tag in tags )
						{
							this.TaggedTextures.Add( tag.Tag, tag.Texture );
						}
					}
				}
			}
			
			public bool IsValid
			{
				get
				{
					return ( string.IsNullOrEmpty( this.GlobalTexture ) == false );
				}
			}
			
			public override string ToString()
			{
				string buffer = string.Empty;
				buffer += this.GlobalTexture + "\nTags:\n";
				foreach( KeyValuePair< string, string > tag in this.TaggedTextures )
				{
					buffer += string.Format( "{0}->{1}\n", tag.Key, tag.Value );
				}
				return buffer;
			}
			
			public string GlobalTexture { get; private set; }
			public Dictionary<string, string> TaggedTextures { get; private set; }
		}
	
	
		public ServerRedeemerTranslation( Hashtable data = null )
		{
			this.Type = string.Empty;
			this.Data = string.Empty;
			this.Label = string.Empty;
			this.Description = string.Empty;
			this.BackgroundColour = Color.white;
			this.QuantityColour = Color.white;
			Hashtable textureData = null;
			
			if( data != null )
			{
				this.Type = EB.Dot.String( "type", data, string.Empty );
				if( string.IsNullOrEmpty( this.Type ) )
				{
					this.Type = EB.Dot.String("t", data, string.Empty );
				}
				this.Data = EB.Dot.String("data", data, string.Empty );
				if( string.IsNullOrEmpty( this.Data ) )
				{
					this.Data = EB.Dot.String("n", data, string.Empty );
				}
				
				textureData = EB.Dot.Object( "texture", data, null );
				this.Label = EB.Dot.String( "label", data, string.Empty );
				this.Description = EB.Dot.String( "desc", data, string.Empty );
				this.BackgroundColour = EB.Dot.Colour( "bgc", data, Color.white );
				this.QuantityColour = EB.Dot.Colour( "qc", data, Color.white );
			}
			
			this.Texture = new TranslatorTextureInfo( textureData );
		}
		
		public bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Type ) == false ) && ( string.IsNullOrEmpty( this.Data ) == false ) && ( this.Texture.IsValid == true ) && ( string.IsNullOrEmpty( this.Label ) == false );
			}
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			buffer += "Type:" + this.Type + "\n";
			buffer += "Data:" + this.Data + "\n";
			buffer += "Valid:" + this.IsValid + "\n";
			buffer += "Texture:" + this.Texture + "\n";
			buffer += "Label:" + this.Label + "\n";
			buffer += "Description:" + this.Description + "\n";
			buffer += "BackgroundColour:" + this.BackgroundColour + "\n";
			buffer += "QuantityColour:" + this.QuantityColour + "\n";
			return buffer;
		}
		
		public string Type { get; private set; }
		public string Data { get; private set; }
		public TranslatorTextureInfo Texture { get; private set; }
		public string Label { get; private set; }
		public string Description { get; private set; }
		public Color BackgroundColour { get; private set; }
		public Color QuantityColour { get; private set; }
	}

	public class RedeemerAPI
	{
		private readonly int RedeemerAPIVersion = 6;
		EndPoint _api;
		
		public RedeemerAPI( EndPoint api )		
		{
			_api = api;
		}
		
		public void Refresh( string checkHash, EB.Action< string, List<ServerRedeemerTranslation>, string, int > cb )
		{
			EB.Sparx.Request setRequest = this._api.Get("/redeemer/refresh");
			setRequest.AddData( "api", RedeemerAPIVersion );
			setRequest.AddData( "hash", checkHash );
			this._api.Service( setRequest, delegate( Response result ){
				if( result.sucessful == true )
				{
					this.OnRedeemerData( result.hashtable, delegate( List<ServerRedeemerTranslation> translations, string updatedCheckHash, int nextRefresh ) {
						cb( null, translations, updatedCheckHash, nextRefresh );
					}); 
				}
				else
				{
					cb( result.localizedError, new List<ServerRedeemerTranslation>(), checkHash, EB.Time.Now );
				}
			});
		}
		
		public void OnRedeemerData( Hashtable data, EB.Action< List<ServerRedeemerTranslation>, string, int > cb )
		{
			List<ServerRedeemerTranslation> translations = null;
			
			string updatedCheck = EB.Dot.String( "check", data, string.Empty );
			int nextRefresh = EB.Dot.Integer( "nextrefresh", data, -1 );
			if( string.IsNullOrEmpty( updatedCheck ) == false )
			{
				translations = EB.Dot.List< ServerRedeemerTranslation >( "translators", data, null );
			}
			
			if( translations == null )
			{
				translations = new List<ServerRedeemerTranslation>();
			}
			
			cb( translations, updatedCheck, nextRefresh );
		}
	}
}
