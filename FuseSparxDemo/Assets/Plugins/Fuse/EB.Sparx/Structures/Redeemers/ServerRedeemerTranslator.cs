using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class ServerRedeemerTranslator : EB.Sparx.IRedeemerTranslator
	{
		private List<ServerRedeemerTranslation> Translations = null;
		private Dictionary< string, List<ServerRedeemerTranslation> > TranslationMapper = new Dictionary< string, List<ServerRedeemerTranslation> >();
	
		public ServerRedeemerTranslator( List<ServerRedeemerTranslation> translations )
		{
			this.Translations = translations;
			foreach( ServerRedeemerTranslation translation in translations )
			{
				List<ServerRedeemerTranslation> typeMapping = null;
				if( this.TranslationMapper.TryGetValue( translation.Type, out typeMapping ) == false )
				{
					typeMapping = new List<ServerRedeemerTranslation>();
					this.TranslationMapper.Add( translation.Type, new List<ServerRedeemerTranslation>() );
				}
				
				typeMapping.Add( translation );
			}
		}
	
		public List<EB.Sparx.IRedeemerDisplayMapping> DisplayMappings
		{
			get
			{
				List<EB.Sparx.IRedeemerDisplayMapping> mappings = new List<EB.Sparx.IRedeemerDisplayMapping>();
				
				foreach( KeyValuePair< string, List<ServerRedeemerTranslation> > pair in this.TranslationMapper )
				{
					mappings.Add( new EB.Sparx.RedeemerDynamicDisplayMapping( pair.Key, "*", TextureCallback, LabelCallback, null, new Color(0.867f, 0.714f, 0.000f, 1.000f), new Color(0.867f, 0.714f, 0.000f, 1.000f) ) );
				}
				
				return mappings;
			}
		}
		
		string TextureCallback(EB.Sparx.RedeemerItem item, string tag = null)
		{
			string texture = string.Empty;
			
			List<ServerRedeemerTranslation> typeMapping = null;
			if( this.TranslationMapper.TryGetValue( item.Type, out typeMapping ) == true )
			{
				foreach( ServerRedeemerTranslation translation in typeMapping )
				{
					if( item.Data == translation.Data )
					{
						texture = translation.Texture.GlobalTexture;
						break;
					}
				}	
			}
			
			return texture;
		}
		
		
		string LabelCallback(EB.Sparx.RedeemerItem item)
		{	
			string label = string.Empty;
			
			List<ServerRedeemerTranslation> typeMapping = null;
			if( this.TranslationMapper.TryGetValue( item.Type, out typeMapping ) == true )
			{
				foreach( ServerRedeemerTranslation translation in typeMapping )
				{
					if( item.Data == translation.Data )
					{
						label = translation.Label;
						break;
					}
				}	
			}
			
			return label;
		}
		
		public override string ToString()
		{
			string buffer = string.Empty;
			
			if( this.Translations != null )
			{
				foreach( ServerRedeemerTranslation translation in this.Translations )
				{
					buffer += translation;
				}
			}
			
			return buffer;
		}
	}
}
