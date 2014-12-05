using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Sparx
{
	public class GachaRedeemerTranslator : EB.Sparx.IRedeemerTranslator
	{
		public string FallbackTexture { get; private set; }
	
		public GachaRedeemerTranslator( string fallbackTexture )
		{
			this.FallbackTexture = fallbackTexture;
		}
	
		public List<EB.Sparx.IRedeemerDisplayMapping> DisplayMappings
		{
			get
			{
				List<EB.Sparx.IRedeemerDisplayMapping> mappings = new List<EB.Sparx.IRedeemerDisplayMapping>();
				
				mappings.Add ( new EB.Sparx.RedeemerDynamicDisplayMapping( "gs", "*", TextureCallback, LabelCallback, null, new Color(0.867f, 0.714f, 0.000f, 1.000f), new Color(0.867f, 0.714f, 0.000f, 1.000f) ) );
				
				return mappings;
			}
		}
		
		string TextureCallback(EB.Sparx.RedeemerItem item, string tag = null)
		{
			string texture = SparxHub.Instance.GachaManager.GetTokenTexture( item.Data );
			if( string.IsNullOrEmpty( texture ) == true )
			{
				texture = this.FallbackTexture;
			}
			
			return texture;
		}
		
		
		string LabelCallback(EB.Sparx.RedeemerItem item)
		{	
			string label = SparxHub.Instance.GachaManager.GetTokenLabel( item.Data );
			if( string.IsNullOrEmpty( label ) == true )
			{
				label = item.Data;
			}
			
			return label;
		}
	}
}
