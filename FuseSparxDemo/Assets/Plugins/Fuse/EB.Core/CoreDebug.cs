#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
#define ENABLE_LOGGING
#define ENABLE_REMOTE_LOGGING
#else
#define ENABLE_REMOTE_LOGGING
#endif
using UnityEngine;
using System.Collections;

namespace EB
{
    public static class Debug
    {
#if ENABLE_REMOTE_LOGGING
		private static Collections.CircularBuffer _buffer = new Collections.CircularBuffer(256);
#endif
		
		class FormatProvider : System.IFormatProvider, System.ICustomFormatter	
		{
			#region IFormatProvider implementation
			public object GetFormat (System.Type formatType)
			{
				return this;
			}
			#endregion
			
			#region ICustomFormatter implementation
			public string Format (string format, object arg, System.IFormatProvider formatProvider)
			{
				var result = string.Empty;
				if ( arg != null )
				{
					result = arg.ToString();
					if ( arg is ICollection )
					{
						result = JSON.Stringify(arg) ?? result;
					}
				}
				else
				{
					result = "null";
				}			
				return result;
			}
			#endregion
		}
		private static FormatProvider _provider = new FormatProvider();
		
        public static string Format( object message, params object[] args)
        {
			if (args.Length == 0)
			{
				return message.ToString();
			}
			
        	return string.Format(_provider, message.ToString(), args);
        }
		
		public static void LogIf( bool condition, object message, params object[] args )
		{
			if ( condition )
			{
				Log(message,args);
			}
		}
		
        public static void Log( object message, params object[] args )
        {
#if ENABLE_LOGGING
            try { UnityEngine.Debug.Log(Format(message, args)); } catch {}
#endif
			
#if ENABLE_REMOTE_LOGGING
			lock(_buffer)
			{
				_buffer.Push( System.DateTime.UtcNow.ToString() + " I:" + Format(message,args) ); 
			}
#endif
        }

        public static void LogWarning(object message, params object[] args)
        {
#if ENABLE_LOGGING
             try { UnityEngine.Debug.LogWarning(Format(message, args)); } catch {}
#endif
			
#if ENABLE_REMOTE_LOGGING
			lock(_buffer)
			{
				_buffer.Push( System.DateTime.UtcNow.ToString()  + " W:" + Format(message,args) ); 
			}
#endif			
		}

        public static void LogError(object message, params object[] args)
        {
#if ENABLE_LOGGING
             try { UnityEngine.Debug.LogError(Format(message, args)); } catch {}
#endif
			
#if ENABLE_REMOTE_LOGGING
			lock (_buffer)
			{
				_buffer.Push( System.DateTime.UtcNow.ToString() + " E:" + Format(message,args) ); 
			}
#endif				
        }
		
		public static void Dump( Hashtable table )
		{
#if ENABLE_REMOTE_LOGGING
			ArrayList list = new ArrayList();
			lock(_buffer)
			{
				foreach( string s in _buffer )
				{
					list.Add(s);
				}
			}
			table["log"] = list;
#endif
		}

    }

}

