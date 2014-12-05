//
//  ConnectViewController.h
//  Unity-iPhone
//
//  Created by Jeff Howell on 2012-11-30.
//
//

#import <UIKit/UIKit.h>

@interface ConnectViewController : UIViewController<UIWebViewDelegate>

@property (retain, nonatomic) NSDictionary* pageInfo;

@property (retain, nonatomic) IBOutlet UIWebView *webView;
@property (retain, nonatomic) IBOutlet UIActivityIndicatorView *activity;
@property (retain, nonatomic) IBOutlet UIToolbar *toolBar;

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation;

@end
