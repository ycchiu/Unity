Shader "EBG/Effects/GaussianBlur" 
{
	Properties 
	{ 
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_BloomTex ("Base (RGB)", 2D) = "black" {}
		_PostFXBloomColor("_PostFXBloomColor", Color) = (1, 1, 1, 1)
		_StepSize("_StepSize", vector) = (1, 1, 1, 1)
		_BlurKernel("_BlurKernel", vector) = (1, 1, 1, 1)
		_Ramp("_Ramp", float) = 1
	}
	SubShader
	{		 
		//PASS 0 : horizontal and/or vertical 3-tap blur
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
			float2 _StepSize; 
			float4 _BlurKernel; 
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 tex0 : TEXCOORD0;
				float2 tex1 : TEXCOORD1;
				float2 tex2 : TEXCOORD2;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 baseTex = TRANSFORM_TEX(v.texcoord, _MainTex); 
				data.tex0 = baseTex - _StepSize; 
				data.tex1 = baseTex;
				data.tex2 = baseTex + _StepSize; 
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				fixed4 res = 0.0;
				res += tex2D(_MainTex, i.tex0) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex1) * _BlurKernel.x;
				res += tex2D(_MainTex, i.tex2) * _BlurKernel.y;
				return res;
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
		
		//PASS 1 : horizontal and/or vertical 3-tap blur w/ ramp
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
			float2 _StepSize; 
			float4 _BlurKernel; 
			float _Ramp;
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 tex0 : TEXCOORD0;
				float2 tex1 : TEXCOORD1;
				float2 tex2 : TEXCOORD2;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 baseTex = TRANSFORM_TEX(v.texcoord, _MainTex); 
				data.tex0 = baseTex - _StepSize; 
				data.tex1 = baseTex;
				data.tex2 = baseTex + _StepSize; 
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				fixed4 res = 0.0;
				res += tex2D(_MainTex, i.tex0) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex1) * _BlurKernel.x;
				res += tex2D(_MainTex, i.tex2) * _BlurKernel.y;
				fixed luminance = dot(res.rgb, fixed3(0.299, 0.587, 0.114));
				fixed luminancePow = pow(luminance, _Ramp) + 0.01;
				return res * (luminancePow / luminance);
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
		
		//PASS 2 : horizontal and/or vertical 7-tap blur
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
			float2 _StepSize; 
			float4 _BlurKernel; 
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 tex0 : TEXCOORD0;
				float2 tex1 : TEXCOORD1;
				float2 tex2 : TEXCOORD2;
				float2 tex3 : TEXCOORD3;
				float2 tex4 : TEXCOORD4;
				float2 tex5 : TEXCOORD5;
				float2 tex6 : TEXCOORD6;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 baseTex = TRANSFORM_TEX(v.texcoord, _MainTex); 
				data.tex0 = baseTex - _StepSize * 3.0; 
				data.tex1 = baseTex - _StepSize * 2.0; 
				data.tex2 = baseTex - _StepSize; 
				data.tex3 = baseTex;
				data.tex4 = baseTex + _StepSize; 
				data.tex5 = baseTex + _StepSize * 2.0; 
				data.tex6 = baseTex + _StepSize * 3.0; 
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				fixed4 res = 0;
				res += tex2D(_MainTex, i.tex0) * _BlurKernel.w;
				res += tex2D(_MainTex, i.tex1) * _BlurKernel.z;
				res += tex2D(_MainTex, i.tex2) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex3) * _BlurKernel.x;
				res += tex2D(_MainTex, i.tex4) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex5) * _BlurKernel.z;
				res += tex2D(_MainTex, i.tex6) * _BlurKernel.w;
				return res;
			}  
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		} 
		
		//PASS 3 : horizontal and/or vertical 7-tap blur with ramp
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
			float2 _StepSize; 
			float4 _BlurKernel; 
			float _Ramp;
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 tex0 : TEXCOORD0;
				float2 tex1 : TEXCOORD1;
				float2 tex2 : TEXCOORD2;
				float2 tex3 : TEXCOORD3;
				float2 tex4 : TEXCOORD4;
				float2 tex5 : TEXCOORD5;
				float2 tex6 : TEXCOORD6;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 baseTex = TRANSFORM_TEX(v.texcoord, _MainTex); 
				data.tex0 = baseTex - _StepSize * 3.0; 
				data.tex1 = baseTex - _StepSize * 2.0; 
				data.tex2 = baseTex - _StepSize; 
				data.tex3 = baseTex;
				data.tex4 = baseTex + _StepSize; 
				data.tex5 = baseTex + _StepSize * 2.0; 
				data.tex6 = baseTex + _StepSize * 3.0; 
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				fixed4 res = 0.0;
				res += tex2D(_MainTex, i.tex0) * _BlurKernel.w;
				res += tex2D(_MainTex, i.tex1) * _BlurKernel.z;
				res += tex2D(_MainTex, i.tex2) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex3) * _BlurKernel.x;
				res += tex2D(_MainTex, i.tex4) * _BlurKernel.y;
				res += tex2D(_MainTex, i.tex5) * _BlurKernel.z;
				res += tex2D(_MainTex, i.tex6) * _BlurKernel.w;
				fixed luminance = dot(res.rgb, fixed3(0.299, 0.587, 0.114));
				fixed luminancePow = pow(luminance, _Ramp) + 0.01;
				return res * (luminancePow / luminance);
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
		
		//PASS 4 : bloom composite, for tone mapping
		Pass
		{
		 	Tags {"Queue"="Geometry" }
			LOD 100			
			Cull Off
			ZWrite Off
			ZTest Off
			Blend One Zero
			CGPROGRAM  
	
			#include "UnityCG.cginc"
			 
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BloomTex;
			fixed4 _PostFXBloomColor;
			
			struct IN 
			{
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
			};
			
			struct VertToFrag
			{
	          	float4 position : SV_POSITION;	
				float2 tex : TEXCOORD0;
			};
					
			VertToFrag vertex_prog(IN v)
			{
				VertToFrag data;
				data.position = mul(UNITY_MATRIX_MVP, v.vertex);
				data.tex = TRANSFORM_TEX(v.texcoord, _MainTex); 
				return data;
			}	
			
			fixed4 fragment_prog(VertToFrag i) : COLOR0
			{		 
				return tex2D(_MainTex, i.tex) + tex2D(_BloomTex, i.tex) * _PostFXBloomColor;
			} 
			
			#pragma vertex vertex_prog 
			#pragma fragment fragment_prog 
	
			ENDCG 
		}
	} 
}

