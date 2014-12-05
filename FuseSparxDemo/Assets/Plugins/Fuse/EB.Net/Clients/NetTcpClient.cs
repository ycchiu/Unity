#define USE_NSTCPCLIENT
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Net
{
	public enum NetworkFailure
	{
		None = 0,
		Unknown = -1,
		BadUrl = -1000,
		TimedOut = -1001,
		CannotConnectToHost = -1004, 
		DNSLookupFailed = -1006, 
		BadServerResponse = -1011, 
		SecureConnectionFailed = -1200
	};

	public interface ITcpClient
	{
		NetworkFailure 		Error {get;}
		int 				ReadTimeout {get;set;}
		int 				WriteTimeout {get;set;}
		System.DateTime 	LastTime {get;}
		bool				Connected {get;}
		bool 				DataAvailable {get;}
		
		bool 				Connect( string host, int port, int connectTimeout );
		void 				Close();
		
		int 				Read( byte[] buffer, int index, int count );
		void 				Write( byte[] buffer, int index, int count );
	}	
	
	public static class TcpClientFactory
	{
		public static ITcpClient Create(bool secure)
		{
#if UNITY_IPHONE && !UNITY_EDITOR && USE_NSTCPCLIENT
			return new TcpClientNS(secure);
#elif UNITY_WEBPLAYER 	// moko: switch to use .net crypto for webplayer to workaround bouncyCastle issue for now
			return new TcpClientMono(secure);
#else
			return new TcpClientBouncy(secure);			
#endif
		}
		
		public static void AddCertificate( byte[] data )
		{
#if UNITY_IPHONE && !UNITY_EDITOR && USE_NSTCPCLIENT
			TcpClientNS.AddCertificate(data);
#elif UNITY_WEBPLAYER
			TcpClientMono.AddCertificate(data);
#else
			TcpClientBouncy.AddCertificate(data);	
#endif
		}
		
		public static void LoadCertStore( string path )
		{
			foreach( TextAsset cert in EB.Assets.LoadAll(path, typeof(TextAsset) ) )
			{
				var lines = cert.text.Split(new char[]{'\n','\r'}, System.StringSplitOptions.RemoveEmptyEntries);
				var buffer = new List<byte>();
				foreach( var line in lines )
				{
					if (!line.StartsWith("-----"))
					{
						buffer.AddRange( EB.Encoding.FromBase64String(line.Trim()));
					}
				}
				//EB.Debug.Log("Loading certificate " + cert.name);
				AddCertificate(buffer.ToArray());
				EB.Assets.Unload(cert);
			}
		}
		
	}
}


