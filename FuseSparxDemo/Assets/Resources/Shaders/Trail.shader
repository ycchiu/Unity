 Shader "EBG/Particle/Trail" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Source Blend Mode", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Destination Blend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _DepthTest ("Depth test", Float) = 4
		[MaterialToggle] EBG_TRAIL_USE_FANCY_UVS ("Use fancy UVs", Float) = 0
		
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100
		Cull Off
		ZWrite Off
		ZTest [_DepthTest]
		Blend [_BlendModeSrc] [_BlendModeDst]
		
		Pass
		{
			CGPROGRAM	
			
			#include "UnityCG.cginc"
			#pragma multi_compile EBG_TRAIL_USE_FANCY_UVS_OFF EBG_TRAIL_USE_FANCY_UVS_ON	
									
			sampler2D _MainTex;	
				
			struct Input 
			{
			    float4 vertex : POSITION;
			    float3 texcoord0 : NORMAL; //need to pass in a float3
				fixed4 color : COLOR;
			};
			
			struct VtoS
			{
	          	float4 position : SV_POSITION;	
				float3 uv : TEXCOORD0; 
				fixed4 color : TEXCOORD1;
			};
			
			VtoS vertex_lm(Input v)
			{   
				VtoS data;
				data.position = mul(UNITY_MATRIX_VP, v.vertex);  
				#if EBG_TRAIL_USE_FANCY_UVS_ON
				data.uv = v.texcoord0; 
				#else
				data.uv.xyz = v.texcoord0;
				data.uv.x /= v.texcoord0.z; //fixup uvs to do pre-perspective divide
				#endif
				
				data.color = v.color;
				return data;
			}
			
			fixed4 fragment_lm(VtoS IN) : COLOR0
			{
				//return fixed4(IN.uv, 0, 1);
				//return IN.color;
				#if EBG_TRAIL_USE_FANCY_UVS_ON
				return tex2D(_MainTex, float2(IN.uv.x/IN.uv.z, IN.uv.y)) * IN.color;
				#else
				return tex2D(_MainTex, IN.uv.xy) * IN.color;
				#endif
			}   
				
			#pragma vertex vertex_lm 
			#pragma fragment fragment_lm 
			
			ENDCG 
		}
	}
}
