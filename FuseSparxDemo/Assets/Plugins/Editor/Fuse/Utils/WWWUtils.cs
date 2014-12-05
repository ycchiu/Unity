using UnityEngine;
using UnityEditor;
using System.Collections;

public static class WWWUtils
{
	public enum Environment
	{
		Local,
		Dev,
		Prod
	};

	public static Environment env = Environment.Dev;

	public static string ADMIN_ADDRESS
	{
		get
		{
			return GetAdminUrl(env);
		}
	}

	public static string GetAdminUrl( Environment _env )
	{
		switch(_env)
			{
			case Environment.Local:
				return "http://local.sparx.io:8090";
			case Environment.Dev:
				return "http://api-dev.fast.sparx.io:8090";
			case Environment.Prod:
				return "http://admin.fast.sparx.io:8090";
			default:
				throw new System.ArgumentException("Unknown environemnt " + env);
			}
	}

	public static string AdminUrl( string uri )
	{
		return ADMIN_ADDRESS + uri;
	}

	public static string AdminLogin()
	{
		return AdminLogin(env);
	}

	public static string AdminLogin( Environment e )
	{
		WWWForm form = new WWWForm();
        form.AddField("format", "plain");
        form.AddField("email", "builds@explodingbarrelgames.com");
        form.AddField("password", "5xSia34uX8Y4mM6HJamxf8dvsP3toJCaBONEGwI95bWz0VOzGzYY3MBErwwxdTN");
        return Post(GetAdminUrl(e) + "/session/login", form);
	}

	public static void WaitOnWWW( WWW www, bool throwOnError )
	{
		while (!www.isDone)
		{
			System.Threading.Thread.Sleep(10);
		}

        if (!string.IsNullOrEmpty(www.error) && throwOnError)
		{
			EB.Debug.Log("Error: " + www.error);
			EB.Debug.Log("Headers: {0}",www.responseHeaders );
			throw new System.Exception("WWW Failed: " + www.url + " error: " + www.error );
		}

		//Debug.Log(www.text);
	}

    public static string HandleRequest(string url, WWWForm form, bool throwOnError)
    {
        EB.Debug.Log(url);
        string lastError = string.Empty;
        for ( int i = 0; i < 1; ++i )
        {
            var www = form != null ? new WWW(url, form) : new WWW(url);
            WaitOnWWW(www, false);

			var status = "";
			if ( !www.responseHeaders.TryGetValue("STATUS", out status))
			{
				status = string.Empty;
			}

            if ( string.IsNullOrEmpty(www.error) )
            {
                return www.text;
            }
			else if ( status.Contains("303 See Other") )
			{
				return string.Empty;
			}
			else if ( status.Contains("404 Not Found") )
			{
				lastError = www.error;
				break;
			}

            lastError = www.error;
            EB.Debug.LogError("Request " + url + " Failed: " + lastError + " Status: " + status);
			EB.Debug.LogError("Headers: {0}",www.responseHeaders );
            System.Threading.Thread.Sleep(5 * 1000);

			EditorUtility.UnloadUnusedAssets();
        }

        if (!string.IsNullOrEmpty(lastError) && throwOnError)
        {
            throw new System.Exception("Build Failed: " + lastError);
        }
        return string.Empty;
    }

    public static string Get(string url)
	{
        return HandleRequest(url, null, true);
	}

    public static object GetJson(string url)
    {
        return EB.JSON.Parse(Get(url));
    }

   	public static string Post(string url, WWWForm form)
    {
        return HandleRequest(url, form, true);
    }

	public static object PostJson(string url, WWWForm form)
    {
        return EB.JSON.Parse(Post(url,form));
    }

}
