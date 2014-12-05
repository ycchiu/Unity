#include "OtherLevels.h"

extern "C" void OtherLevels_SetTrackingID(char* trackingId)
{
    NSString *stringFromChar = [NSString stringWithCString:trackingId encoding:NSUTF8StringEncoding];
    [OtherLevels setTrackingID:stringFromChar];
}

extern "C" void OtherLevels_PushPhashForTracking(char* phash)
{
    NSString *stringFromChar = [NSString stringWithCString:phash encoding:NSUTF8StringEncoding];
    [OtherLevels pushPhashForTracking:stringFromChar];
}

extern "C" void OtherLevels_TrackLastPhashOpen()
{
    [OtherLevels trackLastPhashOpen];
}

extern "C" void OtherLevels_RegisterEvent(char* eventType, char* eventLabel)
{
    NSString *stringFromCharA = [NSString stringWithCString:eventType encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:eventLabel encoding:NSUTF8StringEncoding];
    [OtherLevels registerEvent:stringFromCharA label:stringFromCharB];
}

extern "C" void OtherLevels_RegisterEventWithPhash(char* eventType, char* eventLabel, char* phash)
{
    NSString *stringFromCharA = [NSString stringWithCString:eventType encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:eventLabel encoding:NSUTF8StringEncoding];
    NSString *stringFromCharC = [NSString stringWithCString:phash encoding:NSUTF8StringEncoding];
    [OtherLevels registerEvent:stringFromCharA label:stringFromCharB phash:stringFromCharC];
}

extern "C" void OtherLevels_ClearLocalNotificationsPending()
{
    [OtherLevels clearLocalNotificationsPending];
}

extern "C" void OtherLevels_ScheduleLocalNotification(char* notification, int badge, char* campaign, double secondsFromNow)
{
    NSDate *date = [NSDate dateWithTimeIntervalSinceNow:secondsFromNow];
    
    NSString *stringFromCharA = [NSString stringWithCString:notification encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:campaign encoding:NSUTF8StringEncoding];
    
    [OtherLevels scheduleLocalNotification:stringFromCharA badge:badge campaign:stringFromCharB date:date];
}

extern "C" void OtherLevels_ScheduleLocalNotificationEx(char* notification, int badge, char* action, char* campaign, double secondsFromNow)
{
    NSString *stringFromCharA = [NSString stringWithCString:notification encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:action encoding:NSUTF8StringEncoding];
    NSDate *date = [NSDate dateWithTimeIntervalSinceNow:secondsFromNow];
    NSString *stringFromCharC = [NSString stringWithCString:campaign encoding:NSUTF8StringEncoding];
    
    [OtherLevels scheduleLocalNotification:stringFromCharA badge:badge action:stringFromCharB campaign:stringFromCharC date:date];
}

extern "C" void OtherLevels_ScheduleLocalNotificationWithMetadata(char* notification, int badge, char* campaign, double secondsFromNow, char* userInfo)
{
    NSDate *date = [NSDate dateWithTimeIntervalSinceNow:secondsFromNow];
    
    NSString *stringFromCharA = [NSString stringWithCString:notification encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:campaign encoding:NSUTF8StringEncoding];
    NSString *stringFromCharC = [NSString stringWithCString:userInfo encoding:NSUTF8StringEncoding];
    
    NSArray *kvPairs = [stringFromCharC componentsSeparatedByString: @","];
    NSMutableDictionary *metaData = [[NSMutableDictionary alloc] init];

    for(int i=0; i<[kvPairs count];i++){
         NSArray *keyValue = [kvPairs[i] componentsSeparatedByString: @":"];
        [metaData setObject:keyValue[1] forKey:keyValue[0]];
    }
    
    [OtherLevels scheduleLocalNotification:stringFromCharA badge:badge campaign:stringFromCharB date:date userInfo:[[metaData copy] autorelease]];
    
    [metaData release];
}

extern "C" void OtherLevels_ScheduleLocalNotificationExWithMetadata(char* notification, int badge, char* action, char* campaign, double secondsFromNow, char* userInfo)
{
    NSString *stringFromCharA = [NSString stringWithCString:notification encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:action encoding:NSUTF8StringEncoding];
    NSDate *date = [NSDate dateWithTimeIntervalSinceNow:secondsFromNow];
    NSString *stringFromCharC = [NSString stringWithCString:campaign encoding:NSUTF8StringEncoding];
    NSString *stringFromCharD = [NSString stringWithCString:userInfo encoding:NSUTF8StringEncoding];
    
    NSArray *kvPairs = [stringFromCharD componentsSeparatedByString: @","];
    NSMutableDictionary *metaData = [[NSMutableDictionary alloc] init];
    
    for(int i=0; i<[kvPairs count];i++){
        NSArray *keyValue = [kvPairs[i] componentsSeparatedByString: @":"];
        [metaData setObject:keyValue[1] forKey:keyValue[0]];
    }

    [OtherLevels scheduleLocalNotification:stringFromCharA badge:badge action:stringFromCharB campaign:stringFromCharC date:date userInfo:[[metaData copy] autorelease]];
    
    [metaData release];
}

extern "C" void OtherLevels_RegisterDevice(char* deviceToken, char* trackingId)
{
    NSString *stringFromCharA = [NSString stringWithCString:deviceToken encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:trackingId encoding:NSUTF8StringEncoding];
    
    [OtherLevels registerDevice:stringFromCharA withTrackingId:stringFromCharB];
}

extern "C" void OtherLevels_UnregisterDevice(char* deviceToken)
{
    NSString *stringFromCharA = [NSString stringWithCString:deviceToken encoding:NSUTF8StringEncoding];
    
    [OtherLevels unregisterDevice:stringFromCharA];
}

extern "C" void OtherLevels_SetTagValue(char* trackingId, char* tagName, char* tagValue, char* tagType )
{
    NSString *stringFromCharA = [NSString stringWithCString:trackingId encoding:NSUTF8StringEncoding];
    NSString *stringFromCharB = [NSString stringWithCString:tagName encoding:NSUTF8StringEncoding];
    NSString *stringFromCharC = [NSString stringWithCString:tagValue encoding:NSUTF8StringEncoding];
    NSString *stringFromCharD = [NSString stringWithCString:tagType encoding:NSUTF8StringEncoding];
    
    [OtherLevels setTagValue:stringFromCharA :stringFromCharB :stringFromCharC :stringFromCharD];
}
