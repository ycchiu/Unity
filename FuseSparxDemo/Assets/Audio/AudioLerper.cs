using UnityEngine;
using System.Collections;

public class AudioLerper
{
	float m_Value = 0.0f;
	float m_TargetValue = 0.0f;
	float m_Slope = 0.0f;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public void Lerp(float TargetValue, float Duration)
	{
		//Debug.Log("Lerp from " + m_Value.ToString("F2") + " to " + TargetValue.ToString("F2") + " over duration " + Duration.ToString());
		
		if ((false == GetIsLerping()) && (true == Utilities.AreFloatsEqual(m_Value, TargetValue)))
		{	
			return;
		}

		Duration = Mathf.Max(Duration, 0.0f);

		m_TargetValue = TargetValue;
		
		if (true == Utilities.AreFloatsEqual(0.0f, Duration))
		{
			m_Value = TargetValue;
		}
		else
		{
			m_Slope = (TargetValue - m_Value) / Duration;
		}	
	}

	public void Update(float TimeDelta)
	{
		if (true == GetIsLerping())
		{
			m_Value += (m_Slope * TimeDelta);
			if (m_Slope > 0.0f)
			{
				m_Value = Mathf.Min(m_Value, m_TargetValue);
			}
			else
			{
				m_Value = Mathf.Max(m_Value, m_TargetValue);
			}
		}		
	}

	public bool GetIsLerping()
	{
		if (false == Utilities.AreFloatsEqual(m_Value, m_TargetValue))
		{
			return (true);
		}
		else
		{
			return (false);
		}
	}

	public float GetValue()
	{
		return (m_Value);
	}

	public float GetTargetValue()
	{
		return (m_TargetValue);
	}
}

	