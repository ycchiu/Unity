//
//  UnityMessage.h
//  KabamSODAUnity
//
//  Created by Andrew Young on 2/10/14.
//  Copyright (c) 2014 Kabam. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface UnityMessage : NSObject

@property (copy) NSString *name;
@property (copy) NSString *message;

- (id)initWithName:(NSString*)name message:(NSString*)message;

@end
