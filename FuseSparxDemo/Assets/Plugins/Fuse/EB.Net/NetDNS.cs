using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB.Net
{
	using IPAddress = System.Net.IPAddress;
	using DnsBase = System.Net.Dns;
	
	// a dns cache that works well with Amazon's ELB
	public static class DNS	
	{
		class Entry
		{
			public IPAddress 		last;
			public System.DateTime	lastT;
			public IPAddress[] 	addresses = new IPAddress[0];
		};
		static Dictionary<string,Entry> _entries = new Dictionary<string, Entry>();
		
		static Entry GetEntry( string host )
		{
			lock(_entries)
			{
				Entry e;
				if (!_entries.TryGetValue(host, out e))
				{
					e = new Entry();
					_entries.Add(host, e);
				}
				return e;
			}
		}
		
		public static void StoreLast( string hostname, IPAddress addr )
		{
			var entry = GetEntry(hostname);
			lock(entry)
			{
				entry.last = addr;
				entry.lastT = System.DateTime.Now;
			}
		}
		
		public static IPAddress[] Lookup( string hostname )
		{
			IPAddress addr;
			if (IPAddress.TryParse(hostname, out addr))
			{
				return new IPAddress[]{ addr };
			}
			
			Entry e = GetEntry(hostname);
			
			// try to get the host entries
			List<IPAddress> addresses = new List<IPAddress>();
			try 
			{
				var async = DnsBase.BeginGetHostAddresses(hostname, null, null);
				var timeout = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
				
				while( System.DateTime.Now < timeout )
				{
					if (async.IsCompleted)
					{
						var tmp = DnsBase.EndGetHostAddresses(async);
						if (tmp != null)
						{
							foreach( var ip in tmp )
							{
								if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork )
								{
									//Debug.Log("addr " + ip);
									addresses.Add(ip);
								}
							}
						}
						break;
					}
				}
				
			}
			catch (System.Exception ex)
			{
				// failure, 
				EB.Debug.LogError("failed to lookup " + hostname + " " + ex);
			}
						
			lock(e)
			{
				if (addresses.Count > 0)
				{
					e.addresses = addresses.ToArray();
				}
				else
				{
					EB.Debug.LogError("failed to lookup " + hostname);
				}
				
				if (e.last != null)
				{
					var index = System.Array.IndexOf(e.addresses, e.last);
					if ( index >= 0 )
					{
						// move to the front
						var tmp = e.addresses[0];
						e.addresses[0] = e.addresses[index];
						e.addresses[index] = tmp;
					}
				}	
				
				return e.addresses;
			}
			

			
		}
			
	}
}

