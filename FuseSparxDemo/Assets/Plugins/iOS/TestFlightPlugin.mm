#include "TestFlight.h"

extern "C" 
{
    void TestFlight_TakeOff( const char* apiToken )
    {
        NSString* token = [NSString stringWithUTF8String:apiToken];
        [TestFlight takeOff: token];
        [TestFlight setOptions:[NSDictionary dictionaryWithObject:[NSNumber numberWithBool:NO] forKey:@"logToConsole"]];
        [TestFlight setOptions:[NSDictionary dictionaryWithObject:[NSNumber numberWithBool:NO] forKey:@"logToSTDERR"]];
    }
    
    void TestFlight_SetUDID()
    {
      //  [TestFlight setDeviceIdentifier:[[UIDevice currentDevice] uniqueIdentifier]];
    }
    
    void TestFlight_Log( const char* log )
    {
        TFLog(@"%s", log);
    }
    
    void TestFlight_Checkpoint( const char* name )
    {
        NSString* str = [NSString stringWithUTF8String:name];
        [TestFlight passCheckpoint: str];
    }
    
}