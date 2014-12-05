//
//  UnityMessage.m
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/10/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import "UnityMessage.h"

@implementation UnityMessage

- (id)initWithName:(NSString*)name message:(NSString*)message {
    self = [super init];
    if (self) {
        self.name = name;
        self.message = message;
    }
    return self;
}

@end
