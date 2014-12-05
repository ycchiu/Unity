sampler2D _MainTex;
float4 _MainTex_ST; 

#if defined(BLOOM_ON)
half3 _PostFXBloomColor;
half _PostFXBloomIntensity;
sampler2D _BloomTex;

#endif
#if defined(VIGNETTE_ON)
fixed _PostFXVignetteIntensity;
sampler2D _VignetteTex;

#endif
#if defined(WARP_ON)
half2 _PostFXWarpIntensity;
sampler2D _WarpTex;

#endif
#if defined(TONEMAP_ON)
fixed _PostFXToneMapping;

#endif
#if defined(COLOURGRADE_ON)
sampler3D _ColourGradeTex

#endif

struct VertToFrag
{
	float4 position : SV_POSITION;	
	float2 screenUV : TEXCOORD0;
#if defined(BLOOM_ON)
	half3 bloomColor : TEXCOORD1;
#endif
};
		
VertToFrag vertex_prog(appdata_img v)
{
	VertToFrag data;
	data.position = mul(UNITY_MATRIX_MVP, v.vertex);
	data.screenUV = v.texcoord;
#if defined(BLOOM_ON)
	data.bloomColor = _PostFXBloomColor * _PostFXBloomIntensity;
#endif
	return data;
}	

fixed4 fragment_prog(VertToFrag IN) : COLOR0
{		 
	half2 sampleUV = IN.screenUV;
#if defined(WARP_ON)
	half2 warpSample = (tex2D(_WarpTex, sampleUV).rg - 0.5f) * _PostFXWarpIntensity;
	IN.screenUV += warpSample;
#endif
	half4 screen = tex2D(_MainTex, IN.screenUV);
#if defined(BLOOM_ON)
	half3 bloomSample = tex2D(_BloomTex, sampleUV).rgb;
	screen.rgb += bloomSample * IN.bloomColor;
#endif
#if defined(TONEMAP_ON)
	screen *= _PostFXToneMapping;
#endif
#if defined(VIGNETTE_ON)
	screen *= lerp(1.0, tex2D(_VignetteTex, sampleUV).rgb, _PostFXVignetteIntensity);
#endif
#if defined(COLOURGRADE_ON)
	screen.rgb = tex3D(_ColourGradeTex, screen).rgb;
#endif
	
	return screen;
} 