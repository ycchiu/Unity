
#import "EBKeyboard.h"
#import "UnityAppController.h"

@implementation EBKeyboard

@synthesize GameObjectName = mGameObjectName;
@synthesize OnKeyboardSendClickMethodName = mOnKeyboardSendClickMethodName;
@synthesize OnKeyboardSendTextChangeMethodName = mOnKeyboardSendTextChangeMethodName;
@synthesize OnKeyboardHiddenMethodName = mOnKeyboardHiddenMethodName;

//--------------------------------------------------------------------------------------------------------------------------------
-(id)init
{
    if (self = [super init])
    {
        mView = [[UIView alloc] init];
        
        UIWindow* curWindow = [[UIApplication sharedApplication] keyWindow];
        UIView* mainView = [[curWindow subviews] objectAtIndex:0];
        [mainView addSubview:mView];
        
        //Add TextField********************************************************************
        mTextField = [[UITextField alloc] initWithFrame:CGRectMake(0, 0, 30, 29)];
        mTextField.delegate = self;
        mTextField.keyboardType = UIKeyboardTypeDecimalPad;
        mTextField.autocapitalizationType = UITextAutocapitalizationTypeNone;
        mTextField.autocorrectionType = UITextAutocorrectionTypeNo;
        mTextField.placeholder = @"";
        mTextField.backgroundColor = [UIColor whiteColor];
        mTextField.textColor = [UIColor blackColor];
        mTextField.returnKeyType = UIReturnKeySend;
        mTextField.tag = EB_KEYBOARD_TAG;
        mTextField.text = @"";
        
        [mTextField addTarget:self action:@selector(textFieldDidChange:) forControlEvents:UIControlEventEditingChanged];
        
        [mView addSubview:mTextField];
        
        mTextField.hidden = true;
        [mTextField release];
        
        // Arbitrary maximum length until assigned by caller.
        mMaxStringLength = 20;
        
        //Add Keyboard Listener ************************************************************
        //IOS 5.0 or later
        if( [ [[UIDevice currentDevice] systemVersion] compare: @"5.0" options: NSNumericSearch ] != NSOrderedAscending )
        {
            [[NSNotificationCenter defaultCenter] addObserver:self
                                                     selector:@selector(keyboardChangeNotification:)
                                                         name:UIKeyboardDidChangeFrameNotification
                                                       object:nil];
        }
        //IOS Older than 5.0
        else
        {
            [[NSNotificationCenter defaultCenter] addObserver:self
                                                     selector:@selector(keyboardWasShown:)
                                                         name:UIKeyboardWillShowNotification
                                                       object:nil];
            
            [[NSNotificationCenter defaultCenter] addObserver:self
                                                     selector:@selector(keyboardWasHidden:)
                                                         name:UIKeyboardWillHideNotification
                                                       object:nil];
        }
    }
    return self;
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)initializeKeyboard:(const char*)gameObjectName
  onSendClickedMethodName:(const char*)onSendClickedMethodName
onSendTextChangeMethodName:(const char*)onSendTextChangeMethodName
onKeyboardHiddenMethodName:(const char*)onKeyboardHiddenMethodName
             keyboardType:(int)keyboardType
            returnKeyType:(int)returnKeyType
          maxStringLength:(int)maxStringLength
{
    [self setGameObjectName:[NSString stringWithUTF8String:gameObjectName]];
    [self setOnKeyboardSendClickMethodName:[NSString stringWithUTF8String:onSendClickedMethodName]];
    [self setOnKeyboardSendTextChangeMethodName:[NSString stringWithUTF8String:onSendTextChangeMethodName]];
    [self setOnKeyboardHiddenMethodName:[NSString stringWithUTF8String:onKeyboardHiddenMethodName]];
    
    mTextField.keyboardType = (UIKeyboardType)keyboardType;
    mTextField.returnKeyType = (UIReturnKeyType)returnKeyType;
    
    mMaxStringLength = maxStringLength;
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)displayKeyboard
{
    [mTextField becomeFirstResponder];
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)hideKeyboard
{
    [mTextField resignFirstResponder];
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)keyboardChangeNotification:(NSNotification*)aNotification
{
    CGRect curKeyboardRect = [[[aNotification userInfo] objectForKey:UIKeyboardFrameEndUserInfoKey] CGRectValue];
    CGRect screenRect = [[UIScreen mainScreen] bounds];
    
    if(CGRectIntersectsRect(curKeyboardRect, screenRect))
    {
        [self keyboardWasShown:aNotification];
    }
    else
    {
        [self keyboardWasHidden:aNotification];
    }
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)keyboardWasShown:(NSNotification*)aNotification
{
    NSDictionary* info = [aNotification userInfo];
    mKeyboardArea = [[info objectForKey:UIKeyboardFrameEndUserInfoKey] CGRectValue];
    
    // get window height
    UIWindow* curWindow = [[UIApplication sharedApplication] keyWindow];
    float windowHeight = curWindow.bounds.size.height;
    
    // calculate textfieldWidth
    float textFieldWidth = curWindow.bounds.size.width;
    
    
    UIInterfaceOrientation curOrientation = [[UIApplication sharedApplication] statusBarOrientation];
    if (curOrientation == UIInterfaceOrientationLandscapeLeft || curOrientation == UIInterfaceOrientationLandscapeRight)
    {
        [self swapKeyboardArea];
        
        windowHeight = curWindow.bounds.size.width;
        textFieldWidth = curWindow.bounds.size.height;
    }
    
    mTextField.frame = CGRectMake(mTextField.frame.origin.x,
                                  windowHeight - mKeyboardArea.size.height - mKeyboardArea.origin.y - mTextField.frame.size.height,
                                  textFieldWidth,
                                  mTextField.frame.size.height);
    
    mTextField.hidden = true;
    //mTextField.hidden = false;
    [mTextField setNeedsDisplay];
}

//---------------------------------------------------------------------------------------------------------------------------------
-(void)swapKeyboardArea
{
    //Swap keyboardArea Width, Height
    float tmp  = mKeyboardArea.size.width;
    mKeyboardArea.size.width = mKeyboardArea.size.height;
    mKeyboardArea.size.height = tmp;
    
    //Swap keyboardArea X,Y
//    tmp  = mKeyboardArea.origin.x;
//    mKeyboardArea.origin.x = mKeyboardArea.origin.y;
//    mKeyboardArea.origin.y = tmp;
}

//--------------------------------------------------------------------------------------------------------------------------------
-(BOOL)textField:(UITextField *) textField shouldChangeCharactersInRange:(NSRange)range replacementString:(NSString *)string
{
    NSUInteger oldLength = [textField.text length];
    NSUInteger replacementLength = [string length];
    NSUInteger rangeLength = range.length;
    
    NSUInteger newLength = oldLength - rangeLength + replacementLength;
    
    BOOL returnKey = [string rangeOfString: @"\n"].location != NSNotFound;
    
    BOOL result = newLength <= mMaxStringLength || returnKey;
    
    return result;
}

//--------------------------------------------------------------------------------------------------------------------------------
// the method to call on a change
- (void)textFieldDidChange:(NSNotification*)aNotification
{
    UnitySendMessage([[self GameObjectName] UTF8String],
                     [[self OnKeyboardSendTextChangeMethodName] UTF8String],
                     [mTextField.text UTF8String]);
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)keyboardWasHidden:(NSNotification*)aNotification
{
    if([[[aNotification userInfo] valueForKey:@"UIKeyboardFrameChangedByUserInteraction"] intValue] == 1)
    {
        [self keyboardWasShown:aNotification];
        return;
    }
    
    mTextField.hidden = true;
    UnitySendMessage([[self GameObjectName] UTF8String],
                     [[self OnKeyboardHiddenMethodName] UTF8String],
                     "");
}

//--------------------------------------------------------------------------------------------------------------------------------
-(BOOL)textFieldShouldReturn:(UITextField*)textField
{
    if(textField.tag == EB_KEYBOARD_TAG)
    {
        UnitySendMessage([[self GameObjectName] UTF8String],
                         [[self OnKeyboardSendClickMethodName] UTF8String],
                         [textField.text UTF8String]);
        
        textField.text = @"";
    }
    return false;
}

//--------------------------------------------------------------------------------------------------------------------------------
-(CGRect)getKeyboardArea
{
    return mKeyboardArea;
}

//--------------------------------------------------------------------------------------------------------------------------------
-(void)dealloc
{
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    
    [mView removeFromSuperview];
    [mView release];
    [mTextField release];
    
    [super dealloc];
}
@end

//--------------------------------------------------------------------------------------------------------------------------------

//*****************EXTERNAL METHODS START HERE**********************

static EBKeyboard* ebKeyboard = nil;
extern "C"
{
    //--------------------------------------------------------------------------------------------------------
    void displayEBKeyboard()
    {
        
        if (ebKeyboard == nil)
			ebKeyboard = [[EBKeyboard alloc] init];
        
        [ebKeyboard displayKeyboard];
    }
    
    //--------------------------------------------------------------------------------------------------------
    void hideEBKeyboard()
    {
        
        if (ebKeyboard == nil)
			ebKeyboard = [[EBKeyboard alloc] init];
        
        [ebKeyboard hideKeyboard];
    }
    
    
    //--------------------------------------------------------------------------------------------------------
    void initializeEBKeyboard(const char* gameObjectName, const char* onSendClickedMethodName, const char* onSendTextChangeMethodName, const char* onKeyboardHiddenMethodName, int keyboardType, int returnKeyType, int maxStringLength)
    {
        if (ebKeyboard == nil)
			ebKeyboard = [[EBKeyboard alloc] init];
        
        [ebKeyboard initializeKeyboard:gameObjectName
               onSendClickedMethodName:onSendClickedMethodName
            onSendTextChangeMethodName:onSendTextChangeMethodName
            onKeyboardHiddenMethodName:onKeyboardHiddenMethodName
                          keyboardType:keyboardType
                         returnKeyType:returnKeyType
                       maxStringLength:maxStringLength];
        
    }
    
    //--------------------------------------------------------------------------------------------------------
    void getEBKeyboardArea(CGRect* area)
    {
        if (ebKeyboard == nil)
			ebKeyboard = [[EBKeyboard alloc] init];
        
        *area = [ebKeyboard getKeyboardArea];
    }
    
}
//*******************************************************************

