//
//  SodaSettings.h
//  Soda
//
//  Created by Daniel Sell on 2/7/14.
//  Copyright (c) 2014 Kabam, Inc. All rights reserved.
//

#import <Foundation/Foundation.h>

/**
 * The key for Wske Env value in settings dictionary.
 */
extern NSString *const SodaSettingsEnvKey;

/**
 * The key for Wske Url value in settings dictionary.
 */
extern NSString *const SodaSettingsWskeUrlKey;

/**
 * The key for Client Id value in settings dictionary.
 */
extern NSString *const SodaSettingsClientIdKey;

/**
 * The key for Client Mobile Key value in settings dictionary.
 */
extern NSString *const SodaSettingsClientMobileKeyKey;

/**
 * The key for Logging value in settings dictionary.
 */
extern NSString *const SodaSettingsLoggingKey;

/**
 * SodaSettings is used to provide options that Soda
 * will use while talking to WSKE
 *
 * The default implementation will pull configuration
 * from the .plist file.
 */
@interface SodaSettings : NSObject

/// @name Getting Soda version

/**
 * Current version of Soda. Its value is passed in 
 * at compile time via the preprocessor definition 
 * "SODA_VERSION".
 */
@property (readonly, copy) NSString *sodaVersion;

/// @name Getting WSKE url

/**
 * Base URL used when talking to WSKE
 */
@property (readonly, copy) NSString *wskeUrl;

/// @name Getting Client ID

/**
 * ID for the client talking to WSKE
 */
@property (readonly, copy) NSString *clientId;

/// @name Getting Client Mobile Key

/**
 * Mobile Key used in conjunction with Client ID
 * while talking to WSKE
 */
@property (readonly, copy) NSString *clientMobileKey;

/// @name Getting User Agent

/**
 * User Agent used in the User-Agent HTTP header while
 * communicating to WSKE
 */
@property (readonly, copy) NSString *userAgent;

/// @name Getting Device Header

/**
 * Device information set in HTTP header while
 * communicating to WSKE
 */
@property (readonly, copy) NSString *deviceHeader;

/// @name Logging

/**
 * Whether Soda should write log messages
 */
@property (readonly) BOOL logging;

/// @name Convenience Methods

/**
 * A globally-accessible SodaSettings instance.
 *
 * This provides the default settings used if you
 * start a SodaSession without passing any settings.
 *
 * @return Shared settings instance.
 */
+ (instancetype)sharedSettings;

/**
 * Initialize SodaSettings by passing a dictionary of options
 *
 * Required Keys:
 *  SodaSettingsEnvKey
 *  SodaSettingsClientIdKey
 *  SodaSettingsClientMobileKeyKey
 *
 * Optional Keys:
 *  SodaSettingsWskeUrlKey (will override what is set via Env)
 */
- (instancetype)initWithOptions:(NSDictionary *)options;

@end
