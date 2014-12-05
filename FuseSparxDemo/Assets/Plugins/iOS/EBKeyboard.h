#import <Foundation/Foundation.h>


#define EB_KEYBOARD_TAG 1000000000

@interface EBKeyboard : NSObject <UITextFieldDelegate>
{

    //Put member variables here
    UIView *mView;
    UITextField* mTextField;
    
    NSInteger mMaxStringLength;
    
    NSString* mGameObjectName;
    NSString* mOnKeyboardSendClickMethodName;
    NSString* mOnKeyboardSendTextChangeMethodName;
    NSString* mOnKeyboardHiddenMethodName;
    
    CGRect mKeyboardArea;
}


@property(nonatomic, copy) NSString* GameObjectName;
@property(nonatomic, copy) NSString* OnKeyboardSendClickMethodName;
@property(nonatomic, copy) NSString* OnKeyboardSendTextChangeMethodName;
@property(nonatomic, copy) NSString* OnKeyboardHiddenMethodName;


-(void)displayKeyboard;
-(void)hideKeyboard;
-(CGRect)getKeyboardArea;
-(void)initializeKeyboard:(const char*)gameObjectName
                            onSendClickedMethodName:(const char*)onSendClickedMethodName
                            onSendTextChangeMethodName:(const char*)onSendTextChangeMethodName
                            onKeyboardHiddenMethodName:(const char*)onKeyboardHiddenMethodName
                            keyboardType:(int)keyboardType
                            returnKeyType:(int)returnKeyType
                            maxStringLength:(int)maxStringLength;


@end

