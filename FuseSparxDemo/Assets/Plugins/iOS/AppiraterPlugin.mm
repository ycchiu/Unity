#import "Appirater.h"

void UnitySendMessage(const char*,const char*, const char*);

const char* kObjectName = "appirater_callbacks";

NSString* kMessage          = @"If you enjoy using %@, would you mind taking a moment to rate it? It won't take more than a minute. Thanks for your support!";
NSString* kTitle            = @"Rate %@";
NSString* kCancelButton     = @"No, Thanks";
NSString* kRateButton       = @"Rate %@";
NSString* kLaterButton      = @"Remind me later";

@interface MyAppiraterDelegate : NSObject<AppiraterDelegate>
-(void)appiraterDidDisplayAlert:(Appirater *)appirater;
-(void)appiraterDidDeclineToRate:(Appirater *)appirater;
-(void)appiraterDidOptToRate:(Appirater *)appirater;
-(void)appiraterDidOptToRemindLater:(Appirater *)appirater;
@end

@implementation MyAppiraterDelegate

-(void)appiraterDidDisplayAlert:(Appirater *)appirater
{
    UnitySendMessage(kObjectName, "OnDisplay", "");
}

-(void)appiraterDidDeclineToRate:(Appirater *)appirater
{
    UnitySendMessage(kObjectName, "OnDeclined", "");
}

-(void)appiraterDidOptToRate:(Appirater *)appirater
{
    UnitySendMessage(kObjectName, "OnRated", "");
}

-(void)appiraterDidOptToRemindLater:(Appirater *)appirater
{
    UnitySendMessage(kObjectName, "OnRemind", "");
}

@end


extern "C"
{
    MyAppiraterDelegate* _delegate = nil;
    
    void _Appirater_SetTitle( const char* str )
    {
        kTitle = [NSString stringWithUTF8String:str];
        [kTitle retain];
    }
    
    void _Appirater_SetMessage( const char* str )
    {
        kMessage = [NSString stringWithUTF8String:str];
       [kMessage retain];
    }
    
    void _Appirater_SetCancelButton( const char* str )
    {
        kCancelButton = [NSString stringWithUTF8String:str];
       [kCancelButton retain];
    }
    
    void _Appirater_SetRateButton( const char* str )
    {
        kRateButton = [NSString stringWithUTF8String:str];
        [kRateButton retain];
    }
    
    void _Appirater_SetRemindButton( const char* str)
    {
        kLaterButton = [NSString stringWithUTF8String:str];
        [kLaterButton retain];
    }
    
    void _Appirater_SetAppId( const char* appId)
    {
        NSString* str = [NSString stringWithUTF8String:appId];
        [str retain];
        [Appirater setAppId:str];
        if (_delegate == nil)
        {
            _delegate = [MyAppiraterDelegate alloc];
            [_delegate retain];
            [Appirater setDelegate:_delegate];
        }
    }
    
    void _Appirater_SetDaysUntilPrompt( double days )
    {
        [Appirater setDaysUntilPrompt:days];
    }
    
    void _Appirater_SetUsesUntilPrompt( int uses )
    {
        [Appirater setUsesUntilPrompt:uses];
    }
    
    void _Appirater_SetSignificantEventsUntilPrompt( int events )
    {
        [Appirater setSignificantEventsUntilPrompt:events];
    }
    
    void _Appirater_SetTimeBeforeReminding(double time)
    {
        [Appirater setTimeBeforeReminding:time];
    }
    
    void _Appirater_SetDebug( bool debug)
    {
        [Appirater setDebug:debug];
    }
    
    void _Appirater_AppLaunched( bool canPrompt )
    {
        [Appirater appLaunched:canPrompt];
    }
    
    void _Appirater_AppEnteredForeground(bool canPrompt )
    {
        [Appirater appEnteredForeground:canPrompt];
    }
    
    void _Appirater_UserDidSignificantEvent(bool canPrompt )
    {
        [Appirater userDidSignificantEvent:canPrompt];
    }


}