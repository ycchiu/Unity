Shader "EBG/PostFX/BloomWarp"
{
	Properties
	{
		_MainTex("", 2D) = "black" {}
		_BloomTex("Bloom", 2D) = "black" {}
		_VignetteTex("Vignette", 2D) = "black" {}
		_WarpTex("Warp", 2D) = "white" {}
	}
	Category
	{
		Tags
		{
			"Queue"="Geometry"
		}
		Lighting Off
		Fog { Mode Off }
		Cull Off
		ZWrite Off
		ZTest Always
		Blend Off
		Subshader
		{
			LOD 100
			Pass
			{
				CGPROGRAM
				#include "UnityCG.cginc"
				#pragma target 3.0
				#pragma vertex vertex_prog
				#pragma fragment fragment_prog
				//BLOOM_ON WARP_ON
				sampler2D _MainTex;
				float4 _MainTex_ST; 
				half3 _PostFXBloomColor;
				half _PostFXBloomIntensity;
				sampler2D _BloomTex;
				half2 _PostFXWarpIntensity;
				sampler2D _WarpTex;
				struct VertToFrag
				{
					float4 position : SV_POSITION;	
					float2 screenUV : TEXCOORD0;
					half3 bloomColor : TEXCOORD1;
				};
				VertToFrag vertex_prog(appdata_img v)
				{
					VertToFrag data;
					data.position = mul(UNITY_MATRIX_MVP, v.vertex);
					data.screenUV = v.texcoord;
					data.bloomColor = _PostFXBloomColor * _PostFXBloomIntensity;
					return data;
				}	
				fixed4 fragment_prog(VertToFrag IN) : COLOR0
				{		 
					half2 sampleUV = IN.screenUV;
					half2 warpSample = (tex2D(_WarpTex, sampleUV).rg - 0.5f) * _PostFXWarpIntensity;
					IN.screenUV += warpSample;
					half4 screen = tex2D(_MainTex, IN.screenUV);
					half3 bloomSample = tex2D(_BloomTex, sampleUV).rgb;
					screen.rgb += bloomSample * IN.bloomColor;
					return screen;
				} 
				
				ENDCG
			}
		}
	}
}
