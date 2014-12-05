//
//  SodaError.h
//  Soda
//
//  Created by Matt Kantor on 2/19/14.
//  Copyright (c) 2014 Kabam, Inc. All rights reserved.
//

#import <Foundation/Foundation.h>

/**
 * Indicates an error occurred in Soda.
 */
extern NSString *const SodaErrorDomain;


/**
 * Error codes for the Soda error domain.
 */
typedef NS_ENUM(NSInteger, SodaErrorCode) {
    /**
     * Indicates a generic Soda error occurred.
     */
    SodaErrorGeneric,

    /**
     * Indicates that WSKE reported an error or that there was a problem
     * communicating with WSKE.
     */
    SodaErrorWskeFail,

    /**
     * Indicates that a WSKE token was invalid (when checked by Soda).
     */
    SodaErrorInvalidToken
};


/**
 * A subclass of NSError with some extra convinience methods.
 */
@interface SodaError : NSError

/**
 * Creates and initializes an error object for the SodaErrorDomain with a given 
 * code and userInfo dictionary.
 * 
 * @param message Message for the error.
 * @param code The error code for the error.
 * @param userInfo The `userInfo` dictionary for the error.
 * @return An error object for the SodaErrorDomain with the specified error 
 * code and the dictionary of arbitrary data userInfo.
 */
+ (instancetype)error:(NSString *)message code:(SodaErrorCode)code userInfo:(NSDictionary *)userInfo;

/**
 * Creates and initializes an error object for the SodaErrorDomain with a given
 * code.
 *
 * @param message Message for the error.
 * @param code The error code for the error.
 * @return An error object for the SodaErrorDomain with the specified error
 * code and the dictionary of arbitrary data userInfo.
 */
+ (instancetype)error:(NSString *)message code:(SodaErrorCode)code;

/**
 * Creates and initializes an error object for the SodaErrorDomain indicating 
 * an error from WSKE with a userInfo dictionary.
 *
 * @param message Message for the error.
 * @param wskeError An object representing the error from WSKE.
 * @param userInfo The `userInfo` dictionary for the error.
 * @return An error object for the SodaErrorDomain with the specified error
 * code and the dictionary of arbitrary data userInfo.
 */
+ (instancetype)error:(NSString *)message wskeError:(NSError *)wskeError userInfo:(NSDictionary *)userInfo;

/**
 * Creates and initializes an error object for the SodaErrorDomain indicating
 * an error from WSKE.
 *
 * @param message Message for the error.
 * @param wskeError An object representing the error from WSKE.
 * @return An error object for the SodaErrorDomain with the specified error
 * code and the dictionary of arbitrary data userInfo.
 */
+ (instancetype)error:(NSString *)message wskeError:(NSError *)wskeError;
@end
