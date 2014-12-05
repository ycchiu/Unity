#import <NewRelic.h>

extern "C"
{
    void _NewRelic_Initialize( const char* apiKey )
    {
        NSString* token = [NSString stringWithUTF8String:apiKey];
        [NewRelic startWithApplicationToken:token];
    }
    
    NRTimer* _NewRelic_CreateTimer()
    {
        NRTimer* timer = [NewRelic createAndStartTimer];
        [timer retain];
        return timer;
    }
    
    void _NewRelic_StopTimer( NRTimer* timer )
    {
        if (timer)
        {
            [timer stopTimer];
        }
    }
    
    void _NewRelic_DisposeTimer( NRTimer* timer )
    {
        if (timer)
        {
            [timer release];
        }
    }
    
    void _NewRelic_NotifyHttpRequest( const char* url, int statusCode, NRTimer* timer, int bytesSent, int bytesReceived, const char* response )
    {
        NSData* data = nil;
        if (response != NULL)
        {
            NSString* strResponse = [NSString stringWithUTF8String:response];
            data = [strResponse dataUsingEncoding:NSUTF8StringEncoding];
        }
        
        NSURL* nsUrl = [NSURL URLWithString:[NSString stringWithUTF8String:url] ];
       [NewRelic noticeNetworkRequestForURL:nsUrl withTimer:timer responseHeaders:nil statusCode:statusCode bytesSent:bytesSent bytesReceived:bytesReceived responseData:data andParams:nil];

    }

}
