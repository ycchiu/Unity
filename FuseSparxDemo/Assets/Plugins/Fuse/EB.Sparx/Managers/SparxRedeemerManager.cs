using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class RedeemerConfig
	{
		public string GachaFallbackTokenTexture = "ui_gacha/free_gagha_spin_gold";
	}

	public class RedeemerItem : EB.Dot.IDotListItem
	{
		public RedeemerItem( Hashtable data = null )
		{
			this.Type = string.Empty;
			this.Data = string.Empty;
			this.Quantity = 0;
			this.Source = data;
		
			if( data != null )
			{
				this.Type = EB.Dot.String("type", data, "");
				if( string.IsNullOrEmpty(this.Type))
				{
					this.Type = EB.Dot.String("t", data, "");
				}
				this.Data = EB.Dot.String("data", data, "");
				if( string.IsNullOrEmpty(this.Data))
				{
					this.Data = EB.Dot.String("n", data, "");
				}
				this.Quantity = EB.Dot.Integer("quantity", data, 0);
				if( this.Quantity == 0 )
				{
					this.Quantity = EB.Dot.Integer("q", data, 0);
				}
			}
		}
		
		public RedeemerItem( string type, string data, int quantity )
		{
			this.Type = type;
			this.Data = data;
			this.Quantity = quantity;
		}
		
		public override string ToString()
		{
			return string.Format("Type:{0} Data:{1} Quantity:{2}", this.Type, this.Data, this.Quantity );
		}
		
		public bool IsValid
		{
			get
			{
				return (this.Quantity > 0) && (string.IsNullOrEmpty(this.Type) == false);
			}
		}
		
		public bool IsSameItem( RedeemerItem candidate )
		{
			return ( this.Type == candidate.Type ) && ( this.Data == candidate.Data );
		}
		
		public static bool operator ==( RedeemerItem lhs, RedeemerItem rhs )
		{
			//check if one side is null first
			if((object)rhs == null || (object)lhs == null)
			{
				if((object)rhs == null && (object)lhs == null)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			return ( lhs.Type == rhs.Type ) && ( lhs.Data == rhs.Data ) && ( lhs.Quantity == rhs.Quantity );
		}
		
		public static bool operator !=( RedeemerItem lhs, RedeemerItem rhs )
		{
			//check if one side is null first
			if((object)rhs == null || (object)lhs == null)
			{
				if((object)rhs == null && (object)lhs == null)
				{
					return false;
				}
				else
				{
					return true;
				}
			}

			return ( lhs.Type != rhs.Type ) || ( lhs.Data != rhs.Data ) || ( lhs.Quantity != rhs.Quantity );
		}
		
		public override bool Equals(object obj)
		{
			RedeemerItem lhs = obj as RedeemerItem;
			if( lhs != null )
			{
				return lhs == this;
			}
			return false;
		}
		
		public override int GetHashCode()
		{
			return this.Type.GetHashCode() ^ this.Data.GetHashCode() ^ this.Quantity.GetHashCode();
		} 

		public string Type { get; private set; }
		public string Data { get; private set; }
		public int Quantity { get; private set; }
		public Hashtable Source { get; private set; }
	}


	public abstract class IRedeemerDisplayMapping
	{
		public IRedeemerDisplayMapping( string type, string data )
		{
			this.Type = type;
			this.Data = data;
		}
		
		public string Type { get; protected set; }
		public string Data { get; protected set; }

		public abstract string GetTexture(RedeemerItem item, string tag = null );
		public abstract Color  GetQuantityColour(RedeemerItem item);
		public abstract Color GetBackgroundColour(RedeemerItem item);
		public abstract string GetLabel(RedeemerItem item);
		public abstract string GetDescription(RedeemerItem item);
		
		public abstract bool IsValid { get; }
		
		public bool UseForAllData
		{
			get
			{
				return this.Data == "*";
			}
		}
		
	};

	
	public class RedeemerStaticDisplayMapping : IRedeemerDisplayMapping
	{
		public RedeemerStaticDisplayMapping( Hashtable data = null )
			:
		base(string.Empty, string.Empty)
		{
					
			this.Texture = string.Empty;
			this.QuantityColour = Color.white;
			this.BackgroundColour = Color.white;
			this.Label = string.Empty;
			
			if( data != null )
			{
				this.Type = EB.Dot.String( "type", data, string.Empty );
				this.Data = EB.Dot.String( "data", data, string.Empty );
				
				this.Texture =  EB.Dot.String("texture", data, string.Empty );
				this.QuantityColour = EB.Dot.Colour("color_quantity", data, Color.white );
				this.BackgroundColour = EB.Dot.Colour("color_background", data, Color.white );
				this.Label = EB.Dot.String("label", data, string.Empty );
			}
		}
		
		public RedeemerStaticDisplayMapping( string type, string data, string texture, string label )
			:
		base ( type, data )
		{			
			this.Texture = texture;
			this.QuantityColour = Color.white;
			this.BackgroundColour = Color.white;
			this.Label = label;
		}

		public RedeemerStaticDisplayMapping( string type, string data, string texture, string label, string description )
			:
		base ( type, data )
		{			
			this.Texture = texture;
			this.QuantityColour = Color.white;
			this.BackgroundColour = Color.white;
			this.Label = label;
			this.Description = description;
		}
		
		public RedeemerStaticDisplayMapping( string type, string data, string texture, string label, string description, Color quantityColour, Color backgroundColour )
			:
		base ( type, data )
		{
			this.Texture = texture;
			this.QuantityColour = quantityColour;
			this.BackgroundColour = backgroundColour;
			this.Label = label;
			this.Description = description;
			
		}	
		
		public readonly string Texture;
		public readonly Color QuantityColour;
		public readonly Color BackgroundColour;
		public readonly string Label;
		public readonly string Description;
		
		#region IRedeemerDisplayMapping
		public override string GetTexture(RedeemerItem item, string tag) { return this.Texture; }
		public override Color GetQuantityColour(RedeemerItem item) { return this.QuantityColour; }
		public override Color GetBackgroundColour(RedeemerItem item) { return this.BackgroundColour; }
		public override string GetLabel(RedeemerItem item) { return this.Label; }
		public override string GetDescription(RedeemerItem item) { return this.Description; }
		
		public override bool IsValid
		{
			get
			{
				return ( string.IsNullOrEmpty( this.Texture ) == false ) && ( string.IsNullOrEmpty( this.Label ) == false ) && ( string.IsNullOrEmpty( this.Type ) == false ) && ( string.IsNullOrEmpty( this.Data ) == false );
			}
		}
		#endregion
	}
	
	public class RedeemerDynamicDisplayMapping : IRedeemerDisplayMapping
	{
		public RedeemerDynamicDisplayMapping( string type, string data, EB.Function<string, RedeemerItem, string> texture, EB.Function<string, RedeemerItem> label )
			:
		base( type, data )
		{			
			this.Texture = texture;
			this.Label = label;
		}
		
		public RedeemerDynamicDisplayMapping( string type, string data, EB.Function<string, RedeemerItem, string> texture, EB.Function<string, RedeemerItem> label, EB.Function<string, RedeemerItem> description, Color quantityColour, Color backgroundColour )
			:
		base( type, data )
		{			
			this.Texture = texture;
			this.QuantityColour = quantityColour;
			this.BackgroundColour = backgroundColour;
			this.Label = label;
			this.Description = description;
		}
		
		public readonly Color QuantityColour;
		public readonly Color BackgroundColour;

		
		private EB.Function<string, RedeemerItem, string> Texture;
		private EB.Function<string, RedeemerItem> Label;
		private EB.Function<string, RedeemerItem> Description;
		
		#region IRedeemerDisplayMapping
		public override string GetTexture(RedeemerItem item, string tag = null) { return (this.Texture != null)  ? this.Texture(item, tag) : string.Empty; }
		public override Color GetQuantityColour(RedeemerItem item) { return this.QuantityColour; }
		public override Color GetBackgroundColour(RedeemerItem item) { return this.BackgroundColour; }
		public override string GetLabel(RedeemerItem item) { return (this.Label != null) ? this.Label(item) : string.Empty; }
		public override string GetDescription(RedeemerItem item) { return (this.Description != null) ? this.Description(item) : string.Empty; }
		
		public override bool IsValid { get {return true; } }
		
		#endregion
	}
	
	public interface IRedeemerTranslator
	{
		List< IRedeemerDisplayMapping > DisplayMappings { get; }
	}
	
	public abstract class FallbackTextureRedeemerTranslator : IRedeemerTranslator
	{
		public string FallbackTexture { get; private set; }
		
		public FallbackTextureRedeemerTranslator( string fallbackTexture )
		{
			this.FallbackTexture = fallbackTexture;
		}
		
		public abstract List< IRedeemerDisplayMapping > DisplayMappings { get; }
	}
	
	public class RedeemerManager : SubSystem, Updatable
	{
		private RedeemerConfig Config = new RedeemerConfig();
		private RedeemerAPI API = null;
		private string CheckHash = string.Empty;
		private int NextRefresh = -1;
		
		private ServerRedeemerTranslator ServerRedeemerTranslator = null;
		
		private class RedeemerTypeDisplayMapper
		{
			public Dictionary< string, IRedeemerDisplayMapping > DataMappings = new Dictionary<string, IRedeemerDisplayMapping>();
			public IRedeemerDisplayMapping Global = null;
			public bool UseGlobal
			{
				get
				{
					return this.Global != null;
				}
			}
		}
		private Dictionary< string, RedeemerTypeDisplayMapper > TypeTexterMappers = new Dictionary<string, RedeemerTypeDisplayMapper>();
		
		public void AddRedeemerTranslator( IRedeemerTranslator translator )
		{
			foreach( IRedeemerDisplayMapping mapping in translator.DisplayMappings )
			{
				if( mapping.IsValid == true )
				{
					RedeemerTypeDisplayMapper mapper = null;
					if( this.TypeTexterMappers.TryGetValue( mapping.Type, out mapper ) == false )
					{
						mapper = new RedeemerTypeDisplayMapper();
						this.TypeTexterMappers.Add( mapping.Type, mapper );
					}
					
					if( mapper != null )
					{
						if( mapping.UseForAllData == true )
						{
							mapper.Global = mapping;
						}
						else
						{
							mapper.DataMappings[ mapping.Data ] = mapping;
						}
					}
				}
			}
		}
		
		public string GetTextureName( RedeemerItem item, string tag = null )
		{
			string texture = string.Empty;
			
			RedeemerTypeDisplayMapper mapper = null;
			if( this.TypeTexterMappers.TryGetValue( item.Type, out mapper ) == true )
			{
				IRedeemerDisplayMapping display = null;
				if( mapper.DataMappings.TryGetValue( item.Data, out display ) == false )
				{
					if( mapper.UseGlobal == true )
					{
						display = mapper.Global;
					}
				}
				
				if( display != null )
				{
					texture = display.GetTexture(item, tag);
				}
			}
			
			return texture;
		}
		
		public Color GetQuantityTextColour( RedeemerItem item )
		{
			Color c = Color.white;
			
			RedeemerTypeDisplayMapper mapper = null;
			if( this.TypeTexterMappers.TryGetValue( item.Type, out mapper ) == true )
			{
				IRedeemerDisplayMapping display = null;
				if( mapper.DataMappings.TryGetValue( item.Data, out display ) == false )
				{
					if( mapper.UseGlobal == true )
					{
						display = mapper.Global;
					}
				}
				
				if( display != null )
				{
					c = display.GetQuantityColour(item);
				}
			}
			
			return c;
		}
		
		public Color GetBackgroundColour( RedeemerItem item )
		{
			Color c = Color.white;
			
			RedeemerTypeDisplayMapper mapper = null;
			if( this.TypeTexterMappers.TryGetValue( item.Type, out mapper ) == true )
			{
				IRedeemerDisplayMapping display = null;
				if( mapper.DataMappings.TryGetValue( item.Data, out display ) == false )
				{
					if( mapper.UseGlobal == true )
					{
						display = mapper.Global;
					}
				}
				
				if( display != null )
				{
					c = display.GetBackgroundColour(item);
				}
			}
			
			return c;
		}
		
		public string GetLabel( RedeemerItem item)
		{
			string label = string.Empty;
			
			RedeemerTypeDisplayMapper mapper = null;
			if( this.TypeTexterMappers.TryGetValue( item.Type, out mapper ) == true )
			{
				IRedeemerDisplayMapping display = null;
				if( mapper.DataMappings.TryGetValue( item.Data, out display ) == false )
				{
					if( mapper.UseGlobal == true )
					{
						display = mapper.Global;
					}
				}
				
				if( display != null )
				{
					label = display.GetLabel(item);
				}
			}
			
			return label;
		}

		public string GetDescription( RedeemerItem item )
		{
			string desc = string.Empty;
			
			RedeemerTypeDisplayMapper mapper = null;
			if( this.TypeTexterMappers.TryGetValue( item.Type, out mapper ) == true )
			{
				IRedeemerDisplayMapping display = null;
				if( mapper.DataMappings.TryGetValue( item.Data, out display ) == false )
				{
					if( mapper.UseGlobal == true )
					{
						display = mapper.Global;
					}
				}
				
				if( display != null )
				{
					desc = display.GetDescription(item);
				}
			}
			
			return desc;
		}
		
		public void Refresh( EB.Action<bool> cb )
		{
			this.API.Refresh( this.CheckHash, delegate( string err, List<ServerRedeemerTranslation> translations, string updatedCheckHash, int nextRefresh ) {
				if( string.IsNullOrEmpty( err ) == false )
				{
					cb( false );
				}
				else
				{
					this.NextRefresh = nextRefresh;
					if( this.CheckHash == updatedCheckHash )
					{
						cb( false );
					}
					else
					{
						this.OnServerTranslations( translations );
						this.CheckHash = updatedCheckHash;
						cb( true );
					}
				}
			});
		}
		
		private void OnServerTranslations( List<ServerRedeemerTranslation> translations  )
		{
			if( ( translations != null ) && ( translations.Count > 0 ) )
			{
				this.ServerRedeemerTranslator = new ServerRedeemerTranslator( translations );
				this.AddRedeemerTranslator( this.ServerRedeemerTranslator );
			}
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			buffer += "ServerTranslations:" + ( ( this.ServerRedeemerTranslator != null ) ? this.ServerRedeemerTranslator.ToString() : "None" );
			return buffer;

		}
		
		#region implemented abstract members of EB.Sparx.Manager
		public override void Initialize( Config config )
		{
			this.Config = config.RedeemerConfig;
			this.API = new RedeemerAPI( Hub.ApiEndPoint );
			this.AddRedeemerTranslator( new GachaRedeemerTranslator( this.Config.GachaFallbackTokenTexture ) );
		}
		
		public bool UpdateOffline { get { return false;} }
		
		public override void Connect()
		{
			var redeemersData = Dot.Object( "redeemers", Hub.LoginManager.LoginData, null );
			if( redeemersData != null )
			{
				this.API.OnRedeemerData( redeemersData, delegate( List<ServerRedeemerTranslation> translations, string updatedCheckHash, int nextRefresh ) {
					this.OnServerTranslations( translations );
					this.CheckHash = updatedCheckHash;
					this.NextRefresh = nextRefresh;
					State = SubSystemState.Connected;
				});
			}
			else
			{
				this.Refresh( delegate( bool updated ) {
					State = SubSystemState.Connected;
				});
			}
		}
		
		public void Update()
		{
			if( ( this.NextRefresh != -1 ) && ( EB.Time.Now > this.NextRefresh ) )
			{
				this.API.Refresh( this.CheckHash, delegate( string err, List<ServerRedeemerTranslation> translations, string updatedCheckHash, int nextRefresh ) {
					this.OnServerTranslations( translations );
					this.CheckHash = updatedCheckHash;
					this.NextRefresh = nextRefresh;
				});
			}
		}
		
		public override void Disconnect (bool isLogout)
		{
		}
		#endregion
	}
}

