using System.Collections;

namespace EB
{
	// Uri replacement for the crappy mono Uri that regularly crashes on iOS
	public class Uri
	{
		public enum Component
		{
			Original,
			Protocol,
			User,
			Password,
			Host,
			Port,
			Path,
			Query,
			
			Count
		}
		
		public string Protocol { get { return GetComponent(Component.Protocol, string.Empty); } }
		public string Scheme { get { return GetComponent(Component.Protocol, string.Empty); } }
		public string User { get { return GetComponent(Component.User, string.Empty); } }
		public string Password { get { return GetComponent(Component.Password, string.Empty); } }
		public string Host { get { return GetComponent(Component.Host, string.Empty); } }
		public string Query { get { return GetComponent(Component.Query, string.Empty); } }
		public string Path { get { return GetComponent(Component.Path, "/"); } }
		public int    Port { get { return int.Parse( GetComponent(Component.Port, GetDefaultPort().ToString() ) ); } }
		
		public string HostAndPort
		{
			get
			{
				var r = Host;
				if (Port != GetDefaultPort())
				{
					r += ":" + Port;
				}
				return r;
			}
		}
		
		public string PathAndQuery
		{
			get
			{
				var r = Path;
				if (!string.IsNullOrEmpty(Query))
				{
					r += '?' + Query;
				}
				return r;
			}
		}
		
		private string[] _components = new string[(int)Component.Count];
		
		public Uri( string url )
		{
			Parse(url);
		}
		
		public Uri()
		{
		}
		
		public bool Parse( string url )
		{
			// supports the following format
			// protocol://<user>:<password>@<host>:<port>/<url-path>?<query>
			var index = 0;
			SetComponent(Component.Original, url);
			
			// protocol
			index = url.IndexOf(':');
			if ( index <= 0 )
			{
				//EB.Debug.LogError("failed to parse protocol " + url);
				return false;
			}
			
			var protocol = url.Substring(0,index);
			SetComponent(Component.Protocol, protocol);
			
			url = url.Substring(index);
			if (!url.StartsWith("://"))
			{
				//EB.Debug.LogError("failed to parse protocol " + url);
				return false;
			}
			url = url.Substring(3);
			
			if(Scheme.Equals("file"))
			{
				// if it's a filepath there's not going to be a username/password or port... I think. Remove the drive ("C:/") for windows machines
				index = url.IndexOf(":/");
				if(index > 0)
				{
					url = url.Substring(index +1);
				}
			}
			
			// check for @
			var slash = url.IndexOf('/');
			var at = url.IndexOf('@');
			
			if (at >=0 && (slash<0||slash>at))
			{
				// username:password
				var auth = url.Substring(0,at);
				url = url.Substring(at+1);
				
				index = auth.IndexOf(':');
				if (index >=0 )
				{
					SetComponent(Component.User, auth.Substring(0,index) );	
					SetComponent(Component.User, auth.Substring(index+1) );	
				}
				else
				{
					SetComponent(Component.User, auth);	
				}				
			}
			
			index = url.IndexOfAny( new char[]{ '/', '?', ':' } ); 
			if ( index < 0 )
			{
				// the rest is the host name
				SetComponent(Component.Host, url);
			}
			else
			{
				SetComponent(Component.Host, url.Substring(0,index) );
				
				var tmp = url[index];
				url = url.Substring(index+1);
				
				// check for port
				if (tmp == ':')
				{
					index = url.IndexOfAny( new char[]{ '/', '?' });
					var portString = string.Empty;
					
					if (index <0 )
					{
						portString = url;
						url = string.Empty;
					}
					else
					{
						portString = url.Substring(0, index);
						url = url.Substring(index+1);
					}					
					var port = 0;
					if (!int.TryParse( portString, out port))
					{
						//EB.Debug.LogError("failed to parse port " + url);
						return false;
					}
					SetComponent(Component.Port, port.ToString());
				}
				
				// search for query
				index = url.IndexOf('?');
				if ( index < 0 )
				{
					SetComponent(Component.Path, "/"+url);
				}
				else
				{
					SetComponent(Component.Path, "/"+url.Substring(0,index) );
					SetComponent(Component.Query, url.Substring(index+1) );
				}
			}
			
			//Print();
			
			return true;
		}
		
		public int GetDefaultPort()
		{
			switch(Protocol)
			{
			case "http":
			case "ws":
				return 80;
			case "https":
			case "wss":
				return 443;
			case "ssh":
				return 22;
			case "ftp":
				return 21;
			}
			return 0;
		}
		
		public void Print()
		{
			for ( int i = 0; i < _components.Length; ++i)
			{
				EB.Debug.Log("Component {0}:{1}", (Component)i, _components[i] );
			}
		}
		
		public override string ToString ()
		{
			string result = Protocol + "://";
			
			if ( !string.IsNullOrEmpty(User))
			{
				result += User;
				if (!string.IsNullOrEmpty(Password))
				{
					result += ":" + Password;
				}
				result += "@";
			}
			
			result += HostAndPort;
			result += PathAndQuery;
					
			return result;
		}
		
		public void SetComponent( Component c, string value)
		{
			_components[(int)c] = value;
		}
			
		public string GetComponent( Component c, string def )
		{
			var r = _components[(int)c];
			if (!string.IsNullOrEmpty(r))
			{
				return r;
			}
			return def;
		}
		
	}
}

