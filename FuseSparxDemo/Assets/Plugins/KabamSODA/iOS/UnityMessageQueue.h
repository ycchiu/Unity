//
//  UnityMessageQueue.h
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/10/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import <Foundation/Foundation.h>

#import "UnityMessage.h"

@interface UnityMessageQueue : NSObject

@property (copy) NSString *receiverObjectName;

- (void)processQueue;
- (void)sendMessageWithName:(NSString*)name message:(NSString*)message;

@end
