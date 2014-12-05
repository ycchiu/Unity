#import "NSTcpClient.h"
#import <CommonCrypto/CommonDigest.h>
#import <Security/SecRandom.h>

static NSMutableArray* pinnedCerts = [ [NSMutableArray alloc] initWithCapacity:8 ];

@implementation NSTcpClient{
    NSInputStream* _inputStream;
    NSOutputStream* _outputStream;
    bool _secure;
    bool _connected;
    bool _error;
    bool _certOk;
    
    bool _inConnected;
    bool _outConnected;
}

-(id)init:(bool)secure
{
    self = [super init];
    if (self)
    {
        _inputStream = nil;
        _outputStream = nil;
        _connected = false;
        _error = false;
        _secure = secure;
        _certOk = false;
        
        _inConnected = false;
        _outConnected= false;
    }
    return self;
}

-(void)dealloc
{
    if (_inputStream != nil && _outputStream != nil)
    {
        // NSLog(@"dealloc %p %d \n", self, [self retainCount]);
        
        _inputStream.delegate = nil;
        _outputStream.delegate = nil;
        
        [_inputStream removeFromRunLoop:[NSRunLoop currentRunLoop] forMode:NSDefaultRunLoopMode];
        [_outputStream removeFromRunLoop:[NSRunLoop currentRunLoop] forMode:NSDefaultRunLoopMode];
        
        [_inputStream close];
        [_outputStream close];
        
        [_inputStream release];
        [_outputStream release];
        
        _inputStream = nil;
        _outputStream = nil;
    }
    [super dealloc];
}

-(void)connect:(NSString *)host port:(NSInteger)port
{
    CFReadStreamRef readStream = NULL;
    CFWriteStreamRef writeStream = NULL;
    CFStreamCreatePairWithSocketToHost(NULL, (CFStringRef)host, port, &readStream, &writeStream);
    
    if (!readStream  || !writeStream )
    {
        NSLog(@"Failed to create streams for host %@", host);
        _connected = false;
        _error = true;
        return;
    }
    
    _outputStream = (NSOutputStream*)(writeStream);
    _inputStream = (NSInputStream*)(readStream);
    
    if (_secure) {
        NSMutableDictionary *SSLOptions = [[NSMutableDictionary alloc] init];
        [SSLOptions setValue:[NSNumber numberWithBool:NO] forKey:(id)kCFStreamSSLValidatesCertificateChain];
        
        [_outputStream setProperty:(id)kCFStreamSocketSecurityLevelNegotiatedSSL forKey:(id)kCFStreamPropertySocketSecurityLevel];
        [_outputStream setProperty:SSLOptions forKey:(id)kCFStreamPropertySSLSettings];
    }
    
    _inputStream.delegate = self;
    _outputStream.delegate = self;
    
    // TODO schedule in a better run loop
    [_outputStream scheduleInRunLoop:[NSRunLoop mainRunLoop] forMode:NSDefaultRunLoopMode];
    [_inputStream scheduleInRunLoop:[NSRunLoop mainRunLoop] forMode:NSDefaultRunLoopMode];
    
    [_outputStream open];
    [_inputStream open];
}

- (void)stream:(NSStream *)theStream handleEvent:(NSStreamEvent)streamEvent
{
    switch (streamEvent) {
        case NSStreamEventOpenCompleted:
        {
            if ( theStream == _inputStream )
            {
                _inConnected = true;
            }
            else if ( theStream == _outputStream)
            {
                _outConnected = true;
            }
            
            if (_inConnected && _outConnected)
            {
                //NSLog(@"NSTcpClient: Connected", _host );
                _connected = true;
            }
        }
            break;
        case NSStreamEventErrorOccurred:
        {
            NSLog(@"NSTcpClient: Connection Error");
            _connected = false;
            _error = true;
        }
            break;
        case NSStreamEventEndEncountered:
        {
            //NSLog(@"NSTcpClient: Conneciton Closed");
            _connected = false;
        }
            break;
        default:
            break;
    }
}

-(int)write:(const uint8_t*)data bytes:(NSInteger)bytes
{
    if (!_connected)
    {
        return -1;
    }
    
    if (_outputStream.streamError != nil)
    {
        NSLog(@"NSTcpClient: Conneciton Write Error %@", _outputStream.streamError);
        return -1;
    }
    
    if (!_outputStream.hasSpaceAvailable)
    {
        //NSLog(@"No space available");
        return 0;
    }
    
    if (![self checkCerts])
    {
        NSLog(@"cert check failed");
        return -1;
    }
    
    int result = [_outputStream write:data maxLength:bytes];
    if (result < 0 )
    {
        NSLog(@"Write Failed!");
    }
    return result;
}

-(int)read:(uint8_t*)buffer maxLength:(NSInteger)maxLength
{
    if (!_connected)
    {
        return -1;
    }
    
    if (_inputStream.streamError != nil)
    {
        NSLog(@"NSTcpClient: Conneciton Read Error %@", _inputStream.streamError);
        return -1;
    }
    
    if (!_inputStream.hasBytesAvailable)
    {
        return 0;
    }
    
    int result = [_inputStream read:buffer maxLength:maxLength];
    if (result < 0)
    {
        NSLog(@"Read Failed!");
    }
    return result;
}

-(bool)dataAvailable
{
    if (!_connected)
    {
        return false;
    }
    
    if (_inputStream)
    {
        return _inputStream.hasBytesAvailable;
    }
    return false;
}

-(bool)connected
{
    return _connected;
}

-(bool)error
{
    return _error;
}

-(bool)checkCerts
{
    if (!_secure || _certOk)
    {
        return true;
    }
    
    if (!_outputStream)
    {
        return false;
    }

    // wait for the stream to be ready
    while(_connected && !_outputStream.hasSpaceAvailable)
    {
        timespec ts = { 0, 100*1000 };
        nanosleep(&ts,NULL);
    }
    
    @autoreleasepool {
        

        SecTrustRef secTrust = (SecTrustRef)[_outputStream propertyForKey:(id)kCFStreamPropertySSLPeerTrust];
        if (secTrust)
        {
            NSInteger numCerts = SecTrustGetCertificateCount(secTrust);
            for (NSInteger i = 0; i < numCerts && !_certOk; i++)
            {
                SecCertificateRef cert  = SecTrustGetCertificateAtIndex(secTrust, i);
                NSData *certData        = CFBridgingRelease(SecCertificateCopyData(cert));
                
                for (id ref in pinnedCerts)
                {
                    SecCertificateRef trustedCert = (SecCertificateRef)ref;
                    NSData *trustedCertData = CFBridgingRelease(SecCertificateCopyData(trustedCert));
                    
                    if ([trustedCertData isEqualToData:certData])
                    {
                        //NSLog(@"Cert IS OK!!!");
                        _certOk = YES;
                        break;
                    }
                }
            }
        }
        else
        {
            NSLog(@"trust is missing!");
        }
        
    }

    return _certOk;
}


@end


extern "C"
{
    void _NSImportCertificate( uint8_t* data, int dataLength )
    {
        NSData* tmpData = [NSData dataWithBytes:data length:dataLength];
        SecCertificateRef ref = SecCertificateCreateWithData(nil, (CFDataRef)tmpData);
        if (ref)
        {
            //NSLog(@"Added Certificate!");
            [pinnedCerts addObject:(id)ref];
        }
        else
        {
            NSLog(@"Failed to add certificate!!!");
        }
    }
    
    typedef NSTcpClient* Client;
    
    void* _NSTcpClientCreate(bool secure)
    {
        Client client = [[NSTcpClient alloc] init:secure];
        [client retain];
        return client;
    }
    
    void _NSTcpClientDestory(Client client)
    {
        if (client)
        {
            //[client dealloc];
            [client release];
            [client release];
        }
    }
    
    void _NSTcpClientConnect( Client client, const char* host, int port )
    {
        NSString* nsHost = [NSString stringWithUTF8String:host];
        [client connect:nsHost port:port];
    }
    
    bool _NSTcpClientDataAvailable( Client client )
    {
        if (client)
        {
            return [client dataAvailable];
        }
        return false;
    }
    
    bool _NSTcpClientConnected( Client client)
    {
        if (client)
        {
            return [client connected];
        }
        return false;
    }
    
    bool _NSTcpClientError( Client client)
    {
        if (client)
        {
            return [client error];
        }
        return false;
    }
    
    int _NSTcpClientWrite( Client client, const uint8_t* buffer, int offset, int count )
    {
        if (client)
        {
            buffer = buffer + offset;
            return [client write:buffer bytes:count];
        }
        return -1;
    }
    
    int _NSTcpClientRead( Client client, uint8_t* buffer, int offset, int count)
    {
        if (client)
        {
            buffer = buffer + offset;
            return [client read:buffer maxLength:count];
        }
        return -1;
    }
    
    void* _NSCreatePool()
    {
        // NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
        //return pool;
        return NULL;
    }
    
    void _NSDestroyPool( NSAutoreleasePool* pool)
    {
        if (pool != nil)
        {
            [pool release];
        }
    }
    
    
}


