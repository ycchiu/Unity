 Shader "EBG/Particle/Trail" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		
		[Whitespace] _Whitespace("Blending", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Source Blend Mode", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Destination Blend Mode", Float) = 1
		
		[Whitespace] _Whitespace("Depth Testing", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _DepthTest ("Depth test", Float) = 4
		
		[Whitespace] _Whitespace("Fancy UVs", Float) = 0
		[MaterialToggle] EBG_TRAIL_USE_FANCY_UVS ("Use Fancy UVs", Float) = 0
		
		[Whitespace] _Whitespace("Hue Shift", Float) = 0
		[MaterialToggle] EBG_HUE_SHIFT ("Enable", Float) = 0
		_HueShift ("Hue Shift", Float) = 0
		_Sat ("Saturation", Float) = 1
		_Value ("Value", Float) = 1
		
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
			#pragma glsl_no_auto_normalization
			#pragma multi_compile EBG_TRAIL_USE_FANCY_UVS_OFF EBG_TRAIL_USE_FANCY_UVS_ON
			#pragma multi_compile EBG_HUE_SHIFT_OFF EBG_HUE_SHIFT_ON	
									
			sampler2D _MainTex;	
			
			#if EBG_HUE_SHIFT_ON
				fixed4 _HueShiftR;
				fixed4 _HueShiftG;
				fixed4 _HueShiftB;
			#endif
				
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
				#if EBG_TRAIL_USE_FANCY_UVS_ON
					fixed4 col = tex2D(_MainTex, float2(IN.uv.x/IN.uv.z, IN.uv.y)) * IN.color;
				#else
					fixed4 col = tex2D(_MainTex, IN.uv.xy) * IN.color;
				#endif
				
				#if EBG_HUE_SHIFT_ON
					fixed r = dot(col, _HueShiftR);
					fixed g = dot(col, _HueShiftG);
					fixed b = dot(col, _HueShiftB);
					fixed a = col.a;
					return fixed4(r, g, b, a);
				#else
					return col;
				#endif
			}   
				
			#pragma vertex vertex_lm 
			#pragma fragment fragment_lm 
			
			ENDCG 
		}
	}
}
