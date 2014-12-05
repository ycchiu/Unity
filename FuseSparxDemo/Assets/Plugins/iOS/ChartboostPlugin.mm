#import "Chartboost.h"
#import <Foundation/Foundation.h>

extern "C"
{
    void _Chartboost_StartSession( const char* appId, const char* appSignature )
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        cb.appId = [NSString stringWithUTF8String:appId];
        cb.appSignature =[NSString stringWithUTF8String:appSignature];
        
        [cb startSession];
    }
    
    void _Chartboost_ShowInterstitial()
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        [cb showInterstitial];
    }
    
    void _Chartboost_ShowInterstitial2(const char* location)
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        [cb showInterstitial: [NSString stringWithUTF8String:location] ];
    }
    
    void _Chartboost_CacheInterstitial()
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        [cb cacheInterstitial];
    }
    
    void _Chartboost_ShowMoreApps()
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        [cb showMoreApps];
    }
    
    void _Chartboost_CacheMoreApps()
    {
        Chartboost *cb = [Chartboost sharedChartboost];
        [cb cacheMoreApps];
    }
    

}


