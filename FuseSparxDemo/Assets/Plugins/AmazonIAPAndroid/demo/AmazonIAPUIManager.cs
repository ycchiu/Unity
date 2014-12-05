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
using System.Collections.Generic;

/// <summary>
/// Amazon IAP sample menu.
/// </summary>
public class AmazonIAPUIManager : MonoBehaviour {
#if UNITY_ANDROID
    
    // The Button Clicker test application for Amazon IAP contains a JSON file
    // intended for use with the SDK Tester. This file has some simple test SKUs,
    // and this sample application has been written to make use of the same JSON file.
    string [] buttonClickerSkus = new string[] {
        "com.amazon.buttonclicker.blue_button",
        "com.amazon.buttonclicker.green_button", 
        "com.amazon.buttonclicker.purple_button", 
        "com.amazon.buttonclicker.subscription.1mo",
        "com.amazon.buttonclicker.ten_clicks",
    };
    
    #region UI data
    // scroll position of the example UI
    Vector2 scroll = Vector2.zero;
    // Initialization status of the UI
    bool uiInitialized = false;
    #endregion
    
    #region strings
    // The title of the plugin, displayed at the top of the menu.
    private const string pluginName = "Amazon IAP";
    // The label for the button that initiates the item data request.
    private const string itemDataRequestButtonLabel = "Initiate Item Data Request";
    // The label displayed once items have been retrieved
    private const string amazonItemsAvailableLabel = "Amazon Items Available";
    // The label displayed before the list of unavailable skus
    private const string unavailableAmazonSkusLabel = "Unavailable Skus";
    // The label for the button that initiates a purchase request
    private const string purchaseItemButtonlabel = "Initiate Purchase Request";
    // The label for the button that retrieves the user ID
    private const string initializeUserIdRequestLabel = "Initialize User ID Request";
    // The label that displays a retrieved user ID.
    private const string userIDLabel = "User ID {0}";
    // The label for the title of an Amazon item
    private const string itemTitleLabel = "{0}";
    // The label for the sku and price of an Amazon item
    private const string itemSkuAndPriceLabel = "Sku: {0}, Price: {1}";
    // The label for the description of an Amazon item
    private const string itemDescriptionLabel = "{0}";
    // The label for the type and url of an Amazon item
    private const string itemTypeAndUrlLabel = "Type: {0}, Url: {1}";
    
    #endregion
    
    /// <summary>
    /// Unity override of the OnGUI function, to display Amazon IAP.
    /// </summary>
    void OnGUI() {
        // Some initialization behaviour can only be called from within the OnGUI function.
        // initialize UI early returns if it is already initialized
        InitializeUI();
        
        // This simple menu layout groups everything together.
        AmazonGUIHelpers.BeginMenuLayout();       
              
        // Wrapping all of the menu in a scroll view allows the individual menu systems to not need to handle being off screen.
        scroll = GUILayout.BeginScrollView(scroll);
        
        // The menu header is the title of the plugin in a box.
        DisplayIAPMenuHeader();
        
        // a little white space between sections of the menu makes it easier to view
        GUILayout.Label(GUIContent.none);
        
        // Displays the list of Amazon items, or a button to retrieve them if they haven't been retrieved yet.
        DisplayAmazonItems();
        
        // a little white space between sections of the menu makes it easier to view
        GUILayout.Label(GUIContent.none);
        
        // Displays a button for retrieving the Amazon user ID.
        DisplayAmazonUserID();
        
        // Scroll views are great, they allow the rest of the menu controls to not need to track on screen position.
        GUILayout.EndScrollView();
       
        // Always end Unity GUI behavior before exiting OnGUI.
        AmazonGUIHelpers.EndMenuLayout();
        
    }
    
    /// <summary>
    /// Displays the IAP menu header.
    /// </summary>
    void DisplayIAPMenuHeader() {
        AmazonGUIHelpers.BoxedCenteredLabel(pluginName);
    }
    
    /// <summary>
    /// Displays the Amazon items.
    /// </summary>
    void DisplayAmazonItems() {
        // If the Amazon items have not been retrieved yet, display a button to retrieve them.
        if(null == AmazonIAPEventListener.AvailableItems) {
            if(GUILayout.Button(itemDataRequestButtonLabel)) {
                AmazonIAP.initiateItemDataRequest(buttonClickerSkus);
            }
        }
        else {
            AmazonGUIHelpers.CenteredLabel(amazonItemsAvailableLabel);
            foreach(AmazonItem item in AmazonIAPEventListener.AvailableItems) {
                DisplayAmazonItem(item);   
            }
            // If there are any unavailable skus, display them.
            if(null != AmazonIAPEventListener.UnavailableSkus && AmazonIAPEventListener.UnavailableSkus.Count > 0) {
                AmazonGUIHelpers.CenteredLabel(unavailableAmazonSkusLabel);
                foreach(string unavailable in AmazonIAPEventListener.UnavailableSkus) {
                    AmazonGUIHelpers.CenteredLabel(unavailable);       
                }
            }
        }
    }
    
    /// <summary>
    /// Displays the Amazon item.
    /// </summary>
    /// <param name='item'>
    /// Item.
    /// </param>
    void DisplayAmazonItem(AmazonItem item) {
        // Group all the UI for an Amazon item in a box.
        GUILayout.BeginVertical(GUI.skin.box);
        
        // Display item information: title, price, sku, description, type, and url.
        AmazonGUIHelpers.CenteredLabel(string.Format(itemTitleLabel,item.title));
        AmazonGUIHelpers.CenteredLabel(string.Format(itemSkuAndPriceLabel,item.sku,item.price));
        AmazonGUIHelpers.CenteredLabel(string.Format(itemDescriptionLabel,item.description));
        AmazonGUIHelpers.CenteredLabel(string.Format(itemTypeAndUrlLabel,item.type,item.smallIconUrl));
        
        // This button allows the user to attempt to purchase this item.
        if(GUILayout.Button(purchaseItemButtonlabel)) {
            AmazonIAP.initiatePurchaseRequest( item.sku );
        }
        
        GUILayout.EndVertical();
    }
    
    /// <summary>
    /// Displays the Amazon user ID.
    /// </summary>
    void DisplayAmazonUserID() {
        // If the user ID has not been retrieved yet, display a button that retrieves the user ID.
        if(string.IsNullOrEmpty(AmazonIAPEventListener.UserId)) {
            if( GUILayout.Button(initializeUserIdRequestLabel) ) {
                AmazonIAP.initiateGetUserIdRequest();
            }
        }
        else {
            // Once the user ID is retrieved, display it.
            AmazonGUIHelpers.CenteredLabel(string.Format(userIDLabel,AmazonIAPEventListener.UserId));   
        }
    }
    
    #region UI Utility Functions
    /// <summary>
    /// Initializes the UI for the Insights example menu. If already initialized, bails out.
    /// This function needs to be called from OnGUI to access GUI features.
    /// </summary>
    void InitializeUI() {
        if(uiInitialized) {
            return;
        }
        uiInitialized = true;
       
        // Make buttons and other control elements bigger so they are easier to touch.
        AmazonGUIHelpers.SetGUISkinTouchFriendly(GUI.skin);
       
    }
    #endregion
#endif
}
