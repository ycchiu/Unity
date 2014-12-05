using UnityEngine;
using System.Collections;

public class Timer 
{
	private float m_Length = 0.0f;
	private float m_Remaining = 0.0f;	

	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	public Timer(float Length, bool BeginElapsed)
	{
		m_Length = Mathf.Max(Length, 0.0f);
		if (true == BeginElapsed)
		{
			Elapse();
		}
		else
		{
			Reset();
		}
	}

	public float GetLength()
	{
		return (m_Length);
	}

	public void SetLength(float Length)
	{
		m_Length = Mathf.Max(Length, 0.0f);
	}

	public float GetRemaining()
	{
		return (m_Remaining);
	}

	public float GetRemainingRatio()
	{
		if (true == Utilities.AreFloatsEqual(0.0f, m_Length))
		{
			return (Mathf.Infinity);
		}
		else
		{
			return (GetRemaining() / GetLength());
		}
	}

	public float GetElapsed()
	{
		return (m_Length - m_Remaining);
	}

	public float GetElapsedRatio()
	{
		if (true == Utilities.AreFloatsEqual(0.0f, m_Length))
		{
			return (Mathf.Infinity);
		}
		else
		{
			return (GetElapsed() / GetLength());
		}
	}

	public void SetRemaining(float Remaining)
	{
		m_Remaining = Mathf.Max(Remaining, 0.0f);
	}

	public bool GetIsElapsed()
	{
		return (m_Remaining <= 0.0f);
	}

	public bool GetIsRunning()
	{
		return (!(GetIsElapsed()));
	}	
		
	public void Reset()
	{
		m_Remaining = m_Length;
	}

	public void Elapse()
	{
		m_Remaining = 0.0f;
	}
	
	public void Update(float Delta)
	{
		if (false == GetIsElapsed())
		{
			m_Remaining = Mathf.Max((m_Remaining - Delta), 0.0f);	
		}
	}

	public void Update()
	{
		Update(Time.deltaTime);
	}
}
