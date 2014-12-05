//
//  EBWebViewController.m
//  Unity-iPhone
//
//  Created by Jeff Howell on 2012-11-23.
//
//

#import "EBWebViewController.h"

#import <QuartzCore/QuartzCore.h>

enum ViewType
{
    TournamentStarted = 100,
    TournamentEnded = 101,
    Last = TournamentEnded
};

@interface EBWebViewController ()
{
    CGRect _rect;
}

@end

@implementation EBWebViewController

@synthesize baseUrl;

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
	return interfaceOrientation==UIInterfaceOrientationLandscapeRight || interfaceOrientation == UIInterfaceOrientationLandscapeLeft;
}

- (void) hideGradientBackground:(UIView*)theView
{
    for (UIView * subview in theView.subviews)
    {
        if ([subview isKindOfClass:[UIImageView class]])
            subview.hidden = YES;
        
        if ([[subview class] isSubclassOfClass: [UIScrollView class]])
            ((UIScrollView *)subview).bounces = NO;
        
        [self hideGradientBackground:subview];
    }
}

-(id)initWithBaseURL:(NSString*)baseUrlStr andRect:(CGRect)rect
{
	NSString* nib = @"EBWebView";
    self = [super initWithNibName:nib bundle:nil];
    if (self) {
		self.baseUrl = baseUrlStr;
	
        CGRect screenRect = [[UIScreen mainScreen] bounds];
        self.view.frame = rect;

        //NSLog(@"Rect %f, %f, %f, %f", rect.origin.x, rect.origin.y, rect.size.width, rect.size.height);
        //NSLog(@"SRect %f, %f, %f, %f", screenRect.origin.x, screenRect.origin.y, screenRect.size.width, screenRect.size.height);
        
		self.webView.scalesPageToFit = YES;
        [self hideGradientBackground:self.webView];
        
        UIWindow* window = [[UIApplication sharedApplication] keyWindow];
        UIView* mainView = [[window subviews] objectAtIndex:0];
        [mainView addSubview:self.view];
    }
    return self;
}

- (BOOL)webView:(UIWebView *)webView shouldStartLoadWithRequest:(NSURLRequest *)request navigationType:(UIWebViewNavigationType)navigationType
{
    NSURL* url    = request.mainDocumentURL;
    NSString* scheme = url.scheme;
    if ( [scheme hasPrefix:@"client"] )
    {
        NSString* path = [url relativeString];
        UnitySendMessage("WebViewCallbacks", "OnPopupUrl", [path UTF8String]);
        return NO;
    }
    
    return YES;
}

- (void)webViewDidFinishLoad:(UIWebView *)webView
{
    [webView setHidden:NO];
}

-(void)webViewDidStartLoad:(UIWebView *)webView
{
    [webView setHidden:YES];
}

- (void)viewDidLoad
{
    self.webView.delegate = self;    
    [super viewDidLoad];
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
}

- (void)dealloc {
    [_webView release];
    [super dealloc];
}
- (void)viewDidUnload {
    [self setWebView:nil];
    [super viewDidUnload];
}

- (IBAction)onCloseButton:(id)sender {
    NSLog(@"Close me");
    [self.view removeFromSuperview];
}

- (void)loadUrl:(NSString*) url
{
    NSURL* nsUrl = [NSURL URLWithString:url];
    NSURLRequest * myRequest = [NSURLRequest requestWithURL:nsUrl];
    [self.webView loadRequest:myRequest];
}

-(void)loadPage
{
	NSLog(@"%@", self.baseUrl);
    [self loadUrl:self.baseUrl];
}

@end

extern "C"
{
    EBWebViewController* ctrl = nil;
    
    void _WebViewClose()
    {
        if (ctrl)
        {
            [ctrl.view removeFromSuperview];
            [ctrl release];
            ctrl = nil;
        }
    }
    
    void _WebViewOpenRect( const char* url, float x, float y, float w, float h )
    {
        _WebViewClose();
        NSString* baseurl = [NSString stringWithUTF8String:url];
        
        CGRect screenRect = [[UIScreen mainScreen] bounds];
        float sw = screenRect.size.height;
        float sh = screenRect.size.width;
        CGRect rect = CGRectMake(x*sw, y*sh, w*sw, h*sh);
        
        ctrl = [[EBWebViewController alloc] initWithBaseURL:baseurl andRect:rect];
        [ctrl loadPage];
    }
    
    void _WebViewOpen( const char* url )
    {
        _WebViewClose();
        _WebViewOpenRect(url, 0.0f, 0.0f, 1.0f, 1.0f);
    }
}
