using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.IAP.Internal
{
	internal class DefaultProvider : Provider
	{
		public static DefaultProvider Create( Config config )
		{
			return new DefaultProvider(config);
		}
		
		Config _config;
		
		DefaultProvider(Config config)
		{
			_config = config;
		}
		
		public void PurchaseItem( Item item) 
		{
			if (_config.OnPurchaseFailed != null)
			{
				_config.OnPurchaseFailed("Not supported");
			}
		}

		#region Provider implementation
		public void Enumerate (List<Item> items)
		{
			foreach( var item in items )
			{
				item.valid = true;
			}
			
			if (_config.OnEnumerate!=null)
			{
				_config.OnEnumerate();
			}
		}
		#endregion

		#region Provider implementation
		public void Complete (Transaction transaction)
		{
			
		}
		#endregion

		#region Provider implementation
		public string Name 
		{
			get 
			{
				return "default";
			}
		}
		#endregion
	}
}
