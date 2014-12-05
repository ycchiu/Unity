Shader "EBG/Misc/LensFlare" 
{
    Properties 
    {
        _MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
        _Alpha ("Alpha ", Float) = 1
    }
    Category
    {
        Tags 
        { 
            "Queue"="Transparent+200"
            "LightMode" = "Always"
            "RenderType" = "Transparent"
        }
        Lighting Off
        AlphaTest Off
        Cull Off
        ZWrite Off
        Ztest Always
        Blend SrcAlpha One
        Fog { Mode Off }
        
        Subshader
        {
            LOD 100
            
            Pass 
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #pragma exclude_renderers xbox360 ps3 flash 
                #pragma target 3.0
                
                uniform sampler2D _MainTex; 
                float _Alpha;
                float4x4 _Positions1;
                float4x4 _Positions2;
                struct VertexInput 
                {
                    float4 vertex : POSITION;     // this will hold your quad size (XY) and quad index (Z)
                    fixed4 color : COLOR;
                    float2 uv0 : TEXCOORD0;        // this will hold your texture uv
                };
                
                struct VertexOutput 
                {
                    float4 pos : SV_POSITION;
                    float2 uv0 : TEXCOORD0;
                    fixed4 color : TEXCOORD1;
                };
                
                VertexOutput vert (VertexInput v)
                {
                    VertexOutput o;
                    
                    float index = round(v.vertex.z);
                    float index03 = fmod(index, 4.0);
                    
                    float4 center_d = step(index, 3.5) * _Positions1[index03] + step(3.5, index) * _Positions2[index03];
                    
                    float3 center = center_d.xyz;
                    
                    
                    float d = center_d.w;
                    
                    float2x2 rotations = float2x2(
                    	cos(d), -sin(d),
                    	sin(d),cos(d)
                    );
                    
                    
                    float2 rotatedXY = mul(rotations, v.vertex.xy);
                    float4 viewPos = mul(UNITY_MATRIX_V, float4(center,1));
                    viewPos.xy += rotatedXY;  
                    o.pos = mul(UNITY_MATRIX_P, viewPos);

                    //texture uv
                    o.uv0 = v.uv0;
                    o.color = v.color * _Alpha;
                    
                    return o;
                }
                
                fixed4 frag(VertexOutput i) : COLOR 
                {
                	fixed4 c = tex2D(_MainTex, i.uv0) * i.color;
                    return c;
                }
                ENDCG
            }
        }
    }
}