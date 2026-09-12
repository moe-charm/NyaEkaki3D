sampler2D _MainTex;
float4 _TintLinear;
float4 _EmissionLinear;
half _Metallic;
half _Smoothness;
struct Input { float2 uv_MainTex; float facing : VFACE; };
void surf(Input input, inout SurfaceOutputStandard output)
{
    half4 texel = tex2D(_MainTex, input.uv_MainTex);
    output.Albedo = texel.rgb * _TintLinear.rgb;
    output.Metallic = _Metallic;
    output.Smoothness = _Smoothness;
    output.Emission = _EmissionLinear.rgb;
    output.Alpha = texel.a * _TintLinear.a;
    output.Normal = float3(0, 0, input.facing >= 0 ? 1 : -1);
    output.Occlusion = 1;
}
