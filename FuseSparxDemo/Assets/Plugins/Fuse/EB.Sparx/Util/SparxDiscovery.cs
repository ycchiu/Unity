using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;

namespace EB.Sparx
{
	public class Discovery : EventEmitter
	{
		public class Server
		{
			public string Name {get;private set;}
			public string Url {get;private set;}

			public Server(string name, string url)
			{
				this.Name = name;
				this.Url = url;
			}
		}

		UdpClient _client;
		object _receive = null;
		object _send = null;
		Hashtable _servers = new Hashtable();
		byte[] _sendBytes = null;

		public Discovery(string gameId)
		{
			_sendBytes = EB.Encoding.GetBytes(gameId);

			int bindPort = 20000;
			
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
		}

		public void Start()
		{
			Debug.Log("Starting Discovery");
			Stop ();
			_receive = Coroutines.SetInterval(this.Recieve, 10);
			_send = Coroutines.SetInterval(this.Send, 1000);
		}

		public new void Dispose()
		{
			base.Dispose();
			Stop();
		}

		public void Stop()
		{
			Coroutines.ClearInterval(_receive);
			_receive = null;

			Coroutines.ClearInterval(_send);
			_send = null;
		}

		void Send()
		{
			var endpoint = new IPEndPoint(IPAddress.Broadcast, 1132);
			_client.Send(_sendBytes, _sendBytes.Length, endpoint);
		}

		void AddServer( Server s )
		{
			if (_servers[s.Name] == null)
			{
				_servers[s.Name] = s;
				this.Emit("server", s);
			}
		}

		void Recieve()
		{
			if (_client.Available>0)
			{
				try {
					IPEndPoint from = null;
					var data = _client.Receive( ref from );
					var str = EB.Encoding.GetString(data);
					var obj = (Hashtable)JSON.Parse(str);

					var name = EB.Dot.String("name", obj, "");
					var port = EB.Dot.Integer("port", obj, 443);
					var ssl  = EB.Dot.Bool("ssl", obj, true);

					var url = (ssl ? "https" : "http") + "://" + from.Address.ToString() + ":" + port;
					AddServer( new Server(name,url));
				}
				catch 
				{

				}
			}
		}


	}

}