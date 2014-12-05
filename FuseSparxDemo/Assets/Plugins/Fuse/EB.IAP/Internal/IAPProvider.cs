using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.IAP.Internal
{
	public interface Provider
	{
		string Name {get;}
		void PurchaseItem( Item item);
		void Enumerate(List<Item> items);
		void Complete( Transaction transaction );
	}
	
	internal class ProviderFactory
	{
		public static Provider Create( Config config )
		{
#if UNITY_EDITOR
			return DebugProvider.Create( config );
#elif UNITY_WEBPLAYER
			return FacebookProvider2.Create(config);   // moko: added facebook-unity implementation
#elif UNITY_IPHONE
			return AppleProvider.Create(config);
#elif UNITY_ANDROID
			if (EB.Sparx.Device.MobilePlatform == "amazonapp")
			{
				return AmazonProvider.Create( config );
			}
			else
			{
				return GoogleProvider.Create( config );
			}
#else
			return DefaultProvider.Create( config );	
#endif
		}
	}
	
}
