﻿Shader "EBG/UI/BlendColoredMask" 
{
	Properties 
	{
		_Color ("Base Color", Color) = (1, 1, 1, 1)
		_MainTex ("Base (RGBA)", 2D) = "white" {}
		_MaskTex ("Mask (RGBA)", 2D) = "white" {}
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		LOD 100
		Lighting Off
		AlphaTest Off
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Mode Off }
		
		Pass
		{
			CGPROGRAM	
			
			#include "UnityCG.cginc"	
									
			sampler2D _MainTex;
			sampler2D _MaskTex;
			fixed4 _Color;
			
			struct Input 
			{
			    float4 vertex : POSITION;
			    float4 texcoord0 : TEXCOORD0;
			    float4 texcoord1 : TEXCOORD1;
			    float4 color: COLOR;
			};
			
			struct VtoS
			{
	          	float4 position : SV_POSITION;	
				float2 uv_MainTex : TEXCOORD0; 
				float2 uv_MaskTex : TEXCOORD1; 
				float4 color: TEXCOORD2;
			};
			
			VtoS vertex_lm(Input v)
			{   
				VtoS data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);  
				data.uv_MainTex = v.texcoord0.xy; 
				data.uv_MaskTex = v.texcoord1.xy;
				data.color = v.color * _Color;
				return data;
			}
			
			fixed4 fragment_lm(VtoS IN) : COLOR0
			{
				// return fixed4(IN.uv_MaskTex, 0, 1);
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
				c *= tex2D(_MaskTex, IN.uv_MaskTex);
				return c;
			}   
				
			#pragma vertex vertex_lm 
			#pragma fragment fragment_lm 
			
			ENDCG 
		}
	}
}

