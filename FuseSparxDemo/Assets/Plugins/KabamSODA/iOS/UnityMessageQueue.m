//
//  UnityMessageQueue.m
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/10/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import "UnityMessageQueue.h"

@implementation UnityMessageQueue {
    NSMutableArray *queue;
}

@synthesize receiverObjectName = _receiverObjectName;

// This is located in libiPhone-lib.a
extern void UnitySendMessage(const char *, const char *, const char *);

- (id)init {
    self = [super init];
    if (self) {
        queue = [[NSMutableArray alloc] init];
    }
    return self;
}

- (void)processQueue {
    if (self.receiverObjectName != nil) {
        // Process queue
        for (UnityMessage *message in queue) {
            // Send message
            UnitySendMessage([self.receiverObjectName cStringUsingEncoding:NSUTF8StringEncoding],
                             [message.name cStringUsingEncoding:NSUTF8StringEncoding],
                             [message.message cStringUsingEncoding:NSUTF8StringEncoding]);
        }
        [queue removeAllObjects];
    }
}

- (void)sendMessageWithName:(NSString*)name message:(NSString*)message {
    [queue addObject:[[[UnityMessage alloc] initWithName:name message:message] autorelease]];
    [self processQueue];
}

- (void)setReceiverObjectName:(NSString*)receiverObjectName {
    if (_receiverObjectName == nil || ![_receiverObjectName isEqualToString:receiverObjectName]) {
        [_receiverObjectName release];
        _receiverObjectName = [receiverObjectName copy];
        [self processQueue];
    }
}

- (NSString*)receiverObjectName {
    return _receiverObjectName;
}

@end
