using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class S3Utils 
{
	static EB.ThreadPool _pool = new EB.ThreadPool(4);
	
	private static string AccessKeyId 
	{
		get
		{
			return EnvironmentUtils.Get("AWS_ACCESS_KEY", string.Empty);	
		}
	}
	
	public static string Bucket
	{
		get
		{
			return EnvironmentUtils.Get("AWS_BUCKET", string.Empty);	
		}
	}
	
	public static string BasePath
	{
		get
		{
			return EnvironmentUtils.Get("AWS_BASEPATH", string.Empty);	
		}
	}
	
	public static string Policy
	{
		get
		{
			return EnvironmentUtils.Get("AWS_POLICY", string.Empty);	
		}
	}
	
	public static string Signature
	{
		get
		{
			return EnvironmentUtils.Get("AWS_SIGNATURE", string.Empty);	
		}
	}
	
	
	public static string CalculateMD5( byte[] bytes )
	{
		var md5 = System.Security.Cryptography.MD5.Create();
		var digest = md5.ComputeHash(bytes);
		return EB.Encoding.ToHexString(digest);
	}
	
	public static string CalculateMD5( string path )
	{
		return CalculateMD5( File.ReadAllBytes(path) );
	}
	
	public static string PutData( byte[] bytes, string name, string path )
	{
		var key = Path.Combine(BasePath, path);
		
		EB.Debug.Log("S3 Put: {0} -> {1}:/{2}", name, Bucket, key );
		
		if (string.IsNullOrEmpty(Bucket) )
		{
			return string.Empty;
		}
		
		WWWForm form = new WWWForm();
		form.AddField("key", key);
		form.AddField("acl", "public-read");
		form.AddField("AWSAccessKeyId", AccessKeyId);
		form.AddField("Policy", Policy );
		form.AddField("Signature", Signature );
		form.AddBinaryData("file", bytes, name );
		
		var contentType = MimeUtils.GetMimeType( Path.GetExtension(name) );
		if (!string.IsNullOrEmpty(contentType) )
		{
			form.AddField("Content-Type", contentType);
		}
		
		// make sure to upload the md5
		form.AddField("Content-MD5", CalculateMD5(bytes) );
		
		var url = "http://"+Bucket+".s3.amazonaws.com/";
		var result = WWWUtils.Post(url, form); 
		EB.Debug.Log("key {0} result: {1}", key, result);
		if (result.Contains("Error") )
		{
			throw new System.Exception("S3 upload failed! key: " + key + " result: " + result ); 
		}
		
		// GC
		System.GC.Collect();
		
		// return the url
		return url + key;
	}
	
	// post a local file to a s3
	public static string Put( string file, string path )
	{
	
		string name = Path.GetFileName(file);
	
		var key = Path.Combine(BasePath, path);
		
		EB.Debug.Log("S3 Put: {0} -> {1}:/{2}", name, Bucket, key );
		
		if (string.IsNullOrEmpty(Bucket) )
		{
			return string.Empty;
		}
		
		string md5 = CalculateMD5(file);
		
		string url = "http://"+Bucket+".s3.amazonaws.com/";
		
		string args = string.Format("{0} -F file=@{1} -F key='{2}' -F acl='public-read' -F AWSAccessKeyId='{3}' -F Policy='{4}' -F Signature='{5}' -F Content-MD5='{6}'", url, file, key, AccessKeyId, Policy, Signature, md5);
		var contentType = MimeUtils.GetMimeType( Path.GetExtension(name) );
		if (!string.IsNullOrEmpty(contentType) )
		{
			args += (" -F Content-Type="+contentType);
		}
		
		Debug.Log("Uploading S3: curl "+args);
		
		string s3PutResult = CommandLineUtils.Run("curl", args);
		EB.Debug.Log(s3PutResult);
		
		// return the url
		return url + key;
	}
	
	public static EB.ThreadPool.AsyncTask PutAsync(string local, string path) 
	{
		return _pool.Queue( delegate(object s){
			Put(local, path);	
		}, null);
	}
	
	public static void PutDirectory( string local, string path )
	{
		var files = Directory.GetFiles(local, "*.*", SearchOption.AllDirectories );
		var jobs = new List<EB.ThreadPool.AsyncTask>();
		foreach ( var file in files )
		{
			var task = PutAsync(file,Path.Combine(path, file.Substring(local.Length+1)) );
			jobs.Add(task);
		}
		_pool.Wait();
		
		foreach( var job in jobs )
		{
			if ( job.exception != null )
			{
				throw job.exception;
			}
		}
	}
}
