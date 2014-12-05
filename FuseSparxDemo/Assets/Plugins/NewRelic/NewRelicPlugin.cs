#if UNITY_ANDROID && !UNITY_EDITOR
#define NEWRELIC_ANDROID
#endif

#if UNITY_IPHONE && !UNITY_EDITOR
#define NEWRELIC_IPHONE
using System.Runtime.InteropServices;
#endif


using UnityEngine;
using System.Collections;

public static class NewRelicPlugin
{
	static bool _initialized = false;
	
#if NEWRELIC_IPHONE
	[DllImport("__Internal")]
	static extern void _NewRelic_Initialize(string apiKey);

	[DllImport("__Internal")]
	static extern System.IntPtr _NewRelic_CreateTimer();

	[DllImport("__Internal")]
	static extern void _NewRelic_StopTimer(System.IntPtr timer);

	[DllImport("__Internal")]
	static extern void _NewRelic_DisposeTimer(System.IntPtr timer);

	[DllImport("__Internal")]
	static extern void _NewRelic_NotifyHttpRequest(string url, int statusCode, System.IntPtr timer, int bytesSent, int bytesReceived,string response);
#endif

	static System.DateTime _epoch = new System.DateTime(1970,1,1);

	static long ToPosix( System.DateTime dt )
	{
		return (long)(dt - _epoch).TotalMilliseconds;
	}

	public class Timer : System.IDisposable
	{
		#region IDisposable implementation

		public void Dispose ()
		{
#if NEWRELIC_IPHONE
			_NewRelic_DisposeTimer(handle);
			handle = System.IntPtr.Zero;
#endif
		}

		#endregion

#if NEWRELIC_ANDROID
		public System.DateTime start {get; private set;}
		public System.DateTime end {get; private set;}
#endif

#if NEWRELIC_IPHONE
		public System.IntPtr handle {get;private set;}
#endif

		bool _running = false;

		public Timer()
		{
			Start();
		}

		void Start()
		{
			if (_running)
			{
				return;
			}
			_running = true;
#if NEWRELIC_IPHONE
			handle = _NewRelic_CreateTimer();
#endif

#if NEWRELIC_ANDROID
			start = System.DateTime.UtcNow;
#endif
		}

		public void Stop()
		{
			if (!_running)
			{
				return;
			}
			_running = false;

#if NEWRELIC_IPHONE
			_NewRelic_StopTimer(handle);
#endif

#if NEWRELIC_ANDROID
			end = System.DateTime.UtcNow;
#endif
		}

	}

	public static void Init( string apiKey )
	{
		if ( _initialized || string.IsNullOrEmpty(apiKey) )
		{
			return;
		}

		EB.Debug.Log("Initializing new relic");
	
#if NEWRELIC_ANDROID
		try
		{
			using( AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer") )
			{
				using ( AndroidJavaObject playerActivityContext = actClass.GetStatic<AndroidJavaObject>("currentActivity") )
				{
					using (var newRelicClass = new AndroidJavaClass("com.newrelic.agent.android.NewRelic") )
					{
						using (var agent = new AndroidJavaClass("com.newrelic.agent.android.AndroidAgentImpl"))
						{
							agent.CallStatic("init", playerActivityContext, apiKey, "mobile-collector.newrelic.com", true, true, null);
							using ( var newRelicInstance = newRelicClass.CallStatic<AndroidJavaObject>("withApplicationToken", apiKey) )
							{
								newRelicInstance.Call("start", playerActivityContext);
							}

						}

					}

				}

			}

			_initialized = true;
		}
		catch (System.Exception ex)
		{
			Debug.LogError("Failed to initialize New Relic: " + ex);
		}
#endif

#if NEWRELIC_IPHONE
		_NewRelic_Initialize(apiKey);
		_initialized = true;
#endif
	}

	public static Timer CreateTimer()
	{
		return new Timer();
	}

	public static void LogHttpFailure(string url, Timer timer, EB.Net.NetworkFailure failure )
	{
		if (_initialized && timer != null)
		{
			timer.Stop();
			
			EB.Debug.Log("LogHttpFailure {0}, {1}", url, failure);
			
#if NEWRELIC_ANDROID
			try
			{
				var exceptionClassName = "java.lang.Exception";
				switch(failure)
				{
				case EB.Net.NetworkFailure.DNSLookupFailed:
					exceptionClassName = "java.net.UnknownHostException"; break;
				case EB.Net.NetworkFailure.TimedOut:
					exceptionClassName = "java.net.SocketTimeoutException"; break;
				case EB.Net.NetworkFailure.BadUrl:
					exceptionClassName = "java.net.MalformedURLException"; break;
				case EB.Net.NetworkFailure.SecureConnectionFailed:
					exceptionClassName = "javax.net.ssl.SSLException"; break;
				case EB.Net.NetworkFailure.BadServerResponse:
					exceptionClassName = "jorg.apache.http.client.HttpResponseException"; break;
				case EB.Net.NetworkFailure.CannotConnectToHost:
					exceptionClassName = "java.net.ConnectException"; break;
				default:
					break;
				}

				long startTime = ToPosix(timer.start);
				long endTime = ToPosix(timer.end);
				
				using( var exception = new AndroidJavaObject(exceptionClassName) )
				{
					using (var newRelicClass = new AndroidJavaClass("com.newrelic.agent.android.NewRelic") )
					{
						newRelicClass.CallStatic("noticeNetworkFailure", url, startTime, endTime, exception);
					}
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError("Failed to log http failure: " + ex);
			}
#endif
			
#if NEWRELIC_IPHONE

#endif
		}

	}

	public static void LogHttpRequest(string url, int statusCode, Timer timer, long bytesSent, long bytesReceived, string responseBody)
	{
		if (_initialized && timer != null)
		{
			timer.Stop();
			
			//EB.Debug.Log("LogHttpRequest {0}, {1}, {2}, {3}, {4}", url, statusCode, bytesSent, bytesReceived, responseBody);
			
			#if NEWRELIC_ANDROID
			try
			{
				long startTime = ToPosix(timer.start);
				long endTime = ToPosix(timer.end);
				using (var newRelicClass = new AndroidJavaClass("com.newrelic.agent.android.NewRelic") )
				{
					newRelicClass.CallStatic("noticeHttpTransaction", url, statusCode, startTime, endTime, bytesSent, bytesReceived, responseBody);
				}

			}
			catch (System.Exception ex)
			{
				Debug.LogError("Failed to log http request: " + ex);
			}
			#endif
			
			#if NEWRELIC_IPHONE
			_NewRelic_NotifyHttpRequest(url, statusCode, timer.handle, (int)bytesSent, (int)bytesReceived, responseBody);
			#endif
		}
	}

}
