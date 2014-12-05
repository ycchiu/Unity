Shader "EBG/Effects/WarpReplacement" 
{
	//CHARACTER
	SubShader
	{
		Tags {
			"Queue"="Geometry"
			"RenderType"="Character"
			"LightMode"="ForwardBase"
		}
		Cull Back
		Lighting Off
		ZWrite On
		ZTest Lequal
		Blend Off
		ColorMask 0
	    
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct VertexInput 
            {
                float4 vertex : POSITION;
            };
            
            struct VertexOutput 
            {
                float4 pos : SV_POSITION;
            };
            
            VertexOutput vert (VertexInput v)
             {
                VertexOutput o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }
            
            fixed4 frag(VertexOutput i) : COLOR 
            {
                return 0;
            }
            ENDCG
        }
	}
}

