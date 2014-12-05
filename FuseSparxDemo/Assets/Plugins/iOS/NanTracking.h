
#import <Foundation/Foundation.h>
#import <AdSupport/ASIdentifierManager.h>

@interface NanTracking : NSObject
{
}

+(void)trackNanigansEvent:(NSString *)uid type:(NSString *)type name:(NSString *)name;
+(void)trackNanigansEvent:(NSString *)uid type:(NSString *)type name:(NSString *)name value:(NSString *)value;
+(void)trackNanigansEvent:(NSString *)uid type:(NSString *)type name:(NSString *)name extraParams:(NSDictionary *)extraParams;
+(void)setFbAppId:(NSString *)fbAppId;

@end
