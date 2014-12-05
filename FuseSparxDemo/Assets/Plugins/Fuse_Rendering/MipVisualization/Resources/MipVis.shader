Shader "Hidden/EBG/MipVis" 
{
	Properties 
	{
		_MainTex ("_MainTex", 2D) = "white" {}
		_MipVis ("_MipVis", 2D) = "white" {}
	}
	    
    Subshader
    {
	    LOD 25
	    
        Tags 
        {
			"Queue"="Geometry"
			"RenderType"="UnlitBase"
			"LightMode"="ForwardBase"
        }
		
		Cull Back
		Lighting Off
		ZWrite On
		ZTest Lequal
		Blend Off
	    Fog { Mode Off }
	    
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma exclude_renderers xbox360 ps3 flash 
            #pragma target 3.0
            uniform sampler2D _MipVis; 
            uniform float4 _MainTex_ST;
            
            struct VertexInput 
            {
                float4 vertex : POSITION;
                float4 uv0 : TEXCOORD0;
            };
            
            struct VertexOutput 
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            
            VertexOutput vert (VertexInput v)
             {
                VertexOutput o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                return o;
            }
            
            fixed4 frag(VertexOutput i) : COLOR 
            {
                return tex2D(_MipVis, i.uv0);
            }
            ENDCG
        }
	}
}
