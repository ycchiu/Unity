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
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Amazon item for IAP.
/// </summary>
public class AmazonItem {
    // The description of the item.
    public string description;
    // The type of the item: entitled, subscription, or consumable.
    public string type;
    // The price of the iem.
    public string price;
    // The sku of the item.
    public string sku;
    // A link to an image meant to visually represent the item.
    public string smallIconUrl;
    // The title of the item.
    public string title;
    
    /// <summary>
    /// Creates a list of AmazonItems from an ArrayList.
    /// </summary>
    /// <returns>
    /// The array list.
    /// </returns>
    /// <param name='array'>
    /// Array.
    /// </param>
    public static List<AmazonItem> fromArrayList( ArrayList array ) {
        var items = new List<AmazonItem>();

        // create DTO's from the Hashtables
        foreach( Hashtable ht in array )
            items.Add( new AmazonItem( ht ) );
        
        return items;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonItem"/> class.
    /// </summary>
    /// <param name='ht'>
    /// Hashtable containing item information.
    /// </param>
    public AmazonItem( Hashtable ht ) {
        description = ht["description"].ToString();
        type = ht["type"].ToString();
        price = ht["price"].ToString();
        sku = ht["sku"].ToString();
        smallIconUrl = ht["smallIconUrl"].ToString();
        title = ht["title"].ToString();
    }
    
    /// <summary>
    /// Returns a <see cref="System.String"/> that represents the current <see cref="AmazonItem"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents the current <see cref="AmazonItem"/>.
    /// </returns>
    public override string ToString() {
        return string.Format( "<AmazonItem> type: {0}, sku: {1}, price: {2}, title: {3}, description: {4}", type, sku, price, title, description );
    }

}
