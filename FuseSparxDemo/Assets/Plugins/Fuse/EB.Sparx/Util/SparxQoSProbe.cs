#if !UNITY_WEBPLAYER	
using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;

namespace EB.Sparx
{
	public class QosProbe : System.IDisposable
	{
		UdpClient 	_client;
		int 		_sent;
		int 		_received;
		int 		_size;
		int 		_num;
		string 		_host;
		int 		_port;
		int			_timeout;
		IPAddress 	_hostEntry;
		byte[]		_data;

		float		_sum;
		float		_min;
		float		_max;

		public int AvgPing
		{
			get
			{
				if ( _received == 0 )
				{
					return 0;
				}
				else
				{
					return Mathf.RoundToInt( _sum / _received );
				}
			}
		}

		public byte[] Data
		{
			get { return _data; }
		}

		public event Action 		OnComplete;
		public event Action<string> OnError;

		public QosProbe( int numProbes, int timeoutMs, int size, string hostname, int port )
		{
			_num = numProbes;
			_sent = 0;
			_size = size;
			_host = hostname;
			_port = port;
			_timeout = timeoutMs;
			_min = System.Int32.MaxValue;
			_max = 0;

			int bindPort = 10000;

			// find a bind port
			while(_client==null)
			{
				try {
					_client = new UdpClient(bindPort, AddressFamily.InterNetwork);
				}
				catch {
					bindPort++;
				}
			}

			EB.Debug.Log("QosProbe Bound to " + bindPort + " for  " + hostname);

			SendProbe();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			OnComplete = null;

			try {
				_client.Close();
			}
			catch {}
		}
		#endregion

		void SendProbe()
		{
			if ( _sent < _num )
			{
				Coroutines.Run(_Send());
			}
			else if (OnComplete != null)
			{
				OnComplete();
			}
		}

		IEnumerator _Send()
		{
			++_sent;

			var bytes = Crypto.RandomBytes(_size);

			// resolve
			if (_hostEntry == null)
			{
				var dns = Dns.BeginGetHostAddresses(_host, null, null);
				while(!dns.IsCompleted)
				{
					yield return 1;
				}

				try {
					var ips = Dns.EndGetHostAddresses(dns);
					foreach( var ip in ips )
					{
						if (ip.AddressFamily == AddressFamily.InterNetwork)
						{
							_hostEntry = ip;
							break;
						}
					}
				}
				catch {

				}
			}

			if (_hostEntry == null )
			{
				if (OnError != null)
				{
					OnError( Localizer.GetString("ID_SPARX_ERROR_FAILED_DNS") );
					yield break;
				}
			}

			// get the ip
			var ep = new IPEndPoint( _hostEntry, _port);
			try {
				_client.Send(bytes, bytes.Length, ep);
			}
			catch {
				OnError( Localizer.GetString("ID_SPARX_ERROR_UNKNOWN") );
				yield break;
			}

			var ping = 0.0f;
			var start = System.DateTime.Now;

			var async = _client.BeginReceive(delegate(System.IAsyncResult ar){
				ping = (float) (System.DateTime.Now - start).TotalMilliseconds;
				//Debug.Log("receive complete " + ping + " " + _host);
			}, null);


			var timeout = Time.realtimeSinceStartup + _timeout/1000.0f;
			while(!async.IsCompleted)
			{
				if ( Time.realtimeSinceStartup >= timeout )
				{
					// we timed out;
					EB.Debug.LogWarning("QosProbe timed out to " + _host);
					yield return new WaitForFixedUpdate();
					SendProbe();
					yield break;
				}
				yield return 1;
			}

			_received++;
			_data = _client.EndReceive(async, ref ep);

			_sum += ping;
			_max = Mathf.Max(_max, ping);
			_min = Mathf.Min(_min, ping);

			SendProbe();
		}
	}
}
#endif
