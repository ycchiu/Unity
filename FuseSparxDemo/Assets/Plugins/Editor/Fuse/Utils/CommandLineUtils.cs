using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class CommandLineUtils  
{	

	const int kMaxThreads = 4;
	
	public class AsyncTask
	{
		public string command;
		public string args;
		public string dir;
		public string result;
		public bool   done;
	};
	static Queue<AsyncTask> _queue = new Queue<AsyncTask>(1024);
	static System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(0,kMaxThreads);
	static bool _running = false;
	
	
	static void _Thread()
	{
		while (_running)
		{
			while(true)
			{
				AsyncTask task = null;
				
				lock(_queue)
				{
					if ( _queue.Count > 0 )
					{
						task = _queue.Dequeue();
					}
				}
				
				if ( task == null )
				{
					break;
				}
				
				task.result = Run( task.command, task.args, task.dir ); 
				task.done = true;
			}
		
			_semaphore.WaitOne();
		}
	}
	
	static System.Diagnostics.Process _RunAsync(string command, string arguments, string workingDir) 
	{
		//Debug.Log("Runing: " + command + " " + arguments);
		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo.FileName = command;
		p.StartInfo.Arguments = arguments;
		p.StartInfo.WorkingDirectory = workingDir;
		p.StartInfo.UseShellExecute = true;
		p.StartInfo.CreateNoWindow = false;			
		p.Start();
		return p;
	}
	
	static int _Run(string command, string arguments, string workingDir) 
	{
		var p = _RunAsync(command, arguments, workingDir);
		p.WaitForExit();
		//EB.Debug.Log("Run done " + p.ExitCode);
		var code = p.ExitCode;
		p.Dispose();
		System.GC.Collect();
		return code;
	}
	
	static void _DeleteFile(string file)
	{
		try
		{
			File.Delete(file);
		}
		catch {}
	}
	
	public static AsyncTask RunAsync( string command, string arguments )
	{
		return RunAsync( command, arguments, Directory.GetCurrentDirectory() );	
	}
	
	public static AsyncTask RunAsync( string command, string arguments, string workingDir )
	{
		if ( _running == false )
		{
			_running = true;
			for ( int i = 0; i < kMaxThreads; ++i )
			{
				new System.Threading.Thread(_Thread).Start();
			}
		}
		
		AsyncTask task = new AsyncTask();
		task.command = command;
		task.args = arguments;
		task.dir = workingDir;
		lock(_queue)
		{
			_queue.Enqueue(task);
		}
		
		// wake up threads
		try
		{
			_semaphore.Release();
		}
		catch {}
		
		return task;
	}
	
	public static void WaitForTasks()
	{
		while( true )
		{
			bool done = true;
			lock(_queue)
			{
				done = _queue.Count == 0;
			}
			
			if ( done )
			{
				return;
			}
			
			System.Threading.Thread.Sleep(100);
		}
	}
	
	public static string Run(string command, string arguments)
	{
		return Run(command,arguments,Directory.GetCurrentDirectory());
	}
	
	public static string RunWithAppleScript(string command, string arguments)
	{
		return Run(command,arguments,Directory.GetCurrentDirectory(),true);
	}
		
	public static string Run(string command, string arguments, string workingDir, bool applescript = false) 
	{
		EB.Debug.Log("Running: " + command + " " + arguments + " " + applescript );
		
		int hash		= System.Diagnostics.Process.GetCurrentProcess().Id + Random.Range( 1, 100000 );
		int threadId	= System.Threading.Thread.CurrentThread.ManagedThreadId;

		string folder	= EnvironmentUtils.Get("BUILD_FOLDER", "/tmp");
		try
		{
			Directory.CreateDirectory(folder);	// moko: make sure the tmp folder exist first. 
		}
		catch (System.Exception) {}
		finally
		{
			if (!Directory.Exists(folder))			// moko: if for some reason we cant create tmp folder, then revert back to default
				folder = "/tmp";
		}

		string file 	= string.Format("{2}/unity_command_{0}_{1}.sh", hash, threadId, folder);
		string outfile 	= string.Format("{2}/unity_command_{0}_{1}.out.txt", hash, threadId, folder);
		string script 	= string.Format("#!/bin/bash\nulimit -s 16384\ntouch {2}\n{0} {1} > {2}\n", command, arguments, outfile);

		_DeleteFile(outfile);
		File.WriteAllText(file, script);
		_Run("chmod", "u+x " + file, workingDir);		
		
		int returnCode = 0;
		if( applescript == true )
		{
			string applescriptFile 	= string.Format("{2}/unity_command_{0}_{1}.scpt", hash, threadId, folder);
			string applescriptScript = string.Empty;
			applescriptScript += "on is_running(appName)\n";
			applescriptScript += "tell application \"System Events\" to (name of processes) contains appName\n";
			applescriptScript += "end is_running\n";
			applescriptScript += "set terminalRunning to is_running(\"Terminal\")\n";			
			applescriptScript += "tell application \"Terminal\"\n";
			applescriptScript += "activate\n";
			applescriptScript += string.Format("set runningTab to do script \"{0}\"\n", file);
			applescriptScript += "delay 1\n";
			applescriptScript += "repeat until busy of runningTab is false\n";
			applescriptScript += "delay 1\n";
			applescriptScript += "end repeat\n";
			applescriptScript += "if terminalRunning is false then\n";
			applescriptScript += "quit\n";
			applescriptScript += "else\n";
			applescriptScript += "close window 1\n";
			applescriptScript += "end if\n";
			applescriptScript += "end tell\n";
			
			_DeleteFile(applescriptFile);
			File.WriteAllText(applescriptFile, applescriptScript);
			_Run("chmod", "u+x " + applescriptFile, workingDir);
			
			returnCode = _Run("osascript", applescriptFile, workingDir);
		}
		else
		{
			returnCode = _Run(file, string.Empty, workingDir);
		}

		string output = string.Empty;
		try
		{
			output = File.ReadAllText(outfile);
		}
		catch {}
		
		//_DeleteFile(outfile);
		
		if (returnCode != 0)		// moko: dump out the cmdline output if return code is non-zero (ie error)
		{
			EB.Debug.LogWarning("Cmd Return Code: (" + returnCode + ") \"" + output + "\"");
		}
		
		return output;
	}
	
	public static void RunWindowsSyncCommand(string fileName, string command)
	{
           System.Diagnostics.Process process = new System.Diagnostics.Process();
           System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

           startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
           startInfo.FileName = fileName;
           startInfo.Arguments = command;
           process.StartInfo = startInfo;
           process.Start();
           
           if(!process.WaitForExit(10000))
           {
                   Debug.LogError("Could not run windows command: " + fileName + " " + command);
           }
   }
   
   private static Dictionary<string,string> _customArgs = null;
   private static string _commandLineArgs = null;
   
	public static string GetCommandLineArgs()
	{
		if (_commandLineArgs == null)
		{
			_commandLineArgs = System.String.Join(" ",System.Environment.GetCommandLineArgs());
		}
		return _commandLineArgs;
	}
   
	public static Dictionary<string,string> GetBatchModeCommandArgs()
	{
   		if (_customArgs == null) 
   		{
   			_customArgs = new Dictionary<string, string>();
			string args = GetCommandLineArgs(); 			
			string matchString = "-batchmodeargs:";
			
			string argsLower = args.ToLower();
			int bmaStartIndex = argsLower.IndexOf(matchString);
			if (bmaStartIndex > -1)
   			{
   				int startIndex = bmaStartIndex + matchString.Length;
   				int endIndex = args.IndexOf(' ', startIndex);
   				string argsValue = "";
   				if (endIndex < 0) 
   				{
   					argsValue = args.Substring(startIndex);
   				} 
   				else
   				{
   					argsValue = args.Substring(startIndex, endIndex - startIndex);
   				}
   				string[] argsValuesSplit = argsValue.Split(',');
   				foreach(string entry in argsValuesSplit)
   				{
   					string[] splitEntry = entry.Split('=');
					string key, value;
					int length = splitEntry.Length;
   					if (length == 1) {
   						key = splitEntry[0];
   						value = key;
   					} else if (length == 2) {
   						key = splitEntry[0];
   						value = splitEntry[1];
   					} else {
   					
   						Debug.LogError("Warning incorrect # of values: "+argsValue);
   						continue;
   					}
					Debug.Log("Adding BatchModeArgs: |"+key+"|,"+value);
					_customArgs[key] = value;
   				}
   			}
   		}
   		return _customArgs;
   	}
   
   
}
