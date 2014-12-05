 Shader "EBG/UI/OpaqueColored" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader
	{
		Tags
		{
			//these seem to be the tags that NGUI wants, even for opaque objects
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		LOD 100
		Lighting Off
		AlphaTest Off
		Cull Off
		ZWrite Off
		Blend Off
		Fog { Mode Off }
		
		Pass
		{
			CGPROGRAM	
			
			#include "UnityCG.cginc"	
									
			sampler2D _MainTex;	
				
			struct Input 
			{
			    float4 vertex : POSITION;
			    float4 texcoord0 : TEXCOORD0;
			    float4 color: COLOR;
			};
			
			struct VtoS
			{
	          	float4 position : SV_POSITION;	
				float2 uv_MainTex : TEXCOORD0; 
				float4 color: TEXCOORD1;
			};
			
			float4 _MainTex_ST;
			
			VtoS vertex_lm(Input v)
			{   
				VtoS data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);  
				data.uv_MainTex = v.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw; 
				data.color = v.color;
				return data;
			}
			
			fixed4 fragment_lm(VtoS IN) : COLOR0
			{
				return tex2D(_MainTex, IN.uv_MainTex) * IN.color;
			}   
				
			#pragma vertex vertex_lm 
			#pragma fragment fragment_lm 
			
			ENDCG 
		}
	}
}
