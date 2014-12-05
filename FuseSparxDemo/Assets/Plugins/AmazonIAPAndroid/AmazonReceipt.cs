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
/// Amazon receipt.
/// </summary>
public class AmazonReceipt {
    // The type of receipt.
    public string type;
    // A token to help track the receipt.
    public string token;
    // The sku the receipt is for.
    public string sku;
    // The start date of a subscription.
    public string subscriptionStartDate;
    // The end date of an expired subscription.
    public string subscriptionEndDate;
    
    /// <summary>
    /// Creates a list of AmazonReceipts from an ArrayList.
    /// </summary>
    /// <returns>
    /// The array list.
    /// </returns>
    /// <param name='array'>
    /// Array.
    /// </param>
    public static List<AmazonReceipt> fromArrayList( ArrayList array ) {
        var items = new List<AmazonReceipt>();

        // create DTO's from the Hashtables
        foreach( Hashtable ht in array )
            items.Add( new AmazonReceipt( ht ) );
        
        return items;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonReceipt"/> class.
    /// </summary>
    /// <param name='ht'>
    /// Ht.
    /// </param>
    public AmazonReceipt( Hashtable ht ) {
        type = ht["type"].ToString();
        token = ht["token"].ToString();
        sku = ht["sku"].ToString();
        
        if( ht.ContainsKey( "subscriptionStartDate" ) )
            subscriptionStartDate = ht["subscriptionStartDate"].ToString();
        
        if( ht.ContainsKey( "subscriptionEndDate" ) )
            subscriptionEndDate = ht["subscriptionEndDate"].ToString();
    }
    
    /// <summary>
    /// Returns a <see cref="System.String"/> that represents the current <see cref="AmazonReceipt"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents the current <see cref="AmazonReceipt"/>.
    /// </returns>
    public override string ToString() {
        return string.Format( "<AmazonReceipt> type: {0}, token: {1}, sku: {2}", type, token, sku );
    }

}
