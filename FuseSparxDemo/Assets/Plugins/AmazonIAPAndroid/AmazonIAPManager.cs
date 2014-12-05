/**
 * © 2012-2013 Amazon Digital Services, Inc. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"). You may not use this file except in compliance with the License. A copy
 * of the License is located at
 *
 * http://aws.amazon.com/apache2.0/
 *
 * or in the "license" file accompanying this file. This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
 */
using AmazonCommon;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Amazon IAP manager. This should be on a persistent object in your scene,
/// as it receives callbacks from native code.
/// </summary>
public class AmazonIAPManager : MonoBehaviour {
#if UNITY_ANDROID
    // Fired when the item data request fails
    public static event Action itemDataRequestFailedEvent;

    // Fired when the item data request finishes. Includes a list of unavailable skus and a list of sellable AmazonItems.
    public static event Action<List<string>, List<AmazonItem>> itemDataRequestFinishedEvent;

    // Fired when a purchase fails
    public static event Action<string> purchaseFailedEvent;

    // Fires when a purchase succeeds and returns a purchase receipt
    public static event Action<AmazonReceipt> purchaseSuccessfulEvent;

    // Fired when the purchase updates request fails
    public static event Action purchaseUpdatesRequestFailedEvent;

    // Fired when the purchase updates request succeeds and returns a list of revoked skus and receipts
    public static event Action<List<string>, List<AmazonReceipt>> purchaseUpdatesRequestSuccessfulEvent;

    // Fired when Amazon Payments is ready to make a purchase. Returns true of it is in debug mode.
    public static event Action<bool> onSdkAvailableEvent;

    // Fired when the user's ID is returned
    public static event Action<string> onGetUserIdResponseEvent;

    #region Unity overrides
    /// <summary>
    /// Called when this monobehavior becomes awake.
    /// </summary>
    void Awake() {
        // Set the GameObject name to the class name for easy access from native code
        gameObject.name = this.GetType().ToString();
        DontDestroyOnLoad( this );
    }
    #endregion

    /// <summary>
    /// Callback for when an item data request fails.
    /// </summary>
    /// <param name='empty'>
    /// Empty.
    /// </param>
    public void itemDataRequestFailed( string empty ) {
        if( itemDataRequestFailedEvent != null )
            itemDataRequestFailedEvent();
    }

    /// <summary>
    /// Callback for when an item data request finishes.
    /// </summary>
    /// <param name='json'>
    /// Json.
    /// </param>
    public void itemDataRequestFinished( string json ) {
        if( itemDataRequestFinishedEvent != null ) {
            var ht = json.hashtableFromJson();
            var unavailableSkus = ht["unavailableSkus"] as ArrayList;
            var availableSkus = ht["availableSkus"] as ArrayList;
            
            itemDataRequestFinishedEvent( unavailableSkus.ToList<string>(), AmazonItem.fromArrayList( availableSkus ) );
        }
    }

    /// <summary>
    /// Callback for when a purchase fails.
    /// </summary>
    /// <param name='reason'>
    /// Reason.
    /// </param>
    public void purchaseFailed( string reason ) {
        if( purchaseFailedEvent != null )
            purchaseFailedEvent( reason );
    }

    /// <summary>
    /// Callback for when a purchase is successful.
    /// </summary>
    /// <param name='json'>
    /// Json.
    /// </param>
    public void purchaseSuccessful( string json ) {
        if( purchaseSuccessfulEvent != null ) {
            purchaseSuccessfulEvent( new AmazonReceipt( json.hashtableFromJson() ) );
        }
    }

    /// <summary>
    /// Callback for when a purchase update request fails.
    /// </summary>
    /// <param name='empty'>
    /// Empty.
    /// </param>
    public void purchaseUpdatesRequestFailed( string empty ) {
        if( purchaseUpdatesRequestFailedEvent != null )
            purchaseUpdatesRequestFailedEvent();
    }

    /// <summary>
    /// Callback for when a purchase update request is successful.
    /// </summary>
    /// <param name='json'>
    /// Json.
    /// </param>
    public void purchaseUpdatesRequestSuccessful( string json )  {
        if( purchaseUpdatesRequestSuccessfulEvent != null ) {
            var ht = json.hashtableFromJson();
            var revokedSkus = ht["revokedSkus"] as ArrayList;
            var receipts = ht["receipts"] as ArrayList;
            
            purchaseUpdatesRequestSuccessfulEvent( revokedSkus.ToList<string>(), AmazonReceipt.fromArrayList( receipts ) );
        }
    }

    /// <summary>
    /// Callback when the SDK is available.
    /// </summary>
    /// <param name='param'>
    /// Parameter.
    /// </param>
    public void onSdkAvailable( string param ) {
        if( onSdkAvailableEvent != null )
            onSdkAvailableEvent( param == "1" );
    }

    /// <summary>
    /// Callback for when the user ID is available.
    /// </summary>
    /// <param name='param'>
    /// Parameter.
    /// </param>
    public void onGetUserIdResponse( string param ) {
        if( onGetUserIdResponseEvent != null )
            onGetUserIdResponseEvent( param );
    }
#endif
}

/// <summary>
/// Array list extensions.
/// </summary>
public static class ArrayListExtensions
{
    /// <summary>
    /// Converts an array list to a list.
    /// </summary>
    /// <returns>
    /// The list.
    /// </returns>
    /// <param name='arrayList'>
    /// Array list.
    /// </param>
    /// <typeparam name='T'>
    /// The 1st type parameter.
    /// </typeparam>
    public static List<T> ToList<T>( this ArrayList arrayList ) {
        List<T> list = new List<T>( arrayList.Count );
        foreach( T instance in arrayList )
            list.Add( instance );

        return list;
    }
}

