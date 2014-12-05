#import <Foundation/Foundation.h>

@interface NSTcpClient : NSObject <NSStreamDelegate>

-(id)init:(bool)secure;

-(void)dealloc;

-(void)connect:(NSString*) host port:(NSInteger)port;

-(int)write:(const uint8_t*)data bytes:(NSInteger)bytes;

-(int)read:(uint8_t*)buffer maxLength:(NSInteger)maxLength;

-(bool)connected;

-(bool)error;

-(bool)dataAvailable;

@end