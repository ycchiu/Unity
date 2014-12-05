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

#if UNITY_ANDROID
public class AmazonIAP {
    
    // This is the level Amazon IAP will log messages.
    // It is recommended to keep this on verbose as you first implement IAP,
    // and then select a less verbose messaging to miminize Insights log output for day to day development.
    public const AmazonLogging.AmazonLoggingLevel errorLevel = AmazonLogging.AmazonLoggingLevel.Verbose;
    // This is used by anything that needs to display the name of this service.
    private const string serviceName = "Amazon Insights";   
    
    private static AndroidJavaObject _plugin;
    
    
    static AmazonIAP() {
        if( Application.platform != RuntimePlatform.Android )
            return;

        // find the plugin instance
        using( var pluginClass = new AndroidJavaClass( "com.amazon.AmazonIAPPlugin" ) )
            _plugin = pluginClass.CallStatic<AndroidJavaObject>( "instance" );
    }
    

    // Sends off a request to fetch all the avaialble products. This MUST be called before any other methods
    public static void initiateItemDataRequest( string[] items )  {
        if( Application.platform != RuntimePlatform.Android )
            return;
        
        var initMethod = AndroidJNI.GetMethodID( _plugin.GetRawClass(), "initiateItemDataRequest", "([Ljava/lang/String;)V" );
        AndroidJNI.CallVoidMethod( _plugin.GetRawObject(), initMethod, AndroidJNIHelper.CreateJNIArgArray( new object[] { items } ) );
    }


    // Purchases the given sku
    public static void initiatePurchaseRequest( string sku )  {
        if( Application.platform != RuntimePlatform.Android )
            return;
        
        _plugin.Call( "initiatePurchaseRequest", sku );
    }
    
    
    // Sends off a request to fetch the logged in user's id
    public static void initiateGetUserIdRequest()  {
        if( Application.platform != RuntimePlatform.Android )
            return;
        
        _plugin.Call( "initiateGetUserIdRequest" );
    }
    
    #region error and warning messaging
    /// <summary>
    /// Logs the Insights error.
    /// </summary>
    /// <param name='errorMessage'>
    /// Error message.
    /// </param>
    public static void LogError(string errorMessage) {
        // Use the InsightsClient to have one error level for all of Insights.
        AmazonLogging.LogError(errorLevel,serviceName,errorMessage);
    }
   
    /// <summary>
    /// Logs the Insights warning.
    /// </summary>
    /// <param name='errorMessage'>
    /// Error message.
    /// </param>
    public static void LogWarning(string errorMessage) {
        // Use the InsightsClient to have one error level for all of Insights.
        AmazonLogging.LogWarning(errorLevel,serviceName,errorMessage);
    }
    
    /// <summary>
    /// Log the specified message.
    /// </summary>
    /// <param name='message'>
    /// Message.
    /// </param>
    public static void Log(string message) {
        AmazonLogging.Log(errorLevel,serviceName,message);
    }
    #endregion
}
#endif
