using UnityEngine;
using System.Collections;

public class UILogger : MonoBehaviour
{
	public static UILogger Instance {get; private set;}

	public System.Text.StringBuilder logBuffer {get; private set;}

	public int lastUpdateFrame {get; private set;}

	private int currentFrame = 0;
	private const int logBufferSize = 10000;

	public void Log(string message)
	{
		if (logBuffer == null)
		{
			return;
		}

		string combined = message + "\n" + GetCallStack() + "\n";
		if (combined.Length > logBuffer.MaxCapacity)
		{
			EB.Debug.LogError("Log too big!");
			return;
		}
		int spaceLeft = logBuffer.MaxCapacity - logBuffer.Length;
		if (combined.Length > spaceLeft)
		{
			logBuffer.Remove(0, combined.Length - spaceLeft);
		}
		logBuffer.Append(combined);
		// Debug.Log (logBuffer.ToString());

		lastUpdateFrame = currentFrame;
	}
	
	private string GetCallStack()
	{
		System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
		System.Text.StringBuilder callstackInfo = new System.Text.StringBuilder();
		for (int i = 3; i < st.GetFrames().Length; ++i)
		{
			System.Diagnostics.StackFrame sf = st.GetFrame(i);
			callstackInfo.AppendFormat("({0}) {1}\n", sf.GetMethod().DeclaringType.Name, sf.GetMethod().Name);
		}
		
		return callstackInfo.ToString();
	}

	private void Awake()
	{
		Instance = this;
#if UNITY_EDITOR || ENABLE_PROFILER || UNITY_WEBPLAYER || USE_DEBUG
		logBuffer = new System.Text.StringBuilder(logBufferSize, logBufferSize);
#endif
	}

	private void Update()
	{
		currentFrame = Time.frameCount;
	}
}
