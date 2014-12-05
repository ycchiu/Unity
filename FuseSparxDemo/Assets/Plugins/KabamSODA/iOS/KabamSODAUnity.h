//
//  KabamSODAUnity.h
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/5/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <Soda/Soda.h>

#import "UnityMessage.h"
#import "UnityMessageQueue.h"

@interface SodaDelegateImpl : NSObject <SodaSessionDelegate>
@end

extern "C" {
    void _KabamSODAConfig(const char* clientId, const char* mobileKey, const char* wskeUrl, const char* unityVersion);
    void _KabamSODAInit(const char* receiverObjectName);
    void _KabamSODALogin(const char *playerId, const char *playerCertificate);
    void _KabamSODALogRevenueEvent(const char *json);
    const char* _KabamSODAVersion();
}