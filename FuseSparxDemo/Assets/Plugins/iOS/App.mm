#import "UnityAppController.h"
#import "Mono.h"
#include <sys/types.h>
#include <sys/sysctl.h>

void UnityPause(bool);

static MonoImage *image = NULL;

MonoImage* GetMonoImage()
{
    if( image == NULL )
    {
        
        MonoDomain *domain = mono_domain_get();
        NSString *assemblyPath = [[[NSBundle mainBundle] bundlePath]
                                  
                                  stringByAppendingPathComponent:@"Data/Managed/Assembly-CSharp-firstpass.dll"];
        
        MonoAssembly *assembly = mono_domain_assembly_open(domain, [assemblyPath UTF8String] );
        image = mono_assembly_get_image(assembly);
    }
    return image;
}

bool _didPause = false;
bool _pauseOnResign = true;

/*


@interface UnityAppController (Rivets)
@end


@implementation UnityAppController (Rivets)


- (void)applicationDidEnterBackground:(UIApplication *)application
{
    // let sparx know that we have entered the background
    MonoImage* image = GetMonoImage();
    if (image)
    {
        MonoMethodDesc *desc = mono_method_desc_new("EB.Sparx.Hub:OnEnteredBackgroundStatic(int)", FALSE);
        MonoMethod *method = mono_method_desc_search_in_image(desc, image);
        mono_method_desc_free(desc);
        
        if (method)
        {
            int willPause = !_didPause ? 1 : 0;
            void* args[1];
            args[0] = &willPause;
            
            mono_runtime_invoke(method, NULL, args, NULL);
        }
        else
        {
            NSLog(@"failed to find sparx method!!");
        }
    }
    
    if (!_didPause)
    {
        // pause unity
        UnityPause(true);
    }
    _didPause = true;
    printf_console("-> applicationDidEnterBackground EBG()\n");
}

// For iOS 4
// Callback order:
//   applicationWillEnterForeground()
//   applicationDidBecomeActive()
- (void)applicationWillEnterForeground:(UIApplication *)application
{
    printf_console("-> applicationWillEnterForeground EBG(()\n");
    
    MonoImage* image = GetMonoImage();
    if (image)
    {
        MonoMethodDesc *desc = mono_method_desc_new("EB.Sparx.Hub:OnEnteredForegroundStatic()", FALSE);
        MonoMethod *method = mono_method_desc_search_in_image(desc, image);
        mono_method_desc_free(desc);
        
        if (method)
        {
            mono_runtime_invoke(method, NULL, NULL, NULL);
        }
        else
        {
            NSLog(@"failed to find sparx method!!");
        }
    }
    
}

- (void) applicationDidBecomeActive:(UIApplication*)application
{
    // clear application badge
    [UIApplication sharedApplication].applicationIconBadgeNumber= 0;
    
    printf_console("-> applicationDidBecomeActive EBG(()\n");
    if (_didPause)
    {
        UnityPause(false);
    }
    
    _didPause = false;
}

- (void) applicationWillResignActive:(UIApplication*)application
{
    printf_console("-> applicationWillResignActive EBG(()\n");
    if (_pauseOnResign && !_didPause)
    {
        UnityPause(true);
        _didPause = true;
    }
    //
}

@end

 */

extern "C"
{
    void _AppSetIconBadgeNumber(int value)
    {
        [UIApplication sharedApplication].applicationIconBadgeNumber = value;
    }
    
    void _AppSetPauseOnResign(int value)
    {
        _pauseOnResign = value != 0;
    }
    
    
}