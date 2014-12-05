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
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sample Amazon IAP event listener.
/// Use this as reference for building your own solution for listening to IAP callbacks and events.
/// </summary>
public class AmazonIAPEventListener : MonoBehaviour {
#if UNITY_ANDROID
    // Store the retrieved list of available items so they can be used by the sample menu.
    static public List<AmazonItem> AvailableItems {
        get { return availableItems; }
    }
    static List<AmazonItem> availableItems = null;
    
    // Store the retrieved list of unavailable skus so they can be used by the sample menu.
    static public List<string> UnavailableSkus {
        get { return unavailableSkus; }
    }
    static List<string> unavailableSkus = null;
        
    // Store the retrieved user id so it can be displayed by the sample menu.
    static public string UserId {
        get { return userId; }
    }
    static string userId = null;
    
    #region Unity overrides
    /// <summary>
    /// Unity override of OnEnable
    /// </summary>
    void OnEnable() {
        // Listen to all events for illustration purposes
        AmazonIAPManager.itemDataRequestFailedEvent += itemDataRequestFailedEvent;
        AmazonIAPManager.itemDataRequestFinishedEvent += itemDataRequestFinishedEvent;
        AmazonIAPManager.purchaseFailedEvent += purchaseFailedEvent;
        AmazonIAPManager.purchaseSuccessfulEvent += purchaseSuccessfulEvent;
        AmazonIAPManager.purchaseUpdatesRequestFailedEvent += purchaseUpdatesRequestFailedEvent;
        AmazonIAPManager.purchaseUpdatesRequestSuccessfulEvent += purchaseUpdatesRequestSuccessfulEvent;
        AmazonIAPManager.onSdkAvailableEvent += onSdkAvailableEvent;
        AmazonIAPManager.onGetUserIdResponseEvent += onGetUserIdResponseEvent;
    }
 
    /// <summary>
    /// Unity override of OnDisable
    /// </summary>
    void OnDisable() {
        // Remove all event handlers
        AmazonIAPManager.itemDataRequestFailedEvent -= itemDataRequestFailedEvent;
        AmazonIAPManager.itemDataRequestFinishedEvent -= itemDataRequestFinishedEvent;
        AmazonIAPManager.purchaseFailedEvent -= purchaseFailedEvent;
        AmazonIAPManager.purchaseSuccessfulEvent -= purchaseSuccessfulEvent;
        AmazonIAPManager.purchaseUpdatesRequestFailedEvent -= purchaseUpdatesRequestFailedEvent;
        AmazonIAPManager.purchaseUpdatesRequestSuccessfulEvent -= purchaseUpdatesRequestSuccessfulEvent;
        AmazonIAPManager.onSdkAvailableEvent -= onSdkAvailableEvent;
        AmazonIAPManager.onGetUserIdResponseEvent -= onGetUserIdResponseEvent;
    }
    #endregion
 
    /// <summary>
    /// callback for when an item data request has failed.
    /// </summary>
    void itemDataRequestFailedEvent() {
        AmazonIAP.Log("itemDataRequestFailedEvent" );
    }
 
    /// <summary>
    /// Callback for when an item data request has finished.
    /// </summary>
    /// <param name='unavailableSkus'>
    /// Unavailable skus.
    /// </param>
    /// <param name='availableItems'>
    /// Available items.
    /// </param>
    void itemDataRequestFinishedEvent( List<string> unavailableSkus, List<AmazonItem> availableItems ) {
        AmazonIAPEventListener.availableItems = availableItems;
        AmazonIAPEventListener.unavailableSkus = unavailableSkus;
        AmazonIAP.Log("itemDataRequestFinishedEvent. unavailable skus: " + unavailableSkus.Count + ", avaiable items: " + availableItems.Count );
    }
 
    /// <summary>
    /// Callback for when a purchase has failed, with a reason.
    /// </summary>
    /// <param name='reason'>
    /// Reason.
    /// </param>
    void purchaseFailedEvent( string reason ) {
        AmazonIAP.Log("purchaseFailedEvent: " + reason );
    }
 
    /// <summary>
    /// Callback for when a purchase was successful, with a receipt.
    /// </summary>
    /// <param name='receipt'>
    /// Receipt.
    /// </param>
    void purchaseSuccessfulEvent( AmazonReceipt receipt ) {
        AmazonIAP.Log("purchaseSuccessfulEvent: " + receipt );
    }
 
    /// <summary>
    /// Callback for when the purchase update request failed.
    /// </summary>
    void purchaseUpdatesRequestFailedEvent() {
        AmazonIAP.Log("purchaseUpdatesRequestFailedEvent" );
    }
 
    /// <summary>
    /// Callback for when the purchase update request succeeded.
    /// </summary>
    /// <param name='revokedSkus'>
    /// Revoked skus.
    /// </param>
    /// <param name='receipts'>
    /// Receipts.
    /// </param>
    void purchaseUpdatesRequestSuccessfulEvent( List<string> revokedSkus, List<AmazonReceipt> receipts ) {
        AmazonIAP.Log("purchaseUpdatesRequestSuccessfulEvent. revoked skus: " + revokedSkus.Count );
        foreach( AmazonReceipt receipt in receipts )
            AmazonIAP.Log(receipt.ToString());
    }
 
    /// <summary>
    /// Callback for when the SDK is available.
    /// </summary>
    /// <param name='isTestMode'>
    /// Is test mode.
    /// </param>
    void onSdkAvailableEvent( bool isTestMode ) {
        AmazonIAP.Log("onSdkAvailableEvent. isTestMode: " + isTestMode );
    }

    /// <summary>
    /// Callback for when the user id is retrieved.
    /// </summary>
    /// <param name='userId'>
    /// User identifier.
    /// </param>
    void onGetUserIdResponseEvent( string userId ) {
        AmazonIAPEventListener.userId = userId;
        AmazonIAP.Log("onGetUserIdResponseEvent: " + userId );
    }

#endif
}


