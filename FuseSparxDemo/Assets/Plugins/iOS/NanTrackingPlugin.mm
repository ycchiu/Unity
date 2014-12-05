
#import "NanTracking.h"


extern "C"
{
    void _Nanigans_Init( const char* fbId )
    {
        NSString* nsFbId = [NSString stringWithUTF8String:fbId];
        [nsFbId retain];
        [NanTracking setFbAppId:nsFbId];
    }
    
    void _Nanigans_TrackInstall( const char* uid )
    {
        NSString* nsUid = [NSString stringWithUTF8String:uid];
        [NanTracking trackNanigansEvent:nsUid type:@"install" name:@"main"];
    }
    
    void _Nanigans_TrackVisit( const char* uid )
    {
        NSString* nsUid = [NSString stringWithUTF8String:uid];
        [NanTracking trackNanigansEvent:nsUid type:@"visit" name:@"dau"];
    }
    
}
