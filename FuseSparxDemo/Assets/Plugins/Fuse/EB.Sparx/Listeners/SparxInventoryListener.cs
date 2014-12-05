using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public interface InventoryListener
	{
		void OnInventoryUpdated();
		void OnInventorySynced(int requestId);
		void OnInventoryItemsAdded(int requestId, Hashtable items );
		void OnInventoryPurchaseFailed(int requestId);
	}
}
