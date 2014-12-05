//
//  OtherLevels.h
//  OtherLevels
//
//  1.1.15
//  Introduced new calls, modified existing calls, URL cleanup and general fixes
//  1.1.14
//  Fixed up the encoding issue in some of the OtherLevels calls
//  1.1.13
//  Deleted the registerRichInboxEvent call and modified the existing registerEvent call and added Get/Set Tag API call
//  1.1.12
//  Added registerRichInboxEvent call to track Rich Inbox messages
//  1.1.11
//  Fixed crash if the OtherLevels settings are cleared out
//  1.1.10
//  Fixed crash in deleting Old OL Requests and URL cleanup
//  1.1.9
//  iOS 7 support and Geo added
//  1.1.8
//  Fixed memory leaks and code cleanup
//  1.1.7
//  Adds tracking id link and unlink functions, urldecodes pushes
//  1.1.6
//  Adds in app session start for alert and interstitial A/B testing
//  1.1.5
//  Adds automatic language and timezone tagging for push registered devices
//
//  Created by Timothy Marks
//  Copyright 2011-2013 OtherLevels. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <sys/utsname.h>
#import <CoreLocation/CoreLocation.h>

#define OtherLevels_Library_Version @"1.1.15"

@interface OtherLevels : NSObject {
}

#pragma mark -
#pragma mark Debugging

/**
 * Debug versions of the OL startSessionWithLaunchOptions Lib API with NSLogging enabled. Make sure the OL_App_Key is set in the info.plist file
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 */
+ (void)debugSessionWithLaunchOptions:(NSDictionary *)launchOptions;

/**
 * Debug versions of the OL startSessionWithLaunchOptions Lib API with NSLogging enabled. Make sure the OL_App_Key is set in the info.plist file
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 * @param deviceId A custom deviceId to use for tracking purposes, usually an email, accountId or account hash to manage a user on multiple devices
 */
+ (void)debugSessionWithLaunchOptions:(NSDictionary *)launchOptions clientDeviceId:(NSString*)deviceId;

/**
 * Debug versions of the OL startSessionWithAppKey Lib API with NSLogging enabled
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 */
+ (void)debugSessionWithAppKey:(NSString*)appKey launchOptions:(NSDictionary *)launchOptions;

/**
 * Debug versions of the OL startSessionWithAppKey Lib API with NSLogging enabled
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 * @param deviceId A custom deviceId to use for tracking purposes, usually an email, accountId or account hash to manage a user on multiple devices
 */
+ (void)debugSessionWithAppKey:(NSString*)appKey launchOptions:(NSDictionary *)launchOptions clientDeviceId:(NSString*)deviceId;

#pragma mark -
#pragma mark SessionHandling

/**
 * Start a session and pass in the launch options for the App. Make sure the OL_App_Key is set in the info.plist file
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 */
+ (void)startSessionWithLaunchOptions:(NSDictionary *)launchOptions;

/**
 * Start a session and pass in the launch options for the App. Make sure the OL_App_Key is set in the info.plist file
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 * @param clientDeviceId A custom deviceId to use for tracking purposes, usually an email, accountId or account hash to manage a user on multiple devices
 */
+ (void)startSessionWithLaunchOptions:(NSDictionary *)launchOptions clientDeviceId:(NSString*)deviceId;

/**
 * Start a session with your AppKey and pass in the launch options for the App
 * @param appKey Your application key (String) from your OtherLevels.com account
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 */
+ (void)startSessionWithAppKey:(NSString*)appKey launchOptions:(NSDictionary *)launchOptions;

/**
 * Start a session with your AppKey and pass in the launch options for the App
 * @param appKey Your application key (String) from your OtherLevels.com account
 * @param launchOptions The launch options dictionary from the UIApplicationDelegate
 * @param clientDeviceId A custom deviceId to use for tracking purposes, usually an email, accountId or account hash to manage a user on multiple devices
 */
+ (void)startSessionWithAppKey:(NSString*)appKey launchOptions:(NSDictionary *)launchOptions clientDeviceId:(NSString*)deviceId;

#pragma mark -
#pragma mark RemoteNotificationHandling

/**
 * Used as a placeholder currently for future features when a notification is delivered while the application is running in foreground
 * Call from within UIApplicationDelegate didReceiveNotification with same parameters to track a push open
 * @param application The UIApplication from the parent
 * @param notification The notification from the parent
 */
+ (void)didReceiveNotification:(UIApplication*)application notification:(NSDictionary*)notification;

#pragma mark -
#pragma mark LocalNotificationHandling

/**
 * Call from within UIApplicationDelegate didAcceptLocalNotification with same parameters to track a local push open
 * @param application The UIApplication from the parent
 * @param notification The notification from the parent
 */
+ (void)didAcceptLocalNotification:(UIApplication*)application notification:(UILocalNotification*)notification;

#pragma mark -
#pragma mark TrackingId

/**
 * Associate a trackingId with a device. This allows the devices to be tracked on an individual basis and still hold a reference for retargeting
 * @param trackingId The trackingId of the user, usually and email, accountId or account hash to help send retargeted messages to a device
 */ 
+ (void)setTrackingID:(NSString*)trackingId;

#pragma mark -
#pragma mark InAppSessionStart

/**
 * Register a phash assigned to an in App alert or interstitial
 * @param phash The phash from the split associated with the message or nil if phash failed
 */
+ (void)pushPhashForTracking:(NSString*)phash;

/**
 * Track a message open from an in App alert or interstitial, uses the last phash pushed into the tracking list
 */
+ (void)trackLastPhashOpen;

#pragma mark -
#pragma mark EventHandling

/**
 * Register an event for the session
 * @param eventType The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
 * @param eventLabel The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
 */
+ (void)registerEvent:(NSString*)eventType label:(NSString*)eventLabel;

/**
 * Register an event for the session with phash
 * @param eventType The type of event (should be an explanative top level ie. overview, purchase, registered, opened)
 * @param eventLabel The event label (should be a more descriptive label ie. Purchased Magic Beans $5.99 package)
 * @param phash The phash passed in separately with the event call
 */
+ (void)registerEvent:(NSString*)eventType label:(NSString*)eventLabel phash:(NSString*)phash;

#pragma mark -
#pragma mark DeviceInitiatedPushes

/**
 * This call is used to perform a split test and phash generation for pushes initiated from the device
 * @param notification The message to perform a split test on
 * @param campaign The campaignToken to track the push under
 * @param block The block to execute when the split has been fulfilled - this block should physically send the push. Phash could be nil in case of network outage
 */
+ (void)splitTestNotification:(NSString*)notification campaign:(NSString*)campaign pushSend:(void(^)(NSString* message, NSString* phash, NSData *content))block;

#pragma mark -
#pragma mark LocalNotifications

/**
 * Clear all local notifications that haven't been been delivered yet
 */
+ (void)clearLocalNotificationsPending;

/**
 * Perform a split test and schedule a local notification
 * @param notification The message to perform a split test on
 * @param badge The badge to set the app to
 * @param campaign The campaignToken to track the push under
 * @param date The date to show the notification
 */
+ (void)scheduleLocalNotification:(NSString*)notification badge:(int)badge campaign:(NSString*)campaign date:(NSDate*)date;

/**
 * Perform a split test and schedule a local notification
 * @param notification The message to perform a split test on
 * @param badge The badge to set the app to
 * @param campaign The campaignToken to track the push under
 * @param date The date to show the notification
 * @param userInfo A dictionary(key-value pairs) for passing custom information to the notified application.
 */
+ (void)scheduleLocalNotification:(NSString*)notification badge:(int)badge campaign:(NSString*)campaign date:(NSDate*)date userInfo:(NSDictionary*)userInfo;

/**
 * Perform a split test and schedule a local notification
 * @param notification The message to perform a split test on
 * @param badge The badge to set the app to
 * @param action The name of the action button to show
 * @param campaign The campaignToken to track the push under
 * @param date The date to show the notification
 */
+ (void)scheduleLocalNotification:(NSString*)notification badge:(int)badge action:(NSString*)action campaign:(NSString*)campaign date:(NSDate*)date;

/**
 * Perform a split test and schedule a local notification
 * @param notification The message to perform a split test on
 * @param badge The badge to set the app to
 * @param action The name of the action button to show
 * @param campaign The campaignToken to track the push under
 * @param date The date to show the notification
 * @param userInfo A dictionary(key-value pairs) for passing custom information to the notified application.
 */
+ (void)scheduleLocalNotification:(NSString*)notification badge:(int)badge action:(NSString*)action campaign:(NSString*)campaign date:(NSDate*)date userInfo:(NSDictionary*)userInfo;

#pragma mark -
#pragma mark OtherLevelsPushLibrary

/**
 * Register a device with OtherLevels push service
 * @param deviceToken The deviceToken of the device
 * @param trackingId A publishers userId. This should be a unique identifier of the user (ie. email, phone no.) or nil for an anonymous user
 */
+ (void)registerDevice:(NSString*)deviceToken withTrackingId:(NSString*)trackingId;

/**
 * Register a device with OtherLevels push service
 * @param deviceToken The deviceToken of the device
 * @param trackingId A publishers userId. This should be a unique identifier of the user (ie. email, phone no.) or nil for an anonymous user
 * @param tags Any tags to tag the registered user/device (An NSArray of NSDictionary tag objects. Each tag is an NSDictionary object with name:value, value:value, type:value) Example: ({name = city; type = string; value = Brisbane;}, {name = time; type = timestamp; value = 1356998412000;}, {name = age; type = numeric; value = 25;}) 
 */
+ (void)registerDevice:(NSString*)deviceToken withTrackingId:(NSString*)trackingId withTags:(NSArray*)tags;

/**
 * UnRegister a device from OtherLevels push service, this puid will no longer receive pushes for that puid, nil puids will no longer receive broadcasts
 * @param deviceToken The deviceToken of the device
 */
+ (void)unregisterDevice:(NSString*)deviceToken;

/**
 * Send the users current location
 * @param currentLocation The CLLocation object which represents the location data
 * @param trackingId The trackingId that was linked to the device
 */
+ (void) geoLocationUpdate:(CLLocation*)currentLocation :(NSString*)trackingId;

/**
 * Get the tag value for a tag name associated with a trackingId
 * @param trackingId The trackingId that was linked to the device
 * @param tagName The tag name associated with the trackingId
 * @param block The block to execute when the get tagValue is returned. TagValue could be nil when the tagName does not exist
 */
+ (void) getTagValue:(NSString*)trackingId :(NSString*)tagName :(void(^)(NSString* tagValue))block;

/**
 * Set the tag value for a tag name associated with a trackingId
 * @param trackingId The trackingId that was linked to the device
 * @param tagName The tag name associated with the trackingId
 * @param tagValue The tag Value that is set
 * @param tagType The datatype of the Value that is set (send as "numeric" OR "string" OR "timestamp" only depending on your value)
   Example1: To pass in tagName:Age, send in tagValue:25, send in tagType:numeric (all passed in as strings)
   Example2: To pass in tagName:City, send in tagValue:London, send in tagType:string (all passed in as strings)
   Example3: To pass in tagName:Time, send in tagValue:1356998412000 (Needs to be UnixTimeStamp in milliseconds - [[NSdate date] timeIntervalSince1970] * 1000), send in tagType:timestamp (all passed in as strings)
 */
+ (void) setTagValue:(NSString*)trackingId :(NSString*)tagName :(NSString*)tagValue :(NSString*)tagType;

/**
 * Set a batch of tag values for a trackingId
 * @param trackingId The trackingId that was linked to the device
 * @param tags Any tags to tag the registered user/device (An NSArray of NSDictionary tag objects. Each tag is an NSDictionary object with name:value, value:value, type:value) Example: ({name = city; type = string; value = Brisbane;}, {name = time; type = timestamp; value = 1356998412000;}, {name = age; type = numeric; value = 25;}) 
 */
+ (void) batchSetTag:(NSString*)trackingId withTags:(NSArray*)tags;

@end