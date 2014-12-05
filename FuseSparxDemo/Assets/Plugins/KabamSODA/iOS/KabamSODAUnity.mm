//
//  KabamSODAUnity.m
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/5/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import "KabamSODAUnity.h"

NSString* ON_REWARD_CALLBACK = @"SODAOnReward";
NSString* ON_VISIBILITY_CHANGE_CALLBACK = @"SODAOnVisibilityChange";
NSString* ON_CERTIFICATE_EXPIRED_CALLBACK = @"SODAOnCertificateExpired";

static UnityMessageQueue *messageQueue = [[UnityMessageQueue alloc] init];

@implementation SodaDelegateImpl

- (void)sessionLoginDidSucceed:(SodaSession *)session {
    // This currently does nothing
}

- (void)session:(SodaSession *)session playerCertificateDidExpireWithError:(NSError *)error {
    // TODO: Create JSON
    NSString *json = @"{}";
    [messageQueue sendMessageWithName:ON_CERTIFICATE_EXPIRED_CALLBACK message:json];
}

@end

static SodaDelegateImpl *delegate = [[SodaDelegateImpl alloc] init];
static SodaSession *session = nil;
static SodaSettings *settings = nil;

///// Helper Methods /////

// Converts C style string to NSString
NSString* createNSString (const char* string) {
	if (string) {
		return [NSString stringWithUTF8String: string];
	} else {
		return [NSString stringWithUTF8String: ""];
    }
}

// Makes a copy of a C string for use when returning a string from a method.
// (This code is copied from the Unity docs.)
char* MakeStringCopy (const char* string) {
    if (string == NULL) return NULL;
    char* res = (char*)malloc(strlen(string) + 1);
    if (res != NULL) strcpy(res, string);
    return res;
}

///// Exported Methods /////

void _KabamSODAConfig(const char* clientId, const char* mobileKey, const char* wskeUrl, const char* unityVersion) {
    NSDictionary *options = @{
                              SodaSettingsClientIdKey: createNSString(clientId),
                              SodaSettingsClientMobileKeyKey: createNSString(mobileKey),
                              SodaSettingsWskeUrlKey: createNSString(wskeUrl),
                              };
    
    settings = [[SodaSettings alloc] initWithOptions:options];
}

void _KabamSODAInit(const char *receiverObjectName) {
    messageQueue.receiverObjectName = createNSString(receiverObjectName);
    [Soda setup];
    if (settings == nil) {
        settings = [SodaSettings sharedSettings];
    }
    session = [[SodaSession alloc] initWithDelegate:delegate settings:settings];
}

void _KabamSODALogin(const char *playerId, const char *playerCertificate) {
    [session loginWithPlayerId:createNSString(playerId) playerCertificate:createNSString(playerCertificate)];
}

void _KabamSODALogRevenueEvent(const char* json) {
    NSData *jsonData = [[NSData alloc] initWithBytes:json length:strlen(json)];
    NSError *error;
    NSDictionary *data = [NSJSONSerialization JSONObjectWithData:jsonData
                                                         options:kNilOptions
                                                           error:&error];
    // TODO: Do something useful with the input data
    SKPaymentTransaction *transaction = [[SKPaymentTransaction alloc] init];
    [session logRevenueEvent:transaction];
}

const char* _KabamSODAVersion() {
    return MakeStringCopy([[SodaSettings sharedSettings].sodaVersion cStringUsingEncoding:NSUTF8StringEncoding]);
}

