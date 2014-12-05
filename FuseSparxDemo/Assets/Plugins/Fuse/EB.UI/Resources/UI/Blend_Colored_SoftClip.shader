Shader "EBG/UI/BlendColored (SoftClip)" 
{
	Properties 
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
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
			
			struct Input 
			{
			    float4 vertex : POSITION;
			    float4 color: COLOR;
			    float2 texcoord0 : TEXCOORD0;
			};
			
			struct VtoS
			{
	          	float4 position : SV_POSITION;	
				float2 uv_MainTex : TEXCOORD0; 
				float2 worldPos : TEXCOORD1;
				float4 color: TEXCOORD2;
			};
			
			float4 _MainTex_ST;
			float2 _ClipSharpness = float2(20.0, 20.0);
			
			VtoS vertex_lm(Input v)
			{   
				VtoS data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);  
				data.uv_MainTex = v.texcoord0.xy; 
				data.worldPos = TRANSFORM_TEX(v.vertex.xy, _MainTex);
				data.color = v.color;
				return data;
			}
			
			fixed4 fragment_lm(VtoS IN) : COLOR0
			{
				float2 factor = (float2(1.0, 1.0) - abs(IN.worldPos)) * _ClipSharpness;
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
				c.a *= clamp( min(factor.x, factor.y), 0.0, 1.0);
				return c;
			}   
				
			#pragma vertex vertex_lm 
			#pragma fragment fragment_lm 
			
			ENDCG 
		}
	}
}


