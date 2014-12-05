//
//  ConnectViewController.m
//  Unity-iPhone
//
//  Created by Jeff Howell on 2012-11-30.
//
//

#import <Foundation/NSJSONSerialization.h>
#import "ConnectViewController.h"
#import "CustomBadge.h"
#import "UIButton+Property.h"

ConnectViewController* controller = nil;

UIViewController* UnityGetGLViewController();

@interface ConnectViewController ()

@property (retain, nonatomic) UIButton *backButton;
@property (retain, nonatomic) NSMutableArray *badgeCount;
@property (retain, nonatomic) NSMutableArray *buttons;


@end

@implementation ConnectViewController

@synthesize backButton;
@synthesize badgeCount;

@synthesize buttons;

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    return interfaceOrientation==UIInterfaceOrientationLandscapeRight || interfaceOrientation == UIInterfaceOrientationLandscapeLeft;
}

- (BOOL)shouldAutorotate
{
    return YES;
}

- (UIInterfaceOrientation)preferredInterfaceOrientationForPresentation {
    return [UIApplication sharedApplication].statusBarOrientation;
}

- (NSUInteger)supportedInterfaceOrientations {
    return UIInterfaceOrientationMaskLandscape;
}

- (id) initConnect:(NSString*)info
{
    self = [super initWithNibName:@"ConnectViewController" bundle:nil];
    if (self) {
        NSError* error = nil;
        NSData *theData = [info dataUsingEncoding:NSUTF8StringEncoding];
        self.pageInfo = [NSJSONSerialization JSONObjectWithData:theData options:0 error:&error];
                
        NSArray* tabs = [self.pageInfo objectForKey:@"tabs"];
        
        self.buttons = [[NSMutableArray alloc] initWithCapacity:tabs.count];
        
        self.badgeCount = [[NSMutableArray alloc] initWithCapacity:tabs.count];
        for ( int i = 0; i < (int)tabs.count; ++i ) {
            [self.badgeCount addObject:[NSNull null]];
        }
    
        self.webView.scalesPageToFit = YES;
        
        CGRect screenRect = [[UIScreen mainScreen] bounds];
                
        self.view.frame = CGRectMake(0, 0, screenRect.size.width, screenRect.size.height);
                
        UIViewController* unity = UnityGetGLViewController();
        
        if([[[UIDevice currentDevice] systemVersion] intValue] >= 5)
        {
            [unity presentViewController:self animated:YES completion:^(){
                UnitySendMessage("WebViewCallbacks", "WebViewDidShow", "");
            }];
        }
        else
        {
            // the old way
            [unity presentModalViewController:self animated:YES];
            UnitySendMessage("WebViewCallbacks", "WebViewDidShow", "");
        }
    
        
    }
    return self;
}

- (void)webViewDidStartLoad:(UIWebView *)webView
{
    NSLog(@"did start loading");
    if (self.activity) {
        [self.activity setHidden:NO];
    }
    
    if (self.webView) {
        [self.webView setHidden:YES];
    }
}

- (void)webViewDidFinishLoad:(UIWebView *)webView
{
    NSLog(@"did finish loading");
    if (self.activity) {
        [self.activity setHidden:YES];
    }
    
    if (self.webView) {
        [self.webView setHidden:NO];
    }
}

+(void)setBackgroundImage:(UIImage*)img andColor:(UIColor*)color forToolbar:(UIToolbar*)toolbar {
    
    if (img)
    {
        UIColor* tile= [UIColor colorWithPatternImage:img];
        
        UIImageView *iv = [[UIImageView alloc] init];
        iv.frame = CGRectMake(0, 0, toolbar.frame.size.width, toolbar.frame.size.height);
        iv.backgroundColor = tile;
        iv.autoresizingMask = UIViewAutoresizingFlexibleWidth;
        
        if([[[UIDevice currentDevice] systemVersion] intValue] >= 5)
            [toolbar insertSubview:iv atIndex:1]; // iOS5 atIndex:1
        else
            [toolbar insertSubview:iv atIndex:0]; // iOS4 atIndex:0
    }
    else
    {
        toolbar.backgroundColor = color;
    }
}

- (void)viewDidLoad
{
    [super viewDidLoad];
        
    self.webView.delegate = self;
    
    UIFont *buttonFont = [UIFont fontWithName:@"HelveticaNeue-CondensedBold" size:13];
    if (buttonFont == nil)
    {
        //IOS 4
        buttonFont = [UIFont fontWithName:@"HelveticaNeue-Bold" size:13];
    }
    
    self.backButton = [UIButton buttonWithType:UIButtonTypeCustom];
    NSDictionary *title = [self.pageInfo objectForKey:@"title"];
    NSString *closeImagePath = [title objectForKey:@"close_image"];
    UIImage *closeImage = [self getImageFromURL:closeImagePath];
    [self.backButton setBackgroundImage:closeImage forState:UIControlStateNormal];
    self.backButton.frame = CGRectMake(0, 0, 47, 44);
    [self.backButton addTarget:self action:@selector(onHome:) forControlEvents:UIControlEventTouchUpInside];
    UIBarButtonItem *backButtonItem = [[[UIBarButtonItem alloc] initWithCustomView:self.backButton] autorelease];
    
    
    NSArray *tabs = [self.pageInfo objectForKey:@"tabs"];
    
    
    UIBarButtonItem *fixedSpace = [[UIBarButtonItem alloc] initWithBarButtonSystemItem:UIBarButtonSystemItemFixedSpace target:nil action:nil];
    fixedSpace.width = 10;
    
    NSMutableArray *barButtons = [NSMutableArray array];
    [barButtons addObject:backButtonItem];

    for(NSUInteger i = 0; i < tabs.count; i++) {
    
        UIButton *button = [UIButton buttonWithType:UIButtonTypeCustom];
        button.frame = CGRectMake(0, 0, 65, 44);
        button.titleLabel.font = buttonFont;
        NSString *url = [tabs[i] objectForKey:@"url"];
        [button setProperty:url];
        [button addTarget:self action:@selector(onButtonPress:) forControlEvents:UIControlEventTouchUpInside];
        UIBarButtonItem *barButton = [[[UIBarButtonItem alloc] initWithCustomView:button] autorelease];
        [buttons addObject:button];
        [barButtons addObject:barButton];
        if(i != tabs.count)
        {
            [barButtons addObject:fixedSpace];
        }
    }
    self.toolBar.items = [NSArray arrayWithArray:barButtons];
    
    NSString *bgImagePath = [title objectForKey:@"background_image"];
    UIImage *bgImage = [self getImageFromURL:bgImagePath];
    [ConnectViewController setBackgroundImage:bgImage andColor:[UIColor blackColor] forToolbar:self.toolBar];
}

- (BOOL)webView:(UIWebView *)webView shouldStartLoadWithRequest:(NSURLRequest *)request navigationType:(UIWebViewNavigationType)navigationType
{
    NSURL* url    = request.mainDocumentURL;
    NSString* scheme = url.scheme;
    if ( [scheme hasPrefix:@"client"] )
    {
        NSMutableDictionary *queryStringDictionary = [[NSMutableDictionary alloc] init];
        NSArray *urlComponents = [url.query componentsSeparatedByString:@"&"];
        
        for (NSString *keyValuePair in urlComponents)
        {
            NSArray *pairComponents = [keyValuePair componentsSeparatedByString:@"="];
            NSString *key = [pairComponents objectAtIndex:0];
            NSString *value = [pairComponents objectAtIndex:1];
            [queryStringDictionary setObject:value forKey:key];
        }
        
        if ( [url.path hasPrefix:@"/badge"] )
        {
            NSString* idStr = [queryStringDictionary objectForKey:@"page"];
            NSString* value = [queryStringDictionary objectForKey:@"value"];
             
            if (idStr != nil)
            {
                if ( value == nil || [value isEqual:@"0"] ) {
                    value = @"";
                }
                
                NSInteger page = [idStr integerValue];
                [self setBadge:page string:value];
            }
        }
        else if ( [url.path hasPrefix:@"/close"] )
        {
            [self onHome:nil];
        }
        
        NSLog(@"Skipping client request %@", url);
        return NO;
    }

    return YES;
}

-(void)setBadge:(NSInteger)page string:(NSString*)string;
{
    id previous = [self.badgeCount objectAtIndex:(int)page];
    [self.badgeCount replaceObjectAtIndex:(int)page withObject:[NSNull null]];
    if (previous != nil && previous != [NSNull null]) {
        [previous removeFromSuperview];
        [previous release];
    }
    
    if ( string.length > 0 )
    {
        UIView* badge = [ConnectViewController badgeWithString:string];
        [self.badgeCount replaceObjectAtIndex:(int)page withObject:badge];
        for(UIButton* b in buttons)
        {
            if(b.tag == page)
            {
                [b addSubview:badge];
            }
        }
    }
}

+(CustomBadge *)badgeWithString:(NSString *)string {
    CustomBadge *badge = [CustomBadge customBadgeWithString:string];
    CGRect badgeFrame = CGRectMake((-badge.frame.size.width) + 5, 0.0f, badge.frame.size.width, badge.frame.size.height);
    [badge setFrame:badgeFrame];
    return badge;
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
}

- (IBAction)onHome:(id)sender {
    UnitySendMessage("WebViewCallbacks", "WebViewWillHide", "");
    if([[[UIDevice currentDevice] systemVersion] intValue] >= 5)
    {
        [self dismissViewControllerAnimated:true completion:nil];
    }
    else
    {
        [self dismissModalViewControllerAnimated:YES];
    }
}

- (IBAction)onButtonPress:(id)sender {
    UIButton* button = (UIButton*)sender;
    [self loadPage:(NSString*)button.property];
}

- (void)dealloc {
    NSLog(@"dealloc connect");
    controller = nil;
    [_webView release];
    [_activity release];
    [self.backButton release];
    [_toolBar release];
    [super dealloc];
}
- (void)viewDidUnload {
    NSLog(@"viewDidUnload connect");
    [self setWebView:nil];
    [self setActivity:nil];
    self.backButton = nil;
    [self setToolBar:nil];
    [super viewDidUnload];
}

- (void)loadUrl:(NSString*) url2
{
    if (self.activity) {
        [self.activity setHidden:NO];
    }
    
    NSURL* nsUrl = [NSURL URLWithString:url2];
    NSURLRequest * myRequest = [NSURLRequest requestWithURL:nsUrl];
    [self.webView loadRequest:myRequest];
}

- (UIImage *)getImageFromURL:(NSString*)imagePath
{
    UIImage *image = nil;
    if( [ imagePath hasPrefix:@"http://"] || [ imagePath hasPrefix:@"https://"] || [ imagePath hasPrefix:@"file://"] )
    {
        NSURL *fileUrl = [NSURL URLWithString:imagePath];
        NSData *fileData = [NSData dataWithContentsOfURL:fileUrl];
        image = [UIImage imageWithData:fileData];
    }
    else
    {
        image = [UIImage imageNamed:imagePath];
    }
    return image;
}

-(void)updateTabBar:(NSString*)url
{
    NSArray *tabs = [self.pageInfo objectForKey:@"tabs"];
    for(NSUInteger i = 0; i < tabs.count; i++) {
        NSString *tabUrl = (NSString *)[tabs[i] objectForKey:@"url"];
        NSString *imagePath = [tabUrl isEqualToString:url] ? (NSString *)[tabs[i] objectForKey:@"selected_image"] : (NSString *)[tabs[i] objectForKey:@"unselected_image"];
        
        UIImage* image = [self getImageFromURL:imagePath];
        if( image != nil )
        {
            UIButton* b = buttons[ i ];
            [b setImage:image forState:UIControlStateNormal];
        }
    }
}

-(void)loadPage:(NSString*)url
{
    [self updateTabBar:url];
    [self loadUrl:url];
}



@end

extern "C"
{    
    void _ConnectShow( const char* initialUrl, const char* pageInfo )
    {
        NSString* initialUrlStr = [NSString stringWithUTF8String:initialUrl];
        NSString* info = [NSString stringWithUTF8String:pageInfo];
        if (!controller)
        {
            controller = [[ConnectViewController alloc] initConnect:info];
            [controller release];
        }
        [controller loadPage:initialUrlStr];
    }
    
}
