//
//  EBWebViewController.h
//  Unity-iPhone
//
//  Created by Jeff Howell on 2012-11-23.
//
//

#import <UIKit/UIKit.h>

@interface EBWebViewController : UIViewController<UIWebViewDelegate, UITabBarDelegate>

@property (retain, nonatomic) NSString* baseUrl;

@property (retain, nonatomic) IBOutlet UIWebView *webView;
@property (retain, nonatomic) IBOutlet UIActivityIndicatorView *activity;
@property (retain, nonatomic) IBOutlet UINavigationItem *titleNavItem;
@property (retain) NSString* url;

@end
