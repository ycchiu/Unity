Shader "EBG/Effects/ReflectionBlur" 
{
	Properties 
	{  
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_HalfTexelOffset("Half Texel Offset", Vector) = (0, 0, 0, 0)
	}
	SubShader
	{		 
		//PASS 0 : Horizontal Blur
		Pass
		{
		 	Tags {"Queue"="Geometry" }
			LOD 100			
			Cull Off
			ZWrite Off
			ZTest Off
			Blend Off
			CGPROGRAM  
	
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float2 _HalfTexelOffset;
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				data.uv0 = uv - 2.0 *_HalfTexelOffset;
				data.uv1 = uv + 2.0 * _HalfTexelOffset;
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				half3 sample1 = tex2D(_MainTex, i.uv0).rgb;
				half3 sample2 = tex2D(_MainTex, i.uv1).rgb;
				return fixed4((sample1 + sample2) / 2.0, 1.0); 
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
		
		//PASS 1 : Vertical Blur + Ramp
		Pass
		{
		 	Tags {"Queue"="Geometry" }
			LOD 100			
			Cull Off
			ZWrite Off
			ZTest Off
			Blend Off
			CGPROGRAM   
	
			#include "UnityCG.cginc"
			 
			sampler2D _MainTex; 
			float4 _MainTex_ST;
			half _Ramp;
			float2 _HalfTexelOffset;
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				data.uv0 = uv - 4.0 * _HalfTexelOffset;
				data.uv1 = uv;
				data.uv2 = uv + 4.0 * _HalfTexelOffset;
				data.uv3 = uv + 8.0 * _HalfTexelOffset;
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				fixed3 sample0 = tex2D(_MainTex, i.uv0).rgb;
				fixed3 sample1 = tex2D(_MainTex, i.uv1).rgb;
				fixed3 sample2 = tex2D(_MainTex, i.uv2).rgb;
				fixed3 sample3 = tex2D(_MainTex, i.uv3).rgb;
				fixed3 sample = ((sample0 + sample1) * 0.25) + ((sample2 + sample3) * 0.25);
				fixed luminance = dot(sample, fixed3(0.299, 0.587, 0.114));
				fixed luminancePow = pow(luminance, _Ramp) + 0.01;
				return fixed4(sample * (luminancePow / luminance), 1.0);
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
	} 
}
