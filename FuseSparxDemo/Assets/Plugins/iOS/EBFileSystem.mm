#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

extern "C" 
{
	uint64_t EBFileSystem_GetAvailableBlocks(const char *path) 
    {
		NSDictionary *dict = [[NSFileManager defaultManager] fileSystemAttributesAtPath:[NSString stringWithUTF8String:path]];
		NSNumber *free = [dict valueForKey:@"NSFileSystemFreeSize"];
		return [free unsignedLongLongValue];
	}  	
}



