sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _MetallicGlossMap;
half _BumpScale;
half _BumpTexCoord;
half _MetallicGlossTexCoord;
float4 _TintLinear;
float4 _EmissionLinear;
half _Metallic;
half _Smoothness;
struct Input { float2 uv_MainTex; float2 uv_BumpMap; float2 uv2_BumpMap; float2 uv_MetallicGlossMap; float2 uv2_MetallicGlossMap; float facing : VFACE; };
void surf(Input input, inout SurfaceOutputStandard output)
{
    half4 texel = tex2D(_MainTex, input.uv_MainTex);
    output.Albedo = texel.rgb * _TintLinear.rgb;
    #ifdef _METALLICGLOSSMAP
    float2 metallicGlossUv = _MetallicGlossTexCoord > 0.5 ? input.uv2_MetallicGlossMap : input.uv_MetallicGlossMap;
    half4 metallicGloss = tex2D(_MetallicGlossMap, metallicGlossUv);
    // glTF stores metallic in B and roughness in G. The decoded preview map
    // carries metallic in R and smoothness (1-roughness) in A; apply the
    // scalar factors here so a textured material keeps both image and factor.
    output.Metallic = saturate(metallicGloss.r * _Metallic);
    output.Smoothness = saturate(1.0h - (1.0h - metallicGloss.a) * (1.0h - _Smoothness));
    #else
    output.Metallic = _Metallic;
    output.Smoothness = _Smoothness;
    #endif
    output.Emission = _EmissionLinear.rgb;
    output.Alpha = texel.a * _TintLinear.a;
    #ifdef _NORMALMAP
    float2 normalUv = _BumpTexCoord > 0.5 ? input.uv2_BumpMap : input.uv_BumpMap;
    output.Normal = UnpackScaleNormal(tex2D(_BumpMap, normalUv), _BumpScale);
    #else
    output.Normal = float3(0, 0, input.facing >= 0 ? 1 : -1);
    #endif
    output.Occlusion = 1;
}
