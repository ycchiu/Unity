#import <GameKit/GameKit.h>
#import <Foundation/NSJSONSerialization.h>

#define SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(v)  ([[[UIDevice currentDevice] systemVersion] compare:v options:NSNumericSearch] != NSOrderedAscending)


extern "C"
{
    void UnitySendMessage(const char* name, const char* fn, const char* data);
    const char* kObjName = "gca_callbacks";
    
    bool _GameCenterAuthenticationSupported()
    {
        return SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"7.0");
    }

    void _GameCenterGenerateIdentity()
    {
        GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
        [localPlayer generateIdentityVerificationSignatureWithCompletionHandler:^(NSURL *publicKeyUrl, NSData *signature, NSData *salt, uint64_t timestamp, NSError *error) {
            
            if(error != nil)
            {
                NSString* errorStr = [NSString stringWithFormat:@"%@",[error localizedDescription ] ];
                NSLog(@"GC Auth Error Failed to Generate: %@", errorStr);
                UnitySendMessage(kObjName, "OnAuthenticateError", [errorStr UTF8String] );
                return; //some sort of error, can't authenticate right now
            }
            
            NSMutableDictionary* data = [[NSMutableDictionary alloc] initWithCapacity:6];
            [data setObject:[publicKeyUrl absoluteString] forKey:@"publicKeyUrl"];
            [data setObject:[signature base64Encoding] forKey:@"signature"];
            [data setObject:[salt base64Encoding] forKey:@"salt"];
            [data setObject:[NSNumber numberWithUnsignedLongLong:timestamp] forKey:@"timestamp"];
            [data setObject:localPlayer.playerID forKey:@"id"];
            [data setObject:localPlayer.alias forKey:@"alias"];
            [data setObject:[[NSBundle mainBundle] bundleIdentifier] forKey:@"bundle"];
            
            NSError* err = nil;
            NSData* jsonData = [NSJSONSerialization dataWithJSONObject:data options:0 error:&err];
            NSString* json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            
            //NSLog(@"Authenticate result: %@", json);
            UnitySendMessage(kObjName, "OnAuthenticate", [json UTF8String] );
        }];
    }
    
    void _GameCenterAuthenticate()
    {
        GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
        
        if (localPlayer.authenticated)
        {
            _GameCenterGenerateIdentity();
            return;
        }
        
        [localPlayer authenticateWithCompletionHandler:^(NSError *error) {
            if(error != nil)
            {
                // check for app not setup
                if (error.code == GKErrorGameUnrecognized || error.code == GKErrorNotSupported) {
                    NSLog(@"GC: App not setup");
                    UnitySendMessage(kObjName, "OnAuthenticateError", "" );
                    return;
                }
                
                NSString* errorStr = [NSString stringWithFormat:@"%@",[error localizedDescription ] ];
                NSLog(@"GC Auth Error: %@", errorStr);
                UnitySendMessage(kObjName, "OnAuthenticateError", [errorStr UTF8String] );
                return; //some sort of error, can't authenticate right now
            }
            else if (!localPlayer.authenticated) {
                UnitySendMessage(kObjName, "OnAuthenticateError", "" );
            }
            else {
                _GameCenterGenerateIdentity();
                return;
            }
        }];
    }
         

}
