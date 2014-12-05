#include <string.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/sysctl.h>
#include <net/if.h>
#include <net/if_dl.h>
#import "AdSupport/ASIdentifierManager.h"
#import "OpenUDID.h"

extern "C"
{
    const char* strdup_s(const char* src)
    {
        if (src == NULL )
        {
            src = "";
        }
        return strdup(src);
    }
    
    const char* _GetBundleVersion()
    {
        NSString* version   = [[NSBundle mainBundle] objectForInfoDictionaryKey:(NSString *)kCFBundleVersionKey];
        const char* cstr    = [version UTF8String];
        return strdup_s(cstr);
    }
    
    const char* _GetPreferredLanguageCode()
    {
        NSString *code = [[NSLocale preferredLanguages] objectAtIndex:0];
        const char* cstr    = [code UTF8String];
        return strdup_s(cstr);
    }
    
    const char* _GetLanguageCode()
    {
        NSLocale* locale = [NSLocale currentLocale];
        NSString* code = [locale objectForKey:NSLocaleLanguageCode];
        const char* cstr    = [code UTF8String];
        return strdup_s(cstr);
    }
    
    const char* _GetCountryCode()
    {
        NSLocale* locale = [NSLocale currentLocale];
        NSString* code = [locale objectForKey:NSLocaleCountryCode];
        const char* cstr    =  code != nil ? [code UTF8String] : "";
        return strdup_s(cstr);
    }
    
    const char* _GetOpenUDID()
    {
        NSString* openUDID = [OpenUDID value];
        const char* cstr    = [openUDID UTF8String];
        return strdup_s(cstr);
    }
    
    const char* _GetUDID()
    {
        return _GetOpenUDID();
    }
    
    const char* _GetModel()
    {
        size_t size;
        sysctlbyname("hw.machine", NULL, &size, NULL, 0);
        char *machine = new char[size+1];
        memset(machine, 0, size+1);
        sysctlbyname("hw.machine", machine, &size, NULL, 0);
        return machine;
    }
    
    const char* _GetIFA()
    {
        NSString* ifa = @"";
        id klass = NSClassFromString(@"ASIdentifierManager");
        if (klass){
            ifa = [ [[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
        }
        const char* cstr= [ifa UTF8String];
        return strdup_s(cstr);
    }
    
    const char* _GetMACAddress()
    {
    	int					mib[6];
        size_t				len;
        char				*buf;
        unsigned char		*ptr;
        struct if_msghdr	*ifm;
        struct sockaddr_dl	*sdl;
        
        mib[0] = CTL_NET;
        mib[1] = AF_ROUTE;
        mib[2] = 0;
        mib[3] = AF_LINK;
        mib[4] = NET_RT_IFLIST;
        
        if ((mib[5] = if_nametoindex("en0")) == 0)
        {
            printf("Error: if_nametoindex error\n");
            return NULL;
        }
        
        if (sysctl(mib, 6, NULL, &len, NULL, 0) < 0)
        {
            printf("Error: sysctl, take 1\n");
            return NULL;
        }
        
        if ((buf = (char*)malloc(len)) == NULL)
        {
            printf("Could not allocate memory. error!\n");
            return NULL;
        }
        
        if (sysctl(mib, 6, buf, &len, NULL, 0) < 0)
        {
            printf("Error: sysctl, take 2");
            return NULL;
        }
        
        ifm = (struct if_msghdr *)buf;
        sdl = (struct sockaddr_dl *)(ifm + 1);
        ptr = (unsigned char *)LLADDR(sdl);
        // NSString *outstring = [NSString stringWithFormat:@"%02x:%02x:%02x:%02x:%02x:%02x", *ptr, *(ptr+1), *(ptr+2), *(ptr+3), *(ptr+4), *(ptr+5)];
        NSString *outstring = [NSString stringWithFormat:@"%02x%02x%02x%02x%02x%02x", *ptr, *(ptr+1), *(ptr+2), *(ptr+3), *(ptr+4), *(ptr+5)];
        free(buf);
        outstring = [outstring uppercaseString];
        
        const char* cstr    = [outstring UTF8String];
        return strdup_s(cstr);
    }
    
}


